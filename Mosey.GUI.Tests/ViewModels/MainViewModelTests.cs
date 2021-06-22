using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using FluentAssertions;
using Mosey.Models.Imaging;
using Mosey.Services.Imaging;
using Mosey.Tests.AutoData;
using Mosey.Tests.Extensions;
using NSubstitute;
using NUnit.Framework;

namespace Mosey.GUI.ViewModels.Tests
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
            public void SetScanningProperties(MainViewModel sut)
            {
                sut.StartScan();

                sut.IsScanRunning.Should().BeTrue();
                sut.ScanRepetitionsCount.Should().Be(0);
                sut.ScanNextTime.Should().Be(TimeSpan.Zero);
            }

            [Theory, MainViewModelAutoData]
            public void RaisePropertyChanged(MainViewModel sut)
            {
                using (var monitoredSubject = sut.Monitor())
                {
                    monitoredSubject.Subject.StartScan();

                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.IsScanRunning);
                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.ScanFinishTime);
                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.StartStopScanCommand);
                }
            }
        }

        public class RefreshDevicesAsyncShould
        {
            [Theory, MainViewModelAutoData]
            public async Task RepeatRefreshDevices([Frozen] IImagingDevices<IImagingDevice> imagingDevices, MainViewModel sut)
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(1000);

                await sut.RefreshDevicesAsync(0, cts.Token);

                imagingDevices
                    .ReceivedCalls().Where(x => x.GetMethodInfo().Name == nameof(imagingDevices.RefreshDevices))
                    .Count().Should().BeGreaterThan(1);
            }

            [Theory, MainViewModelAutoData]
            public async Task CancelTask([Frozen] IImagingDevices<IImagingDevice> imagingDevices, MainViewModel sut)
            {
                using var cts = new CancellationTokenSource();
                cts.Cancel();

                await sut.RefreshDevicesAsync(1, cts.Token);

                imagingDevices
                    .DidNotReceiveWithAnyArgs().RefreshDevices(null, true);
            }

            [Theory, MainViewModelAutoData]
            public async Task RaisePropertyChanged(MainViewModel sut)
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(1000);

                using (var monitoredSubject = sut.Monitor())
                {
                    await sut.RefreshDevicesAsync(0, cts.Token);

                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.ScanningDevices);
                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.StartScanCommand);
                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.StartStopScanCommand);
                }
            }
        }
    }
}
