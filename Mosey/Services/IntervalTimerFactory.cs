using Mosey.Models;

namespace Mosey.Services
{
    internal class IntervalTimerFactory : IFactory<IIntervalTimer>
    {
        public IIntervalTimer Create()
        {
            return new IntervalTimer();
        }
    }
}
