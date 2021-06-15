using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit3;

namespace MoseyTests.AutoData
{
    public class AutoNSubstituteDataAttribute : AutoDataAttribute
    {
        public AutoNSubstituteDataAttribute(Action<IFixture> initialize) : base(() =>
        {
            var fixture = new Fixture();

            fixture.Customize(new AutoNSubstituteCustomization()
            {
                ConfigureMembers = true,
                GenerateDelegates = true
            });

            initialize(fixture);

            return fixture;
        })
        { }

        public AutoNSubstituteDataAttribute() : this(Fixture => { }) { }
    }
}
