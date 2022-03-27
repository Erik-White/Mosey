using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using DNTScanner.Core;
using Mosey.Core.Imaging;
using Mosey.Application.Imaging.Extensions;

namespace Mosey.Tests.Customizations
{
    public class ScannerSettingsCustomization : ICustomization
    {
        public static IImagingDevice.ImageFormat SupportedImageFormat { get; set; } = IImagingDevice.ImageFormat.Jpeg;

        public void Customize(IFixture fixture)
        {
            fixture.Register(() =>
            {
                var scannerSettings = GetInstance(fixture);
                // Ensure that there is at least one real SupportedTransferFormat
                scannerSettings
                    .SupportedTransferFormats
                    .Add(SupportedImageFormat.ToWIAImageFormat().Value, "SupportedWIAImageFormat");

                return scannerSettings;
            });
        }

        /// <summary>
        /// Creates a new instance manually to avoid recursion problems
        /// </summary>
        internal static ScannerSettings GetInstance(IFixture fixture)
            => new()
            {
                Id = fixture.Create<string>(),
                Name = fixture.Create<string>(),
                IsAutomaticDocumentFeeder = fixture.Create<bool>(),
                IsDuplex = fixture.Create<bool>(),
                IsFlatbed = fixture.Create<bool>(),
                SupportedResolutions = fixture.CreateMany<int>().ToList(),
                SupportedTransferFormats = fixture.Create<IDictionary<string, string>>(),
                SupportedEvents = fixture.Create<IDictionary<string, string>>(),
                ScannerDeviceSettings = fixture.Create<IDictionary<string, object>>(),
                ScannerPictureSettings = fixture.Create<IDictionary<string, object>>()
            };
    }
}
