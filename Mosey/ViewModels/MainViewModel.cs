using System;
using System.Windows.Input;
using Mosey.Common;
using Mosey.Models;
using IronPython.Hosting;

namespace Mosey.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IntervalTimer scanLag;
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
        public int ScanRepetitionCount
        {
            get
            {
                return scanLag.RepetitionsCount;
            }
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

        private ICommand _StopScanCommand;
        public ICommand StopScanCommand
        {
            get
            {
                if (_StopScanCommand == null)
                    _StopScanCommand = new RelayCommand(o => scanLag.Stop(), o => true);

                return _StopScanCommand;
            }
        }
        #endregion Commands


        public void StartScan()
        {
            //scanLag.Start(ScanDelay, ScanInterval, ScanRepetitions);
            //Block thread until finished signal is recieved
            //scanLag.ScanComplete.WaitOne();
        }

        void ScanLagAnalysis()
        {
            // Create a new ScanLag object using IronPython
            IExternalInstance ScanLag = new ExternalInstance(Python.CreateEngine(), "", "ScanLag");
            //ScanLag.ExecuteMethod("ScanLag", param1, param2);
        }
    }
}
