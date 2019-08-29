using System;

namespace Mosey.Models
{
    public interface IIntervalTimer
    {
        public TimeSpan Delay { get; }
        public TimeSpan Interval { get; }
        public int Repetitions { get; }
        public int RepetitionsCount { get;}
        public bool Enabled { get; }
        public bool Paused { get; }
        public event EventHandler Tick;
        public event EventHandler Complete;

        public void Start(TimeSpan delay, TimeSpan interval, int repetitions) { }
        public void Stop() { }
        public void Pause() { }
        public void Resume() { }
    }
}
