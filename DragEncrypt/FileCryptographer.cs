using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using DragEncrypt.Properties;
using Newtonsoft.Json;

namespace DragEncrypt
{
    public class FileCryptographer
    {
        public FileCryptographer()
        {
        }

        public byte[] HashedKey { set; private get; }

        private static void Error(Exception e)
        {
            MessageBox.Show(e.ToString());
            Application.Exit();
        }

        /// <summary>
        /// Checks if the file fi is already encrypted or not
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        public static bool IsEncrypted(FileSystemInfo fi)
        {
            return fi.Extension.Equals(Settings.Default.Extension, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Tries to decrypt the file fi, using the private hashed key
        /// </summary>
        /// <param name="encryptedFileInfo"></param>
        public void DecryptFile(FileInfo encryptedFileInfo)
        {
            try
            {
                EncryptInfo encryptInfo;
                //Encoding headerEncoding;
                // obtaining original json header
                using (var encryptedFileStream = encryptedFileInfo.OpenText())
                {
                    var js = new JsonSerializer {CheckAdditionalContent = false };
                    encryptInfo = (EncryptInfo) js.Deserialize(encryptedFileStream, typeof (EncryptInfo));
                }
                var encryptedPortionLength = encryptedFileInfo.Length - encryptInfo.EncryptedLength;

                // decrypting the file
                var newFileInfo = new FileInfo(encryptedFileInfo.FullName.Substring(0, 
                    encryptedFileInfo.FullName.Length-Settings.Default.Extension.Length));
                using (var tempFiles = new TempFileInfoGenerator())
                {
                    // readying the encrypted file stream to start reading after the json header
                    // for that we create a new temporary file that only contains the encrypted data
                    var onlyEncryptedFileInfo = tempFiles.CreateFile();
                    var zippedFileInfo = tempFiles.CreateFile();
                    using (var onlyEncryptedFileStream = onlyEncryptedFileInfo.OpenWrite())
                    using (var encryptedFileStream = encryptedFileInfo.OpenRead())
                    {
                        // move encrypted stream position after the header
                        encryptedFileStream.Seek(encryptedPortionLength, SeekOrigin.Begin);
                        encryptedFileStream.CopyTo(onlyEncryptedFileStream);
                    }

                    // decrypting to temporary gzipped file
                    using (var onlyEncryptedFileStream = onlyEncryptedFileInfo.OpenRead())
                    using (var crypter = new AesManaged())
                    {
                        // loading cryptography parameters
                        crypter.Key = HashedKey;
                        crypter.IV = encryptInfo.Iv;

                        using (
                            var cs = new CryptoStream(onlyEncryptedFileStream, crypter.CreateDecryptor(),
                                CryptoStreamMode.Read))
                        using (var zippedFileStream = zippedFileInfo.OpenWrite())
                        {
                            cs.CopyTo(zippedFileStream);
                        }
                    }
                    
                    // unzip from the temporary file into the final permanent file
                    using (var zippedFileStream = zippedFileInfo.OpenRead())
                    using (var newFileStream = newFileInfo.Open(FileMode.Create, FileAccess.Write))
                    using (var zipper = new GZipStream(zippedFileStream, CompressionMode.Decompress))
                    {
                        zipper.CopyTo(newFileStream);
                    }
                }

                // check the hash of the final product, must match to the hash stored in the header
                var newHash = Hash(newFileInfo);
                if (newHash.Equals(encryptInfo.Hash, StringComparison.CurrentCultureIgnoreCase))
                    return;
                throw new CryptographicException("Result hash does not match initial hash");
            }
            catch (Exception e)
            {
                Error(e);
            }
        }

        /// <summary>
        /// Encrypts the given file, using the private hashed key 
        /// </summary>
        /// <param name="originalFileInfo"></param>
        public void EncryptFile(FileInfo originalFileInfo)
        {
            try
            {
                // hash original file
                var hash = Hash(originalFileInfo);
                var newFileInfo = new FileInfo(originalFileInfo.FullName + Settings.Default.Extension);

                // encrypt original file with info header in the start
                using (var tempFiles = new TempFileInfoGenerator())
                using (var crypter = new AesManaged())
                {
                    // load key and IV into cryptography service
                    crypter.Key = HashedKey;
                    crypter.GenerateIV();
                    //Debug.Assert(crypter.ValidKeySize(256));

                    // create temporary files
                    var zippedFileInfo = tempFiles.CreateFile();
                    var encryptedFileInfo = tempFiles.CreateFile();

                    // zip original file into temporary zipped file
                    using (var zippedFileStream = zippedFileInfo.OpenWrite())
                    using (var zipper = new GZipStream(zippedFileStream, CompressionMode.Compress))
                    using (var originalFileStream = originalFileInfo.OpenRead())
                    {
                        originalFileStream.CopyTo(zipper);
                    }
                    //progressBar.BeginInvoke(new Action(() => { progressBar.Increment(5); }));

                    // encrypt zipped file into temporary encrypted file
                    using (var zippedResultFileStream = zippedFileInfo.OpenRead())
                    using (var cs = new CryptoStream(zippedResultFileStream, crypter.CreateEncryptor(), CryptoStreamMode.Read))
                    using (var encryptedStream = encryptedFileInfo.OpenWrite())
                    {
                        cs.CopyTo(encryptedStream);
                    }

                    // add Json header to final file with a text stream
                    using (var newFileTextStream = newFileInfo.CreateText())
                    {
                        var info = new EncryptInfo(encryptedFileInfo.Length, hash, crypter.IV);
                        newFileTextStream.Write(JsonConvert.SerializeObject(info));
                    }

                    // join the two files (encypted and final)
                    using (var encryptedStream = encryptedFileInfo.OpenRead())
                    using (var newFileStream = newFileInfo.Open(FileMode.Append, FileAccess.Write))
                    {
                        encryptedStream.CopyTo(newFileStream);
                    }
                }
            }
            catch (Exception e)
            {
                Error(e);
            }
            Application.Exit();
        }

        /// <summary>
        /// Hashes the given file under SHA256
        /// </summary>
        /// <param name="file">The file to obtain the fash from</param>
        /// <returns>The hash, written a sequence of hexadecimal digits duplets</returns>
        private static string Hash(FileInfo file)
        {
            using (var fileStream = file.OpenRead())
            using (var hasher = new SHA256Managed())
            {
                var hash = hasher.ComputeHash(fileStream);
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private class EncryptInfo
        {
            public long EncryptedLength { get; private set; }

            public string Hash { get; private set; }

            public string Version { get; private set; }

            public Dictionary<string, string> Methods { get; private set; }

            public byte[] Iv { get; private set; }

            [JsonConstructor]
            // ReSharper disable once UnusedMember.Local
            private EncryptInfo(long encryptedLength, string hash, string version, Dictionary<string, string> methods, byte[] iv)
            {
                EncryptedLength = encryptedLength;
                Hash = hash;
                Version = version;
                Methods = methods;
                Iv = iv;
            }
            public EncryptInfo(long encryptedLength, string hash, byte[] iv)
            {
                EncryptedLength = encryptedLength;
                Version = Application.ProductVersion;
                Methods = new Dictionary<string, string> {{"Hash", "SHA256"}, {"Encryption", "AES CBC"}};
                Iv = iv;
                Hash = hash;
            }
        }
    }
}