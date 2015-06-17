using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DragEncrypt
{
    public static class Core
    {
        // TODO: Prevent infinite loop if a match is never found (somehow)
        /// <summary>
        /// Gets a name for the given <paramref name="fileProposal"/> that won't conflict with any already existant file.
        /// </summary>
        /// <remarks>
        /// If a conflict is detected, the returned file will be named as "name (n).extension".
        /// </remarks>
        /// <param name="fileProposal">The targetted name</param>
        /// <returns>FileInfo of the first non-conflict file possible</returns>
        public static FileInfo GetNonCollidingFile(string fileProposal)
        {
            var collidingFile = new FileInfo(fileProposal);
            if (!collidingFile.Exists) return collidingFile;
            for (var i = 1;; i++)
            {
                var target = $"{collidingFile.DirectoryName}/{Path.GetFileNameWithoutExtension(collidingFile.Name)} ({i}){collidingFile.Extension}";
                if (!File.Exists(target)) return new FileInfo(target);
            }
        }

        public static DirectoryInfo GetNonCollidingDirectory(string directoryProposal)
        {
            var collidingDirectory = new DirectoryInfo(directoryProposal);
            if (!collidingDirectory.Exists) return collidingDirectory;
            for (var i = 1; ; i++)
            {
                var target = $"{collidingDirectory.Parent}/{Path.GetFileNameWithoutExtension(collidingDirectory.Name)} ({i})";
                if (!Directory.Exists(target)) return new DirectoryInfo(target);
            }
        }

        public static void ShallowEraseList<T>(IList<T> bytes)
        {
            if (bytes == null) return;
            for (var i = 0; i < bytes.Count; i++)
                bytes[i] = default(T);
        }

        public static FileInfo CreateRandomFilledFile(string location)
        {
            var file = new FileInfo(location);
            using (var fileStream = file.Open(FileMode.Create))
            {
                var random = new Random();
                for (var i = 0; i < 1024; i++)
                {
                    var buffer = new byte[1024];
                    random.NextBytes(buffer);
                    fileStream.Write(buffer, 0, buffer.Length);
                }
            }
            return file;
        }

        public static void SafeOverwriteFile(FileInfo file)
        {
            //var buffer = new byte[1024];
            //for (var i = buffer.Length - 1; i >= 0; i--)
            //    buffer[i] = 0;
            var buffer = Enumerable.Repeat((byte)0, 1024).ToArray();
            using (var fs = file.OpenWrite())
            {
                for (var i = file.Length / buffer.Length; i >= 0; i--)
                    fs.Write(buffer, 0, buffer.Length);
            }
        }
    }
}