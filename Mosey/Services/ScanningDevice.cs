using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mosey.Models;
using DNTScanner.Core;

namespace Mosey.Services
{
    public class ScanningDevices : IImagingDevices
    {
        public List<ScanningDevice> devices { get; private set; }

        public ScanningDevices()
        {
            // Populate list of scanners using default image settings
            RefreshDevices(new ScanningDeviceSettings());
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
            if(devices != null)
            {
                devices.Clear();
            }
            devices = SystemScanners((ScanningDeviceSettings)deviceConfig);
        }

        public void Add(ScanningDevice device)
        {
            devices.Add(device);
        }

        public void EnableDevice(IImagingDevice device)
        {
            devices.Find(x => x.ID == device.ID).Enabled = true;
        }

        public void EnableDevice(string deviceName)
        {
            devices.Find(x => x.Name == deviceName).Enabled = true;
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
            devices.All(x => {x.Enabled = enabled; return true; });
        }

        public IEnumerable<IImagingDevice> GetByEnabled( bool enabled)
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
                     * ScannerDevices will be disposed with using
                     * This prevents their use elsewhere
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
                            //.Extent(width: 1000 * dpi, height: 1000 * dpi)
                        );

                    // Store the device in the collection
                    deviceList.Add(new ScanningDevice(device, settings));
                }
            }
            catch (COMException ex)
            {
                Console.WriteLine(ex.GetComErrorMessage());
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

        public void SaveImage()
        {
            SaveImage(Directory.GetCurrentDirectory(), "image{WIAImageFormat.Jpeg.ToString()}");
        }

        public IEnumerable<string> SaveImage(string directory, string fileName)
        {
            // TODO: Get directory from user or default to appdir
            fileName = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            return scannerDevice.SaveScannedImageFiles(fileName);
            /*
            foreach (var file in scannerDevice.SaveScannedImageFiles(fileName))
            {
                System.Diagnostics.Debug.WriteLine(file.ToString());
            }
            */
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
