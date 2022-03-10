using System;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mosey.GUI.Configuration;
using Mosey.GUI.Models;
using Mosey.GUI.Models.Dialog;
using Mosey.GUI.Services;
using Mosey.GUI.Services.Dialog;
using Mosey.GUI.ViewModels;
using Mosey.Models;
using Mosey.Models.Imaging;
using Mosey.Services;
using Mosey.Services.Imaging;

[assembly: InternalsVisibleTo("Mosey.GUI.Tests")]
namespace Mosey.GUI
{
    /// <summary>
    /// Set up dependency injection, configuration and logging for application
    /// </summary>
    public partial class App : Application
    {
        private IConfiguration _appConfig;
        private IConfiguration _userConfig;
        private ILogger<App> _log;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _appConfig = CreateConfiguration("appsettings.json");
            _userConfig = CreateConfiguration("usersettings.json");

            var serviceProvider = new ServiceCollection()
                // Configuration
                .Configure<AppSettings>(_appConfig)
                .ConfigureWritable<AppSettings>(_userConfig, name: "UserSettings", file: "usersettings.json")
                .PostConfigureAll<AppSettings>(config =>
                {
                    if (string.IsNullOrEmpty(config.ImageFile.Directory))
                    {
                        // Ensure default directory is user's Pictures folder
                        config.ImageFile = config.ImageFile with { Directory = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToString(),
                            "Mosey")};
                    }
                })

                // Logging
                .AddLogging(options =>
                {
                    options.AddConfiguration(_appConfig.GetSection("Logging"));
                    options.AddConsole();
                    options.AddDebug();
                    // Logging to file
                    options.AddFile("mosey.log", append: true);
                })

                // Services
                .AddSingleton<IFileSystem, FileSystem>()
                .AddTransient<IFactory<IIntervalTimer>, IntervalTimerFactory>()
                .AddTransient<IFolderBrowserDialog, FolderBrowserDialog>()
                .AddScoped<IDialogManager, DialogManager>()
                .AddTransient<IImagingDevice, ScanningDevice>()
                .AddSingleton<IImagingDevices<IImagingDevice>, ScanningDevices>()
                .AddTransient<IImageHandler<SixLabors.ImageSharp.PixelFormats.Rgba32>, ImageHandler>()
                .AddTransient<IImageFileHandler, ImageFileHandler>()
                .AddSingleton<IImagingHost, DntScanningHost>()

                // Aggregate services
                .AddTransient<UIServices>()

                // ViewModels
                .AddSingleton<IViewModel, SettingsViewModel>()
                .AddSingleton<MainViewModel>()

                // Windows
                .AddTransient<Views.Settings>()
                .AddTransient<Views.MainWindow>()

                // Finalize
                .BuildServiceProvider();

            _log = serviceProvider.GetService<ILoggerFactory>().CreateLogger<App>();
            _log.LogDebug("Starting application");

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
            _log.LogDebug("Closing application");
        }
    }
}
