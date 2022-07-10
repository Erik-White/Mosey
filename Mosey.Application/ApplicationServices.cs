using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mosey.Application.Configuration;
using Mosey.Application.Imaging;
using Mosey.Core;
using Mosey.Core.Imaging;

namespace Mosey.Application
{
    public static class ApplicationServices
    {
        public static IConfiguration ApplicationSettings { get; } = CreateConfigurationFile(AppSettings.DefaultSettingsFileName, false);
        public static IConfiguration UserSettings { get; } = CreateConfigurationFile(AppSettings.UserSettingsFileName, false);

        public static IServiceCollection ConfigureApplicationSettings(this IServiceCollection service, IConfiguration appConfig, IConfiguration userConfig)
        {
            service.Configure<AppSettings>(appConfig);
            service.ConfigureWritable<AppSettings>(userConfig, name: AppSettings.UserSettingsKey, fileName: AppSettings.UserSettingsFileName);

            return service;
        }

        public static IServiceCollection RegisterApplicationServices(this IServiceCollection service)
        {
            service.AddTransient<IIntervalTimer, IntervalTimer>();
            service.AddTransient<IImagingDevice, ScanningDevice>();
            service.AddSingleton<IImagingDevices<IImagingDevice>, ScanningDevices>();
            service.AddTransient<IImageHandler<SixLabors.ImageSharp.PixelFormats.Rgba32>, ImageHandler>();
            service.AddTransient<IImageFileHandler, ImageFileHandler>();
            service.AddSingleton<IImagingHost, DntScanningHost>();
            service.AddSingleton<IIntervalScanningService, IntervalScanningService>();

            return service;
        }

        internal static IConfiguration CreateConfigurationFile(string fileName, bool optional)
        {
            // Register settings file
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile(fileName, optional: optional, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return config;
        }
    }
}