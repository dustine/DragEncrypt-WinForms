using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace DragEncrypt.Decryption
{
    /// <summary>
    /// The original encryption version, where the files created by it all have the 1.0.0.0 version.
    /// </summary>
    public class DecryptAlexia : IDecryptionAlgorithm
    {
        DecryptAlexia()
        {
            TempFileGenerator = new SecureTempFileGenerator("", "tmp");
        }

        private TempFileGenerator TempFileGenerator { get; }
        public Version TargettedVersion { get; } = new Version(1,0,0);

        private const int KeySize = 256;
        private const int SaltSize = 128;

        public byte[] HashKey(string key, EncryptionInfo info)
        {
            byte[] salt;
            var hashedKey = HashKey(key, out salt);
            info.KeySize = KeySize;
            info.SaltSize = SaltSize;
            info.Salt = salt;
            return hashedKey;
        }

        public EncryptionInfo GetDefaultEncryptionInfo()
        {
            return new EncryptionInfo
            {
                Version = "1.0.0.0",
                EncryptionAlgorithm = typeof(AesCryptoServiceProvider),
                HashAlgorithm = typeof(SHA256CryptoServiceProvider),
                KeySize = 256,
                BlockSize = 128,
                SaltSize = 128
            };
        }

        public FileSystemInfo Deflate(FileSystemInfo original)
        {
            if(original.Attributes.HasFlag(FileAttributes.Directory))
                throw new ArgumentException($"{TargettedVersion} cannot deflate directories");

            var originalFile = new FileInfo(original.FullName);
            var zipped = TempFileGenerator.CreateFile();

            // zip original file into temporary deflated file
            using (var zippedFs = zipped.OpenWrite())
            using (var zipper = new GZipStream(zippedFs, CompressionMode.Compress))
            using (var originalFs = originalFile.OpenRead())
                originalFs.CopyTo(zipper);

            return zipped;
        }

        public FileSystemInfo Inflate(FileSystemInfo deflated)
        {
            var inflated = TempFileGenerator.CreateFile();

            // unzip from the temporary file into the final permanent file
            using (var deflatedFs = ((FileInfo)deflated).OpenRead())
            using (var newFs = inflated.Open(FileMode.Create, FileAccess.Write))
            using (var zipper = new GZipStream(deflatedFs, CompressionMode.Decompress))
                zipper.CopyTo(newFs);

            return inflated;
        }

        public FileSystemInfo Encrypt(FileSystemInfo original, byte[] hashedKey, EncryptionInfo info)
        {
            if (original.Attributes.HasFlag(FileAttributes.Directory))
                throw new ArgumentException($"{TargettedVersion} cannot deflate directories");

            var originalFile = new FileInfo(original.FullName);
            var newFile = TempFileGenerator.CreateFile();

            using (var crypter = new AesCryptoServiceProvider())
            {
                crypter.KeySize = info.KeySize;
                crypter.BlockSize = info.BlockSize;
                crypter.Key = hashedKey;
                crypter.GenerateIV();

                info.Iv = crypter.IV;
                info.EncryptionAlgorithm = typeof (AesCryptoServiceProvider);

                using (var newFs = newFile.CreateText())
                {
                    newFs.Write(JsonConvert.SerializeObject(info));
                }

                // encrypt zipped file into final file, as an append
                using (var originalFs = originalFile.OpenRead())
                using (
                    var cs = new CryptoStream(originalFs, crypter.CreateEncryptor(), CryptoStreamMode.Read))
                using (var newFs = newFile.Open(FileMode.Open, FileAccess.ReadWrite))
                {
                    newFs.Seek(0, SeekOrigin.End);
                    cs.CopyTo(newFs);
                }
            }

            return newFile;
        }

        public EncryptionInfo GetEncryptionInfo(FileSystemInfo encrypted)
        {
            var encryptedFile = new FileInfo(encrypted.FullName);

            using (var encryptedFs = encryptedFile.OpenText())
            {
                var js = new JsonSerializer { CheckAdditionalContent = false };
                return (EncryptionInfo)js.Deserialize(encryptedFs, typeof(EncryptionInfo));
            }
        }

        public FileSystemInfo Decrypt(FileSystemInfo encrypted, byte[] hashedKey, EncryptionInfo info)
        {
            var decrypted = TempFileGenerator.CreateFile();
            // find the "end" of the JSON header
            var encryptedFile = new FileInfo(encrypted.FullName);
            var encryptedPortionLength = SeekEndOfJsonHeader(encryptedFile);
            // decrypting to temporary gzipped file
            using (var encryptedFs = encryptedFile.OpenRead())
            using (var crypter = Activator.CreateInstance(info.EncryptionAlgorithm) as SymmetricAlgorithm)
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
                using (var decryptedFs = decrypted.OpenWrite())
                    cs.CopyTo(decryptedFs);
            }

            return decrypted;
        }

        public string Hash(FileInfo original, EncryptionInfo info)
        {
            info.HashAlgorithm = typeof(SHA256CryptoServiceProvider);

            using (var fs = original.OpenRead())
            using (var hasher = new SHA256CryptoServiceProvider())
            {
                var hash = hasher.ComputeHash(fs);
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
        }

        private static byte[] HashKey(string key, out byte[] salt)
        {
            var keyGen = new Rfc2898DeriveBytes(key, SaltSize/8);
            var hashedKey = keyGen.GetBytes(KeySize/8);
            salt = keyGen.Salt;
            return hashedKey;
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