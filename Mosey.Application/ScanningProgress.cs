namespace Mosey.Application
{
    public record ScanningProgress(int RepetitionCount, Exception? Exception = null)
    {
        public enum ProgressResult
        {
            Success,
            Error
        };

        public ProgressResult Result
            => Exception is null
                ? ProgressResult.Success
                : ProgressResult.Error;
    };
}
