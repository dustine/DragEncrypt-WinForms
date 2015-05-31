using System;
using System.IO;

namespace DragEncrypt.Decryption
{
    public interface IDecryptionAlgorithm
    {
        Version TargettedVersion { get; }
        byte[] HashKey(string key, EncryptionInfo info);
        EncryptionInfo GetDefaultEncryptionInfo();
        FileInfo Encrypt(FileSystemInfo original, string key, EncryptionInfo info);
        FileSystemInfo Decrypt(FileInfo encrypted, string key, EncryptionInfo info);
        EncryptionInfo GetEncryptionInfo(FileInfo encrypted);
    }
}