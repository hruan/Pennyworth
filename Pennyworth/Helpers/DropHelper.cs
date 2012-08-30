using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pennyworth.Helpers {
	public static class DropHelper {
		/// <summary>
		/// Find assemblies in the files and/or directories
		/// </summary>
		/// <param name="paths">paths to files to sift through</param>
		/// <returns>absolute paths to assemblies found in <paramref name="paths"/></returns>
		public static IEnumerable<String> GetAssembliesFromDropData(IEnumerable<String> paths) {
			Func<FileInfo, Boolean> isDir  = fi => ((fi.Attributes & FileAttributes.Directory) == FileAttributes.Directory);
			Func<FileInfo, Boolean> isFile = fi => ((fi.Attributes & FileAttributes.Directory) != FileAttributes.Directory);

			var fileInfos = paths.Select(x => new FileInfo(x)).ToList();
			var files = fileInfos.Where(isFile);
			var dirs  = fileInfos.Where(isDir);

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
		public static String GetBaseDir(IEnumerable<String> paths) {
			if (paths == null || !paths.Any()) return String.Empty;

			return new FileInfo(paths.First()).FullName;
		}
	}
}
