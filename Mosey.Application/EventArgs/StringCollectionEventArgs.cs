namespace Mosey.Application
{
    public class StringCollectionEventArgs : EventArgs
    {
        public IEnumerable<string> Values { get; init; }

        public StringCollectionEventArgs(IEnumerable<string> values)
        {
            Values = values;
        }
    }
}
