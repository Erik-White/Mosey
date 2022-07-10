using Mosey.Applicaton.Configuration;
using Mosey.Core;
using Mosey.Core.Imaging;

namespace Mosey.Application.Configuration
{
    public record class AppSettings
    {
        internal const string DefaultSettingsFileName = "appsettings.json";
        internal const string UserSettingsFileName = "usersettings.json";

        public const string UserSettingsKey = "UserSettings";

        public ImageFileConfig ImageFile { get; set; } = new();
        public IntervalTimerConfig ScanTimer { get; set; } = new();
        public IntervalTimerConfig UITimer { get; set; } = new();
        public ImagingDeviceConfig Image { get; set; } = new();
        public DeviceConfig Device { get; set; } = new();
    }
}
