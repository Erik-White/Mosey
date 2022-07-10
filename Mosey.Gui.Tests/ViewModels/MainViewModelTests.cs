using System.Collections.Generic;
using System.Linq;
using AutoFixture.NUnit3;
using FluentAssertions;
using Mosey.Core.Imaging;
using Mosey.Gui.Tests.AutoData;
using Mosey.Tests.Extensions;
using NSubstitute;
using NUnit.Framework;
using Mosey.Application;

namespace Mosey.Gui.ViewModels.Tests
{
    public class MainViewModelTests
    {
        public class ConstructorShould
        {
            [Theory, MainViewModelAutoData]
            public void InitializeProperties(MainViewModel sut) => sut.AssertAllPropertiesAreNotDefault();

            [Theory, MainViewModelAutoData]
            public void InitializeScanningDevicesCollection([Frozen] IEnumerable<IImagingDevice> imagingDevices, MainViewModel sut)
            {
                sut
                    .ScanningDevices.Devices.Select(d => d.DeviceID)
                    .Should().BeEquivalentTo(imagingDevices.Select(d => d.DeviceID));
            }
        }

        public class StartScanShould
        {
            [Theory, MainViewModelAutoData]
            public void RaisePropertyChanged(MainViewModel sut)
            {
                using (var monitoredSubject = sut.Monitor())
                {
                    monitoredSubject.Subject.StartScanning();

                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.IsScanRunning);
                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.ScanFinishTime);
                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.StartStopScanCommand);
                }
            }
        }

        public class ScanningServiceDevicesRefreshedShould
        {
            [Theory, MainViewModelAutoData]
            public void RaisePropertyChanged([Frozen] IIntervalScanningService scanningService, MainViewModel sut)
            {
                using (var monitoredSubject = sut.Monitor())
                {
                    scanningService.DevicesRefreshed += Raise.Event();

                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.ScanningDevices);
                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.StartScanCommand);
                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.StartStopScanCommand);
                }
            }
        }
    }
}
