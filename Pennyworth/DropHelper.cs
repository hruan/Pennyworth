﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pennyworth {
    public static class DropHelper {
        public static IEnumerable<String> GetAssembliesFromDropData(IEnumerable<FileInfo> data) {
            Func<FileInfo, Boolean> isDir = fi => ((fi.Attributes & FileAttributes.Directory) == FileAttributes.Directory);
            Func<FileInfo, Boolean> isFile = fi => ((fi.Attributes & FileAttributes.Directory) != FileAttributes.Directory);
            var files = data.Where(isFile);
            var dirs  = data
                .Where(isDir)
                .Select(fi => {
                            if (fi.FullName.EndsWith("bin", StringComparison.InvariantCultureIgnoreCase))
                                return new FileInfo(fi.Directory.FullName);

                            return fi;
                        })
                .SelectMany(fi => Directory.EnumerateDirectories(fi.FullName, "bin", SearchOption.AllDirectories));
            var firstAssemblies = dirs.Select(dir => Directory.EnumerateFiles(dir, "*.exe", SearchOption.AllDirectories)
                    .FirstOrDefault(path => !path.Contains("vshost")))
                .Where(dir => !String.IsNullOrEmpty(dir));

            return files.Select(fi => fi.FullName).Concat(firstAssemblies);
        }
    }
}