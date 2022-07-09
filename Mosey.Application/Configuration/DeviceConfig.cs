using static Mosey.Applicaton.Configuration.DeviceConfig;

namespace Mosey.Applicaton.Configuration
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

        // Parameterless constructor required for use with WriteableOptions
        public DeviceConfig()
            : this(default, default, default, default)
        {
        }

        /// <summary>
        /// Get the available meta data for a particular image resolution
        /// </summary>
        /// <param name="resolution">An image resolution, in DPI</param>
        /// <returns></returns>
        public ResolutionMetadata GetResolutionMetadata(int resolution)
            => ResolutionData.FirstOrDefault(r => r.Resolution == resolution);

        private static IEnumerable<int> GetResolutions(IEnumerable<ResolutionMetadata> resolutionData)
            => resolutionData.Select(r => r.Resolution).Distinct().ToList();

        /// <summary>
        /// Information related to an image resolution.
        /// </summary>
        public record struct ResolutionMetadata(int Resolution, TimeSpan ImagingTime, long FileSize)
        {
        }
    }
}
