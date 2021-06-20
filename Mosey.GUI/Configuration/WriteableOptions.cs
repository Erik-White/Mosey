using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mosey.GUI.Configuration
{
    public interface IWritableOptions<out T> : IOptionsSnapshot<T> where T : class, new()
    {
        void Update(Action<T> applyChanges);
    }

    public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly IOptionsSnapshot<T> _options;
        private readonly string _file;

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

            var optionsInstance = JsonSerializer.Deserialize<T>(File.ReadAllText(physicalPath)) ?? new T();

            applyChanges(optionsInstance);

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            File.WriteAllText(physicalPath, JsonSerializer.Serialize(optionsInstance, jsonOptions));
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureWritable<T>(
            this IServiceCollection services,
            IConfiguration config,
            string name = null,
            string file = "appsettings.json") where T : class, new()
        {
            if (string.IsNullOrEmpty(name))
            {
                services.Configure<T>(config);
            }
            else
            {
                services.Configure<T>(name, config);
            }

            services.AddTransient<IWritableOptions<T>>(provider =>
            {
                var options = provider.GetService<IOptionsSnapshot<T>>();
                return new WritableOptions<T>(options, file);
            });

            return services;
        }
    }
}
