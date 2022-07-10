using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            public void SetScanningProperties(MainViewModel sut)
            {
                sut.StartScanning();

                sut.IsScanRunning.Should().BeTrue();
                sut.ScanRepetitionsCount.Should().Be(0);
                sut.ScanNextTime.Should().Be(TimeSpan.Zero);
            }

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
            //[Theory, MainViewModelAutoData]
            //public async Task RepeatRefreshDevices([Frozen] IImagingHost scanningHost, MainViewModel sut)
            //{
            //    using var cts = new CancellationTokenSource();
            //    cts.CancelAfter(1000);

            //    await sut.BeginRefreshDevicesAsync(TimeSpan.FromMilliseconds(100), cts.Token);

            //    scanningHost
            //        .ReceivedCalls()
            //        .Count(x => x.GetMethodInfo().Name == nameof(scanningHost.RefreshDevicesAsync))
            //        .Should().BeInRange(5, 15);
            //}

            //[Theory, MainViewModelAutoData]
            //public async Task CancelTask([Frozen] IImagingHost scanningHost, MainViewModel sut)
            //{
            //    using var cts = new CancellationTokenSource();
            //    cts.Cancel();

            //    await sut.BeginRefreshDevicesAsync(TimeSpan.FromSeconds(1), cts.Token);

            //    await scanningHost
            //        .DidNotReceiveWithAnyArgs()
            //        .RefreshDevicesAsync(default, default);
            //}

            //[Theory, MainViewModelAutoData]
            //public async Task RaisePropertyChanged(MainViewModel sut)
            //{
            //    using var cts = new CancellationTokenSource();
            //    cts.CancelAfter(1000);

            //    using (var monitoredSubject = sut.Monitor())
            //    {
            //        await sut.BeginRefreshDevicesAsync(TimeSpan.Zero, cts.Token);

            //        monitoredSubject.Should().RaisePropertyChangeFor(x => x.ScanningDevices);
            //        monitoredSubject.Should().RaisePropertyChangeFor(x => x.StartScanCommand);
            //        monitoredSubject.Should().RaisePropertyChangeFor(x => x.StartStopScanCommand);
            //    }
            //}

            [Theory, MainViewModelAutoData]
            public async Task RaisePropertyChanged([Frozen] IScanningService scanningService, MainViewModel sut)
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
