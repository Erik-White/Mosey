using AutoFixture;
using Mosey.Models;
using Mosey.Services;
using MoseyTests.Customizations;

namespace MoseyTests.AutoData
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
