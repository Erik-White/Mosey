using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DNTScanner.Core;
using Mosey.Models;
using Mosey.Services.Imaging.Extensions;

namespace Mosey.Services.Imaging
{


    /// <summary>
    /// Provides functionality for WIA enabled scanners
    /// </summary>
    public class ScanningDevice : IImagingDevice
    {
        public string Name { get { return _scannerSettings.Name; } }
        public int ID { get { return GetSimpleID(_scannerSettings.Id); } }
        public string DeviceID { get { return _scannerSettings.Id; } }
        public IList<KeyValuePair<string, object>> DeviceSettings { get { return _scannerSettings.ScannerDeviceSettings.ToList(); } }
        public IList<byte[]> Images { get; protected internal set; } = new List<byte[]>();
        public IList<int> SupportedResolutions { get { return _scannerSettings.SupportedResolutions; } }
        public IImagingDeviceConfig ImageSettings { get; set; }
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetField(ref _isEnabled, value); }
        }
        public bool IsConnected
        {
            get { return _isConnected; }
            protected internal set { SetField(ref _isConnected, value); }
        }
        public bool IsImaging
        {
            get { return _isImaging; }
            private set { SetField(ref _isImaging, value); }
        }
        public bool IsAutomaticDocumentFeeder { get { return _scannerSettings.IsAutomaticDocumentFeeder; } }
        public bool IsDuplex { get { return _scannerSettings.IsDuplex; } }
        public bool IsFlatbed { get { return _scannerSettings.IsFlatbed; } }
        public int ScanRetries
        {
            get { return _scanRetries; }
            set { SetField(ref _scanRetries, value); }
        }
        public enum ImageFormat
        {
            Bmp,
            Png,
            Gif,
            Jpeg,
            Tiff
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _isEnabled;
        private bool _isConnected = true;
        private bool _isImaging;
        private int _scanRetries = 10;
        private ScannerSettings _scannerSettings;

        public ScanningDevice(ScannerSettings settings)
        {
            _scannerSettings = settings;
        }

        public ScanningDevice(ScannerSettings settings, IImagingDeviceConfig config)
        {
            _scannerSettings = settings;
            ImageSettings = config;
        }

        public void GetImage()
        {
            GetImage(WiaImageFormat.Bmp);
        }

        public void ClearImages()
        {
            Images = new List<byte[]>();
        }

        public void GetImage(WiaImageFormat format)
        {
            if (!IsConnected)
            {
                throw new COMException("The scanner is not connected.");
            }

            if (!_scannerSettings.SupportedTransferFormats.ContainsKey(format.Value))
            {
                throw new ArgumentException($"The image format {format} is not supported by this device.");
            }

            // Wait until the scanner is ready
            IsImaging = true;
            while (_scanRetries > 0)
            {
                try
                {
                    // Connect to the specified device and create a COM object representation
                    using (ScannerDevice scannerDevice = new ScannerDevice(_scannerSettings))
                    {
                        // Configure the device
                        IImagingDeviceConfig deviceConfig = ImageSettings;
                        if (ImageSettings is null)
                        {
                            deviceConfig = new ScanningDeviceSettings
                            {
                                Brightness = 1,
                                Contrast = 1,
                                ColorFormat = ImageColorFormat.Color,
                                // Select the highest resolution available
                                Resolution = SupportedResolutions.Max()
                            };
                        }

                        // Check that the selected resolution is supported by this device
                        if (!SupportedResolutions.Contains(deviceConfig.Resolution))
                        {
                            // Find the closest supported resolution instead
                            deviceConfig.Resolution = SupportedResolutions
                                .OrderBy(v => v)
                                .OrderBy(item => Math.Abs(deviceConfig.Resolution - item))
                                .First();
                        }

                        scannerDevice.ScannerPictureSettings(pictureConfig =>
                            pictureConfig
                                .ColorFormat(ScanningDeviceSettings.ColorTypeFromFormat(deviceConfig.ColorFormat))
                                .Resolution(deviceConfig.Resolution)
                                .Brightness(deviceConfig.Brightness)
                                .Contrast(deviceConfig.Contrast)
                                .StartPosition(left: 0, top: 0)
                            );

                        // Replace any existing images
                        ClearImages();

                        // Retrieve image(s) from scanner
                        scannerDevice.PerformScan(format);

                        // Store images for processing etc
                        foreach (byte[] image in scannerDevice.ExtractScannedImageFiles())
                        {
                            // Convert image to PNG format before storing byte array
                            // Greatly reduces memory footprint compared to raw BMP
                            Images.Add(image.AsFormat(ImageFormat.Png.ToDrawingImageFormat()));
                        }
                    }

                    // Cancel retry loop if successful
                    break;
                }
                catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                {
                    if (--_scanRetries > 0)
                    {
                        // Wait until the scanner is ready if it is warming up, busy etc
                        // Also retry if the WIA driver does not respond in time (NullReference)

                        // Connecting a scanner when a scan is already in progress
                        // COMException: The remote procedure call failed. (0x800706BE)
                        // HResult	-2146233088	int

                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }

                    // Mark the device as unreachable if the limit is reached
                    IsConnected = false;
                    throw;
                }
                catch (Exception ex) when (ex is InvalidOperationException)
                {
                    // Scanner has been disconnected or similar
                    IsConnected = false;
                    throw;
                }
                finally
                {
                    IsImaging = false;
                }
            }
        }

        /// <summary>
        /// Write an image captured with <see cref="GetImage"/> to disk
        /// Images are stored in the user's <see cref="Environment.SpecialFolder.MyPictures"/> directory as PNGs
        /// </summary>
        public void SaveImage()
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToString(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
            SaveImage("image", directory, "png");
        }

        /// <summary>
        /// Write an image captured with <see cref="GetImage"/> to disk
        /// </summary>
        /// <param name="fileName">The image file name, the file extension ignored and instead inferred from <paramref name="fileFormat"/></param>
        /// <param name="directory">The directory path to use when storing the image</param>
        /// <param name="fileFormat">The image format file extension <see cref="string"/> used to store the image</param>
        /// <returns>A collection of file path <see cref="string"/>s for the newly created images</returns>
        public IEnumerable<string> SaveImage(string fileName, string directory, string fileFormat = "png")
        {
            // Parse the file extension and ensure it is valid
            var imageFormat = new ImageFormat().FromString(fileFormat);

            return SaveImage(fileName, directory, imageFormat);
        }

        /// <summary>
        /// Write an image captured with <see cref="GetImage"/> to disk
        /// Images are stored losslessly (in supported formats) with a colour depth of 24 bit per pixel
        /// </summary>
        /// <param name="fileName">The image file name, the file extension ignored and instead inferred from <paramref name="fileFormat"/></param>
        /// <param name="directory">The directory path to use when storing the image</param>
        /// <param name="imageFormat">The <see cref="ImageFormat"/> used to store the image</param>
        /// <returns>A collection of file path <see cref="string"/>s for the newly created images</returns>
        public IEnumerable<string> SaveImage(string fileName, string directory, ImageFormat imageFormat = ImageFormat.Png)
        {
            if (Images == null | Images.Count == 0)
            {
                throw new InvalidOperationException($"No images available. Please call the {nameof(GetImage)} method first.");
            }
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("A valid filename and directory must be supplied");
            }

            // Get full filename and path
            Directory.CreateDirectory(directory);
            fileName = Path.Combine(directory, fileName);
            fileName = Path.ChangeExtension(fileName, imageFormat.ToString().ToLower());

            // Use lossless compression with highest quality
            using (EncoderParameters encoderParameters = new EncoderParameters().AddParams(
                compression: EncoderValue.CompressionLZW,
                quality: 100,
                colorDepth: 24
                ))
            {
                // Write all images to disk
                foreach (var imageBytes in Images)
                {
                    imageBytes.ToImage().Save(
                        fileName,
                        imageFormat.ToDrawingImageFormat().CodecInfo(),
                        encoderParameters
                        );
                    yield return fileName;
                }
            }
        }

        private static int GetSimpleID(string deviceID)
        {
            // Get the last two characters of the device instance path
            string shortID = deviceID.Substring(Math.Max(0, deviceID.Length - 2));

            if (int.TryParse(shortID, out int intID))
            {
                return intID;
            }
            else
            {
                // Return the numeric representation of the characters instead
                return shortID.GetHashCode();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public bool Equals(IImagingDevice device)
        {
            return null != device && DeviceID == device.DeviceID;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ScanningDevice);
        }

        public override int GetHashCode()
        {
            return DeviceID.GetHashCode();
        }
    }
}
