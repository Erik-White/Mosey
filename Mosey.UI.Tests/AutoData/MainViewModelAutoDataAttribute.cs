using AutoFixture;
using Mosey.Application;
using Mosey.Core;
using Mosey.Tests.AutoData;

namespace Mosey.UI.Tests.AutoData
{
    public class MainViewModelAutoDataAttribute : AutoNSubstituteDataAttribute
    {
        public MainViewModelAutoDataAttribute() : base(fixture =>
        {
            fixture.Register<IFactory<IIntervalTimer>>(fixture.Create<IntervalTimerFactory>);
        })
        { }
    }
}
