using System;
using System.IO;

namespace DragEncrypt.Decryption
{
    internal interface IDecryptionAlgorithm
    {
        Version TargettedVersion { get; }
        byte[] HashKey(EncryptionInfo info, string key);
        FileSystemInfo Encrypt(FileSystemInfo originalFile, string key);
        FileSystemInfo Encrypt(FileSystemInfo originalFile, string key, EncryptionInfo method);
        FileSystemInfo Decrypt(FileSystemInfo encryptedFile, string key);
    }
}