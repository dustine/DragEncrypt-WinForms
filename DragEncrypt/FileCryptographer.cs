using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using DragEncrypt.Properties;
using Newtonsoft.Json;

namespace DragEncrypt
{
    public class FileCryptographer
    {
        public byte[] HashedKey { set; private get; }

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
        /// <param name="deleteOriginalSafely"></param>
        public void DecryptFile(FileInfo encryptedFileInfo, bool deleteOriginalSafely = false)
        {
            EncryptionInfo encryptionInfo;
            //Encoding headerEncoding;
            // obtaining original json header
            using (var encryptedFileStream = encryptedFileInfo.OpenText())
            {
                var js = new JsonSerializer {CheckAdditionalContent = false };
                encryptionInfo = (EncryptionInfo) js.Deserialize(encryptedFileStream, typeof (EncryptionInfo));
            }
            // var encryptedPortionLength = encryptedFileInfo.Length - encryptionInfo.EncryptedLength;

            // decrypting the file
            var newFileInfo = new FileInfo(encryptedFileInfo.FullName.Substring(0, 
                encryptedFileInfo.FullName.Length-Settings.Default.Extension.Length));
            using (var tempFiles = new TempFileInfoGenerator())
            {
                //var onlyEncryptedFileInfo = tempFiles.CreateFile();
                var zippedFileInfo = tempFiles.CreateFile();
                long encryptedPortionLength;

                // find the "end" of the JSON header
                // TODO: Make this unhackish again
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
                    } while (!encryptedTs.EndOfStream && curlyBraces > 0 );
                    encryptedPortionLength = position;
                }

                // decrypting to temporary gzipped file
                using (var encryptedFs = encryptedFileInfo.OpenRead())
                using (var crypter = Activator.CreateInstance(encryptionInfo.EncryptionAlgorithm) as SymmetricAlgorithm)
                {
                    // loading cryptography parameters
                    Debug.Assert(crypter != null, "crypter != null");
                    crypter.KeySize = encryptionInfo.KeySize;
                    crypter.BlockSize = encryptionInfo.BlockSize;
                    crypter.Key = HashedKey;
                    crypter.IV = encryptionInfo.Iv;

                    // readying the encrypted file stream to start reading after the json header
                    encryptedFs.Seek(encryptedPortionLength, SeekOrigin.Begin);

                    using (
                        var cs = new CryptoStream(encryptedFs, crypter.CreateDecryptor(),
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
            var newHash = Hash(newFileInfo, encryptionInfo.HashAlgorithm);
            if (newHash.Equals(encryptionInfo.OriginalHash, StringComparison.CurrentCultureIgnoreCase))
                return;
            throw new CryptographicException("Result hash does not match initial hash");
        }

        /// <summary>
        /// Encrypts the given file, using the private hashed key 
        /// </summary>
        /// <param name="originalFileInfo"></param>
        /// <param name="checked"></param>
        public void EncryptFile(FileInfo originalFileInfo, bool deleteOriginalSafely = false)
        {
            var info = new EncryptionInfo();
            // hash original file
            var hash = Hash(originalFileInfo, info.HashAlgorithm);
            var newFileInfo = new FileInfo(originalFileInfo.FullName + Settings.Default.Extension);

            // encrypt original file with info header in the start
            using (var tempFiles = new TempFileInfoGenerator())
            using (var crypter = new AesCryptoServiceProvider())
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
                    info.OriginalHash = hash;
                    info.Iv = crypter.IV;
                    newFileTextStream.Write(JsonConvert.SerializeObject(info));
                }

                // join the two files (encypted and final)
                using (var encryptedStream = encryptedFileInfo.OpenRead())
                using (var newFileStream = newFileInfo.Open(FileMode.Append, FileAccess.Write))
                {
                    encryptedStream.CopyTo(newFileStream);
                }
            }
            if (!deleteOriginalSafely) return;
            var length = originalFileInfo.Length;
            //using (var )
            //using (var originalFileStream = originalFileInfo.OpenWrite())
            //{
            //    var stream = new stream
            //    originalFileStream.Write();
            //}
        }

        /// <summary>
        /// Hashes the given file under SHA256
        /// </summary>
        /// <param name="file">The file to obtain the fash from</param>
        /// <param name="hashAlgorithm"></param>
        /// <returns>The hash, written a sequence of hexadecimal digits duplets</returns>
        private static string Hash(FileInfo file, Type hashAlgorithm)
        {
            using (var fileStream = file.OpenRead())
            using (var hasher = Activator.CreateInstance(hashAlgorithm) as HashAlgorithm)
            {
                Debug.Assert(hasher != null, "hasher != null");
                var hash = hasher.ComputeHash(fileStream);
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
        }
    }
}