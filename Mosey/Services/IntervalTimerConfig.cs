using System;
using Mosey.Models;

namespace Mosey.Services
{
    public record IntervalTimerConfig : IIntervalTimerConfig
    {
        public TimeSpan Delay { get; set; }
        public TimeSpan Interval { get; set; }
        public int Repetitions { get; set; }
    }
}
