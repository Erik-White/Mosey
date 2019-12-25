using System;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Mosey.Models;
using DNTScanner.Core;

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
        private static bool _isRefreshInProgress;
        private bool _disposed;

        public ScanningDevices()
        {
        }

        public ScanningDevices(IImagingDeviceSettings deviceConfig)
        {
            GetDevices(deviceConfig);
        }

        public void GetDevices()
        {
            // Populate a new collection of scanners using default image settings
            GetDevices(new ScanningDeviceSettings());
        }

        public void GetDevices(IImagingDeviceSettings deviceConfig)
        {
            // Empty the collection
            Clear();

            // Populate a new collection of scanners using specified image settings
            foreach (ScanningDevice device in SystemScanners((ScanningDeviceSettings)deviceConfig, ConnectRetries))
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

        public void RefreshDevices(IImagingDeviceSettings deviceConfig, bool enableDevices = true)
        {
            // Do not attempt if already in progress
            if (_isRefreshInProgress | IsImagingInProgress)
            {
                return;
            }

            IEnumerable<IImagingDevice> systemScanners = SystemScanners((ScanningDeviceSettings)deviceConfig, ConnectRetries);
            foreach (ScanningDevice device in systemScanners)
            {
                if (!Contains(device))
                {
                    device.IsEnabled = enableDevices; 
                    // Add any new devices
                    AddDevice(device);
                }
                else
                {
                    ScanningDevice existingDevice = (ScanningDevice)this.Where(d => d.Equals(device)).First();

                    // If the device is already in the collection but not connected
                    // Replace the existing device with the connected device
                    if (!existingDevice.IsConnected)
                    {
                        bool enabled = existingDevice.IsEnabled;
                        Remove(existingDevice);

                        device.IsEnabled = enabled;
                        AddDevice(device);
                    }
                }
            }
            // Update device status if device(s) removed
            IEnumerable<IImagingDevice> devicesRemoved = this.Except(systemScanners);
            if (devicesRemoved.Count() > 0)
            {
                foreach(ScanningDevice device in devicesRemoved)
                {
                    device.IsConnected = false;
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
                throw new ArgumentException($"This device {device.Name}, ID #{device.ID.ToString()} already exists.");
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
            foreach(IImagingDevice device in this)
            {
                device.IsEnabled = enabled;
            }
        }

        public IEnumerable<IImagingDevice> GetByEnabled(bool enabled)
        {
            return this.Where(x => x.IsEnabled = enabled).AsEnumerable();
        }

        private IEnumerable<IImagingDevice> SystemScanners(ScanningDeviceSettings deviceConfig, int connectRetries)
        {
            List<IImagingDevice> deviceList = new List<IImagingDevice>();
            if (_isRefreshInProgress)
            {
                deviceList.AddRange(Items);
                return deviceList;
            }
            _isRefreshInProgress = true;

            // Wait until the WIA device manager is ready
            while (connectRetries > 0)
            {
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
                        deviceList.Add(new ScanningDevice(settings));
                    }

                    systemScanners = null;
                    break;
                }
                catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                {
                    // Retry if device is warming up, busy etc
                    // Also retry if the WIA driver does not respond in time (NullReference)
                    if(ex is NullReferenceException | ex is COMException)
                    {
                        System.Threading.Thread.Sleep(1000);
                        --connectRetries;
                        continue;
                    }
                }
                finally
                {
                    _isRefreshInProgress = false;
                }
            }
            return deviceList;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (IDisposable item in this)
                    {
                        if (item != null)
                        {
                            item.Dispose();
                        }
                    }
                }
                Clear();
                _disposed = true;
            }
        }

        ~ScanningDevices()
        {
            Dispose(false);
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
        public List<KeyValuePair<string, object>> DeviceSettings { get { return _scannerSettings.ScannerDeviceSettings.ToList<KeyValuePair<string, object>>(); } }
        public IList<byte[]> Images { get; protected internal set; } = new List<byte[]>();
        public ScanningDeviceSettings ImageSettings { get; set; }
        public bool IsEnabled
        {
            get { return _isenabled; }
            set { SetField(ref _isenabled, value); }
        }
        public bool IsConnected
        {
            get { return _isconnected; }
            protected internal set { SetField(ref _isconnected, value); }
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
        private bool _isenabled;
        private bool _isconnected = true;
        private bool _isImaging;
        private int _scanRetries = 10;
        private ScannerSettings _scannerSettings;
        private ScanningDeviceSettings _scannerConfig;

        public ScanningDevice(ScannerSettings settings)
        {
            _scannerSettings = settings;
        }

        public ScanningDevice(ScannerSettings settings, ScanningDeviceSettings config)
        {
            _scannerSettings = settings;
            _scannerConfig = config;
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
                        ScanningDeviceSettings deviceConfig = _scannerConfig;
                        if (_scannerConfig is null)
                        {
                            deviceConfig = new ScanningDeviceSettings();
                        }
                        scannerDevice.ScannerPictureSettings(config =>
                            config.ColorFormat(deviceConfig.ColorType)
                                .Resolution(deviceConfig.Resolution)
                                .Brightness(deviceConfig.Brightness)
                                .Contrast(deviceConfig.Contrast)
                                .StartPosition(left: 0, top: 0)
                            );

                        // Replace any existing images
                        Images = new List<byte[]>();

                        // Retrieve image(s) from scanner
                        scannerDevice.PerformScan(format);

                        // Store images for processing etc
                        foreach (byte[] image in scannerDevice.ExtractScannedImageFiles())
                        {
                            Images.Add(image.ToArray());
                        }
                    }

                    // Cancel loop if successful
                    break;
                }
                catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                {
                    if(--_scanRetries > 0)
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
                    break;
                }
                catch (Exception ex) when (ex is InvalidOperationException)
                {
                    // Scanner has been disconnected or similar
                    _isconnected = false;
                    throw;
                }
                finally
                {
                    IsImaging = false;
                }
            }
        }

        public void SaveImage()
        {
            SaveImage("image");
        }

        public IEnumerable<string> SaveImage(string fileName, string directory = "", string fileFormat = "")
        {
            if (Images == null | Images.Count == 0)
            {
                throw new InvalidOperationException($"No images available. Please call the {nameof(GetImage)} method first.");
            }

            // Check that the file extension is valid
            ImageFormat imageFormat = ImageFormatFromString(fileFormat);

            // Save to the user's Pictures folder if none is set
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToString(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
            }
            Directory.CreateDirectory(directory);

            fileName = Path.Combine(directory, fileName);
            fileName = Path.ChangeExtension(fileName, imageFormat.ToString().ToLower());

            // Write all images to disk
            foreach (byte[] image in Images)
            {
                File.WriteAllBytes(fileName, image);
                yield return fileName;
            }
        }

        /// <summary>
        /// Convert a file extension string to an ImageFormat enum.
        /// Allows conversion of type from json settings file
        /// </summary>
        /// <param name="imageFormatStr"></param>
        /// <returns>An ImageFormat enum</returns>
        public static ImageFormat ImageFormatFromString(string imageFormatStr)
        {
            if (Enum.TryParse(imageFormatStr, ignoreCase: true, out ImageFormat imageFormat))
            {
                return imageFormat;
            }
            else
            {
                throw new FileFormatException($"{imageFormatStr} is not a valid image file extension.");
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

    public class ScanningDeviceSettings : IImagingDeviceSettings
    {
        public ColorFormat ColorFormat { get; set; }
        public ColorType ColorType { get { return ColorTypeFromFormat(ColorFormat); } }
        public int Resolution { get; set; }
        public int Brightness { get; set; }
        public int Contrast { get; set; }

        public ScanningDeviceSettings()
        {
            UseDefaults();
        }

        public ScanningDeviceSettings(ColorFormat colorFormat, int resolution, int brightness, int contrast)
        {
            ColorFormat = colorFormat;
            Resolution = resolution;
            Brightness = brightness;
            Contrast = contrast;
        }

        public void UseDefaults()
        {
            // Load default stored settings
            Common.Configuration.Bind("Image", this);
        }

        /// <summary>
        /// Returns a ColorType class from a ColorFormat Enum.
        /// Allows conversion of type from json settings file
        /// </summary>
        /// <param name="colorFormat"></param>
        /// <returns></returns>
        public static ColorType ColorTypeFromFormat(ColorFormat colorFormat)
        {
            // ColorTypes properties are internal and not accessible for comparison
            switch (colorFormat)
            {
                case ColorFormat.BlackAndWhite:
                    return ColorType.BlackAndWhite;
                case ColorFormat.Greyscale:
                    return ColorType.Greyscale;
                default:
                    return ColorType.Color;
            }
        }
    }
}