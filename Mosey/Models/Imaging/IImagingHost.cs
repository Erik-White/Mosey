using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mosey.Models.Imaging
{
    public interface IImagingHost
    {
        IImagingDevices<IImagingDevice> ImagingDevices { get; }

        /// <summary>
        /// Initiate scanning with all available <see cref="IImagingDevice"/>s
        /// </summary>
        /// <param name="cancellationToken">Used to stop current imaging operations</param>
        /// <returns><see cref="string"/>s representing file paths for scanned images</returns>
        /// <exception cref="OperationCanceledException">If imaging was cancelled before completion</exception>
        List<string> PerformImaging(string imageDirectory, bool useHighestResolution = false, CancellationToken cancellationToken = default);


        /// <inheritdoc cref="PerformImaging(string, bool, CancellationToken)"/>
        Task<List<string>> PerformImagingAsync(string imageDirectory, bool useHighestResolution = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update the status of the <see cref="ImagingDevices"/>, and add any newly connected devices
        /// </summary>
        /// <param name="enableDevices">Sets the <see cref="IImagingDevice.IsEnabled"/> property of any newly added devices</param>
        /// <param name="cancellationToken">Used to stop current imaging operations</param>
        /// <exception cref="OperationCanceledException">If the update was cancelled before completion</exception>
        void RefreshDevices(bool enableDevices = true, CancellationToken cancellationToken = default);

        /// <inheritdoc cref="RefreshDevices(bool, CancellationToken)"/>
        Task RefreshDevicesAsync(bool enableDevices = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update the configuration that will be used for imaging
        /// </summary>
        /// <exception cref="OperationCanceledException">If the update was cancelled before completion</exception>
        void UpdateConfig(IImagingDeviceConfig deviceConfig, IImageFileConfig imageFileConfig);

        /// <summary>
        /// Returns when any ongoing imaging or refresh has completed
        /// </summary>
        /// <param name="cancellationToken">Stop waiting for imaging to complete</param>
        /// <exception cref="OperationCanceledException">If waiting was cancelled before imaging was completed</exception>
        Task WaitForImagingToComplete(CancellationToken cancellationToken = default);
    }
}
