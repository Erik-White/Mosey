using System;
using System.Windows.Input;
using Mosey.Common;
using Mosey.Models;
using IronPython.Hosting;

namespace Mosey.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IntervalTimer scanLagTimer;

        public int ScanDelay
        {
            get
            {
                return ScanDelay;
            }
            set
            {
                ScanDelay = value;
                RaisePropertyChanged("ScanDelay");
            }
        }
        public int ScanInterval
        {
            get
            {
                return ScanInterval;
            }
            set
            {
                ScanInterval = value;
                RaisePropertyChanged("ScanInterval");
            }
        }
        public int ScanRepetitions
        {
            get
            {
                return ScanRepetitions;
            }
            set
            {
                ScanRepetitions = value;
                RaisePropertyChanged("ScanRepetitions");
            }
        }
        public int ScanRepetitionsCount
        {
            get
            {
                return scanLagTimer.RepetitionsCount;
            }
        }

        public MainViewModel()
        {
            scanLagTimer.Tick += scanLagTimer_Tick;
        }

        /*
        private ObservableCollection<Type??> _ScannerCollection;

        public ObservableCollection<WasteContainer> ScannerCollection
        {
            get
            {
                _ScannerCollection = ReturnScanners();
                return _ScannerCollection;
            }
            set
            {
                _ScannerCollection = value;
                RaisePropertyChanged("ScannerCollection");
            }
        }
        */

        #region Commands
        private ICommand _StartScanCommand;
        public ICommand StartScanCommand
        {
            get
            {
                if (_StartScanCommand == null)
                    _StartScanCommand = new RelayCommand(o => StartScan(), o => true);

                return _StartScanCommand;
            }
        }

        private ICommand _PauseScanCommand;
        public ICommand PauseScanCommand
        {
            get
            {
                if (_PauseScanCommand == null)
                    _PauseScanCommand = new RelayCommand(o => scanLagTimer.Pause(), o => true);

                return _PauseScanCommand;
            }
        }

        private ICommand _ResumeScanCommand;
        public ICommand ResumeScanCommand
        {
            get
            {
                if (_ResumeScanCommand == null)
                    _ResumeScanCommand = new RelayCommand(o => scanLagTimer.Resume(), o => true);

                return _ResumeScanCommand;
            }
        }

        private ICommand _StopScanCommand;
        public ICommand StopScanCommand
        {
            get
            {
                if (_StopScanCommand == null)
                    _StopScanCommand = new RelayCommand(o => scanLagTimer.Stop(), o => true);

                return _StopScanCommand;
            }
        }
        #endregion Commands


        public void StartScan()
        {
            scanLagTimer = new IntervalTimer(TimeSpan.FromMinutes(ScanDelay), TimeSpan.FromMinutes(ScanInterval), ScanRepetitions);
            // Block thread until finished signal is recieved
            scanLagTimer.TimerComplete.WaitOne();
            // Run ScanLagAnalysis if required
        }

        private void scanLagTimer_Tick(object sender, EventArgs e)
        {
            // Update progress
            RaisePropertyChanged("ScanRepetitionsCount");
            // Call scanner API
        }

        void ScanLagAnalysis()
        {
            // Create a new ScanLag object using IronPython
            IExternalInstance ScanLag = new ExternalInstance(Python.CreateEngine(), "", "ScanLag");
            //ScanLag.ExecuteMethod("ScanLag", param1, param2);
        }
    }
}
