using System;
using System.Threading;
using System.Diagnostics;

namespace Mosey.Models
{
    public class IntervalTimer
    {
        public readonly TimeSpan Delay;
        public readonly TimeSpan Interval;
        public readonly int Repetitions;
        public int RepetitionsCount { get; private set; }
        public bool Paused { get; private set; }
        public event EventHandler Tick;
        public AutoResetEvent TimerComplete = new AutoResetEvent(false);
        private Stopwatch stopWatch = new Stopwatch();
        private Timer timer;
        private TimeSpan intervalRemaining;

        /// <summary>
        /// Provides a timer with callback event. Allows for pausing between intervals
        /// If delay is zero then the first callback will begin immediately
        /// </summary>
        /// <param name="delay">The delay before starting the first scan</param>
        /// <param name="interval">The time between each scan</param>
        /// <param name="repetitions">The number of scan intervals to complete</param>
        public IntervalTimer(TimeSpan delay, TimeSpan interval, int repetitions)
        {
            Delay = delay;
            Interval = interval;
            Repetitions = repetitions;
            stopWatch.Start();
            timer = new Timer(TimerInterval, null, delay, interval);
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
            timer.Dispose();
            stopWatch.Reset();
            TimerComplete.Set();
        }

        /// <summary>
        /// Pause timer callbacks
        /// </summary>
        public void Pause()
        {
            if (!Paused && stopWatch.IsRunning)
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
}
