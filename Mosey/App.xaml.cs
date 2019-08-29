using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mosey.Models;
using Mosey.Services;

namespace Mosey
{
    /// <summary>
    /// Interaction logic for App.xaml
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
                })
                // Register dependencies
                .AddSingleton<IIntervalTimer, IntervalTimer>()
                .AddSingleton<IImagingDevices, ScanningDevices>()
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
    }
}
