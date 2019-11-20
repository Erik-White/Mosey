using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Mosey.Models;
using DNTScanner.Core;

namespace Mosey.Services
{
    public class ScanningDevices : ObservableCollection<IImagingDevice>, IImagingDevices
    {
        public bool DevicesReady
        {
            get
            {
                return this.All(x => x.Ready is true);
            }
        }
        private bool _disposed;

        public ScanningDevices()
        {
            RefreshDevices();
        }

        public ScanningDevices(IImagingDeviceSettings deviceConfig)
        {
            RefreshDevices(deviceConfig);
        }

        public void RefreshDevices()
        {
            // Populate list of scanners using default image settings
            RefreshDevices(new ScanningDeviceSettings());
        }

        public void RefreshDevices(IImagingDeviceSettings deviceConfig)
        {
            // Release all scanners then empty the collection
            foreach (ScanningDevice device in this)
            {
                device.Dispose();
            }
            Clear();

            foreach (ScanningDevice device in SystemScanners((ScanningDeviceSettings)deviceConfig))
            {
                AddDevice(device);
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
                throw new ArgumentException("This device already exists.", device.ID.ToString());
            }
        }

        public void EnableDevice(IImagingDevice device)
        {
            this.Where(x => x.DeviceID == device.DeviceID).First().Enabled = true;
        }
        public void EnableDevice(string deviceID)
        {
            this.Where(x => x.DeviceID == deviceID).First().Enabled = true;
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
            // Set the Enabled property on all members of the collection
            this.All(x => { x.Enabled = enabled; return true; });
        }

        public IEnumerable<IImagingDevice> GetByEnabled(bool enabled)
        {
            return this.Where(x => x.Enabled = enabled).AsEnumerable();
        }

        private IEnumerable<ScanningDevice> SystemScanners(ScanningDeviceSettings deviceConfig)
        {
            List<ScanningDevice> deviceList = new List<ScanningDevice>();
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
            }
            catch (COMException ex)
            {
                // TODO: Retry if device is busy or warming up
                throw new Exception(WiaExceptionExtensions.GetComErrorMessage(ex), ex);
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
        public bool Enabled { get; set; }
        public bool Ready { get; private set; } = true;
        public int ScanRetries { get; set; } = 30;
        public bool IsAutomaticDocumentFeeder { get { return _scannerSettings.IsAutomaticDocumentFeeder; } }
        public bool IsDuplex { get { return _scannerSettings.IsDuplex; } }
        public bool IsFlatbed { get { return _scannerSettings.IsFlatbed; } }
        public enum ImageFormat
        {
            Bmp,
            Png,
            Gif,
            Jpeg,
            Tiff
        }
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

        public void GetImage_bak(WiaImageFormat format)
        {
            if (_scannerDevice != null)
            {
                try
                {
                    _scannerDevice.PerformScan(format);
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    throw new Exception(WiaExceptionExtensions.GetComErrorMessage(ex), ex);
                }
            }
        }

        public void GetImage(WiaImageFormat format)
        {
            int retryCount = 0;

            if (_scannerDevice != null)
            {
                if (!_scannerSettings.SupportedTransferFormats.ContainsKey(format.Value))
                {
                    throw new ArgumentException($"The image format {format} is not supported by this device.");
                }

                // Wait until the scanner is ready
                while (retryCount < ScanRetries)
                {
                    try
                    {
                        Ready = false;
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
                        Ready = true;
                    }
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

        public List<KeyValuePair<string, object>> DeviceSettings { get { return GetScannerDeviceSettings(); } }
        public List<KeyValuePair<string, object>> GetScannerDeviceSettings()
        {

            return _scannerSettings.ScannerDeviceSettings.ToList<KeyValuePair<string, object>>();
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
