using System;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mosey.Models;
using Mosey.Services;
using Mosey.ViewModels;

namespace Mosey
{
    /// <summary>
    /// Set up dependency injection and logging for application
    /// </summary>
    public partial class App : Application
    {
        private IConfiguration _appConfig;

        protected override void OnStartup(StartupEventArgs e) 
        {
            base.OnStartup(e);

            _appConfig = CreateConfiguration("appsettings.json");

            var serviceProvider = new ServiceCollection()
                // Logging
                .AddLogging(options =>
                {
                    options.AddConsole();
                    options.AddDebug();
                    options.AddFile("app.log", append: true);
                })

                // Configuration
                .Configure<IntervalTimerConfig>(_appConfig.GetSection("Timers:Scan"))
                .Configure<TimerConfig>(_appConfig.GetSection("Timers:UI"))
                .Configure<ImageFileConfig>(_appConfig.GetSection("Image:File"))
                .Configure<ScanningDeviceSettings>(_appConfig.GetSection("Image"))

                // Inject strongly typed configuration classes
                // This method removes the need for the Microsoft.Extensions.Options dependencies that come with IOptions
                .AddScoped<ITimerConfig>(c => c.GetService<IOptions<TimerConfig>>().Value)
                .AddScoped<IIntervalTimerConfig>(c => c.GetService<IOptions<IntervalTimerConfig>>().Value)
                .AddScoped<IImageFileConfig>(c => c.GetService<IOptions<ImageFileConfig>>().Value)
                .AddScoped<IImagingDeviceConfig>(c => c.GetService<IOptions<ScanningDeviceSettings>>().Value)

                // Services
                .AddTransient<IIntervalTimer, IntervalTimer>()
                .AddScoped<IFolderBrowserDialog, FolderBrowserDialog>()
                .AddTransient<IImagingDevice, ScanningDevice>()
                .AddSingleton<IImagingDevices<IImagingDevice>, ScanningDevices>()

                // ViewModels
                .AddSingleton<IViewModel, SettingsViewModel>()
                .AddSingleton<MainViewModel>()

                // Windows
                .AddTransient<Views.Settings>()
                .AddTransient<Views.MainWindow>()

                // Finalize
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<App>();
            logger.LogDebug("Starting application");

            // Locate MainViewModel dependencies and create new window
            var window = serviceProvider.GetRequiredService<Views.MainWindow>();
            window.DataContext = serviceProvider.GetRequiredService<MainViewModel>();
            window.Show();
        }

        private static IConfiguration CreateConfiguration(string fileName, bool optional = false)
        {
            // Register settings file
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile(fileName, optional: optional, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return config;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
