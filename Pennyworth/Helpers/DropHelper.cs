using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pennyworth.Helpers {
	public static class DropHelper {
		/// <summary>
		/// Find assemblies in the files and/or directories
		/// </summary>
		/// <param name="data">paths to files to sift through</param>
		/// <returns>absolute paths to assemblies found in <paramref name="data"/></returns>
		public static IEnumerable<String> GetAssembliesFromDropData(IEnumerable<FileInfo> data) {
			Func<FileInfo, Boolean> isDir  = fi => ((fi.Attributes & FileAttributes.Directory) == FileAttributes.Directory);
			Func<FileInfo, Boolean> isFile = fi => ((fi.Attributes & FileAttributes.Directory) != FileAttributes.Directory);

			var files = data.Where(isFile);
			var dirs  = data.Where(isDir);

			var assembliesInDirs =
				dirs.SelectMany(dir => Directory.EnumerateFiles(dir.FullName, "*", SearchOption.AllDirectories));

			return files.Select(fi => fi.FullName)
				.Concat(assembliesInDirs)
				.Where(path => (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
								|| path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
							   && !path.Contains("vshost"));
		}

		/// <summary>
		/// Find the absolute path of the base directory
		/// </summary>
		/// <returns>absolute path to base directory</returns>
		public static String GetBaseDir(IEnumerable<FileInfo> data) {
			if (data == null || !data.Any()) return String.Empty;

			return data.First().FullName;
		}
	}
}
