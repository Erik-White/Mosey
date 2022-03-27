using Mosey.Core;

namespace Mosey.Application
{
    public class IntervalTimerFactory : IFactory<IIntervalTimer>
    {
        public IIntervalTimer Create() => new IntervalTimer();
    }
}
