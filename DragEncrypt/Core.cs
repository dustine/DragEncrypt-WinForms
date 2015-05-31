using System.Collections.Generic;
using System.IO;

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
        public static string GetNonCollidingFile(string fileProposal)
        {
            var conflictFile = new FileInfo(fileProposal);
            if (!conflictFile.Exists) return fileProposal;
            for (var i = 1;; i++)
            {
                var target = $"{conflictFile.DirectoryName}/{GetFilenameWithoutExtension(conflictFile)} ({i}){conflictFile.Extension}";
                if (!File.Exists(target)) return target;
            }
        }

        public static string GetFilenameWithoutExtension(FileSystemInfo conflictFile)
        {
            return conflictFile.Name.Substring(0, conflictFile.Name.Length - conflictFile.Extension.Length);
        }

        public static string GetFilenameWithoutExtension(string conflictFile)
        {
            return GetFilenameWithoutExtension(new FileInfo(conflictFile));
        }

        public static void ShallowEraseList<T>(IList<T> bytes)
        {
            if (bytes == null) return;
            for (var i = 0; i < bytes.Count; i++)
                bytes[i] = default(T);
        }

        public static void SafeOverwriteFile(FileInfo file)
        {
            var buffer = new byte[1024/8];
            for (var i = buffer.Length - 1; i >= 0; i--)
                buffer[i] = 0;
            using (var fs = file.OpenWrite())
            {
                for (var i = file.Length/buffer.Length; i >= 0; i--)
                    fs.Write(buffer, 0, buffer.Length);
            }
        }
    }
}