using System;
using System.IO.Abstractions;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mosey.UI.Models;
using Mosey.UI.Models.Dialog;
using Mosey.UI.Services;
using Mosey.UI.Services.Dialog;
using Mosey.UI.ViewModels;
using Mosey.Core;
using Mosey.Application;
using Mosey.Application.Configuration;
using System.IO;

namespace Mosey.UI
{
    /// <summary>
    /// Set up dependency injection, configuration and logging for application
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private ILogger<App> _log;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configuration and service registration
            var serviceCollection = new ServiceCollection()
                .ConfigureApplicationSettings(ApplicationServices.ApplicationSettings, ApplicationServices.UserSettings)

                .AddLogging(options =>
                {
                    options.AddConfiguration(ApplicationServices.ApplicationSettings.GetSection("Logging"));
                    options.AddConsole();
#if DEBUG
                    options.AddDebug();
#endif
                    // Logging to file
                    options.AddFile("mosey.log", append: true);
                })

                .RegisterApplicationServices()
                .AddSingleton<IFileSystem, FileSystem>()
                // TODO: Move scanning timer to a scanning service
                .AddTransient<IFactory<IIntervalTimer>, IntervalTimerFactory>()
                .AddTransient<IFolderBrowserDialog, FolderBrowserDialog>()
                .AddScoped<IDialogManager, DialogManager>()

                // Aggregate services
                .AddTransient<UIServices>()

                .AddSingleton<IViewModel, SettingsViewModel>()
                .AddSingleton<MainViewModel>()

                .AddTransient<Views.Settings>()
                .AddTransient<Views.MainWindow>();

            serviceCollection = SetDefaultImagePath(serviceCollection);

            // Finalize
            var serviceProvider = serviceCollection.BuildServiceProvider();

            _log = serviceProvider.GetService<ILoggerFactory>().CreateLogger<App>();
            _log.LogDebug("Starting application");

            // Locate MainViewModel dependencies and create new window
            var window = serviceProvider.GetRequiredService<Views.MainWindow>();
            window.DataContext = serviceProvider.GetRequiredService<MainViewModel>();
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _log.LogDebug("Closing application");
        }

        /// <summary>
        /// Ensure default directory is user's Pictures folder
        /// </summary>
        private static IServiceCollection SetDefaultImagePath(IServiceCollection services)
        {
            services.PostConfigureAll<AppSettings>(config =>
            {
                if (string.IsNullOrEmpty(config.ImageFile.Directory))
                {
                    config.ImageFile = config.ImageFile with
                    {
                        Directory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToString(),
                        "Mosey")
                    };
                }
            });

            return services;
        }
    }
}
