using AutoFixture;
using Mosey.Tests.Customizations;

namespace Mosey.Tests.AutoData
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
