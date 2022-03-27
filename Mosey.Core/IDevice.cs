namespace Mosey.Core
{
    /// <summary>
    /// Represents a physical device connected to the system.
    /// </summary>
    public interface IDevice
    {
        /// <summary>
        /// A label to represent the device make or model.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// An internal identifier for the device.
        /// </summary>
        int ID { get; }

        /// <summary>
        /// The unique indentifier of the physical device.
        /// </summary>
        string DeviceID { get; }

        /// <summary>
        /// If the physical device can be found by the system.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Sets whether the device is allowed to operate.
        /// </summary>
        bool IsEnabled { get; set; }
    }
}
