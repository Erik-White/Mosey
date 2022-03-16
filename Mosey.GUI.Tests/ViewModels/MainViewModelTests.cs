using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using FluentAssertions;
using Mosey.Models.Imaging;
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

        public class BeginRefreshDevicesAsyncShould
        {
            [Theory, MainViewModelAutoData]
            public async Task RepeatRefreshDevices([Frozen] IImagingHost scanningHost, MainViewModel sut)
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(1000);

                await sut.BeginRefreshDevicesAsync(TimeSpan.FromMilliseconds(100), cts.Token);

                scanningHost
                    .ReceivedCalls()
                    .Count(x => x.GetMethodInfo().Name == nameof(scanningHost.RefreshDevicesAsync))
                    .Should().BeInRange(5, 15);
            }

            [Theory, MainViewModelAutoData]
            public async Task CancelTask([Frozen] IImagingHost scanningHost, MainViewModel sut)
            {
                using var cts = new CancellationTokenSource();
                cts.Cancel();

                await sut.BeginRefreshDevicesAsync(TimeSpan.FromSeconds(1), cts.Token);

                await scanningHost
                    .DidNotReceiveWithAnyArgs()
                    .RefreshDevicesAsync(default, default);
            }

            [Theory, MainViewModelAutoData]
            public async Task RaisePropertyChanged(MainViewModel sut)
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(1000);

                using (var monitoredSubject = sut.Monitor())
                {
                    await sut.BeginRefreshDevicesAsync(TimeSpan.Zero, cts.Token);

                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.ScanningDevices);
                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.StartScanCommand);
                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.StartStopScanCommand);
                }
            }
        }

        public class GetImageFilePathShould
        {
            [Theory, ScanningDeviceAutoData]
            public void Return_FilePath(IImagingHost.CapturedImage image, [Frozen] ImageFileConfig config, DateTime dateTime)
            {
                var result = MainViewModel.GetImageFilePath(image, config, true, dateTime);

                result.Should().StartWith(config.Directory);
                result.Should().EndWithEquivalentOf(config.ImageFormat.ToString());
            }
        }
    }
}
