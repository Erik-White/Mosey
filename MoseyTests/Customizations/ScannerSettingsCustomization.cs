﻿using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using DNTScanner.Core;
using Mosey.Services.Imaging;
using Mosey.Services.Imaging.Extensions;

namespace MoseyTests.Customizations
{
    public class ScannerSettingsCustomization : ICustomization
    {
        public static ScanningDevice.ImageFormat SupportedImageFormat { get; set; } = ScanningDevice.ImageFormat.Jpeg;

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
            => new ScannerSettings
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
