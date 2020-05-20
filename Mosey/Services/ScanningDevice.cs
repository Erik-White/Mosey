using System;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using Microsoft.Extensions.Configuration;
using DNTScanner.Core;
using Mosey.Models;
using Mosey.Services.Imaging.Extensions;

namespace Mosey.Services
{
    /// <summary>
    /// A persistent collection of WIA enabled scanners
    /// Items are not removed from the collection when it is refreshed
    /// Instead, their status is updated to indicate that they may not be connected
    /// </summary>
    public class ScanningDevices : ObservableItemsCollection<IImagingDevice>, IImagingDevices<IImagingDevice>
    {
        public int ConnectRetries { get; set; } = 10;
        public bool IsImagingInProgress { get { return this.Any(x => x.IsImaging is true); } }
        public bool IsEmpty { get { return (Count == 0); } }
        private static System.Threading.SemaphoreSlim _semaphore = new System.Threading.SemaphoreSlim(1, 1);

        public ScanningDevices()
        {
        }

        public ScanningDevices(IImagingDeviceConfig deviceConfig)
        {
            GetDevices(deviceConfig);
        }

        public void GetDevices()
        {
            // Populate a new collection of scanners using default image settings
            GetDevices(new ScanningDeviceSettings());
        }

        public void GetDevices(IImagingDeviceConfig deviceConfig)
        {
            // Empty the collection
            Clear();

            // Populate a new collection of scanners using specified image settings
            foreach (ScanningDevice device in SystemScanners(deviceConfig, ConnectRetries))
            {
                device.IsEnabled = true;
                AddDevice(device);
            }
        }

        public void RefreshDevices()
        {
            // Get a new collection of devices if none already present
            if (Count == 0)
            {
                GetDevices();
            }
            else
            {
                RefreshDevices(new ScanningDeviceSettings());
            }
        }

        public void RefreshDevices(IImagingDeviceConfig deviceConfig, bool enableDevices = true)
        {
            IList<IDictionary<string, object>> deviceProperties = SystemScannerProperties(connectRetries: ConnectRetries);
            if (deviceProperties.Count == 0)
            {
                // No devices detected, any current devices have been disconnected
                foreach (ScanningDevice device in this)
                {
                    device.IsConnected = false;
                }
                return;
            }

            // Check if devices not already in the collection
            // Or already in the collection, but not connected
            foreach (IDictionary<string, object> properties in deviceProperties)
            {
                string deviceID = properties["Unique Device ID"].ToString();

                if (!this.Where(d => d.DeviceID == deviceID).Any())
                {
                    // Create a new device and add it to the collection
                    ScanningDevice device = AddDevice(deviceID, deviceConfig);
                    device.IsEnabled = enableDevices;
                }
                else
                {
                    ScanningDevice existingDevice = (ScanningDevice)this.Where(d => d.DeviceID == deviceID && !d.IsConnected).FirstOrDefault();
                    if (existingDevice != null)
                    {
                        // Remove the existing device from the collection
                        bool enabled = existingDevice.IsEnabled;
                        Remove(existingDevice);

                        // Replace with the new and updated device
                        ScanningDevice device = AddDevice(deviceID, deviceConfig);
                        device.IsEnabled = enabled;
                    }
                }

                // If the device is in the collection but no longer found
                IEnumerable<IImagingDevice> devicesRemoved = this.Where(l1 => !deviceProperties.Any(l2 => l1.DeviceID == l2["Unique Device ID"].ToString()));
                if (devicesRemoved.Count() > 0)
                {
                    foreach (ScanningDevice device in devicesRemoved)
                    {
                        device.IsConnected = false;
                    }
                }
            }
        }

        public void AddDevice(ScanningDevice device)
        {
            if (!Contains(device))
            {
                Add(device);
            }
            else
            {
                throw new ArgumentException($"This device {device.Name}, ID #{device.ID} already exists.");
            }
        }

        /// <summary>
        /// Attempts to create a <see cref="ScanningDevice"/> instance using the <paramref name="deviceID"/>
        /// Returns <see langword="null"/> if no matching device can be found
        /// </summary>
        /// <param name="deviceID">A unique <see cref="IDevice.DeviceID"/> to match to a device listed by the WIA driver</param>
        /// <returns>A <see cref="ScanningDevice"/> instance matching the <paramref name="deviceID"/></returns>
        public ScanningDevice AddDevice(string deviceID)
        {
            return AddDevice(deviceID, null);
        }

        /// <summary>
        /// Attempts to create a <see cref="ScanningDevice"/> instance using the <paramref name="deviceID"/>
        /// Returns <see langword="null"/> if no matching device can be found
        /// </summary>
        /// <param name="deviceID">A unique <see cref="IDevice.DeviceID"/> to match to a device listed by the WIA driver</param>
        /// <param name="config"><see cref="IImagingDeviceConfig"/> settings for the returned <see cref="ScanningDevice"/></param>
        /// <param name="connectRetries">The number of attempts to retry connecting to the WIA driver</param>
        /// <returns>A <see cref="ScanningDevice"/> instance matching the <paramref name="deviceID"/></returns>
        public ScanningDevice AddDevice(string deviceID, IImagingDeviceConfig config, int connectRetries = 1)
        {
            _semaphore.Wait();
            ScannerSettings settings = null;

            // Wait until the WIA device manager is ready
            while (connectRetries > 0)
            {
                try
                {
                    // Attempt to create a new ScanningDevice from the deviceID
                    settings = SystemDevices.GetScannerDevices().Where(x => x.Id == deviceID).FirstOrDefault();
                    break;
                }
                catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                {
                    if (--connectRetries > 0)
                    {
                        // Wait until the scanner is ready if it is warming up, busy etc
                        // Also retry if the WIA driver does not respond in time (NullReference)
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            if (settings != null)
            {
                ScanningDevice device = new ScanningDevice(settings, config);
                AddDevice(device);

                return device;
            }
            else
            {
                return null;
            }
        }

        public void SetDeviceEnabled(IImagingDevice device, bool enabled)
        {
            this.Where(x => x.DeviceID == device.DeviceID).First().IsEnabled = enabled;
        }

        public void SetDeviceEnabled(string deviceID, bool enabled)
        {
            this.Where(x => x.DeviceID == deviceID).First().IsEnabled = enabled;
        }

        public void EnableAll()
        {
            SetByEnabled(true);
        }

        public void DisableAll()
        {
            SetByEnabled(false);
        }

        private void SetByEnabled(bool enabled)
        {
            // Set the IsEnabled property on all members of the collection
            foreach (IImagingDevice device in this)
            {
                device.IsEnabled = enabled;
            }
        }

        public IEnumerable<IImagingDevice> GetByEnabled(bool enabled)
        {
            return this.Where(x => x.IsEnabled = enabled).AsEnumerable();
        }

        /// <summary>
        /// Lists the static properties of scanners connected to the system.
        /// </summary>
        private IList<IDictionary<string, object>> SystemScannerProperties(int connectRetries = 1)
        {
            _semaphore.Wait();
            IList<IDictionary<string, object>> properties = new List<IDictionary<string, object>>();

            // Wait until the WIA device manager is ready
            while (connectRetries > 0)
            {
                try
                {
                    properties = SystemDevices.GetScannerDeviceProperties();
                    break;
                }
                catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                {
                    if (--connectRetries > 0)
                    {
                        // Wait until the scanner is ready if it is warming up, busy etc
                        // Also retry if the WIA driver does not respond in time (NullReference)
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            return properties;
        }

        private IEnumerable<IImagingDevice> SystemScanners(IImagingDeviceConfig deviceConfig, int connectRetries = 1)
        {
            List<IImagingDevice> deviceList = new List<IImagingDevice>();

            // Wait until the WIA device manager is ready
            while (connectRetries > 0)
            {
                _semaphore.Wait();

                try
                {
                    var systemScanners = SystemDevices.GetScannerDevices();

                    // Check that at least one scanner can be found
                    if (systemScanners.FirstOrDefault() == null)
                    {
                        return deviceList;
                    }

                    foreach (ScannerSettings settings in systemScanners)
                    {
                        // Store the device in the collection
                        deviceList.Add(new ScanningDevice(settings, deviceConfig));
                    }

                    break;
                }
                catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                {
                    if (--connectRetries > 0)
                    {
                        // Wait until the scanner is ready if it is warming up, busy etc
                        // Also retry if the WIA driver does not respond in time (NullReference)
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            return deviceList;
        }
    }

    /// <summary>
    /// Provides functionality for WIA enabled scanners
    /// </summary>
    public class ScanningDevice : IImagingDevice
    {
        public string Name { get { return _scannerSettings.Name; } }
        public int ID { get { return GetSimpleID(_scannerSettings.Id); } }
        public string DeviceID { get { return _scannerSettings.Id; } }
        public IList<KeyValuePair<string, object>> DeviceSettings { get { return _scannerSettings.ScannerDeviceSettings.ToList(); } }
        public IList<byte[]> Images { get; protected internal set; } = new List<byte[]>();
        public IList<int> SupportedResolutions { get { return _scannerSettings.SupportedResolutions; } }
        public IImagingDeviceConfig ImageSettings { get; set; }
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetField(ref _isEnabled, value); }
        }
        public bool IsConnected
        {
            get { return _isConnected; }
            protected internal set { SetField(ref _isConnected, value); }
        }
        public bool IsImaging
        {
            get { return _isImaging; }
            private set { SetField(ref _isImaging, value); }
        }
        public bool IsAutomaticDocumentFeeder { get { return _scannerSettings.IsAutomaticDocumentFeeder; } }
        public bool IsDuplex { get { return _scannerSettings.IsDuplex; } }
        public bool IsFlatbed { get { return _scannerSettings.IsFlatbed; } }
        public int ScanRetries
        {
            get { return _scanRetries; }
            set { SetField(ref _scanRetries, value); }
        }
        public enum ImageFormat
        {
            Bmp,
            Png,
            Gif,
            Jpeg,
            Tiff
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _isEnabled;
        private bool _isConnected = true;
        private bool _isImaging;
        private int _scanRetries = 10;
        private ScannerSettings _scannerSettings;

        public ScanningDevice(ScannerSettings settings)
        {
            _scannerSettings = settings;
        }

        public ScanningDevice(ScannerSettings settings, IImagingDeviceConfig config)
        {
            _scannerSettings = settings;
            ImageSettings = config;
        }

        public void GetImage()
        {
            GetImage(WiaImageFormat.Bmp);
        }

        public void ClearImages()
        {
            Images = new List<byte[]>();
        }

        public void GetImage(WiaImageFormat format)
        {
            if (!IsConnected)
            {
                throw new COMException("The scanner is not connected.");
            }

            if (!_scannerSettings.SupportedTransferFormats.ContainsKey(format.Value))
            {
                throw new ArgumentException($"The image format {format} is not supported by this device.");
            }

            // Wait until the scanner is ready
            IsImaging = true;
            while (_scanRetries > 0)
            {
                try
                {
                    // Connect to the specified device and create a COM object representation
                    using (ScannerDevice scannerDevice = new ScannerDevice(_scannerSettings))
                    {
                        // Configure the device
                        IImagingDeviceConfig deviceConfig = ImageSettings;
                        if (ImageSettings is null)
                        {
                            deviceConfig = new ScanningDeviceSettings
                            {
                                Brightness = 1,
                                Contrast = 1,
                                ColorFormat = ImageColorFormat.Color,
                                // Select the highest resolution available
                                Resolution = SupportedResolutions.Max()
                            };
                        }

                        // Check that the selected resolution is supported by this device
                        if (!SupportedResolutions.Contains(deviceConfig.Resolution))
                        {
                            // Find the closest supported resolution instead
                            deviceConfig.Resolution = SupportedResolutions
                                .OrderBy(v => v)
                                .OrderBy(item => Math.Abs(deviceConfig.Resolution - item))
                                .First();
                        }

                        scannerDevice.ScannerPictureSettings(pictureConfig =>
                            pictureConfig
                                .ColorFormat(ScanningDeviceSettings.ColorTypeFromFormat(deviceConfig.ColorFormat))
                                .Resolution(deviceConfig.Resolution)
                                .Brightness(deviceConfig.Brightness)
                                .Contrast(deviceConfig.Contrast)
                                .StartPosition(left: 0, top: 0)
                            );

                        // Replace any existing images
                        ClearImages();

                        // Retrieve image(s) from scanner
                        scannerDevice.PerformScan(format);

                        // Store images for processing etc
                        foreach (byte[] image in scannerDevice.ExtractScannedImageFiles())
                        {
                            // Convert image to PNG format before storing byte array
                            // Greatly reduces memory footprint compared to raw BMP
                            Images.Add(image.AsFormat(ImageFormat.Png.ToDrawingImageFormat()));
                        }
                    }

                    // Cancel retry loop if successful
                    break;
                }
                catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                {
                    if (--_scanRetries > 0)
                    {
                        // Wait until the scanner is ready if it is warming up, busy etc
                        // Also retry if the WIA driver does not respond in time (NullReference)

                        // Connecting a scanner when a scan is already in progress
                        // COMException: The remote procedure call failed. (0x800706BE)
                        // HResult	-2146233088	int

                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }

                    // Mark the device as unreachable if the limit is reached
                    IsConnected = false;
                    throw;
                }
                catch (Exception ex) when (ex is InvalidOperationException)
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
        }

        /// <summary>
        /// Write an image captured with <see cref="GetImage"/> to disk
        /// Images are stored in the user's <see cref="Environment.SpecialFolder.MyPictures"/> directory as PNGs
        /// </summary>
        public void SaveImage()
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToString(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
            SaveImage("image", directory, "png");
        }

        /// <summary>
        /// Write an image captured with <see cref="GetImage"/> to disk
        /// </summary>
        /// <param name="fileName">The image file name, the file extension ignored and instead inferred from <paramref name="fileFormat"/></param>
        /// <param name="directory">The directory path to use when storing the image</param>
        /// <param name="fileFormat">The image format file extension <see cref="string"/> used to store the image</param>
        /// <returns>A collection of file path <see cref="string"/>s for the newly created images</returns>
        public IEnumerable<string> SaveImage(string fileName, string directory, string fileFormat = "png")
        {
            // Parse the file extension and ensure it is valid
            var imageFormat = new ImageFormat().FromString(fileFormat);

            return SaveImage(fileName, directory, imageFormat);
        }

        /// <summary>
        /// Write an image captured with <see cref="GetImage"/> to disk
        /// Images are stored losslessly (in supported formats) with a colour depth of 24 bit per pixel
        /// </summary>
        /// <param name="fileName">The image file name, the file extension ignored and instead inferred from <paramref name="fileFormat"/></param>
        /// <param name="directory">The directory path to use when storing the image</param>
        /// <param name="imageFormat">The <see cref="ImageFormat"/> used to store the image</param>
        /// <returns>A collection of file path <see cref="string"/>s for the newly created images</returns>
        public IEnumerable<string> SaveImage(string fileName, string directory, ImageFormat imageFormat = ImageFormat.Png)
        {
            if (Images == null | Images.Count == 0)
            {
                throw new InvalidOperationException($"No images available. Please call the {nameof(GetImage)} method first.");
            }
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("A valid filename and directory must be supplied");
            }

            // Get full filename and path
            Directory.CreateDirectory(directory);
            fileName = Path.Combine(directory, fileName);
            fileName = Path.ChangeExtension(fileName, imageFormat.ToString().ToLower());

            // Use lossless compression with highest quality
            using (EncoderParameters encoderParameters = new EncoderParameters().AddParams(
                compression: EncoderValue.CompressionLZW,
                quality: 100,
                colorDepth: 32
                ))
            {
                // Write all images to disk
                foreach (var imageBytes in Images)
                {
                    imageBytes.ToImage().Save(
                        fileName,
                        imageFormat.ToDrawingImageFormat().CodecInfo(),
                        encoderParameters
                        );
                    yield return fileName;
                }
            }
        }

        private static int GetSimpleID(string deviceID)
        {
            // Get the last two characters of the device instance path
            string shortID = deviceID.Substring(Math.Max(0, deviceID.Length - 2));

            if (int.TryParse(shortID, out int intID))
            {
                return intID;
            }
            else
            {
                // Return the numeric representation of the characters instead
                return shortID.GetHashCode();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public bool Equals(IImagingDevice device)
        {
            return null != device && DeviceID == device.DeviceID;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ScanningDevice);
        }

        public override int GetHashCode()
        {
            return DeviceID.GetHashCode();
        }
    }

    public class ScanningDeviceSettings : IImagingDeviceConfig
    {
        public ImageColorFormat ColorFormat { get; set; } = ImageColorFormat.Color;
        public ColorType ColorType { get { return ColorTypeFromFormat(ColorFormat); } }
        public int Resolution { get; set; }
        public int Brightness { get; set; }
        public int Contrast { get; set; }

        public ScanningDeviceSettings() { }

        public ScanningDeviceSettings(ImageColorFormat colorFormat, int resolution, int brightness, int contrast)
        {
            ColorFormat = colorFormat;
            Resolution = resolution;
            Brightness = brightness;
            Contrast = contrast;
        }

        /// <summary>
        /// Returns a ColorType class from a ColorFormat Enum.
        /// Allows conversion of type from json settings file
        /// </summary>
        /// <param name="colorFormat"></param>
        /// <returns></returns>
        public static ColorType ColorTypeFromFormat(ImageColorFormat colorFormat)
        {
            // ColorType properties are internal and not accessible for comparison
            switch (colorFormat)
            {
                case ImageColorFormat.BlackAndWhite:
                    return ColorType.BlackAndWhite;
                case ImageColorFormat.Greyscale:
                    return ColorType.Greyscale;
                default:
                    return ColorType.Color;
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public class ImageFileConfig : IImageFileConfig
    {
        public string Directory { get; set; }
        public string Prefix { get; set; }
        public string Format { get; set; }
        public List<string> SupportedFormats { get; set; }
        public string DateFormat { get; set; }
        public string TimeFormat { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public static class ImageFormatExtensions
    {
        /// <summary>
        /// Convert a file extension string to an <see cref="ScanningDevice.ImageFormat"/> <see cref="Enum"/>.
        /// </summary>
        /// <param name="value">An <see cref="ScanningDevice.ImageFormat"/> instance</param>
        /// <param name="imageFormatStr">A file extension <see cref="string"/></param>
        /// <returns>An <see cref="ScanningDevice.ImageFormat"/> <see cref="Enum"/></returns>
        public static ScanningDevice.ImageFormat FromString(this ScanningDevice.ImageFormat value, string imageFormatStr)
        {
            if (Enum.TryParse(imageFormatStr, ignoreCase: true, out value))
            {
                return value;
            }
            else
            {
                throw new ArgumentException($"{imageFormatStr} is not a supported image file extension.");
            }
        }

        /// <summary>
        /// Convert to a <see cref="System.Drawing.Imaging.ImageFormat"/> instance.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A <see cref="System.Drawing.Imaging.ImageFormat"/> instance</returns>
        public static System.Drawing.Imaging.ImageFormat ToDrawingImageFormat(this ScanningDevice.ImageFormat value)
        {
            return (System.Drawing.Imaging.ImageFormat)typeof(System.Drawing.Imaging.ImageFormat)
                    .GetProperty(value.ToString(), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase)
                    .GetValue(null);
        }
    }
}