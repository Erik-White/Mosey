using System.Diagnostics;
using Mosey.Core;

namespace Mosey.Application
{
    /// <summary>
    /// A timer with interval callback events. Allows for pausing between intervals
    /// </summary>
    public sealed class IntervalTimer : IIntervalTimer
    {
        public DateTime StartTime { get; private set; }
        public DateTime FinishTime => StartTime.Add(Repetitions * Interval) + Delay;
        public TimeSpan Delay { get; private set; } = TimeSpan.Zero;
        public TimeSpan Interval { get; private set; } = TimeSpan.FromSeconds(1);
        public int Repetitions { get; private set; } = -1;
        public int RepetitionsCount { get; private set; }
        public bool Enabled => timer is not null;
        public bool Paused { get; private set; }

        /// <summary>
        /// Raised on every interval. Returns the current repetition number.
        /// </summary>
        public event EventHandler<int>? Tick;

        /// <summary>
        /// Raised when the timer is stopped. Returns the total number of repetitions completed.
        /// </summary>
        public event EventHandler<int>? Complete;

        private Timer? timer;
        private TimeSpan intervalRemaining;
        private readonly Stopwatch stopWatch = new();

        public IntervalTimer()
        {
        }

        public IntervalTimer(IntervalTimerConfig config)
            : this(config.Delay, config.Interval, config.Repetitions)
        {
        }

        public IntervalTimer(TimeSpan delay, TimeSpan interval, int repetitions)
        {
            Delay = delay;
            Interval = interval;
            Repetitions = repetitions;
        }

        /// <summary>
        /// Starts a timer using the current properties
        /// </summary>
        public void Start()
            => Start(Delay, Interval, Repetitions);

        /// <summary>
        /// Starts a timer with no repetition limit
        /// If delay is zero then the first callback will begin immediately
        /// </summary>
        /// <param name="delay">The delay before starting the first interval</param>
        /// <param name="interval">The time between each callback</param>
        public void Start(TimeSpan delay, TimeSpan interval)
            => Start(delay, interval, -1);

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

            if (timer is not null)
            {
                Stop();
            }

            timer = new Timer(TimerInterval, null, delay, interval);
            stopWatch.Restart();
        }

        /// <summary>
        /// Timer callback method. Continues the timer until the maximum repetition count is reached
        /// </summary>
        /// <param name="state"></param>
        private void TimerInterval(object? state)
        {
            if (++RepetitionsCount <= Repetitions || Repetitions == -1)
            {
                // Notify that a repetition was completed
                Tick?.Invoke(this, RepetitionsCount);
                Resume();
            }

            if (RepetitionsCount == Repetitions)
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
            var completedRepetitions = RepetitionsCount;

            Paused = false;
            StartTime = DateTime.MinValue;
            RepetitionsCount = 0;
            stopWatch.Reset();

            if (timer is not null)
            {
                timer.Dispose();
                timer = null;
            }

            Complete?.Invoke(this, completedRepetitions);
        }

        /// <summary>
        /// Pause timer callbacks
        /// </summary>
        public void Pause()
        {
            if (timer is not null && !Paused && stopWatch.IsRunning)
            {
                // Pause the stopwatch and calculate the time remaining until the next callback is due
                stopWatch.Stop();
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                if (Delay > TimeSpan.Zero && RepetitionsCount == 0)
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
            if (timer is null)
            {
                return;
            }

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

        public void Dispose()
        {
            var tickInvocations = Tick?.GetInvocationList().Select(i => i as EventHandler<int>);
            foreach (var del in tickInvocations ?? Enumerable.Empty<EventHandler<int>>())
            {
                Tick -= del;
            }
            var completeInvocations = Tick?.GetInvocationList().Select(i => i as EventHandler<int>);
            foreach (var del in completeInvocations ?? Enumerable.Empty<EventHandler<int>>())
            {
                Complete -= del;
            }

            timer?.Dispose();
        }
    }
}
