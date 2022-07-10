using System.Runtime.InteropServices;
using DNTScanner.Core;
using Mosey.Core;
using Mosey.Core.Imaging;
using Mosey.Application.Imaging.Extensions;

namespace Mosey.Application.Imaging
{
    /// <summary>
    /// Represents a physical scanning device connected to the system.
    /// </summary>
    public sealed class ScanningDevice : PropertyChangedBase, IImagingDevice
    {
        private bool _isConnected = true;
        private bool _isEnabled;
        private bool _isImaging;
        private int _scanRetries = 5;

        private readonly ScannerSettings _scannerSettings;
        private readonly ISystemImagingDevices<ScannerSettings> _systemDevices;

        public string Name => _scannerSettings.Name;

        public int ID => GetSimpleID(_scannerSettings.Id);

        public string DeviceID => _scannerSettings.Id;

        public IList<KeyValuePair<string, object>> DeviceSettings
            => GetScannerDeviceSettings(_scannerSettings);

        public IList<byte[]> Images { get; internal set; } = new List<byte[]>();

        public ImagingDeviceConfig ImageSettings { get; set; }

        /// <inheritdoc cref="ScannerSettings.IsAutomaticDocumentFeeder"/>
        public bool IsAutomaticDocumentFeeder => _scannerSettings.IsAutomaticDocumentFeeder;

        public bool IsConnected
        {
            get => _isConnected;
            internal set => SetField(ref _isConnected, value);
        }

        /// <inheritdoc cref="ScannerSettings.IsDuplex"/>
        public bool IsDuplex => _scannerSettings.IsDuplex;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetField(ref _isEnabled, value);
        }

        /// <inheritdoc cref="ScannerSettings.IsFlatbed"/>
        public bool IsFlatbed => _scannerSettings.IsFlatbed;

        public bool IsImaging
        {
            get => _isImaging;
            private set => SetField(ref _isImaging, value);
        }

        public int ScanRetries
        {
            get => _scanRetries;
            set => SetField(ref _scanRetries, value);
        }

        /// <inheritdoc cref="ScannerSettings.SupportedResolutions"/>
        public IList<int> SupportedResolutions => _scannerSettings.SupportedResolutions;

        /// <summary>
        /// Initialize a new instance using a <see cref="ScannerSettings"/> instance that represents a physical scanner.
        /// </summary>
        /// <param name="settings">A <see cref="ScannerSettings"/> instance representing a physical device</param>
        /// <param name="config">Device settings used when capturing an image</param>
        public ScanningDevice(ScannerSettings settings, ImagingDeviceConfig config)
            : this(settings, config, new DntScannerDevices()) { }

        /// <summary>
        /// Initialize a new instance using a <see cref="ScannerSettings"/> instance that represents a physical scanner.
        /// </summary>
        /// <param name="settings">A <see cref="ScannerSettings"/> instance representing a physical device</param>
        /// <param name="config">Device settings used when capturing an image</param>
        /// <param name="systemDevices">An <see cref="ISystemImagingDevices"/> instance that provide access to the WIA driver devices</param>
        public ScanningDevice(ScannerSettings settings, ImagingDeviceConfig config, ISystemImagingDevices<ScannerSettings> systemDevices)
        {
            _scannerSettings = settings;
            ImageSettings = config;
            _systemDevices = systemDevices;
        }

        public void ClearImages()
            => Images = new List<byte[]>();

        public void PerformImaging()
            => GetImage(IImagingDevice.ImageFormat.Bmp);

        /// <summary>
        /// Retrieve an image from the physical imaging device.
        /// </summary>
        /// <param name="format">The image transfer format used when capturing the image</param>
        /// <exception cref="COMException">If the scanner is not connected</exception>
        /// <exception cref="ArgumentException">If the <see cref="ImageFormat"/> is not supported by the device</exception>
        /// <exception cref="InvalidOperationException">If the operation fails during scanning</exception>
        /// <remarks>
        /// Images are converted to <see cref="ImageFormat.Png"/>, if possible, before being stored as byte arrays.
        /// </remarks>
        public void GetImage(IImagingDevice.ImageFormat format)
        {
            if (!IsConnected)
            {
                throw new COMException("The scanner is not connected.");
            }

            if (!_scannerSettings.SupportedTransferFormats.ContainsKey(format.ToWIAImageFormat().Value))
            {
                throw new ArgumentException($"The image format {format} is not supported by this device.");
            }

            // Default settings if none provided
            var deviceConfig = ImageSettings ?? new ImagingDeviceConfig
            {
                Brightness = 1,
                Contrast = 1,
                ColorFormat = ImageColorFormat.Color,
                Resolution = SupportedResolutions.Max()
            };

            IsImaging = true;
            try
            {
                var images = _systemDevices.PerformImaging(_scannerSettings, deviceConfig, format);

                // Remove any existing images
                ClearImages();

                // Store images for processing etc
                foreach (var image in images)
                {
                    Images.Add(image);
                }
            }
            catch (Exception ex) when (ex is COMException or InvalidOperationException)
            {
                // Scanner has been disconnected or similar
                IsConnected = false;
                throw;
            }
            finally
            {
                IsImaging = false;
            }
        }

        public bool Equals(IImagingDevice? device)
            => device is not null && DeviceID == device.DeviceID;

        public override bool Equals(object? obj)
            => Equals(obj as ScanningDevice);

        public override int GetHashCode()
            => DeviceID.GetHashCode();

        /// <summary>
        /// A simplified version of the unique device identifier, <see cref="DeviceID"/>
        /// </summary>
        /// <param name="deviceID">A unique device identifier</param>
        /// <returns></returns>
        private static int GetSimpleID(string deviceID)
        {
            // Get the last two characters of the device instance path
            var shortID = deviceID.Substring(Math.Max(0, deviceID.Length - 2));

            if (int.TryParse(shortID, out var intID))
            {
                return intID;
            }
            else
            {
                // Return the numeric representation of the characters instead
                return shortID.GetHashCode();
            }
        }

        private static IList<KeyValuePair<string, object>> GetScannerDeviceSettings(ScannerSettings scannerSettings)
            => scannerSettings.ScannerDeviceSettings.ToList();
    }
}
