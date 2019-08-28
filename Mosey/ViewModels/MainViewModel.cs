using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Mosey.Common;
using Mosey.Models;
using IronPython.Hosting;

namespace Mosey.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IIntervalTimer scanLagTimer;
        private IExternalInstance scanLagAnalysis;

        private int _ScanDelay;
        public int ScanDelay
        {
            get
            {
                return _ScanDelay;
            }
            set
            {
                _ScanDelay = value;
                RaisePropertyChanged("ScanDelay");
            }
        }
        private int _ScanInterval;
        public int ScanInterval
        {
            get
            {
                return _ScanInterval;
            }
            set
            {
                _ScanInterval = value;
                RaisePropertyChanged("ScanInterval");
            }
        }
        private int _ScanRepetitions;
        public int ScanRepetitions
        {
            get
            {
                return _ScanRepetitions;
            }
            set
            {
                _ScanRepetitions = value;
                RaisePropertyChanged("ScanRepetitions");
            }
        }
        public int ScanRepetitionsCount
        {
            get
            {
                if (scanLagTimer != null)
                {
                    return scanLagTimer.RepetitionsCount;
                }
                else
                {
                    return 0;
                }
            }
        }
        public bool IsScanRunning
        {
            get
            {
                if(scanLagTimer != null)
                {
                    return scanLagTimer.Enabled;
                }
                else
                {
                    return false;
                }
            }
        }

        private ObservableCollection<IImagingDevice> _ScannerDevices;
        public ObservableCollection<IImagingDevice> ScannerDevices
        {
            get
            {
                return _ScannerDevices;
            }
            set
            {
                _ScannerDevices = value;
                RaisePropertyChanged("ScannerDevices");
            }
        }

        public MainViewModel(IIntervalTimer intervalTimer, IImagingDevices imagingDevices)
        {
            scanLagTimer = intervalTimer;
            _ScannerDevices = new ObservableCollection<IImagingDevice>(imagingDevices);

            scanLagTimer.Tick += ScanLagTimer_Tick;
            scanLagTimer.Complete += ScanLagTimer_Complete;
        }

        #region Commands
        private ICommand _StartScanCommand;
        public ICommand StartScanCommand
        {
            get
            {
                if (_StartScanCommand == null)
                    _StartScanCommand = new RelayCommand(o => StartScan(), o => !IsScanRunning && !scanLagTimer.Paused);

                return _StartScanCommand;
            }
        }

        private ICommand _PauseScanCommand;
        public ICommand PauseScanCommand
        {
            get
            {
                if (_PauseScanCommand == null)
                    _PauseScanCommand = new RelayCommand(o => scanLagTimer.Pause(), o => IsScanRunning && !scanLagTimer.Paused);

                return _PauseScanCommand;
            }
        }

        private ICommand _ResumeScanCommand;
        public ICommand ResumeScanCommand
        {
            get
            {
                if (_ResumeScanCommand == null)
                    _ResumeScanCommand = new RelayCommand(o => scanLagTimer.Resume(), o => scanLagTimer.Paused);

                return _ResumeScanCommand;
            }
        }

        private ICommand _StopScanCommand;
        public ICommand StopScanCommand
        {
            get
            {
                if (_StopScanCommand == null)
                    _StopScanCommand = new RelayCommand(o => scanLagTimer.Stop(), o => IsScanRunning);

                return _StopScanCommand;
            }
        }
        #endregion Commands


        public void StartScan()
        {
            //scanLagTimer.Start(TimeSpan.FromMinutes(ScanDelay), TimeSpan.FromMinutes(ScanInterval), ScanRepetitions);
            //Use seconds instead of minutes for testing functionality
            scanLagTimer.Start(TimeSpan.FromSeconds(ScanDelay), TimeSpan.FromSeconds(ScanInterval), ScanRepetitions);
            RaisePropertyChanged("IsScanRunning");

            // Run ScanLagAnalysis if required
            //ScanLagAnalysis();
        }

        private void ScanLagTimer_Tick(object sender, EventArgs e)
        {
            // Update progress
            RaisePropertyChanged("ScanRepetitionsCount");

            // Call scanner API
            Scan();
        }

        private void ScanLagTimer_Complete(object sender, EventArgs e)
        {
            RaisePropertyChanged("ScanRepetitionsCount");
            RaisePropertyChanged("IsScanRunning");
        }

        public void Scan()
        {
            //ScanTest test = new ScanTest();
            foreach(ScanningDevice scanner in ScannerDevices)
            {
                scanner.GetImage();
                scanner.SaveImage("", "test.jpg");
            }
        }
        public void ScanLagAnalysis()
        {
            // Create a new ScanLag object using IronPython
            IExternalInstance scanLag = new ExternalInstance(Python.CreateEngine(), "", "ScanLag");
            //ScanLag.ExecuteMethod("ScanLag", param1, param2);
        }
    }
}
