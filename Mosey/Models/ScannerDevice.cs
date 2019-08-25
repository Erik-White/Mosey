using System;
using DNTScanner.Core;

namespace Mosey.Models
{
    public interface IScannerDevice
    {
    }

    /// <summary>
    /// Provides a thin wrapper class to allow for dependency injection
    /// </summary>
    public class ScannerDevice : IScannerDevice
    {
        private DNTScanner.Core.ScannerDevice scannerDevice;

        public ScannerDevice()
        {
            scannerDevice = new DNTScanner.Core.ScannerDevice();
        }
    }
}
