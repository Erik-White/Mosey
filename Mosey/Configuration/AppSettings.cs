using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mosey.Models;
using Mosey.Services;
using Mosey.Services.Imaging;

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

    /// <summary>
    /// Configurations settings related to physical scanning devices.
    /// </summary>
    public class DeviceConfig : IConfig
    {
        /// <summary>
        /// If devices should be enabled when they are connected.
        /// </summary>
        public bool EnableWhenConnected { get; set; }

        /// <summary>
        /// If devices that are connected while a scan is already in progress should be enabled.
        /// </summary>
        public bool EnableWhenScanning { get; set; }

        /// <summary>
        /// If the highest available <see cref="StandardResolutions"/> should be used for image capture.
        /// </summary>
        public bool UseHighestResolution { get; set; }

        /// <summary>
        /// Information related image resolutions.
        /// </summary>
        public IEnumerable<ResolutionMetaData> ResolutionData { get; set; }

        /// <summary>
        /// A common set of resolutions that are support by most devices.
        /// </summary>
        public IEnumerable<int> StandardResolutions
        {
            get
            {
                return ResolutionData.Select(r => r.Resolution).Distinct().ToList();
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Get the available meta data for a particular image resolution
        /// </summary>
        /// <param name="resolution">An image resolution, in DPI</param>
        /// <returns></returns>
        public ResolutionMetaData GetResolutionMetaData(int resolution)
        {
            return ResolutionData.Where(r => r.Resolution == resolution).FirstOrDefault();
        }

        /// <summary>
        /// Information related to an image resolution.
        /// </summary>
        public class ResolutionMetaData
        {
            /// <summary>
            /// Image resolution, in Dots Per Inch (DPI).
            /// </summary>
            public int Resolution { get; set; }

            /// <summary>
            /// The time taken for an image to be obtained.
            /// </summary>
            // System.Text.Json doesn't support TimeSpan [de]serialization
            // It is planned for .NET Core 5
            [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonTimeSpanConverter))]
            public TimeSpan ImagingTime { get; set; }

            /// <summary>
            /// A file size, in bytes.
            /// </summary>
            public long FileSize { get; set; }
        }
    }
}
