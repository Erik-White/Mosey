using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

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
                .AddSingleton<Mosey.Models.IIntervalTimer, Mosey.Models.IntervalTimer>()
                .AddSingleton<System.ComponentModel.INotifyPropertyChanged, ViewModels.MainViewModel>()
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<App>();
            logger.LogDebug("Starting application");

            var viewModel = serviceProvider.GetService<System.ComponentModel.INotifyPropertyChanged>();
            var window = new Views.MainWindow { DataContext = viewModel };
            window.Show();
        }
    }
}
