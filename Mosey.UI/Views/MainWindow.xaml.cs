using MahApps.Metro.Controls;
using Mosey.UI.Models;

namespace Mosey.UI.Views
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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
