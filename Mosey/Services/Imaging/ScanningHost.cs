using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using Mosey.Models.Imaging;

namespace Mosey.Services.Imaging
{
    public class DntScanningHost : IImagingHost
    {
        // The apartment state MUST be single threaded for COM interop
        private static readonly StaTaskScheduler _staQueue = new(concurrencyLevel: 1);
        private static readonly SemaphoreSlim _staSemaphore = new(1, 1);

        private readonly IFileSystem _fileSystem;

        private ImagingDeviceConfig _deviceConfig;
        private ImageFileConfig _imageFileConfig;

        /// <inheritdoc cref="IImagingDevices{T}"/>
        public IImagingDevices<IImagingDevice> ImagingDevices { get; private set; }

        public DntScanningHost(IImagingDevices<IImagingDevice> imagingDevices, ImagingDeviceConfig deviceConfig = null, ImageFileConfig imageFileConfig = null, IFileSystem fileSystem = null)
        {
            _deviceConfig = deviceConfig ?? new ImagingDeviceConfig();
            _imageFileConfig = imageFileConfig ?? new ImageFileConfig();
            _fileSystem = fileSystem ?? new FileSystem();

            ImagingDevices = imagingDevices;
        }

        public List<string> PerformImaging(string imageDirectory, bool useHighestResolution = false, CancellationToken cancellationToken = default)
        {
            var scannerIdentifier = string.Empty;
            var saveDateTime = DateTime.Now.ToString(string.Join("_", _imageFileConfig.DateFormat, _imageFileConfig.TimeFormat));
            var imagePaths = new List<string>();

            // Order devices by ID to provide clearer feedback to users
            foreach (var scanner in ImagingDevices.Devices.OrderBy(o => o.DeviceID).ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    scannerIdentifier = scanner.ID.ToString();

                    if (scanner.IsConnected && scanner.IsEnabled && !scanner.IsImaging)
                    {
                        // Update image config in case of changes
                        scanner.ImageSettings = _deviceConfig;

                        if (useHighestResolution)
                        {
                            // Override the config and just use the highest available resolution
                            scanner.ImageSettings = scanner.ImageSettings with { Resolution = scanner.SupportedResolutions.Max() };
                        }

                        // Run the scanner and retrieve the image(s) to memory
                        scanner.GetImage();

                        if (scanner.Images.Any())
                        {
                            var fileName = string.Join("_", _imageFileConfig.Prefix, saveDateTime);
                            var directory = _fileSystem.Path.Combine(imageDirectory, string.Join(string.Empty, "Scanner", scannerIdentifier));
                            _fileSystem.Directory.CreateDirectory(directory);

                            // Write image(s) to filesystem and retrieve a list of saved file names
                            var savedImages = scanner.SaveImage(fileName, directory: directory, fileFormat: _imageFileConfig.Format);
                            foreach (var image in savedImages)
                            {
                                imagePaths.Add(image);
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidOperationException)
                {
                    // Device will show as disconnected, no images returned
                    return imagePaths;
                }
            }

            return imagePaths;
        }

        public async Task<List<string>> PerformImagingAsync(string imageDirectory, bool useHighestResolution = false, CancellationToken cancellationToken = default)
        {
            var results = new List<string>();

            try
            {
                // Ensure device refresh or other operations are complete
                await _staSemaphore.WaitAsync(cancellationToken);

                // Runtime callable wrappers must be disposed manually to prevent problems with early disposal of COM servers
                using (var staQueue = new StaTaskScheduler(concurrencyLevel: 1, disableComObjectEagerCleanup: true))
                {
                    await Task.Factory.StartNew(() =>
                    {
                        // Obtain images from scanners
                        results = PerformImaging(imageDirectory, useHighestResolution, cancellationToken);
                    }, cancellationToken, TaskCreationOptions.LongRunning, staQueue)
                    .ContinueWith(t =>
                    {
                        // Manually clear (RCWs) to prevent memory leaks
                        System.Runtime.InteropServices.Marshal.CleanupUnusedObjectsInCurrentContext();
                        // Force any pending exceptions to propagate up
                        t.Wait();
                    }, staQueue);
                }
            }
            catch (AggregateException aggregateEx)
            {
                // Unpack the aggregate
                var innerException = aggregateEx.Flatten().InnerExceptions.FirstOrDefault();
                // Ensure stack trace is preserved and rethrow
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(innerException).Throw();
            }
            finally
            {
                _staSemaphore.Release();
            }

            return results;
        }

        public void RefreshDevices(bool enableDevices = true, CancellationToken cancellationToken = default)
        {
            try
            {
                _staSemaphore.Wait();
                ImagingDevices.RefreshDevices(_deviceConfig, enableDevices);
            }
            finally
            {
                _staSemaphore.Release();
            }
        }

        public async Task RefreshDevicesAsync(bool enableDevices = true, CancellationToken cancellationToken = default)
        {
            // Use a dedicated thread for refresh tasks
            await Task.Factory.StartNew(
                () => RefreshDevices(enableDevices, cancellationToken),
                cancellationToken,
                TaskCreationOptions.None,
                _staQueue);
        }

        public void UpdateConfig(ImagingDeviceConfig deviceConfig, ImageFileConfig imageFileConfig)
        {
            _deviceConfig = deviceConfig;
            _imageFileConfig = imageFileConfig;
        }

        public async Task WaitForImagingToComplete(CancellationToken cancellationToken = default)
        {
            try
            {
                await _staSemaphore.WaitAsync(cancellationToken);
            }
            finally
            {
                _staSemaphore.Release();
            }
        }
    }
}
