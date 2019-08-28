using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Mosey.Models;

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
                .AddLogging(options =>
                {
                    options.AddConsole();
                    options.AddDebug();
                })
                .AddSingleton<IIntervalTimer, IntervalTimer>()
                .AddSingleton<IImagingDevices, ScanningDevices>()
                .AddSingleton<System.ComponentModel.INotifyPropertyChanged, ViewModels.MainViewModel>()
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<App>();
            logger.LogDebug("Starting application");


            // Locate MainViewModel dependencies and create new window
            var viewModel = serviceProvider.GetService<System.ComponentModel.INotifyPropertyChanged>();
            var window = new Views.MainWindow { DataContext = viewModel };
            window.Show();
        }
    }
}
