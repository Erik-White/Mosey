using System;
using System.Threading;
using System.Diagnostics;
using Mosey.Models;

namespace Mosey.Services
{
    /// <summary>
    /// A timer with interval callback events. Allows for pausing between intervals
    /// </summary>
    public class IntervalTimer : IIntervalTimer
    {
        public DateTime StartTime { get; private set; }
        public DateTime FinishTime { get { return StartTime.Add(Repetitions * Interval) + Delay; } }
        public TimeSpan Delay { get; private set; } = TimeSpan.Zero;
        public TimeSpan Interval { get; private set; } = TimeSpan.FromSeconds(1);
        public int Repetitions { get; private set; } = -1;
        public int RepetitionsCount { get; private set; }
        public bool Enabled { get { return (timer != null); } }
        public bool Paused { get; private set; }
        public event EventHandler Tick;
        public event EventHandler Complete;
        private bool disposed;
        private Timer timer;
        private TimeSpan intervalRemaining;
        private Stopwatch stopWatch = new Stopwatch();

        public IntervalTimer()
        {
        }

        public IntervalTimer(TimeSpan delay, TimeSpan interval, int repetitions)
        {
            Delay = delay;
            Interval = interval;
            Repetitions = repetitions;
        }

        public IntervalTimer(IIntervalTimerConfig config)
        {
            Delay = config.Delay;
            Interval = config.Interval;
            Repetitions = config.Repetitions;
        }

        /// <summary>
        /// Starts a timer using the current properties
        /// </summary>
        public void Start()
        {
            Start(Delay, Interval, Repetitions);
        }

        /// <summary>
        /// Starts a timer with no repetition limit
        /// If delay is zero then the first callback will begin immediately
        /// </summary>
        /// <param name="delay">The delay before starting the first interval</param>
        /// <param name="interval">The time between each callback</param>
        public void Start(TimeSpan delay, TimeSpan interval)
        {
            Start(delay, interval, -1);
        }

        /// <summary>
        /// Start a new timer. If delay is zero then the first callback will begin immediately
        /// </summary>
        /// <param name="delay">The delay before starting the first interval</param>
        /// <param name="interval">The time between each callback</param>
        /// <param name="repetitions">The number of scan intervals to complete. Set to -1 for infinite.</param>
        public void Start(TimeSpan delay, TimeSpan interval, int repetitions)
        {
            Delay = delay;
            Interval = interval;
            Repetitions = repetitions;
            StartTime = DateTime.Now;

            if (timer != null)
            {
                Stop();
            }
            timer = new Timer(TimerInterval, null, delay, interval);
            stopWatch.Restart();
        }

        /// <summary>
        /// Raises an event when an interval has elapsed
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTick(EventArgs e)
        {
            Tick?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises an event when the timer has run to completion
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnComplete(EventArgs e)
        {
            Complete?.Invoke(this, EventArgs.Empty);
        } 

        /// <summary>
        /// Timer callback method. Continues the timer until the maximum repetition count is reached
        /// </summary>
        /// <param name="state"></param>
        private void TimerInterval(object state)
        {
            if (Repetitions == -1 | ++RepetitionsCount <= Repetitions)
            {
                // Notify event subscribers
                OnTick(EventArgs.Empty);
                Resume();
                if(RepetitionsCount == Repetitions)
                {
                    Stop();
                }
            }
            else
            {
                // Finished
                Stop();
            }
        }

        /// <summary>
        /// Cancel and reset the timer and its properties
        /// </summary>
        public void Stop()
        {
            Paused = false;
            StartTime = DateTime.MinValue;
            RepetitionsCount = 0;
            stopWatch.Reset();

            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }

            OnComplete(EventArgs.Empty);
        }

        /// <summary>
        /// Pause timer callbacks
        /// </summary>
        public void Pause()
        {
            if (timer != null && !Paused && stopWatch.IsRunning)
            {
                // Pause the stopwatch and calculate the time remaining until the next callback is due
                stopWatch.Stop();
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                if(Delay > TimeSpan.Zero && RepetitionsCount == 0)
                {
                    intervalRemaining = Delay.Subtract(stopWatch.Elapsed);
                }
                else
                {
                    intervalRemaining = Interval.Subtract(stopWatch.Elapsed);
                }
                Paused = true;
            }
        }

        /// <summary>
        /// Resume timer callback. Retains time that has already elapsed between intervals
        /// </summary>
        public void Resume()
        {
            if (timer != null)
            {
                if (intervalRemaining > TimeSpan.Zero)
                {
                    timer.Change(intervalRemaining, intervalRemaining);
                    intervalRemaining = TimeSpan.Zero;
                    stopWatch.Start();
                }
                else
                {
                    stopWatch.Restart();
                    timer.Change(Interval, Interval);
                }
                Paused = false;
            }
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (Tick != null)
                    {
                        foreach (Delegate del in Tick.GetInvocationList())
                            Tick -= (del as EventHandler);
                    }
                    if (Complete != null)
                    {
                        foreach (Delegate del in Complete.GetInvocationList())
                            Complete -= (del as EventHandler);
                    }
                    if (timer != null)
                    {
                        timer.Dispose();
                    }
                }
                disposed = true;
            }
        }

        ~IntervalTimer()
        {
            Dispose(false);
        }
    }

    public class IntervalTimerConfig : IIntervalTimerConfig
    {
        // System.Text.Json doesn't support TimeSpan [de]serialization
        // It is planned for .NET Core 5
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonTimeSpanConverter))]
        public TimeSpan Delay { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonTimeSpanConverter))]
        public TimeSpan Interval { get; set; }
        public int Repetitions { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
