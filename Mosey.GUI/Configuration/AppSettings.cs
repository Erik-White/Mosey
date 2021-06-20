using System;
using System.Collections.Generic;
using System.Linq;
using Mosey.Models;
using Mosey.Services;
using Mosey.Services.Imaging;

namespace Mosey.GUI.Configuration
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
}
