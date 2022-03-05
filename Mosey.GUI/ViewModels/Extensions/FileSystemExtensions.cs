using System;
using System.IO.Abstractions;
using System.Linq;

namespace Mosey.GUI.ViewModels.Extensions
{
    internal static class FileSystemExtensions
    {
        /// <summary>
        /// Create a <see cref="DriveInfo"/> instance representing a logical drive on the system that matches <paramref name="driveName"/>.
        /// </summary>
        /// <param name="driveName">A drive root name, e.g. C:\</param>
        /// <returns>A <see cref="DriveInfo"/> instance that represents the logical drive <paramref name="driveName"/></returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public static IDriveInfo GetDriveInfo(string driveName, IFileSystem fileSystem)
            => fileSystem.DriveInfo.GetDrives().FirstOrDefault(drive => drive.Name == driveName);

        /// <summary>
        /// The available free space of logical a drive on the system that matches <paramref name="driveName"/>.
        /// </summary>
        /// <param name="driveName">A drive root name, e.g. C:\</param>
        /// <returns>The available free space, in bytes</returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public static long AvailableFreeSpace(string driveName, IFileSystem fileSystem)
            => GetDriveInfo(driveName, fileSystem).AvailableFreeSpace;

        /// <summary>
        /// Check is a file path is a Universal Naming Convention (UNC) network path
        /// </summary>
        /// <param name="path">The path to verify</param>
        /// <returns><see langword="True"/> if <paramref name="path"/> is a UNC path</returns>
        public static bool IsNetworkPath(string path, IFileSystem fileSystem)
        {
            if (!path.StartsWith(@"/") && !path.StartsWith(@"\"))
            {
                // Path may not start with a slash, but could be a network drive
                var rootPath = fileSystem.Path.GetPathRoot(path);
                var driveInfo = fileSystem.DriveInfo.FromDriveName(rootPath);

                return driveInfo.DriveType == System.IO.DriveType.Network;
            }

            return true;
        }
    }
}
