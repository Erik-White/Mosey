using Mosey.Core;
using Mosey.Core.Imaging;

namespace Mosey.UI.Configuration
{
    public record class AppSettings
    {
        internal const string DefaultSettingsFileName = "appsettings.json";
        internal const string UserSettingsFileName = "usersettings.json";
        internal const string UserSettingsKey = "UserSettings";

        public ImageFileConfig ImageFile { get; set; }
        public IntervalTimerConfig ScanTimer { get; set; }
        public IntervalTimerConfig UITimer { get; set; }
        public ImagingDeviceConfig Image { get; set; }
        public DeviceConfig Device { get; set; }
    }
}
