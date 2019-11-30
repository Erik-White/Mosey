using System;

namespace Mosey.Models
{
    public interface IIntervalTimer : IDisposable, ICloneable
    {
        DateTime StartTime { get; }
        DateTime FinishTime { get; }
        TimeSpan Delay { get; }
        TimeSpan Interval { get; }
        int Repetitions { get; }
        int RepetitionsCount { get;}
        bool Enabled { get; }
        bool Paused { get; }
        event EventHandler Tick;
        event EventHandler Complete;
        void Start(TimeSpan delay, TimeSpan interval, int repetitions);
        void Stop();
        void Pause();
        void Resume();
    }
}
