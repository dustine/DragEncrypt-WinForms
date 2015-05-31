using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragEncrypt.Algorithm
{
    class Brooke: ICryptographyAlgorithm
    {
        public Version TargettedVersion { get; }
        public byte[] HashKey(string key, EncryptionInfo info)
        {
            throw new NotImplementedException();
        }

        public EncryptionInfo GetDefaultEncryptionInfo()
        {
            throw new NotImplementedException();
        }

        public FileInfo Encrypt(FileSystemInfo original, string key, EncryptionInfo info)
        {
            throw new NotImplementedException();
        }

        public FileSystemInfo Decrypt(FileInfo encrypted, string key, EncryptionInfo info)
        {
            throw new NotImplementedException();
        }

        public EncryptionInfo GetEncryptionInfo(FileInfo encrypted)
        {
            throw new NotImplementedException();
        }

        public string Hash(FileInfo original, EncryptionInfo info)
        {
            throw new NotImplementedException();
        }
    }
}
