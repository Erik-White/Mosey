using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using DNTScanner.Core;
using Moq;
using Mosey.Core.Imaging;

namespace Mosey.Application.Tests.Customizations
{
    public class SystemDevicesMockCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            var systemDevices = fixture.Create<Mock<ISystemImagingDevices<ScannerSettings>>>();
            var settings = fixture.CreateMany<ScannerSettings>();
            var properties = fixture.Create<List<IDictionary<string, object>>>();

            foreach (var deviceID in settings.Select(s => s.Id))
            {
                properties.Add(new Dictionary<string, object>() { { "Unique Device ID", deviceID } });
            }

            systemDevices
                .Setup(x => x.PerformImaging(
                    It.IsAny<ScannerSettings>(),
                    It.IsAny<ImagingDeviceConfig>(),
                    It.IsAny<IImagingDevice.ImageFormat>()))
                .Returns(fixture.CreateMany<byte[]>());

            systemDevices
                .Setup(mock => mock.GetDeviceSettings())
                .Returns(settings);

            systemDevices
                .Setup(mock => mock.GetDeviceProperties())
                .Returns(properties);

            fixture.Register(() => systemDevices);
        }
    }
}
