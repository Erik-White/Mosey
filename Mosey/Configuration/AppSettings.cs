using System;
using System.Collections.Generic;
using System.Text;
using Mosey.Models;
using Mosey.Services;

namespace Mosey.Configuration
{
    public class AppSettings : IConfigGroup<AppSettings>
    {
        public ImageFileConfig ImageFile { get; set; }
        public IntervalTimerConfig ScanTimer { get; set; }
        public IntervalTimerConfig UITimer { get; set; }
        public ScanningDeviceSettings Image { get; set; }
        public DeviceConfig Device { get; set; }

        public AppSettings Copy()
        {
            AppSettings copy = new AppSettings
            {
                ImageFile = (ImageFileConfig)ImageFile.Clone(),
                ScanTimer = (IntervalTimerConfig)ScanTimer.Clone(),
                UITimer = (IntervalTimerConfig)UITimer.Clone(),
                Image = (ScanningDeviceSettings)Image.Clone(),
                Device = (DeviceConfig)Device.Clone()
            };

            return copy;
        }
    }

    public class DeviceConfig : IConfig
    {
        public bool EnableWhenConnected { get; set; }
        public bool EnableWhenScanning { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
