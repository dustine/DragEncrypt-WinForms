using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace DragEncrypt.Algorithm
{
    // ReSharper disable once UnusedMember.Global
    public class Alice: ICryptographyAlgorithm
    {
        public Version TargettedVersion { get; } = new Version("1.1.0");

        private readonly Alexia _alexia = new Alexia();
        public byte[] HashKey(string key, EncryptionInfo info)
        {
            return _alexia.HashKey(key, info);
        }

        public EncryptionInfo GetDefaultEncryptionInfo()
        {
            var defaultEncryption = _alexia.GetDefaultEncryptionInfo();
            defaultEncryption.Version = TargettedVersion.ToString();
            return defaultEncryption;
        }

        public FileInfo Encrypt(FileSystemInfo original, string key, EncryptionInfo info)
        {
            // setup: ignore the given info
            info = GetDefaultEncryptionInfo();
            // setup: hash key
            var hashedKey = _alexia.HashKey(key, info);

            var encrypted = Core.GetNonCollidingFile(original.FullName + Properties.Settings.Default.Extension);
            using (var generator = new SecureTempFileGenerator())
            {
                // specific zipping
                var zipped = Deflate(original, generator.CreateFile());
                // setup(ish): hash the zipped file, not the original
                info.OriginalHash = Hash(zipped, info);
                // alexia's (private) encryption
                encrypted = _alexia.Encrypt(zipped, hashedKey, info, encrypted);
                Core.ShallowEraseList(hashedKey);
            }
            return encrypted;
        }

        public FileSystemInfo Decrypt(FileInfo encrypted, string key, EncryptionInfo info)
        {
            // setup: hash key
            var hashedKey = HashKey(key, info);
            // setup: get zipped file
            FileSystemInfo decrypted = Core.GetNonCollidingFile(encrypted.DirectoryName + '/' + Path.GetFileNameWithoutExtension(encrypted.Name));

            using (var generator = new SecureTempFileGenerator())
            {
                // decrypt
                var zipped = _alexia.Decrypt(encrypted, hashedKey, info, generator.CreateFile());
                Core.ShallowEraseList(hashedKey);
                // test the hash right here
                if (!string.Equals(info.OriginalHash, Hash(zipped, info)))
                {
                    throw new CryptographicException("Hash of unencrypted file does not match hash from original file");
                }

                // inflate (unzip)
                decrypted = Inflate(zipped, decrypted);
                // delete temp files; generator takes care of that thanks to the using()
            }
            
            return decrypted;
        }

        private FileSystemInfo Inflate(FileInfo zipped, FileSystemInfo decrypted)
        {
            var dummyDir = Core.GetNonCollidingDirectory("dummy");
            ZipFile.CreateFromDirectory(dummyDir.FullName, zipped.FullName);

            ZipFile.ExtractToDirectory(zipped.FullName, dummyDir.FullName);
            // move files to correct position, preventing overlaps
            return decrypted;
        }

        private FileInfo Deflate(FileSystemInfo original, FileInfo zipped)
        {
            // delete the created temp file, it's existence is troublesome
            File.Delete(zipped.FullName);
            // if a directory, just create it...?
            if (original.Attributes.HasFlag(FileAttributes.Directory))
            {
                ZipFile.CreateFromDirectory(original.FullName, zipped.FullName);
                return new FileInfo(zipped.FullName);
            }
            var dummyDir = Core.GetNonCollidingDirectory("dummy");
            ZipFile.CreateFromDirectory(dummyDir.FullName, zipped.FullName);
            Directory.Delete(dummyDir.FullName);

            using (var zippedArchive = new ZipArchive(zipped.Open(FileMode.Open, FileAccess.ReadWrite),ZipArchiveMode.Update))
            {
                zippedArchive.CreateEntryFromFile(original.FullName, original.Name);
            }
            return new FileInfo(zipped.FullName);
        }

        public EncryptionInfo GetEncryptionInfo(FileInfo encrypted)
        {
            return _alexia.GetEncryptionInfo(encrypted);
        }

        public string Hash(FileSystemInfo original, EncryptionInfo info)
        {
            return _alexia.Hash(original, info);
        }
    }
}
