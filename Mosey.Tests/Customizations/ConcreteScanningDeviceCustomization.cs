﻿using AutoFixture;
using Mosey.Models.Imaging;
using Mosey.Services.Imaging;

namespace Mosey.Tests.Customizations
{
    public class ConcreteScanningDeviceCustomization : ICustomization
    {
        public void Customize(IFixture fixture) => fixture.Register<IImagingDevice>(fixture.Create<ScanningDevice>);
    }
}
