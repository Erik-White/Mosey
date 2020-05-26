using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MahApps.Metro.Controls;
using Mosey.Models;

namespace Mosey.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        public MainWindow()
        {
            InitializeComponent();
            Closing += Window_Closing;
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is IClosing context)
            {
                // Handle window closing in ViewModel, if found
                e.Cancel = true;
                context.OnClosingAsync();
            }
        }
    }
}
