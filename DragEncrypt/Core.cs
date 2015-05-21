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
        public static FileInfo GetNonCollidingFile(string fileProposal)
        {
            var conflictFile = new FileInfo(fileProposal);
            if (!conflictFile.Exists) return conflictFile;
            for (var i = 1;; i++)
            {
                var target = $"{conflictFile.DirectoryName}/{GetFilenameWithoutExtension(conflictFile)} ({i}){conflictFile.Extension}";
                if (!File.Exists(target)) return new FileInfo(target);
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
    }
}