using Mosey.Services;
using Mosey.Services.Imaging;

namespace Mosey.GUI.Configuration
{
    public record class AppSettings
    {
        public ImageFileConfig ImageFile { get; set; }
        public IntervalTimerConfig ScanTimer { get; set; }
        public IntervalTimerConfig UITimer { get; set; }
        public ScanningDeviceSettings Image { get; set; }
        public DeviceConfig Device { get; set; }
    }
}
