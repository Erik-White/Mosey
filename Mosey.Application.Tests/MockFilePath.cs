using System.IO.Abstractions;

namespace Mosey.Tests
{
    public class MockFilePath
    {
        public string Path { get; }

        public MockFilePath(IFileSystem fileSystem, string fileName)
        {
            Path = fileSystem.Path.Combine("C:", "temp", fileName);
        }
    }
}
