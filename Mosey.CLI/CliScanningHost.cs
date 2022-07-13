using Microsoft.Extensions.Logging;
using Mosey.Application;

namespace Mosey.Cli
{
    internal class CliScanningHost
    {
        private readonly ILogger<CliScanningHost> _log;
        private readonly IIntervalScanningService _scanningService;

        public CliScanningHost(IIntervalScanningService scanningService, ILogger<CliScanningHost> log)
        {
            _log = log;
            _scanningService = scanningService;
        }

        public void StartScanning(ScanningArgs args)
        {
            _log.LogInformation("Scanning started with {ScanRepetitions} repetitions to complete.", args.Repetitions);

            _scanningService.StopScanning();
            _scanningService.StartScanning(TimeSpan.Zero, TimeSpan.FromMinutes(args.Interval), args.Repetitions);

            _scanningService.ScanRepetitionCompleted += (_, eventArgs) => ScanRepetition_Completed(eventArgs);
            _scanningService.ScanningCompleted += (_, _) =>
            {
                _log.LogInformation("Scanning complete.");
            };
        }

        private void ScanRepetition_Completed(EventArgs eventArgs)
        {
            if (eventArgs is ExceptionEventArgs exceptionEventArgs)
            {
                // An unhandled error occurred, notify the user and cancel scanning
                _log.LogError(exceptionEventArgs.Exception, exceptionEventArgs.Exception.Message);
                _scanningService.StopScanning(false);
            }
        }

        internal record struct ScanningArgs(int Interval, int Repetitions);
    }
}
