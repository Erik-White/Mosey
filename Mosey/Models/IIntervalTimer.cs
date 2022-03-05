using System;

namespace Mosey.Models
{
    public interface IIntervalTimer : ITimer
    {
        DateTime StartTime { get; }
        DateTime FinishTime { get; }
        TimeSpan Delay { get; }
        TimeSpan Interval { get; }
        int Repetitions { get; }
        int RepetitionsCount { get; }
        bool Enabled { get; }
        event EventHandler Tick;
        event EventHandler Complete;
        void Start(TimeSpan delay, TimeSpan interval);
        void Start(TimeSpan delay, TimeSpan interval, int repetitions);
    }

    public interface IIntervalTimerConfig : ITimerConfig
    {
        int Repetitions { get; set; }
    }
}
