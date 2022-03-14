using AutoFixture;
using Mosey.Tests.Customizations;

namespace Mosey.Tests.AutoData
{
    public class ScanningDeviceAutoDataAttribute : AutoNSubstituteDataAttribute
    {
        public ScanningDeviceAutoDataAttribute() : base(fixture =>
        {
            fixture.Customize(new CompositeCustomization(new ScannerSettingsCustomization()));
            // A concrete class is required when retrieving devices from ISystemDevices
            fixture.Customize(new ConcreteScanningDeviceCustomization());
            fixture.Customize(new SystemDevicesMockCustomization());
        })
        { }
    }
}
