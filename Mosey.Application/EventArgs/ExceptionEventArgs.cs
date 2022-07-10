namespace Mosey.Application
{
    public class ExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; init; }

        public ExceptionEventArgs(Exception ex)
        {
            Exception = ex;
        }
    }
}
