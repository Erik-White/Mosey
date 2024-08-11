using System;
using System.IO.Abstractions;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mosey.Gui.Models;
using Mosey.Gui.Models.Dialog;
using Mosey.Gui.Services;
using Mosey.Gui.Services.Dialog;
using Mosey.Gui.ViewModels;
using Mosey.Application;
using Mosey.Application.Configuration;
using Mosey.Application.Imaging;
using Mosey.Core;
using Mosey.Core.Imaging;
using NReco.Logging.File;

namespace Mosey.Gui
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
                        Directory = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToString(),
                            "Mosey")
                    };
                }
            });

            return services;
        }
    }
}
