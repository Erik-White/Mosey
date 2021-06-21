using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using DNTScanner.Core;
using Moq;
using Mosey.Models.Imaging;
using Mosey.Services.Imaging;

namespace Mosey.Tests.Customizations
{
    public class SystemDevicesMockCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            var systemDevices = fixture.Create<Mock<ISystemDevices>>();
            var settings = fixture.CreateMany<ScannerSettings>();
            var properties = fixture.Create<List<IDictionary<string, object>>>();

            foreach (var deviceID in settings.Select(s => s.Id))
            {
                properties.Add(new Dictionary<string, object>() { { "Unique Device ID", deviceID } });
            }

            systemDevices
                .Setup(x => x.PerformScan(
                    It.IsAny<ScannerSettings>(),
                    It.IsAny<IImagingDeviceConfig>(),
                    It.IsAny<ScanningDevice.ImageFormat>()))
                .Returns(fixture.CreateMany<byte[]>());

            systemDevices
                .Setup(mock => mock.GetScannerSettings())
                .Returns(settings);

            systemDevices
                .Setup(mock => mock.GetScannerProperties())
                .Returns(properties);

            fixture.Register(() => systemDevices);
        }
    }
}
