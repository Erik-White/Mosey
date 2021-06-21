namespace Mosey.Models
{
    public interface IFactory<T>
    {
        public T Create();
    }
}
