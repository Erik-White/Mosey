using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit3;

namespace Mosey.Tests.AutoData
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

            fixture.Register<IFileSystem>(() => new MockFileSystem());

            initialize(fixture);

            return fixture;
        })
        { }

        public AutoNSubstituteDataAttribute() : this(fixture => { }) { }
    }
}
