using AutoFixture;
using Mosey.Core;
using Mosey.Tests.AutoData;

namespace Mosey.Application.Tests.AutoData
{
    public class IntervalScanningServiceAutoDataAttribute : AutoNSubstituteDataAttribute
    {
        public IntervalScanningServiceAutoDataAttribute() : base(fixture =>
        {
            fixture.Register<IIntervalTimer>(() => new IntervalTimer());
        })
        { }
    }
}

