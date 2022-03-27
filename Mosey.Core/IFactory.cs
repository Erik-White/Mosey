namespace Mosey.Core
{
    public interface IFactory<T>
    {
        public T Create();
    }
}
