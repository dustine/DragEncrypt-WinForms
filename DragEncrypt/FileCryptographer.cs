using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using DragEncrypt.Decryption;
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
                .Where(t => typeof (IDecryptionAlgorithm).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && !t.IsInterface)
                .Select(t => (IDecryptionAlgorithm) Activator.CreateInstance(t))
                .OrderByDescending(a => new Tuple<int, int>(a.TargettedVersion.Major, a.TargettedVersion.Minor));

            CurrentAlgorithm = DecryptionAlgorithms.First(a => a.TargettedVersion.Major == currentVersion.Major &&
                                                               a.TargettedVersion.Minor == currentVersion.Minor);
        }

        public IDecryptionAlgorithm CurrentAlgorithm { get; private set; }

        public IEnumerable<IDecryptionAlgorithm> DecryptionAlgorithms { get; private set; }

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
            var decrypted = algorithm.Decrypt(encryptedFile, key, info);
            var decryptedFile = decrypted as FileInfo;
            if (decryptedFile != null)
            {
                var newFile = Core.GetNonCollidingFile(encryptedFile.DirectoryName + '/' + Core.GetFilenameWithoutExtension(encryptedFile));
                File.Copy(decryptedFile.FullName, newFile.FullName);
                Core.SafeOverwriteFile(decryptedFile);
                return newFile;
            }
            else if (decrypted is DirectoryInfo)
            {

            }
            return null;
        }

        /// <summary>
        ///     Encrypts the given file, using the private hashed hashedKey
        /// </summary>
        /// <param name="originalFile"></param>
        /// <param name="key"></param>
        /// <param name="deleteOriginalSafely"></param>
        public FileInfo EncryptFile(FileInfo originalFile, string key, bool deleteOriginalSafely = false)
        {
            if (originalFile == null) throw new ArgumentNullException(nameof(originalFile));
            if (!originalFile.Exists) throw new ArgumentException($"{nameof(originalFile)} points to a non-existant file");
            if (Directory.Exists(originalFile.FullName)) throw new ArgumentException($"{nameof(originalFile)} points to a folder, not a file");
            // ready encryption hashedKey and info
            var info = new EncryptionInfo
            {
                Version = "1.0.0",
                HashAlgorithm = typeof (SHA256CryptoServiceProvider),
                EncryptionAlgorithm = typeof (AesCryptoServiceProvider),
                KeySize = 256,
                BlockSize = 128,
                SaltSize = 128
            };
            byte[] hashedKey;
            EncryptKey(key, info, out hashedKey);

            // hash original file
            var hash = Hash(originalFile, info.HashAlgorithm);
            var newFile = Core.GetNonCollidingFile(originalFile.FullName + Settings.Default.Extension);

            // encrypt original file with info header in the start
            using (var tempFiles = new TempFileGenerator())
            using (var crypter = new AesCryptoServiceProvider())
            {
                // load hashedKey and IV into cryptography service
                crypter.Key = hashedKey;
                crypter.GenerateIV();
                //Debug.Assert(crypter.ValidKeySize(256));

                // create temporary files
                var zippedFile = tempFiles.CreateFile();

                // zip original file into temporary zipped file
                using (var zippedFs = zippedFile.OpenWrite())
                using (var zipper = new GZipStream(zippedFs, CompressionMode.Compress))
                using (var originalFs = originalFile.OpenRead())
                    originalFs.CopyTo(zipper);
                //progressBar.BeginInvoke(new Action(() => { progressBar.Increment(5); }));

                // add Json header to final file with a text stream
                using (var newFs = newFile.CreateText())
                {
                    info.OriginalHash = hash;
                    info.Iv = crypter.IV;
                    newFs.Write(JsonConvert.SerializeObject(info));
                }

                // encrypt zipped file into final file, as an append
                using (var zippedFs = zippedFile.OpenRead())
                using (
                    var cs = new CryptoStream(zippedFs, crypter.CreateEncryptor(), CryptoStreamMode.Read))
                using (var newFs = newFile.Open(FileMode.Open, FileAccess.ReadWrite))
                {
                    newFs.Seek(0, SeekOrigin.End);
                    cs.CopyTo(newFs);
                }

                // delete hashed key
                Core.ShallowEraseList(hashedKey);

                // safely delete the zipped file, as it shouldn't stay in the OS like that
                Core.SafeOverwriteFile(zippedFile);
                zippedFile.Delete();
            }
            newFile = new FileInfo(newFile.FullName);

            // safe deleting of the original file
            if (!deleteOriginalSafely) return newFile;
            Core.SafeOverwriteFile(originalFile);
            originalFile.Delete();
            return newFile;
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