using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mosey.Models
{
    public static class FileSystemExtensions
    {
        /// <summary>
        /// Create a <see cref="DriveInfo"/> instance representing a logical drive on the system that matches <paramref name="driveName"/>.
        /// </summary>
        /// <param name="driveName">A drive root name, e.g. C:\</param>
        /// <returns>A <see cref="DriveInfo"/> instance that represents the logical drive <paramref name="driveName"/></returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public static DriveInfo GetDriveInfo(string driveName)
        {
            return DriveInfo.GetDrives().Where(drive => drive.Name == driveName).FirstOrDefault();
        }

        /// <summary>
        /// The available free space of logical a drive on the system that matches <paramref name="driveName"/>.
        /// </summary>
        /// <param name="driveName">A drive root name, e.g. C:\</param>
        /// <returns>The available free space, in bytes</returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public static long AvailableFreeSpace(string driveName)
        {
            return GetDriveInfo(driveName).AvailableFreeSpace;
        }

        /// <summary>
        /// Check is a file path is a Universal Naming Convention (UNC) network path
        /// </summary>
        /// <param name="path">The path to verify</param>
        /// <returns><see langword="True"/> if <paramref name="path"/> is a UNC path</returns>
        public static bool IsNetworkPath(string path)
        {
            if (!path.StartsWith(@"/") && !path.StartsWith(@"\"))
            {
                // Path may not start with a slash, but could be a network drive
                string rootPath = Path.GetPathRoot(path);
                DriveInfo driveInfo = new DriveInfo(rootPath);

                return driveInfo.DriveType == DriveType.Network;
            }

            return true;
        }
    }
}
