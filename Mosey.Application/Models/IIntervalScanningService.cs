using Mosey.Core;

namespace Mosey.Application
{
    public interface IIntervalScanningService : IScanningService
    {
        event EventHandler DevicesRefreshed;

        DateTime StartTime { get; }
        DateTime FinishTime { get; }

        Task StartScanning(IntervalTimerConfig config, IProgress<ScanningProgress>? progress = null);

        void StopScanning(bool waitForCompletion = true);
    }
}
