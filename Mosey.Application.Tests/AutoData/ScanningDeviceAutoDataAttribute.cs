using AutoFixture;
using Mosey.Tests.AutoData;
using Mosey.Application.Tests.Customizations;

namespace Mosey.Application.Tests.AutoData
{
    public class ScanningDeviceAutoDataAttribute : AutoNSubstituteDataAttribute
    {
        public ScanningDeviceAutoDataAttribute() : base(fixture =>
        {
            fixture.Customize(new CompositeCustomization(new ScannerSettingsCustomization()));
            fixture.Customize(new SystemDevicesMockCustomization());
        })
        { }
    }
}
