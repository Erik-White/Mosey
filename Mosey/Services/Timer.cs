using System;
using System.Collections.Generic;
using System.Text;
using Mosey.Models;

namespace Mosey.Services
{
    public class TimerConfig : ITimerConfig
    {
        public TimeSpan Delay { get; set; }
        public TimeSpan Interval { get; set; }
    }
}
