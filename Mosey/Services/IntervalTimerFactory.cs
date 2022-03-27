using Mosey.Models;

namespace Mosey.Core.Services
{
    public class IntervalTimerFactory : IFactory<IIntervalTimer>
    {
        public IIntervalTimer Create() => new IntervalTimer();
    }
}
