using AutoFixture;
using Mosey.Models;
using Mosey.Services.Imaging;

namespace MoseyTests.Customizations
{
    public class ConcreteScanningDeviceCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Register<IImagingDevice>(fixture.Create<ScanningDevice>);
        }
    }
}
