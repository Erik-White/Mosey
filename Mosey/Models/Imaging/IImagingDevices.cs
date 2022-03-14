namespace Mosey.Models.Imaging
{
    /// <summary>
    /// A collection of <see cref="IImagingDevice"/>s representing physical imaging devices.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IImagingDevices<out T> : IDeviceCollection<T> where T : IImagingDevice
    {
        /// <summary>
        /// <see langword="true"/> if the <see cref="IDeviceCollection{T}.Devices"/> collection is empty
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// <see langword="true"/> if any device in the collection has the <see cref="IImagingDevice.IsImaging"/> property of <see langword="true"/>.
        /// </summary>
        bool IsImagingInProgress { get; }

        /// <summary>
        /// Retrieve <see cref="IImagingDevice"/>s that are connected to the system and add them to the collection.
        /// Update the status of any devices are already present in the collection.
        /// </summary>
        /// <param name="deviceConfig">The <see cref="ImagingDeviceConfig"/> used to initialize the device</param>
        /// <param name="enableDevices">Sets the <see cref="IDevice.IsEnabled"/> property of any new devices that are not already in the collection</param>
        void RefreshDevices(ImagingDeviceConfig deviceConfig, bool enableDevices = true);
    }
}
