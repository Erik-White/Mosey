﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Mosey.GUI.Configuration
{
    /// <summary>
    /// Configurations settings related to physical scanning devices.
    /// </summary>
    public class ScanningDeviceConfig
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
        /// Information related image resolutions.
        /// </summary>
        public IEnumerable<ResolutionMetadata> ResolutionData { get; set; }

        /// <summary>
        /// A common set of resolutions that are support by most devices.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<int> StandardResolutions
            => GetResolutions(ResolutionData);

        /// <summary>
        /// If the highest available <see cref="StandardResolutions"/> should be used for image capture.
        /// </summary>
        public bool UseHighestResolution { get; set; }

        /// <summary>
        /// Get the available meta data for a particular image resolution.
        /// </summary>
        /// <param name="resolution">An image resolution, in DPI</param>
        public ResolutionMetadata GetResolutionMetaData(int resolution)
            => ResolutionData.FirstOrDefault(r => r.Resolution == resolution);

        private static IEnumerable<int> GetResolutions(IEnumerable<ResolutionMetadata> resolutionData)
            => resolutionData?.Select(r => r.Resolution).Distinct().ToList();

        /// <summary>
        /// Information related to an image resolution.
        /// </summary>
        public struct ResolutionMetadata
        {
            /// <summary>
            /// Image resolution, in Dots Per Inch (DPI).
            /// </summary>
            public int Resolution { get; set; }

            /// <summary>
            /// The time taken for an image to be obtained.
            /// </summary>
            public TimeSpan ImagingTime { get; set; }

            /// <summary>
            /// A file size, in bytes.
            /// </summary>
            public long FileSize { get; set; }
        }
    }
}