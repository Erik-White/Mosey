namespace Mosey.Application
{
    public interface IIntervalScanningService : IScanningService
    {
        event EventHandler DevicesRefreshed;
        event EventHandler ScanRepetitionCompleted;
        event EventHandler ScanningCompleted;

        int ScanRepetitionsCount { get; }
        DateTime StartTime { get; }
        DateTime FinishTime { get; }

        void StartScanning(TimeSpan delay, TimeSpan interval, int repetitions);

        void StopScanning(bool waitForCompletion = true);
    }
}
