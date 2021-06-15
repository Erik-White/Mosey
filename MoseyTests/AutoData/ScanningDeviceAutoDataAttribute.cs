using MoseyTests.Customizations;

namespace MoseyTests.AutoData
{
    public class ScanningDeviceAutoDataAttribute : AutoNSubstituteDataAttribute
    {
        public ScanningDeviceAutoDataAttribute() : base(fixture =>
        {
            fixture.Customize(new ScannerSettingsCustomization());
            // A concrete class is required when retrieving devices from ISystemDevices
            fixture.Customize(new ConcreteScanningDeviceCustomization());
        })
        { }
    }
}
