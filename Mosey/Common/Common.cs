using System;
using Microsoft.Extensions.Configuration;

namespace Mosey
{
    public static class Common {
        public static IConfiguration Configuration
        {
            get;
        } = CreateConfiguration();

        private static IConfiguration CreateConfiguration()
        {
            // Register settings file
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            return config;
        }
    }
}
