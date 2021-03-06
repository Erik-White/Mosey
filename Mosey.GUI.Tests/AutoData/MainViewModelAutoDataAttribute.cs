﻿using AutoFixture;
using Mosey.Models;
using Mosey.Services;

namespace Mosey.Tests.AutoData
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
