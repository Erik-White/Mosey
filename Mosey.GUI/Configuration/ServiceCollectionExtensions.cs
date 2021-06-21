using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mosey.GUI.Models;

namespace Mosey.GUI.Configuration
{
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
