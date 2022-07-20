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

        private IProgress<ScanningProgress>? _progress;
        private ScanningConfig _config;
        private CancellationTokenSource _scanningCancellationSource = new();

        public event EventHandler? DevicesRefreshed;

        public bool CreateDirectoryPath { get; set; } = true;

        public TimeSpan DeviceRefreshInterval { get; set; } = TimeSpan.FromSeconds(1);

        public IImagingDevices<IImagingDevice> Scanners
            => _imagingHost.ImagingDevices;

        public bool IsScanRunning
            => (_intervalTimer?.Enabled ?? false)
            || _imagingHost.ImagingDevices.IsImagingInProgress;

        public DateTime StartTime
            => _intervalTimer.StartTime;

        public DateTime FinishTime
            => _intervalTimer?.Enabled ?? false
                ? _intervalTimer.FinishTime
                : DateTime.Now;

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

            _ = BeginRefreshDevices(DeviceRefreshInterval, _refreshDevicesCancellationSource.Token).ConfigureAwait(false);
        }

        public void Dispose()
        {
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
        public TimeSpan GetRequiredScanningTime()
            => Scanners.Devices.Count(d => d.IsEnabled) * _config.DeviceConfig.GetResolutionMetadata(_config.ImagingDeviceConfig.Resolution).ImagingTime;

        /// <summary>
        /// Initiate scanning with all available <see cref="ScanningDevice"/>s and save any captured images to disk.
        /// </summary>
        /// <returns><see cref="string"/>s representing file paths for scanned images</returns>
        public async Task<IEnumerable<string>> ScanAndSaveImages()
        {
            _log.LogTrace($"Scan initiated with {nameof(ScanAndSaveImages)} method.");
            Exception? exception = null;
            var results = Enumerable.Empty<string>();
            // Store the value because the timer will be reset on the last interval
            var repetitionsCount = _intervalTimer.RepetitionsCount;

            _progress?.Report(new ScanningProgress(repetitionsCount, ScanningProgress.ScanningStage.Start, exception));

            try
            {
                results = await ScanAndSaveImages(CreateDirectoryPath, _scanningCancellationSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                _log.LogInformation(ex, "Scanning cancelled before it could be completed");
                exception = ex;
            }
            catch (Exception ex)
            {
                _log.LogError("An error occured when attempting to aquire images, or when saving them to disk", ex);
                exception = ex;
            }
            finally
            {
                _log.LogTrace($"Scan completed with {nameof(ScanAndSaveImages)} method.");
                _progress?.Report(new ScanningProgress(repetitionsCount, ScanningProgress.ScanningStage.Finish, exception));
            }

            return results;
        }

        /// <summary>
        /// Begins scanning at set intervals.
        /// </summary>
        /// <param name="config">The interval scanning configuration</param>
        /// <param name="progress">Progress is reported both before and after each successful scan repetition</param>
        public async Task StartScanning(IntervalTimerConfig config, IProgress<ScanningProgress>? progress = null)
        {
            var tcs = new TaskCompletionSource();

            _intervalTimer.Complete += onTimerCompleted;

            _progress = progress;
            _intervalTimer.Start(config.Delay, config.Interval, config.Repetitions);

            _log.LogInformation("Scanning started with {ScanRepetitions} repetitions to complete.", config.Repetitions);
            _log.LogDebug("IntervalTimer started. Delay: {Delay} Interval: {Interval} Repetitions: {Repetitions}", config.Delay, config.Interval, config.Repetitions);

            await tcs.Task.WaitAsync(_scanningCancellationSource.Token).ConfigureAwait(false);
            await _imagingHost.WaitForImagingToComplete(_scanningCancellationSource.Token).ConfigureAwait(false);

            _log.LogInformation("Scanning completed.");

            void onTimerCompleted(object? sender, EventArgs e)
            {
                tcs.SetResult();
                _intervalTimer.Complete -= onTimerCompleted;
            }
        }

        /// <summary>
        /// Stop any ongoing scanning run that has been started with <see cref="StartScanning(IntervalTimerConfig, IProgress{ScanningProgress}?)"/>.
        /// </summary>
        /// <param name="waitForCompletion">If any currently incomplete scanning iterations should be allowed to finish</param>
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
        /// Starts a loop that continually refreshes the list of available scanners.
        /// </summary>
        /// <param name="interval">The duration between refreshes</param>
        /// <param name="cancellationToken">Used to stop the refresh loop</param>
        internal async Task BeginRefreshDevices(TimeSpan interval, CancellationToken cancellationToken)
        {
            _log.LogTrace($"Device refresh initiated with {nameof(BeginRefreshDevices)}");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _log.LogTrace($"Initiating device refresh in {nameof(BeginRefreshDevices)}");
                    var enableDevices = !IsScanRunning ? _config.DeviceConfig.EnableWhenConnected : _config.DeviceConfig.EnableWhenScanning;

                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                    await _imagingHost.RefreshDevicesAsync(enableDevices, cancellationToken).ConfigureAwait(false);

                    DevicesRefreshed?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Error while attempting to refresh devices.");
                }
                finally
                {
                    _log.LogTrace($"Device refresh completed in {nameof(BeginRefreshDevices)}");
                }
            }
        }

        internal static string GetImageFilePath(CapturedImage image, ImageFileConfig config, bool appendIndex, DateTime saveDateTime)
        {
            var index = appendIndex ? image.Index.ToString() : null;
            var dateTime = saveDateTime.ToString(string.Join("_", config.DateFormat, config.TimeFormat));
            var directory = Path.Combine(config.Directory, $"Scanner{image.DeviceId}");
            var fileName = string.Join("_", config.Prefix, dateTime, index);

            return Path.Combine(directory, Path.ChangeExtension(fileName, config.ImageFormat.ToString().ToLower()));
        }

        /// <inheritdoc cref="ScanAndSaveImages"/>
        private async Task<IEnumerable<string>> ScanAndSaveImages(bool createDirectoryPath, CancellationToken cancellationToken)
        {
            var results = new List<string>();
            var saveDateTime = DateTime.Now;

            var images = (await _imagingHost.PerformImagingAsync(_config.DeviceConfig.UseHighestResolution, cancellationToken).ConfigureAwait(false)).ToList();

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
