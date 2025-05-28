using System;
using System.IO;
using System.Linq;

namespace FastPack.Lib
{
	public static class CopyOnWriteDiskInfo
	{
		/// <summary>
		/// Decision for this list: ReFS + Linux file systems
		/// For linux filesystems:
		/// - .NET runtime calls FICLONE: https://github.com/dotnet/runtime/blob/2a47838c6d353b783ca8466e40d7db756f2d2acf/src/native/libs/System.Native/pal_io.c#L1422
		/// - According to Kernel docs this calls remap_file_range internally: https://www.kernel.org/doc/Documentation/filesystems/vfs.txt
		/// - remap_file_range is implemented in the file systems listed here. To find this search remap_file_range on kernel source
		/// - It was assumed that DriveInfo.DriveFormat returns the same string as the directory name in the kernel source
		/// </summary>
		private static readonly string[] FilesystemsWithCopyOnWrite = {
				"refs",
				"bcachefs",
				"btrfs",
				"nfs",
				"ocfs2",
				"overlayfs",
				"smb",
				"xfs"
		};

		public static bool DirectorySupportsCopyOnWrite(string directory)
		{
			var driveInfo = new DriveInfo(directory);
			return driveInfo.IsReady && FilesystemsWithCopyOnWrite.Contains(driveInfo.DriveFormat, StringComparer.OrdinalIgnoreCase);
		}
	}
}