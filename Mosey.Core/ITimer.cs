namespace Mosey.Core
{
    public interface ITimer : IDisposable
    {
        bool Paused { get; }
        void Start();
        void Stop();
        void Pause();
        void Resume();
    }
}
