using System;
using System.Collections.Generic;
using System.Linq;
using static Mosey.GUI.Configuration.DeviceConfig;

namespace Mosey.GUI.Configuration
{
    /// <summary>
    /// Configurations settings related to physical scanning devices.
    /// </summary>
    public record DeviceConfig(bool EnableWhenConnected, bool EnableWhenScanning, bool UseHighestResolution, IEnumerable<ResolutionMetadata> ResolutionData)
    {
        /// <summary>
        /// A common set of resolutions that are support by most devices.
        /// </summary>
        public IEnumerable<int> StandardResolutions
            => GetResolutions(ResolutionData);

        public DeviceConfig()
            : this(default, default, default, Enumerable.Empty<ResolutionMetadata>())
        {
        }

        /// <summary>
        /// Get the available meta data for a particular image resolution
        /// </summary>
        /// <param name="resolution">An image resolution, in DPI</param>
        /// <returns></returns>
        public ResolutionMetadata GetResolutionMetaData(int resolution)
            => ResolutionData.FirstOrDefault(r => r.Resolution == resolution);

        private static IEnumerable<int> GetResolutions(IEnumerable<ResolutionMetadata> resolutionData)
            => resolutionData.Select(r => r.Resolution).Distinct().ToList();

        /// <summary>
        /// Information related to an image resolution.
        /// </summary>
        public record ResolutionMetadata(int Resolution, TimeSpan ImagingTime, long FileSize)
        {
        }
    }
}
