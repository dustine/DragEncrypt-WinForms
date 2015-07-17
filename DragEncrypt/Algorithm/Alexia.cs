using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace DragEncrypt.Algorithm
{
    /// <summary>
    /// The zipped encryption version, where the files created by it all have the 1.0.0.0 version.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class Alexia : ICryptographyAlgorithm
    {
        //private SecureTempFileGenerator TempFileGenerator { get; }
        public Version TargettedVersion { get; } = new Version(1,0);

        public byte[] HashKey(string key, EncryptionInfo info)
        {
            var keyGen = info.Salt == null
                ? new Rfc2898DeriveBytes(key, info.SaltSize / 8)
                : new Rfc2898DeriveBytes(key, info.Salt);

            var hashedKey = keyGen.GetBytes(info.KeySize / 8);
            info.Salt = keyGen.Salt;
            return hashedKey;
        }

        public EncryptionInfo GetDefaultEncryptionInfo()
        {
            return new EncryptionInfo
            {
                Version = TargettedVersion.ToString(),
                HashAlgorithm = typeof(SHA256CryptoServiceProvider),
                EncryptionAlgorithm = typeof(AesCryptoServiceProvider),
                KeySize = 256,
                BlockSize = 128,
                SaltSize = 128
            };
        }

        // ReSharper disable once RedundantAssignment
        public FileInfo Encrypt(FileSystemInfo original, string key, EncryptionInfo info)
        {
            // argument testing
            //if (zipped.Attributes.HasFlag(FileAttributes.Directory))
            //    throw new ArgumentException($"{TargettedVersion} cannot deflate directories");
            var originalFile = original as FileInfo;
            if(original == null)
                // TODO(Dustine) Is this exception valid? Like, it feels it may be a catch-all
                throw new ArgumentException($"{TargettedVersion} cannot deflate directories");

            // setup: disregard zipped info
            info = GetDefaultEncryptionInfo();
            info.OriginalHash = Hash(originalFile, info);
            // setup: hash key
            var hashedKey = HashKey(key, info);
            // setup: get target file
            var encrypted = Core.GetNonCollidingFile(original.FullName + Properties.Settings.Default.Extension);

            using (var generator = new SecureTempFileGenerator())
            {
                // zip (deflate)
                var zipped = Deflate(originalFile, generator.CreateFile());
                // encrypt
                encrypted = Encrypt(zipped, hashedKey, info, encrypted);
                Core.ShallowEraseList(hashedKey);
                // delete temp files; generator takes care of that thanks to the using()
            }
            return encrypted;
        }

        public FileSystemInfo Decrypt(FileInfo encrypted, string key, EncryptionInfo info)
        {
            // setup: hash key
            var hashedKey = HashKey(key, info);
            // setup: get target file
            var decrypted = Core.GetNonCollidingFile(encrypted.DirectoryName+'/'+Path.GetFileNameWithoutExtension(encrypted.Name));

            using (var generator = new SecureTempFileGenerator())
            {
                // decrypt
                var zipped = Decrypt(encrypted, hashedKey, info, generator.CreateFile());
                Core.ShallowEraseList(hashedKey);
                // inflate (unzip)
                decrypted = Inflate(zipped, decrypted);
                // delete temp files; generator takes care of that thanks to the using()
            }
            // test hash
            if (!string.Equals(info.OriginalHash, Hash(decrypted, info)))
            {
                throw new CryptographicException("Hash of unencrypted file does not match hash from original file");
            }
            return decrypted;
        }

        internal FileInfo Deflate(FileInfo original, FileInfo target)
        {
            if(original.Attributes.HasFlag(FileAttributes.Directory))
                throw new ArgumentException($"{TargettedVersion} cannot deflate directories");

            var originalFile = new FileInfo(original.FullName);

            // zip zipped file into temporary deflated file
            using (var zippedFs = target.OpenWrite())
            using (var zipper = new GZipStream(zippedFs, CompressionMode.Compress))
            using (var originalFs = originalFile.OpenRead())
                originalFs.CopyTo(zipper);

            return target;
        }

        internal FileInfo Inflate(FileInfo deflated, FileInfo target)
        { 
            // unzip from the temporary file into the final permanent file
            using (var deflatedFs = deflated.OpenRead())
            using (var newFs = target.Open(FileMode.Create, FileAccess.Write))
            using (var zipper = new GZipStream(deflatedFs, CompressionMode.Decompress))
                zipper.CopyTo(newFs);

            return target;
        }

        internal FileInfo Encrypt(FileInfo zipped, byte[] hashedKey, EncryptionInfo info, FileInfo target)
        {
            using (var crypter = new AesCryptoServiceProvider())
            {
                crypter.KeySize = info.KeySize;
                crypter.BlockSize = info.BlockSize;
                crypter.Key = hashedKey;
                crypter.GenerateIV();

                info.Iv = crypter.IV;
                info.EncryptionAlgorithm = typeof (AesCryptoServiceProvider);

                using (var targetFs = target.CreateText())
                {
                    targetFs.Write(JsonConvert.SerializeObject(info));
                }

                // encrypt zipped file into final file, as an append
                using (var originalFs = zipped.OpenRead())
                using (
                    var cs = new CryptoStream(originalFs, crypter.CreateEncryptor(), CryptoStreamMode.Read))
                using (var targetFs = target.Open(FileMode.Open, FileAccess.ReadWrite))
                {
                    targetFs.Seek(0, SeekOrigin.End);
                    cs.CopyTo(targetFs);
                }
            }

            return target;
        }

        public EncryptionInfo GetEncryptionInfo(FileInfo encrypted)
        {
            var encryptedFile = new FileInfo(encrypted.FullName);

            using (var encryptedFs = encryptedFile.OpenText())
            {
                var js = new JsonSerializer { CheckAdditionalContent = false };
                return (EncryptionInfo)js.Deserialize(encryptedFs, typeof(EncryptionInfo));
            }
        }

        internal FileInfo Decrypt(FileInfo encrypted, byte[] hashedKey, EncryptionInfo info, FileInfo target)
        {
            // find the "end" of the JSON header
            var encryptedFile = new FileInfo(encrypted.FullName);
            var encryptedPortionLength = SeekEndOfJsonHeader(encryptedFile);
            // decrypting to temporary gzipped file
            using (var encryptedFs = encryptedFile.OpenRead())
            using (var crypter = new AesCryptoServiceProvider())
            {
                // loading cryptography parameters
                //Debug.Assert(crypter != null, "crypter != null");
                crypter.KeySize = info.KeySize;
                crypter.BlockSize = info.BlockSize;
                crypter.Key = hashedKey;
                crypter.IV = info.Iv;

                // readying the encrypted file stream to start reading after the json header
                encryptedFs.Seek(encryptedPortionLength, SeekOrigin.Begin);

                using (
                    var cs = new CryptoStream(encryptedFs, crypter.CreateDecryptor(),
                        CryptoStreamMode.Read))
                using (var decryptedFs = target.OpenWrite())
                    cs.CopyTo(decryptedFs);
            }

            return target;
        }

        public string Hash(FileSystemInfo original, EncryptionInfo info)
        {
            //info.HashAlgorithm = typeof(SHA256CryptoServiceProvider);
            var originalFile = original as FileInfo;
            if(originalFile == null) throw new ArgumentException($"{TargettedVersion} cannot hash directories");

            using (var fs = originalFile.OpenRead())
            using (var hasher = new SHA256CryptoServiceProvider())
            {
                var hash = hasher.ComputeHash(fs);
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
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
    }
}