using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using Mosey.Models.Imaging;
using static Mosey.Models.Imaging.IImagingHost;

namespace Mosey.Services.Imaging
{
    public class DntScanningHost : IImagingHost
    {
        // The apartment state MUST be single threaded for COM interop
        private static readonly StaTaskScheduler _scheduler = new(concurrencyLevel: 1);
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        private ImagingDeviceConfig _deviceConfig;

        /// <inheritdoc cref="IImagingDevices{T}"/>
        public IImagingDevices<IImagingDevice> ImagingDevices { get; private set; }

        public IImageFileHandler ImageFileHandler { get; private set; }

        public DntScanningHost(IImagingDevices<IImagingDevice> imagingDevices, IImageFileHandler imageFileHandler, ImagingDeviceConfig deviceConfig = null)
        {
            _deviceConfig = deviceConfig ?? new ImagingDeviceConfig();

            ImagingDevices = imagingDevices;
            ImageFileHandler = imageFileHandler;
        }

        public IEnumerable<CapturedImage> PerformImaging(bool useHighestResolution = false, CancellationToken cancellationToken = default)
        {
            try
            {
                _semaphore.Wait(cancellationToken);

                // Order devices by ID to provide clearer feedback to users
                foreach (var scanner in ImagingDevices.Devices.OrderBy(o => o.DeviceID).ToList())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
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
                            scanner.PerformImaging();
                        }
                    }
                    catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidOperationException)
                    {
                        // Device will show as disconnected, no images retrieved
                    }

                    int imageIndex = 0;
                    foreach (var image in scanner.Images)
                    {
                        yield return new CapturedImage
                        {
                            Image = image,
                            DeviceId = scanner.ID.ToString(),
                            Index = imageIndex++
                        };
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IEnumerable<CapturedImage>> PerformImagingAsync(bool useHighestResolution = false, CancellationToken cancellationToken = default)
        {
            var results = Enumerable.Empty<CapturedImage>();

            try
            {
                // Runtime callable wrappers must be disposed manually to prevent problems with early disposal of COM servers
                using (var staQueue = new StaTaskScheduler(concurrencyLevel: 1, disableComObjectEagerCleanup: true))
                {
                    results = await Task.Factory.StartNew(
                        () => PerformImaging(useHighestResolution, cancellationToken),
                        cancellationToken,
                        TaskCreationOptions.LongRunning,
                        staQueue)
                    .ContinueWith(t =>
                    {
                        // Manually clear (RCWs) to prevent memory leaks
                        System.Runtime.InteropServices.Marshal.CleanupUnusedObjectsInCurrentContext();
                        // Force any pending exceptions to propagate up
                        t.Wait();
                        return t.Result;
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

            return results;
        }

        public void RefreshDevices(bool enableDevices = true, CancellationToken cancellationToken = default)
        {
            try
            {
                _semaphore.Wait();
                ImagingDevices.RefreshDevices(_deviceConfig, enableDevices);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task RefreshDevicesAsync(bool enableDevices = true, CancellationToken cancellationToken = default)
        {
            // Use a dedicated thread for refresh tasks
            await Task.Factory.StartNew(
                () => RefreshDevices(enableDevices, cancellationToken),
                cancellationToken,
                TaskCreationOptions.None,
                _scheduler);
        }

        public void UpdateConfig(ImagingDeviceConfig deviceConfig)
        {
            _deviceConfig = deviceConfig;
        }

        public async Task WaitForImagingToComplete(CancellationToken cancellationToken = default)
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
