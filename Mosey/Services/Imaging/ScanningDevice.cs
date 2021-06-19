using DNTScanner.Core;
using Mosey.Models;
using Mosey.Services.Imaging.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mosey.Services.Imaging
{
    /// <summary>
    /// Represents a physical scanning device connected to the system.
    /// </summary>
    public class ScanningDevice : PropertyChangedBase, IImagingDevice
    {
        public string Name { get { return _scannerSettings.Name; } }
        public int ID { get { return GetSimpleID(_scannerSettings.Id); } }
        public string DeviceID { get { return _scannerSettings.Id; } }
        public IList<KeyValuePair<string, object>> DeviceSettings { get { return _scannerSettings.ScannerDeviceSettings.ToList(); } }
        
        public IList<byte[]> Images { get; protected internal set; } = new List<byte[]>();
        public IImagingDeviceConfig ImageSettings { get; set; }

        /// <inheritdoc cref="ScannerSettings.IsAutomaticDocumentFeeder"/>
        public bool IsAutomaticDocumentFeeder { get { return _scannerSettings.IsAutomaticDocumentFeeder; } }

        public bool IsConnected
        {
            get { return _isConnected; }
            protected internal set { SetField(ref _isConnected, value); }
        }

        /// <inheritdoc cref="ScannerSettings.IsDuplex"/>
        public bool IsDuplex { get { return _scannerSettings.IsDuplex; } }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetField(ref _isEnabled, value); }
        }

        /// <inheritdoc cref="ScannerSettings.IsFlatbed"/>
        public bool IsFlatbed { get { return _scannerSettings.IsFlatbed; } }

        public bool IsImaging
        {
            get { return _isImaging; }
            private set { SetField(ref _isImaging, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public int ScanRetries
        {
            get { return _scanRetries; }
            set { SetField(ref _scanRetries, value); }
        }

        /// <inheritdoc cref="ScannerSettings.SupportedResolutions"/>
        public IList<int> SupportedResolutions { get { return _scannerSettings.SupportedResolutions; } }

        /// <summary>
        /// The image format used for encoding images captured by the <see cref="ScanningDevice"/>.
        /// </summary>
        public enum ImageFormat
        {
            Bmp,
            Png,
            Gif,
            Jpeg,
            Tiff
        }

        private bool _isConnected = true;
        private bool _isEnabled;
        private bool _isImaging;
        private int _scanRetries = 5;
        private readonly IFileSystem _fileSystem;
        private readonly ScannerSettings _scannerSettings;
        private readonly ISystemDevices _systemDevices;

        /// <summary>
        /// Initialize a new instance using a <see cref="ScannerSettings"/> instance that represents a physical scanner.
        /// </summary>
        /// <param name="settings">A <see cref="ScannerSettings"/> instance representing a physical device</param>
        public ScanningDevice(ScannerSettings settings) : this(settings, null, null, null) { }

        /// <summary>
        /// Initialize a new instance using a <see cref="ScannerSettings"/> instance that represents a physical scanner.
        /// </summary>
        /// <param name="settings">A <see cref="ScannerSettings"/> instance representing a physical device</param>
        /// <param name="config">Device settings used when capturing an image</param>
        public ScanningDevice(ScannerSettings settings, IImagingDeviceConfig config) : this(settings, config, null, null) { }

        /// <summary>
        /// Initialize a new instance using a <see cref="ScannerSettings"/> instance that represents a physical scanner.
        /// </summary>
        /// <param name="settings">A <see cref="ScannerSettings"/> instance representing a physical device</param>
        /// <param name="config">Device settings used when capturing an image</param>
        /// <param name="systemDevices">An <see cref="ISystemDevices"/> instance that provide access to the WIA driver devices</param>
        public ScanningDevice(ScannerSettings settings, IImagingDeviceConfig config, ISystemDevices systemDevices, IFileSystem fileSystem)
        {
            _scannerSettings = settings;
            ImageSettings = config;
            _systemDevices = systemDevices ?? new SystemDevices();
            _fileSystem = fileSystem ?? new FileSystem();
        }

        public void ClearImages()
        {
            Images = new List<byte[]>();
        }

        public void GetImage()
        {
            GetImage(ImageFormat.Bmp);
        }

        /// <summary>
        /// Retrieve an image from the physical imaging device.
        /// </summary>
        /// <param name="format">The image transfer format used when capturing the image</param>
        /// <exception cref="COMException">If the scanner is not connected</exception>
        /// <exception cref="ArgumentException">If the <see cref="ImageFormat"/> is not supported by the device</exception>
        /// <exception cref="InvalidOperationException">If the operation fails during scanning</exception>
        /// <remarks>
        /// Images are converted to <see cref="ImageFormat.Png"/>, if possible, before being stored as byte arrays.
        /// </remarks>
        public void GetImage(ImageFormat format)
        {
            if (!IsConnected)
            {
                throw new COMException("The scanner is not connected.");
            }
            if (!_scannerSettings.SupportedTransferFormats.ContainsKey(format.ToWIAImageFormat().Value))
            {
                throw new ArgumentException($"The image format {format} is not supported by this device.");
            }

            // Default settings if none provided
            var deviceConfig = ImageSettings;
            deviceConfig ??= new ScanningDeviceSettings
            {
                Brightness = 1,
                Contrast = 1,
                ColorFormat = ImageColorFormat.Color,
                Resolution = SupportedResolutions.Max()
            };

            IsImaging = true;
            try
            {
                var images = _systemDevices.PerformScan(_scannerSettings, deviceConfig, format);

                // Remove any existing images
                ClearImages();

                // Store images for processing etc
                foreach (var image in images)
                {
                    try
                    {
                        // Convert image to PNG format before storing byte array
                        // Greatly reduces memory footprint compared to raw BMP
                        Images.Add(image.AsFormat(ImageFormat.Png.ToDrawingImageFormat()));
                    }
                    catch (ArgumentException)
                    {
                        // Store the image in its original format
                        Images.Add(image);
                    }
                }
            }
            catch (Exception ex) when (ex is COMException || ex is InvalidOperationException)
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

        /// <summary>
        /// Write an image captured with <see cref="GetImage"/> to disk
        /// Images are stored in the user's <see cref="Environment.SpecialFolder.MyPictures"/> directory as PNGs
        /// </summary>
        public void SaveImage()
        {
            string directory = _fileSystem.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToString(),
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
            SaveImage("image", directory, ImageFormat.Png);
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
        /// Write an image captured with <see cref="GetImage"/> to disk.
        /// Images are stored losslessly (in supported formats) with a colour depth of 24 bit per pixel.
        /// </summary>
        /// <param name="fileName">The image file name, the file extension ignored and instead inferred from <paramref name="fileFormat"/></param>
        /// <param name="directory">The directory path to use when storing the image</param>
        /// <param name="imageFormat">The <see cref="ImageFormat"/> used to store the image</param>
        /// <returns>A collection of file path <see cref="string"/>s for the newly created images</returns>
        /// <exception cref="ArgumentException">If <paramref name="directory"/> is <see langword="null"/> or whitespace</exception>
        /// <exception cref="InvalidOperationException">If the <see cref="Images"/> property returns no images to save</exception>
        public IEnumerable<string> SaveImage(string fileName, string directory, ImageFormat imageFormat = ImageFormat.Png)
        {
            if (Images == null || Images.Count == 0)
            {
                throw new InvalidOperationException($"No images available. Please call the {nameof(GetImage)} method first.");
            }
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("A valid filename and directory must be supplied");
            }

            // Get full filename and path
            _fileSystem.Directory.CreateDirectory(directory);
            fileName = _fileSystem.Path.Combine(directory, fileName);
            fileName = _fileSystem.Path.ChangeExtension(fileName, imageFormat.ToString().ToLower());

            // Use lossless compression with highest quality
            using (var encoderParameters = new EncoderParameters().AddParams(
                compression: EncoderValue.CompressionLZW,
                quality: 100,
                colorDepth: 24
                ))
            {
                // Write image(s) to disk using specified encoding
                // Ensure we enumerate here otherwise the encoderParameters will go out of context
                return SaveImagesToDisk(Images, fileName, imageFormat, encoderParameters).ToList();
            }
        }

        public bool Equals(IImagingDevice device)
        {
            return device is not null && DeviceID == device.DeviceID;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ScanningDevice);
        }

        public override int GetHashCode()
        {
            return DeviceID.GetHashCode();
        }

        /// <summary>
        /// Store image byte arrays to disk.
        /// </summary>
        /// <remarks>
        /// The image count is appended to the filename in case of multiple images.
        /// </remarks>
        /// <param name="images">An image byte array</param>
        /// <param name="filePath">The full file path used to store the images</param>
        /// <param name="format">The <see cref="ImageFormat"/> used to store the images</param>
        /// <param name="encoderParams">Specify image encoding when writing the images</param>
        /// <returns>A collection of file path <see cref="string"/>s for the newly created images</returns>
        protected internal IEnumerable<string> SaveImagesToDisk(IEnumerable<byte[]> images, string filePath, ImageFormat format = ImageFormat.Png, EncoderParameters encoderParams = null)
        {
            int count = 1;
            string fileName = Path.GetFileName(filePath);

            // Write all images to disk
            foreach (var imageBytes in Images)
            {
                // Append count to filename in case of multiple images
                string savePath = filePath;
                if (images.Count() > 1) 
                    savePath = savePath.Replace(
                        fileName,
                        $"{Path.GetFileNameWithoutExtension(fileName)}_{count}{Path.GetExtension(filePath)}");

                SaveImageToDisk(imageBytes, savePath, format, encoderParams);

                yield return savePath;

                count++;
            }
        }

        /// <summary>
        /// Store an image byte array to disk
        /// </summary>
        /// <param name="image">An image byte array</param>
        /// <param name="filePath">The full file path used to store the image</param>
        /// <param name="format">The <see cref="ImageFormat"/> used to store the image</param>
        /// <param name="encoderParams">Specify image encoding when writing the image</param>
        protected internal virtual void SaveImageToDisk(byte[] image, string filePath, ImageFormat format = ImageFormat.Png, EncoderParameters encoderParams = null)
        {
            using (var fileStream = _fileSystem.File.Create(filePath))
            {
                image.ToImage().Save(
                    fileStream,
                    format.ToDrawingImageFormat().CodecInfo(),
                    encoderParams);
            }
        }

        /// <summary>
        /// A simplified version of the unique device identifier, <see cref="DeviceID"/>
        /// </summary>
        /// <param name="deviceID">A unique device identifier</param>
        /// <returns></returns>
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
    }
}
