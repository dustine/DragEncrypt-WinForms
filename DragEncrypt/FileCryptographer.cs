using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using DragEncrypt.Properties;
using Newtonsoft.Json;

namespace DragEncrypt
{
    public static class FileCryptographer
    {
        /// <summary>
        ///     Checks if the file <paramref name="fi"/> is already encrypted or not.
        /// </summary>
        /// <param name="fi">Targetted file</param>
        /// <returns></returns>
        public static bool IsEncrypted(FileSystemInfo fi)
        {
            return fi.Extension.Equals(Settings.Default.Extension, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        ///     Tries to decrypt the file <paramref name="encryptedFile"/>, using the private hashed hashedKey
        /// </summary>
        /// <param name="encryptedFile"></param>
        /// <param name="key"></param>
        public static FileInfo DecryptFile(FileInfo encryptedFile, string key)
        {
            if (encryptedFile == null) throw new ArgumentNullException(nameof(encryptedFile));
            if (key == null) throw new ArgumentNullException(nameof(key));
            // ready encryption hashedKey and info
            EncryptionInfo info;
            // obtaining original json header
            using (var encryptedFs = encryptedFile.OpenText())
            {
                var js = new JsonSerializer {CheckAdditionalContent = false};
                info = (EncryptionInfo) js.Deserialize(encryptedFs, typeof (EncryptionInfo));
            }
            byte[] hashedKey;
            EncryptKey(key, info, out hashedKey);

            // decrypting the file
            // prevent conflict with any existing file
            var newFile = Core.GetNonCollidingFile(Core.GetFilenameWithoutExtension(encryptedFile));
            using (var tempFiles = new TempFileInfoGenerator())
            {
                var zippedFile = tempFiles.CreateFile();
                // find the "end" of the JSON header
                var encryptedPortionLength = SeekEndOfJsonHeader(encryptedFile);
                // decrypting to temporary gzipped file
                using (var encryptedFs = encryptedFile.OpenRead())
                using (var crypter = Activator.CreateInstance(info.EncryptionAlgorithm) as SymmetricAlgorithm)
                {
                    // loading cryptography parameters
                    Debug.Assert(crypter != null, "crypter != null");
                    crypter.KeySize = info.KeySize;
                    crypter.BlockSize = info.BlockSize;
                    crypter.Key = hashedKey;
                    crypter.IV = info.Iv;

                    // readying the encrypted file stream to start reading after the json header
                    encryptedFs.Seek(encryptedPortionLength, SeekOrigin.Begin);

                    using (
                        var cs = new CryptoStream(encryptedFs, crypter.CreateDecryptor(),
                            CryptoStreamMode.Read))
                    using (var zippedFileStream = zippedFile.OpenWrite())
                        cs.CopyTo(zippedFileStream);
                }

                // delete hashed key
                Core.ShallowEraseList(hashedKey);

                // unzip from the temporary file into the final permanent file
                using (var zippedFs = zippedFile.OpenRead())
                using (var newFs = newFile.Open(FileMode.Create, FileAccess.Write))
                using (var zipper = new GZipStream(zippedFs, CompressionMode.Decompress))
                    zipper.CopyTo(newFs);

                // safely delete the zipped file, as it shouldn't stay in the OS like that
                SafeOverwriteFile(zippedFile);
                zippedFile.Delete();
            }
            // check the hash of the final product, must match to the hash stored in the header
            var newHash = Hash(newFile, info.HashAlgorithm);
            if (newHash.Equals(info.OriginalHash, StringComparison.CurrentCultureIgnoreCase))
                return newFile;
            throw new CryptographicException("Result hash does not match initial hash");
        }

        private static long SeekEndOfJsonHeader(FileInfo encryptedFileInfo)
        {
            // TODO: Make this unhackish again
            long encryptedPortionLength;
            using (var encryptedTs = encryptedFileInfo.OpenText())
            {
                var curlyBraces = 0;
                var position = 0;
                do
                {
                    var buffer = new char[1];
                    encryptedTs.Read(buffer, 0, 1);
                    switch (buffer[0])
                    {
                        case '{':
                            curlyBraces++;
                            break;
                        case '}':
                            curlyBraces--;
                            break;
                    }
                    position++;
                } while (!encryptedTs.EndOfStream && curlyBraces > 0);
                encryptedPortionLength = position;
            }
            return encryptedPortionLength;
        }

        /// <summary>
        ///     Encrypts the given file, using the private hashed hashedKey
        /// </summary>
        /// <param name="originalFile"></param>
        /// <param name="key"></param>
        /// <param name="deleteOriginalSafely"></param>
        public static FileInfo EncryptFile(FileInfo originalFile, string key, bool deleteOriginalSafely = false)
        {
            if (originalFile == null) throw new ArgumentNullException(nameof(originalFile));
            if (!originalFile.Exists) throw new ArgumentException($"{nameof(originalFile)} points to a non-existant file");
            if (Directory.Exists(originalFile.FullName)) throw new ArgumentException($"{nameof(originalFile)} points to a folder, not a file");
            // ready encryption hashedKey and info
            var info = new EncryptionInfo
            {
                Version = Application.ProductVersion,
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
            using (var tempFiles = new TempFileInfoGenerator())
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
                SafeOverwriteFile(zippedFile);
                zippedFile.Delete();
            }

            // safe deleting of the original file
            if (!deleteOriginalSafely) return newFile;
            SafeOverwriteFile(originalFile);
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
                Debug.Assert(hasher != null, "hasher != null");
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

        internal static void SafeOverwriteFile(FileInfo file)
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