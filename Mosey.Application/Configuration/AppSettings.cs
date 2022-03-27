using Mosey.Applicaton.Configuration;
using Mosey.Core;
using Mosey.Core.Imaging;

namespace Mosey.Application.Configuration
{
    public record class AppSettings
    {
        internal const string DefaultSettingsFileName = "appsettings.json";
        internal const string UserSettingsFileName = "usersettings.json";
        // TODO: Revert to internal when possible
        public const string UserSettingsKey = "UserSettings";

        public ImageFileConfig ImageFile { get; set; }
        public IntervalTimerConfig ScanTimer { get; set; }
        public IntervalTimerConfig UITimer { get; set; }
        public ImagingDeviceConfig Image { get; set; }
        public DeviceConfig Device { get; set; }
    }
}
