using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DNTScanner.Core;

namespace Mosey.Models
{
    public interface IImagingDevices : IEnumerable<IImagingDevice>
    {
        public void RefreshDevices() { }
        public void EnableDevice(IImagingDevice device) { }
        public void EnableDevice(string deviceName) { }
        public void EnableAll() { }
        public void DisableAll() { }

    }

    public interface IImagingDevice// : IDisposable
    {
        public string Name { get; }
        public string ID { get; }
        public bool Enabled { get; set; }
        public void GetImage() { }
        public void SaveImage() { }
    }

    public interface IImagingDeviceSettings
    {
        public int Resolution { get; set; }
        public int Brightness { get; set; }
        public int Contrast { get; set; }
    }
    public class ScanningDevices : IImagingDevices
    {
        public List<ScanningDevice> devices { get; private set; }

        public ScanningDevices()
        {
            // Get default scanner image settings
            // Should be returned from settings but hard coded for now
            RefreshDevices(new ScanningDeviceSettings());
        }

        public ScanningDevices(ScanningDeviceSettings deviceConfig)
        {
            RefreshDevices(deviceConfig);
        }

        public void RefreshDevices(ScanningDeviceSettings deviceConfig)
        {
            if(devices != null)
            {
                devices.Clear();
            }
            devices = SystemScanners(deviceConfig);
        }

        public void Add(ScanningDevice device)
        {
            devices.Add(device);
        }

        public void EnableDevice(ScanningDevice device)
        {
            devices.Find(x => x.ID == device.ID).Enabled = true;
        }

        public IEnumerable<ScanningDevice> GetByEnabled( bool enabled)
        {
            return devices.FindAll(x => x.Enabled = enabled).ToList();
        }

        public IEnumerator<IImagingDevice> GetEnumerator()
        {
            return devices.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<ScanningDevice> SystemScanners(ScanningDeviceSettings deviceConfig)
        {
            List<ScanningDevice> deviceList = new List<ScanningDevice>();
            try
            {
                // Find the first available scanner
                var systemScanners = SystemDevices.GetScannerDevices();
                if (systemScanners.FirstOrDefault() == null)
                {
                    Console.WriteLine("Please connect your scanner to the system and also make sure its driver is installed.");
                    return deviceList;
                }

                foreach(ScannerSettings settings in systemScanners)
                {
                    /*
                    using (ScannerDevice device = new ScannerDevice(settings))
                    {
                        // Use the same image settings for all scanners
                        device.ScannerPictureSettings(config =>
                            config.ColorFormat(deviceConfig.ColorType)
                                .Resolution(deviceConfig.Resolution)
                                .Brightness(deviceConfig.Brightness)
                                .Contrast(deviceConfig.Contrast)
                                //.StartPosition(left: 0, top: 0)
                                //.Extent(width: 1250 * dpi, height: 1700 * dpi)
                            );

                        deviceList.Add(new ScanningDevice(device, settings);
                    }
                    */
                    ScannerDevice device = new ScannerDevice(settings);
                        // Use the same image settings for all scanners
                        device.ScannerPictureSettings(config =>
                            config.ColorFormat(deviceConfig.ColorType)
                                .Resolution(deviceConfig.Resolution)
                                .Brightness(deviceConfig.Brightness)
                                .Contrast(deviceConfig.Contrast)
                            //.StartPosition(left: 0, top: 0)
                            //.Extent(width: 1250 * dpi, height: 1700 * dpi)
                            );

                        deviceList.Add(new ScanningDevice(device, settings));
                }
            }
            catch (COMException ex)
            {
                var friendlyErrorMessage = ex.GetComErrorMessage(); // How to show a better error message to users
                Console.WriteLine(friendlyErrorMessage);
                Console.WriteLine(ex);
                System.Diagnostics.Debug.WriteLine("{0}; {1}", friendlyErrorMessage, ex);
                throw ex;
            }
            return deviceList;
        }
    }

    /// <summary>
    /// Provides a thin wrapper class to allow for dependency injection
    /// </summary>
    public class ScanningDevice : IImagingDevice
    {
        public string Name { get { return scannerSettings.Name;  } }
        public string ID {  get { return scannerSettings.Id;  } }
        public bool Enabled { get; set; }
        public bool IsAutomaticDocumentFeeder { get { return scannerSettings.IsAutomaticDocumentFeeder; } }
        public bool IsDuplex { get { return scannerSettings.IsDuplex; } }
        public bool IsFlatbed { get { return scannerSettings.IsFlatbed; } }
        private ScannerDevice scannerDevice;
        private ScannerSettings scannerSettings;

        public ScanningDevice(ScannerDevice device, ScannerSettings settings)
        {
            scannerDevice = device;
            scannerSettings = settings;
        }

        public void GetImage()
        {
            GetImage(WiaImageFormat.Jpeg);
        }
        public void GetImage(WiaImageFormat format)
        {
            if (scannerDevice != null)
            {
                scannerDevice.PerformScan(format);
            }
        }

        public void SaveImage(string directory, string fileName)
        {
            // TODO: Get directory from user or default to appdir
            fileName = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            foreach (var file in scannerDevice.SaveScannedImageFiles(fileName))
            {
                System.Diagnostics.Debug.WriteLine(file.ToString());
            }
        }
    }

    public class ScanningDeviceSettings : IImagingDeviceSettings
    {
        public ColorType ColorType { get; set; }
        public int Resolution { get; set; }
        public int Brightness { get; set; }
        public int Contrast { get; set; }

        public ScanningDeviceSettings()
        {
            UseDefaults();
        }
        public ScanningDeviceSettings(ColorType colorType, int resolution, int brightness, int contrast)
        {
            ColorType = colorType;
            Resolution = resolution;
            Brightness = brightness;
            Contrast = contrast;
        }
        public void UseDefaults()
        {
            // Load default stored settings
            // TODO: Replace hard coded value with settings values
            ColorType = ColorType.Color;
            Resolution = 200;
            Brightness = 1;
            Contrast = 1;
        }
    }
}
