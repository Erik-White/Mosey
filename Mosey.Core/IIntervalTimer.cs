namespace Mosey.Core
{
    public interface IIntervalTimer : ITimer
    {
        event EventHandler<int> Tick;
        event EventHandler<int> Complete;

        DateTime StartTime { get; }
        DateTime FinishTime { get; }
        TimeSpan Delay { get; }
        TimeSpan Interval { get; }
        int Repetitions { get; }
        int RepetitionsCount { get; }
        bool Enabled { get; }

        void Start(TimeSpan delay, TimeSpan interval);
        void Start(TimeSpan delay, TimeSpan interval, int repetitions);
    }
}
