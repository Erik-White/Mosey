using Mosey.Applicaton.Configuration;
using Mosey.Core.Imaging;

namespace Mosey.Application
{
    public interface IScanningService
    {
        IImagingDevices<IImagingDevice> Scanners { get; }
        bool IsScanRunning { get; }

        long GetRequiredDiskSpace(int repetitions);
        TimeSpan GetRequiredScanningTime();
        Task<IEnumerable<string>> ScanAndSaveImages();

        void UpdateConfig(ScanningConfig config);

        public record ScanningConfig(DeviceConfig DeviceConfig, ImagingDeviceConfig ImagingDeviceConfig, ImageFileConfig ImageFileConfig);
    }
}