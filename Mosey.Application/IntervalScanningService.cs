using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Mosey.Applicaton.Configuration;
using Mosey.Core;
using Mosey.Core.Imaging;
using static Mosey.Application.IScanningService;
using static Mosey.Core.Imaging.IImagingHost;

namespace Mosey.Application
{
    public sealed class IntervalScanningService : IIntervalScanningService, IDisposable
    {
        private readonly IImagingHost _imagingHost;
        private readonly IIntervalTimer _intervalTimer;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<IntervalScanningService> _log;
        private readonly CancellationTokenSource _refreshDevicesCancellationSource = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private ScanningConfig _config;
        private CancellationTokenSource _scanningCancellationSource = new();

        public event EventHandler? DevicesRefreshed;
        public event EventHandler? ScanRepetitionCompleted;
        public event EventHandler? ScanningCompleted;

        public bool CreateDirectoryPath { get; set; } = true;

        public TimeSpan DeviceRefreshInterval { get; set; } = TimeSpan.FromSeconds(1);

        public IImagingDevices<IImagingDevice> Scanners => _imagingHost.ImagingDevices;

        public bool IsScanRunning
            => (_intervalTimer?.Enabled ?? false) || _imagingHost.ImagingDevices.IsImagingInProgress;

        public int ScanRepetitionsCount
            => _intervalTimer?.RepetitionsCount ?? 0;

        public DateTime StartTime
            => _intervalTimer.StartTime;

        public DateTime FinishTime
            => _intervalTimer.FinishTime;

        public IntervalScanningService(IImagingHost imagingHost, IIntervalTimer intervalTimer, IFileSystem fileSystem, ILogger<IntervalScanningService> logger)
             : this(imagingHost, intervalTimer, new ScanningConfig(new DeviceConfig(), new ImagingDeviceConfig(), new ImageFileConfig()), fileSystem, logger)
        {
        }

        public IntervalScanningService(IImagingHost imagingHost, IIntervalTimer intervalTimer, ScanningConfig config, IFileSystem fileSystem, ILogger<IntervalScanningService> logger)
        {
            _imagingHost = imagingHost;
            _intervalTimer = intervalTimer;
            _config = config;
            _fileSystem = fileSystem;
            _log = logger;

            _intervalTimer.Tick += async (_, _) => await ScanAndSaveImages();
            _intervalTimer.Complete += OnScanningCompleted;

            _ = BeginRefreshDevices(DeviceRefreshInterval, _refreshDevicesCancellationSource.Token).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _intervalTimer.Complete -= OnScanningCompleted;
            _refreshDevicesCancellationSource?.Cancel();
            _refreshDevicesCancellationSource?.Dispose();
            _scanningCancellationSource?.Cancel();
            _scanningCancellationSource?.Dispose();
            _intervalTimer?.Dispose();
        }

        /// <summary>
        /// The estimated amount of disk space required for a full run of images, in bytes.
        /// </summary>
        public long GetRequiredDiskSpace(int repetitions)
            => repetitions
                * Scanners.Devices.Count(d => d.IsEnabled)
                * _config.DeviceConfig.GetResolutionMetadata(_config.ImagingDeviceConfig.Resolution).FileSize;

        /// <summary>
        /// The estimated time taken for all the currently active scanners to complete a single image capture.
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetRequiredScanningTime()
            => Scanners.Devices.Count(d => d.IsEnabled) * _config.DeviceConfig.GetResolutionMetadata(_config.ImagingDeviceConfig.Resolution).ImagingTime;

        /// <summary>
        /// Initiate scanning with all available <see cref="ScanningDevice"/>s and save any captured images to disk
        /// </summary>
        /// <returns><see cref="string"/>s representing file paths for scanned images</returns>
        public async Task<IEnumerable<string>> ScanAndSaveImages()
        {
            _log.LogTrace($"Scan initiated with {nameof(ScanAndSaveImages)} method.");
            var results = Enumerable.Empty<string>();
            var args = EventArgs.Empty;

            try
            {
                await _semaphore.WaitAsync(_scanningCancellationSource.Token);
                results = await ScanAndSaveImages(CreateDirectoryPath, _scanningCancellationSource.Token).ConfigureAwait(false);
                args = new StringCollectionEventArgs(results);
            }
            catch (OperationCanceledException ex)
            {

                _log.LogInformation(ex, "Scanning cancelled before it could be completed");
            }
            catch (Exception ex)
            {
                _log.LogError("An error occured when attempting to aquire images, or when saving them to disk", ex);
                args = new ExceptionEventArgs(ex);
            }
            finally
            {
                _log.LogTrace($"Scan completed with {nameof(ScanAndSaveImages)} method.");
                ScanRepetitionCompleted?.Invoke(this, args);
                _semaphore.Release();
            }

            return results;
        }

        public void StartScanning(TimeSpan delay, TimeSpan interval, int repetitions)
            => _intervalTimer.Start(delay, interval, repetitions);

        public void StopScanning(bool waitForCompletion = true)
        {
            try
            {
                if (!waitForCompletion)
                {
                    _scanningCancellationSource?.Cancel();
                }
                _intervalTimer.Stop();
            }
            finally
            {
                _scanningCancellationSource = new CancellationTokenSource();
            }
        }

        public void UpdateConfig(ScanningConfig config)
        {
            _config = config;
            _imagingHost.UpdateConfig(_config.ImagingDeviceConfig);
        }

        /// <summary>
        /// Starts a loop that continually refreshes the list of available scanners
        /// </summary>
        /// <param name="interval">The duration between refreshes</param>
        /// <param name="cancellationToken">Used to stop the refresh loop</param>
        internal async Task BeginRefreshDevices(TimeSpan interval, CancellationToken cancellationToken)
        {
            var args = EventArgs.Empty;
            _log.LogTrace($"Device refresh initiated with {nameof(BeginRefreshDevices)}");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _log.LogTrace($"Initiating device refresh in {nameof(BeginRefreshDevices)}");
                    var enableDevices = !IsScanRunning ? _config.DeviceConfig.EnableWhenConnected : _config.DeviceConfig.EnableWhenScanning;

                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                    await _imagingHost.RefreshDevicesAsync(enableDevices, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Error while attempting to refresh devices.");
                    args = new ExceptionEventArgs(ex);
                }
                finally
                {
                    _log.LogTrace($"Device refresh completed in {nameof(BeginRefreshDevices)}");
                    DevicesRefreshed?.Invoke(this, args);
                }
            }
        }

        internal async Task<IEnumerable<CapturedImage>> PerformImaging(bool useHighestResolution = false, CancellationToken cancellationToken = default)
            => await _imagingHost.PerformImagingAsync(useHighestResolution, cancellationToken);

        internal static string GetImageFilePath(CapturedImage image, ImageFileConfig config, bool appendIndex, DateTime saveDateTime)
        {
            var index = appendIndex ? image.Index.ToString() : null;
            var dateTime = saveDateTime.ToString(string.Join("_", config.DateFormat, config.TimeFormat));
            var directory = Path.Combine(config.Directory, $"Scanner{image.DeviceId}");
            var fileName = string.Join("_", config.Prefix, dateTime, index);

            return Path.Combine(directory, Path.ChangeExtension(fileName, config.ImageFormat.ToString().ToLower()));
        }

        private async void OnScanningCompleted(object? sender, EventArgs e)
        {
            try
            {
                await _semaphore.WaitAsync(_scanningCancellationSource.Token);
                await _imagingHost.WaitForImagingToComplete(_scanningCancellationSource.Token).ConfigureAwait(false);
                ScanningCompleted?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc cref="ScanAndSaveImages"/>
        private async Task<IEnumerable<string>> ScanAndSaveImages(bool createDirectoryPath, CancellationToken cancellationToken)
        {
            var results = new List<string>();
            var saveDateTime = DateTime.Now;

            var images = (await PerformImaging(_config.DeviceConfig.UseHighestResolution, cancellationToken).ConfigureAwait(false)).ToList();

            if (!cancellationToken.IsCancellationRequested)
            {
                foreach (var scannedImage in images)
                {
                    var filePath = GetImageFilePath(scannedImage, _config.ImageFileConfig, images.Count > 1, saveDateTime);
                    if (createDirectoryPath)
                    {
                        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(filePath));
                    }
                    _imagingHost.ImageFileHandler.SaveImage(scannedImage.Image, _config.ImageFileConfig.ImageFormat, filePath);

                    results.Add(filePath);
                }
            }

            return results;
        }
    }
}
