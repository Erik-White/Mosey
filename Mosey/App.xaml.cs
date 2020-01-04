using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mosey.Models;
using Mosey.Services;

namespace Mosey
{
    /// <summary>
    /// Set up dependency injection and logging for application
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceProvider = new ServiceCollection()
                // Register application logging
                .AddLogging(options =>
                {
                    options.AddConsole();
                    options.AddDebug();
                    options.AddFile("app.log", append: true);
                })
                // Register dependencies
                .AddTransient<IIntervalTimer, IntervalTimer>()
                .AddTransient<IFolderBrowserDialog, FolderBrowserDialog>()
                .AddSingleton<IImagingDevices<IImagingDevice>, ScanningDevices>()
                .AddSingleton<System.ComponentModel.INotifyPropertyChanged, ViewModels.MainViewModel>()

                // Finalize
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<App>();
            logger.LogDebug("Starting application");

            // Locate MainViewModel dependencies and create new window
            var viewModel = serviceProvider.GetService<System.ComponentModel.INotifyPropertyChanged>();
            var window = new Views.MainWindow { DataContext = viewModel };
            window.Show();
        }
        // TODO: Ensure scanner COM objects get released and disposed correctly
        // May not be necessary if ScanningDevices is disposable
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }


        /*
        //Dispose on unhandled exception
        this.DispatcherUnhandledException += (sender, args) => 
        { 
            if (disposableViewModel != null) disposableViewModel.Dispose(); 
        };

        //Dispose on exit
        this.Exit += (sender, args) =>
        { 
            if (disposableViewModel != null) disposableViewModel.Dispose();
        };
        */
    }
}
