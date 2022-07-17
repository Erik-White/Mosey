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
using Mosey.Core;
using System;
using FluentAssertions.Execution;
using System.Threading.Tasks;

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
            public async Task RaisePropertyChanged([Frozen] IntervalTimerConfig timerConfig, MainViewModel sut)
            {
#pragma warning disable S1854 // Unused assignments should be removed
                timerConfig = new IntervalTimerConfig(TimeSpan.Zero, TimeSpan.Zero, 1);
#pragma warning restore S1854 // Unused assignments should be removed

                using (var monitoredSubject = sut.Monitor())
                using (new AssertionScope())
                {
                    await monitoredSubject.Subject.StartScanning();
                    await Task.Delay(500);

                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.IsScanRunning);
                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.ScanFinishTime);
                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.ScanRepetitionsCount);
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
                }
            }
        }
    }
}
