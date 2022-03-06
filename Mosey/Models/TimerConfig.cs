using System;

namespace Mosey.Models
{
    public record TimerConfig(TimeSpan Delay)
    {
    }

    public record IntervalTimerConfig(TimeSpan Delay, TimeSpan Interval, int Repetitions) : TimerConfig(Delay)
    {
        // Parameterless constructor required for use with WriteableOptions
        public IntervalTimerConfig()
            : this(TimeSpan.Zero, TimeSpan.Zero, 0)
        {
        }
    }
}
