using System;
using System.Threading;
using System.Diagnostics;
using Mosey.Models;

namespace Mosey.Services
{
    public class IntervalTimer : IIntervalTimer
    {
        public TimeSpan Delay { get; private set; }
        public TimeSpan Interval { get; private set; }
        public int Repetitions { get; private set; }
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
            Start(delay, interval, repetitions);
        }

        /// <summary>
        /// Starts a timer with callback event. Allows for pausing between intervals
        /// If delay is zero then the first callback will begin immediately
        /// </summary>
        /// <param name="delay">The delay before starting the first scan</param>
        /// <param name="interval">The time between each scan</param>
        /// <param name="repetitions">The number of scan intervals to complete</param>
        public void Start(TimeSpan delay, TimeSpan interval, int repetitions)
        {
            Delay = delay;
            Interval = interval;
            Repetitions = repetitions;
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
        /// Timer callback method. Continues the timer until the maximum repetitions is reached
        /// </summary>
        /// <param name="state"></param>
        private void TimerInterval(object state)
        {
            if (++RepetitionsCount <= Repetitions)
            {
                // Notify event subscribers that a scan is taking place
                OnTick(EventArgs.Empty);
                Resume();
            }
            else
            {
                // Finished
                Stop();
            }
        }

        /// <summary>
        /// Cancel and reset all timers
        /// </summary>
        public void Stop()
        {
            RepetitionsCount = 0;
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
            stopWatch.Reset();
            Paused = false;
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
}
