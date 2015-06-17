using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using DragEncrypt.Algorithm;
using DragEncrypt.Properties;
using Newtonsoft.Json;

namespace DragEncrypt
{
    public class FileCryptographer
    {

        public FileCryptographer(Version currentVersion)
        {
            Algorithms = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof (ICryptographyAlgorithm).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && !t.IsInterface)
                .Select(t => (ICryptographyAlgorithm) Activator.CreateInstance(t))
                .OrderByDescending(a => new Tuple<int, int>(a.TargettedVersion.Major, a.TargettedVersion.Minor));

            CurrentAlgorithm = Algorithms.First(a => a.TargettedVersion.Major == currentVersion.Major &&
                                                               a.TargettedVersion.Minor == currentVersion.Minor);
        }

        public ICryptographyAlgorithm CurrentAlgorithm { get; }

        public IEnumerable<ICryptographyAlgorithm> Algorithms { get; }

        /// <summary>
        ///     Tries to decrypt the file <paramref name="encryptedFile"/>, using the private hashed hashedKey
        /// </summary>
        /// <param name="encryptedFile"></param>
        /// <param name="key"></param>
        public FileSystemInfo Decrypt(FileInfo encryptedFile, string key)
        {
            if (encryptedFile == null) throw new ArgumentNullException(nameof(encryptedFile));
            if (!File.Exists(encryptedFile.FullName)) throw new FileNotFoundException(encryptedFile.FullName);
            if (key == null) throw new ArgumentNullException(nameof(key));
            // ready encryption hashedKey and info
            EncryptionInfo info = null;
            // obtaining original header
            foreach (var da in Algorithms)
            {
                try
                {
                    info = da.GetEncryptionInfo(encryptedFile);
            }
                catch (IOException)
            {
                    throw;
                }
                catch (Exception e)
                {
                    // ignored
                }
                if (info != null) break;
                }
            // TODO(Dustine) Throw sensible exception (header can't be obtained)
            if (info == null) return null;

            // finding decryption algorithm
            var targetVersion = new Version(info.Version);
            var algorithm =
                Algorithms.FirstOrDefault(a => a.TargettedVersion.Major == targetVersion.Major &&
                                                         a.TargettedVersion.Minor == targetVersion.Minor);

            // decrypting the file
            if (algorithm != null) return algorithm.Decrypt(encryptedFile, key, info);
            throw new Exception($"Unknown version {targetVersion}");
        }

        /// <summary>
        ///     Encrypts the given file, using the private hashed hashedKey
        /// </summary>
        /// <param name="original"></param>
        /// <param name="key"></param>
        /// <param name="deleteOriginalSafely"></param>
        public FileInfo Encrypt(FileSystemInfo original, string key, bool deleteOriginalSafely = false)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (!File.Exists(original.FullName)) throw new FileNotFoundException(original.FullName);
            if (key == null) throw new ArgumentNullException(nameof(key));

            var encrypted = CurrentAlgorithm.Encrypt(original, key, CurrentAlgorithm.GetDefaultEncryptionInfo());

            // safe deleting of the original file
            if (!deleteOriginalSafely) return encrypted;
            if (original.Attributes.HasFlag(FileAttributes.Directory))
            {
                // folder, so we need to go recursive
                SafeOverwriteFolder(original);
                ((DirectoryInfo) original).Delete(true);
                }
            else
                {
                // if file, just overwrite it
                Core.SafeOverwriteFile(original as FileInfo);
                original.Delete();
            }
            return encrypted;
        }

        internal static void SafeOverwriteFolder(FileSystemInfo directory)
        {
            foreach (
                    var enumerateFile in Directory.EnumerateFiles(directory.FullName, "*", SearchOption.AllDirectories).Select(s => new FileInfo(s)))
            {
                Core.SafeOverwriteFile(enumerateFile);
        }
        }

        public static bool IsEncrypted(FileSystemInfo target)
            {
            return target.Extension.Equals(Settings.Default.Extension, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}