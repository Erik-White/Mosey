using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Mosey.GUI.Models;

namespace Mosey.GUI.Configuration
{
    public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly IOptionsSnapshot<T> _options;
        private readonly string _file;

        internal JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
        };

        public WritableOptions(
            IOptionsSnapshot<T> options,
            string file)
        {
            _options = options;
            _file = file;
        }

        public T Value => _options.Value;
        public T Get(string name) => _options.Get(name);

        public void Update(Action<T> applyChanges)
        {
            var physicalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _file);
            var content = File.ReadAllText(physicalPath);

            var optionsInstance = JsonSerializer.Deserialize<T>(content, SerializerOptions) ?? new T();

            applyChanges(optionsInstance);

            File.WriteAllText(physicalPath, JsonSerializer.Serialize(optionsInstance, SerializerOptions));
        }
    }
}
