using Mosey.Applicaton.Configuration;
using Mosey.Core.Imaging;

namespace Mosey.Application
{
    public interface IScanningService
    {
        event EventHandler DevicesRefreshed;
        event EventHandler ScanRepetitionCompleted;
        event EventHandler ScanningCompleted;

        IImagingDevices<IImagingDevice> Scanners { get; }
        bool IsScanRunning { get; }
        int ScanRepetitionsCount { get; }
        DateTime StartTime { get; }
        DateTime FinishTime { get; }

        void StartScanning(TimeSpan delay, TimeSpan interval, int repetitions);

        void StopScanning(bool waitForCompletion = true);

        void UpdateConfig(ScanningConfig config);

        public record ScanningConfig(DeviceConfig DeviceConfig, ImagingDeviceConfig ImagingDeviceConfig, ImageFileConfig ImageFileConfig);
    }
}
