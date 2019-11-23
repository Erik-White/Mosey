using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Mosey.Models;
using DNTScanner.Core;

namespace Mosey.Services
{
    public class ScanningDevices : ObservableItemsCollection<IImagingDevice>, IImagingDevices<IImagingDevice>
    {
        public int ConnectRetries { get; set; } = 30;
        public bool IsScanInProgress { get { return this.All(x => x.IsScanning is true); } }
        private bool _disposed;

        public ScanningDevices()
        {
            GetDevices();
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
            // Release all scanners then empty the collection
            foreach (ScanningDevice device in this)
            {
                device.Dispose();
            }
            Clear();

            // Populate a new collection of scanners using specified image settings
            foreach (ScanningDevice device in SystemScanners((ScanningDeviceSettings)deviceConfig))
            {
                AddDevice(device);
            }
        }

        public void RefreshDevices()
        {
            // Get a new collection of devices if none already present
            if(Count == 0)
            {
                GetDevices();
            }
            else
            {
                RefreshDevices(new ScanningDeviceSettings());
            }
        }

        public void RefreshDevices(IImagingDeviceSettings deviceConfig)
        {
            IEnumerable<IImagingDevice> systemScanners = SystemScanners((ScanningDeviceSettings)deviceConfig);
            foreach (ScanningDevice device in systemScanners)
            {
                if (!Contains(device))
                {
                    // Add any new devices
                    AddDevice(device);
                }
                else
                {
                    // If the device is already in the collection but not connected
                    // Replace the existing device with the connected device
                    ScanningDevice existingDevice = (ScanningDevice)this.Where(d => d.Equals(device)).First();
                    if (!existingDevice.IsConnected)
                    {
                        bool enabled = existingDevice.IsEnabled;
                        existingDevice.Dispose();
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
                    device.Dispose();
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

        private IEnumerable<ScanningDevice> SystemScanners(ScanningDeviceSettings deviceConfig)
        {
            int retryCount = 0;
            List<ScanningDevice> deviceList = new List<ScanningDevice>();

            // Wait until the scanner is ready
            while (retryCount < ConnectRetries)
            {
                try
                {
                    // Check that at least one scanner can be found
                    var systemScanners = SystemDevices.GetScannerDevices();
                    if (systemScanners.FirstOrDefault() == null)
                    {
                        Console.WriteLine("No scanner devices could be found. Please connect your scanner to the system and ensure that it's driver is installed.");
                        return deviceList;
                    }

                    foreach (ScannerSettings settings in systemScanners)
                    {
                        // Start each scanner on its own thread
                        // COM objects remain bound to the thread that they were started on
                        // This prevents locking the UI and ensures multiple scanner can run together
                        ScannerDevice device = Task.Run(() => new ScannerDevice(settings)).Result;

                        // Use the same image settings for all scanners
                        device.ScannerPictureSettings(config =>
                            config.ColorFormat(deviceConfig.ColorType)
                                .Resolution(deviceConfig.Resolution)
                                .Brightness(deviceConfig.Brightness)
                                .Contrast(deviceConfig.Contrast)
                            );

                        // Store the device in the collection
                        deviceList.Add(new ScanningDevice(device, settings));
                    }
                    break;
                }
                catch (COMException ex)
                {
                    // Wait until the scanner is ready if it is warming up or busy
                    if (ex.ErrorCode == -2145320954 | ex.ErrorCode == -2145320953)
                    {
                        System.Threading.Thread.Sleep(1000);
                        retryCount += 1;
                        continue;
                    }
                    throw new Exception(WiaExceptionExtensions.GetComErrorMessage(ex), ex);
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
                            try
                            {
                                item.Dispose();
                            }
                            catch (Exception)
                            {
                                // log exception and continue
                            }
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
    /// Provides a thin wrapper class to allow for dependency injection
    /// </summary>
    public class ScanningDevice : IImagingDevice
    {
        public string Name { get { return _scannerSettings.Name; } }
        public int ID { get { return GetSimpleID(); } }
        public string DeviceID { get { return _scannerSettings.Id; } }
        public List<KeyValuePair<string, object>> DeviceSettings { get { return _scannerSettings.ScannerDeviceSettings.ToList<KeyValuePair<string, object>>(); } }
        public bool IsEnabled
        {
            get { return _isenabled; }
            set { SetField(ref _isenabled, value); }
        }
        public bool IsConnected
        {
            get { return _isconnected; }
            set { SetField(ref _isconnected, value); }
        }
        public bool IsScanning
        {
            get { return _isscanning; }
            set { SetField(ref _isscanning, value); }
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
        private bool _isscanning;
        private int _scanRetries = 30;
        private bool _disposed;
        private ScannerDevice _scannerDevice;
        private ScannerSettings _scannerSettings;

        public ScanningDevice(ScannerDevice device, ScannerSettings settings)
        {
            _scannerDevice = device;
            _scannerSettings = settings;
        }

        public void GetImage()
        {
            GetImage(WiaImageFormat.Bmp);
        }

        public void GetImage(WiaImageFormat format)
        {
            int retryCount = 0;

            if (_scannerDevice is null || !IsConnected)
            {
                throw new COMException("The scanner is not connected.");
            }

            if (!_scannerSettings.SupportedTransferFormats.ContainsKey(format.Value))
            {
                throw new ArgumentException($"The image format {format} is not supported by this device.");
            }

            // Wait until the scanner is ready
            while (retryCount < _scanRetries)
            {
                try
                {
                    IsScanning = true;
                    _scannerDevice.PerformScan(format);
                    break;
                }
                catch (COMException ex)
                {
                    // Wait until the scanner is ready if it is warming up or busy
                    if (ex.ErrorCode == -2145320954 | ex.ErrorCode == -2145320953)
                    {
                        System.Threading.Thread.Sleep(1000);
                        retryCount += 1;
                        continue;
                    }
                    throw new COMException(WiaExceptionExtensions.GetComErrorMessage(ex), ex);
                }
                finally
                {
                    IsScanning = false;
                }
            }
        }

        public void SaveImage()
        {
            SaveImage("image");
        }

        public IEnumerable<string> SaveImage(string fileName, string directory = "", string fileFormat = "")
        {
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

            try
            {
                return _scannerDevice.SaveScannedImageFiles(fileName);
            }
            catch (COMException ex)
            {
                throw new Exception(WiaExceptionExtensions.GetComErrorMessage(ex), ex);
            }
        }

        /// <summary>
        /// Convert a file extension string to an ImageFormat enum.
        /// Allows conversion of type from json settings file
        /// </summary>
        /// <param name="imageFormatStr"></param>
        /// <returns>An ImageFormat enum</returns>
        public ImageFormat ImageFormatFromString(string imageFormatStr)
        {
            if(Enum.TryParse(imageFormatStr, ignoreCase: true, out ImageFormat imageFormat))
            {
                return imageFormat;
            }
            else
            {
                throw new FileFormatException($"{imageFormatStr} is not a valid image file extension.");
            }
        }

        private int GetSimpleID()
        {
            // Get the last two characters of the device instance path
            // Hopefully a unique integer
            string shortID = _scannerSettings.Id.Substring(Math.Max(0, _scannerSettings.Id.Length - 2));

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

        public bool Equals(IImagingDevice other)
        {
            return null != other && DeviceID == other.DeviceID;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ScanningDevice);
        }

        public override int GetHashCode()
        {
            return DeviceID.GetHashCode();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed){
                if (disposing)
                {
                    if(_scannerDevice != null)
                    {
                        _scannerDevice.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        ~ScanningDevice()
        {
            Dispose(false);
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
        public ColorType ColorTypeFromFormat(ColorFormat colorFormat)
        {
            // ColorTypes properties are internal and not accessible for comparison
            switch (colorFormat) {
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
