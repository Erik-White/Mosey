using System;
using Mosey.Models;

namespace Mosey.Services
{
    public record IntervalTimerConfig : IIntervalTimerConfig
    {
        // System.Text.Json doesn't support TimeSpan [de]serialization
        // It is planned for .NET 6
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonTimeSpanConverter))]
        public TimeSpan Delay { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonTimeSpanConverter))]
        public TimeSpan Interval { get; set; }
        public int Repetitions { get; set; }
    }
}
