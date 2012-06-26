using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pennyworth {
    public static class DropHelper {
        public static IEnumerable<String> GetAssembliesFromDropData(IEnumerable<FileInfo> data) {
            Func<FileInfo, Boolean> isDir  = fi => ((fi.Attributes & FileAttributes.Directory) == FileAttributes.Directory);
            Func<FileInfo, Boolean> isFile = fi => ((fi.Attributes & FileAttributes.Directory) != FileAttributes.Directory);

            var files = data.Where(isFile);
            var dirs  = data.Where(isDir);

            var assembliesInDirs =
                dirs.SelectMany(dir => Directory.EnumerateFiles(dir.FullName, "*.exe", SearchOption.AllDirectories)
                                                .Where(path => !path.Contains("vshost")));

            return files.Select(fi => fi.FullName).Concat(assembliesInDirs);
        }

        public static String GetBaseDir(IEnumerable<FileInfo> data) {
            if (data == null || !data.Any()) return String.Empty;

            return data.First().FullName;
        }
    }
}
