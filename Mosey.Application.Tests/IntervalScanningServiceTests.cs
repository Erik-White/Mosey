using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using FluentAssertions;
using Mosey.Application.Tests.AutoData;
using Mosey.Core;
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
                _ = sut.StartScanning(new IntervalTimerConfig(TimeSpan.FromDays(1), TimeSpan.Zero, 1));

                sut.IsScanRunning.Should().BeTrue();
                sut.StartTime.Should().BeBefore(sut.FinishTime);
                sut.FinishTime.Should().BeAfter(sut.StartTime);
            }

            [Theory, IntervalScanningServiceAutoData]
            public async Task Report_IterationCount(int iterations, [Greedy] IntervalScanningService sut)
            {
                var iterationCount = 0;
                var progress = new Progress<ScanningProgress>((report) =>
                {
                    iterationCount = report.RepetitionCount;
                });

                await sut.StartScanning(new IntervalTimerConfig(TimeSpan.Zero, TimeSpan.Zero, iterations), progress);
                
                iterationCount.Should().Be(iterations);
            }
        }
    }
}
