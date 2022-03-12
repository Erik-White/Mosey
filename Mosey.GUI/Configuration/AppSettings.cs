using Mosey.Models;
using Mosey.Models.Imaging;

namespace Mosey.GUI.Configuration
{
    public record class AppSettings
    {
        internal const string DefaultSettingsFileName = "appsettings.json";
        internal const string UserSettingsFileName = "usersettings.json";

        public ImageFileConfig ImageFile { get; set; }
        public IntervalTimerConfig ScanTimer { get; set; }
        public IntervalTimerConfig UITimer { get; set; }
        public ImagingDeviceConfig Image { get; set; }
        public DeviceConfig Device { get; set; }
    }
}
