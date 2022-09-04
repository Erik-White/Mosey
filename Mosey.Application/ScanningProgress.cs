using static Mosey.Application.ScanningProgress;

namespace Mosey.Application
{
    public record ScanningProgress(int RepetitionCount, ScanningStage Stage, Exception? Exception = null)
    {
        public enum ProgressResult
        {
            Success,
            Error
        };

        public enum ScanningStage
        {
            Start,
            InProgress,
            Finish
        };

        public ProgressResult Result
            => Exception is null
                ? ProgressResult.Success
                : ProgressResult.Error;
    };
}
