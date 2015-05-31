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
            DecryptionAlgorithms = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof (ICryptographyAlgorithm).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && !t.IsInterface)
                .Select(t => (ICryptographyAlgorithm) Activator.CreateInstance(t))
                .OrderByDescending(a => new Tuple<int, int>(a.TargettedVersion.Major, a.TargettedVersion.Minor));

            CurrentAlgorithm = DecryptionAlgorithms.First(a => a.TargettedVersion.Major == currentVersion.Major &&
                                                               a.TargettedVersion.Minor == currentVersion.Minor);
        }

        public ICryptographyAlgorithm CurrentAlgorithm { get; private set; }

        public IEnumerable<ICryptographyAlgorithm> DecryptionAlgorithms { get; private set; }

        /// <summary>
        ///     Tries to decrypt the file <paramref name="encryptedFile"/>, using the private hashed hashedKey
        /// </summary>
        /// <param name="encryptedFile"></param>
        /// <param name="key"></param>
        public FileSystemInfo DecryptFile(FileInfo encryptedFile, string key)
        {
            if (encryptedFile == null) throw new ArgumentNullException(nameof(encryptedFile));
            if (!encryptedFile.Exists) throw new FileNotFoundException(encryptedFile.FullName);
            if (key == null) throw new ArgumentNullException(nameof(key));
            // ready encryption hashedKey and info
            EncryptionInfo info = null;
            // obtaining original header
            foreach (var da in DecryptionAlgorithms)
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
                DecryptionAlgorithms.FirstOrDefault(a => a.TargettedVersion.Major == targetVersion.Major &&
                                                         a.TargettedVersion.Minor == targetVersion.Minor);

            // decrypting the file
            return algorithm.Decrypt(encryptedFile, key, info);
        }

        /// <summary>
        ///     Encrypts the given file, using the private hashed hashedKey
        /// </summary>
        /// <param name="original"></param>
        /// <param name="key"></param>
        /// <param name="deleteOriginalSafely"></param>
        public FileInfo EncryptFile(FileSystemInfo original, string key, bool deleteOriginalSafely = false)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (!original.Exists) throw new FileNotFoundException(original.FullName);
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

        /// <summary>
        ///     Hashes the given file under SHA256
        /// </summary>
        /// <param name="file">The file to obtain the fash from</param>
        /// <param name="hashAlgorithm"></param>
        /// <returns>The hash, written a sequence of hexadecimal digits duplets</returns>
        internal static string Hash(FileInfo file, Type hashAlgorithm)
        {
            using (var fs = file.OpenRead())
            using (var hasher = Activator.CreateInstance(hashAlgorithm) as HashAlgorithm)
            {
                //Debug.Assert(hasher != null, "hasher != null");
                var hash = hasher.ComputeHash(fs);
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
        }

        internal static void EncryptKey(string key, EncryptionInfo info, out byte[] hashedKey)
        {
            var keyGen = info.Salt == null
                ? new Rfc2898DeriveBytes(key, info.SaltSize/8)
                : new Rfc2898DeriveBytes(key, info.Salt);

            hashedKey = keyGen.GetBytes(info.KeySize/8);
            info.Salt = keyGen.Salt;
        }

        public static bool IsEncrypted(FileSystemInfo target)
        {
            return target.Extension.Equals(Settings.Default.Extension, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}