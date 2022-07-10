using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using FluentAssertions;
using Mosey.Application.Tests.AutoData;
using Mosey.Core.Imaging;
using Mosey.Tests.AutoData;
using Mosey.Tests.Extensions;
using NSubstitute;
using NUnit.Framework;

namespace Mosey.Application.Tests
{
    public class IntervalScanningServiceTests
    {
        public class ConstructorShould
        {
            [Theory, AutoNSubstituteData]
            public void InitializeProperties(IntervalScanningService sut)
            {
                sut.AssertAllPropertiesAreNotDefault();
            }

            [Theory, AutoNSubstituteData]
            public void InitializeScanningDevicesCollection([Frozen] IEnumerable<IImagingDevice> imagingDevices, IntervalScanningService sut)
            {
                sut
                    .Scanners.Devices.Select(d => d.DeviceID)
                    .Should().BeEquivalentTo(imagingDevices.Select(d => d.DeviceID));
            }
        }

        public class BeginRefreshDevicesAsyncShould
        {
            [Theory, AutoNSubstituteData]
            public async Task RepeatRefreshDevices([Frozen] IImagingHost scanningHost, IntervalScanningService sut)
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(1000);

                await sut.BeginRefreshDevices(TimeSpan.FromMilliseconds(100), cts.Token);

                scanningHost
                    .ReceivedCalls()
                    .Count(x => x.GetMethodInfo().Name == nameof(scanningHost.RefreshDevicesAsync))
                    .Should().BeInRange(5, 15);
            }

            [Theory, AutoNSubstituteData]
            public async Task CancelTask([Frozen] IImagingHost scanningHost, IntervalScanningService sut)
            {
                using var cts = new CancellationTokenSource();
                cts.Cancel();

                await sut.BeginRefreshDevices(TimeSpan.FromSeconds(1), cts.Token);

                await scanningHost
                    .DidNotReceiveWithAnyArgs()
                    .RefreshDevicesAsync(default, default);
            }

            [Theory, AutoNSubstituteData]
            public async Task Raise_DevicesRefreshedEvent(IntervalScanningService sut)
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(1000);

                using (var monitoredSubject = sut.Monitor())
                {
                    await sut.BeginRefreshDevices(TimeSpan.Zero, cts.Token);

                    monitoredSubject.Should().Raise(nameof(IntervalScanningService.DevicesRefreshed));
                }
            }
        }

        public class GetImageFilePathShould
        {
            [Theory, AutoData]
            public void Return_FilePath(IImagingHost.CapturedImage image, [Frozen] ImageFileConfig config, DateTime dateTime)
            {
                var result = IntervalScanningService.GetImageFilePath(image, config, true, dateTime);

                result.Should().StartWith(config.Directory);
                result.Should().EndWithEquivalentOf(config.ImageFormat.ToString());
            }
        }

        public class StartScanningShould
        {
            [Theory, IntervalScanningServiceAutoData]
            public void SetScanningProperties(IntervalScanningService sut)
            {
                sut.StartScanning(TimeSpan.FromDays(1), TimeSpan.Zero, 1);

                sut.IsScanRunning.Should().BeTrue();
                sut.ScanRepetitionsCount.Should().Be(0);
            }

            [Theory, IntervalScanningServiceAutoData]
            public async Task Raise_ScanRepetitionCompletedEvent([Greedy] IntervalScanningService sut)
            {
                using (var monitoredSubject = sut.Monitor())
                {
                    sut.StartScanning(TimeSpan.Zero, TimeSpan.Zero, 1);

                    await Task.Delay(1000);

                    monitoredSubject.Should().Raise(nameof(IntervalScanningService.ScanRepetitionCompleted));
                }
            }
        }

        public class StopScanningShould
        {
            [Theory, IntervalScanningServiceAutoData]
            public void Raise_ScanningCompletedEvent(IntervalScanningService sut)
            {

                using (var monitoredSubject = sut.Monitor())
                {
                    sut.StopScanning();

                    monitoredSubject.Should().Raise(nameof(IntervalScanningService.ScanningCompleted));
                }
            }
        }
    }
}
