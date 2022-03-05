using System;
using System.Drawing;
using System.IO;
using AutoFixture;

namespace Mosey.Tests.Customizations
{
    public class ImageBytesCustomization : ICustomization
    {
        public void Customize(IFixture fixture) => fixture.Register(() => GetBitmapData());

        internal static byte[] GetBitmapData(int width = 10, int height = 10)
        {
            var random = new Random();
            byte[] bitmapData;

            using (var image = new Bitmap(width, height))
            using (var memoryStream = new MemoryStream())
            {
                image.SetPixel(random.Next(width), random.Next(height), Color.Black);
                image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
                bitmapData = memoryStream.ToArray();
            }

            return bitmapData;
        }
    }
}
