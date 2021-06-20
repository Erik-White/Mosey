using Mosey.Models;

namespace Mosey.Services
{
    public class IntervalTimerFactory : IFactory<IIntervalTimer>
    {
        public IIntervalTimer Create() => new IntervalTimer();
    }
}
