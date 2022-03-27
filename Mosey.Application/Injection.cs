using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mosey.Application.Imaging;
using Mosey.Core;
using Mosey.Core.Imaging;

namespace Mosey.Application
{
    public static class Injection
    {
        public static IServiceCollection RegisterApplicationServices(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddTransient<IFactory<IIntervalTimer>, IntervalTimerFactory>();
            service.AddTransient<IImagingDevice, ScanningDevice>();
            service.AddSingleton<IImagingDevices<IImagingDevice>, ScanningDevices>();
            service.AddTransient<IImageHandler<SixLabors.ImageSharp.PixelFormats.Rgba32>, ImageHandler>();
            service.AddTransient<IImageFileHandler, ImageFileHandler>();
            service.AddSingleton<IImagingHost, DntScanningHost>();

            return service;
        }
    }
}