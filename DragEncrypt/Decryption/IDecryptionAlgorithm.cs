using System;
using System.IO;

namespace DragEncrypt.Decryption
{
    internal interface IDecryptionAlgorithm
    {
        Version TargettedVersion { get; }
        byte[] HashKey(string key, EncryptionInfo info);
        EncryptionInfo GetDefaultEncryptionInfo();
        FileSystemInfo Deflate(FileSystemInfo original);
        FileSystemInfo Inflate(FileSystemInfo deflated);
        FileSystemInfo Encrypt(FileSystemInfo original, byte[] hashedKey, EncryptionInfo info);
        EncryptionInfo GetEncryptionInfo(FileSystemInfo encrypted);
        FileSystemInfo Decrypt(FileSystemInfo encrypted, byte[] hashedKey, EncryptionInfo info);
        string Hash(FileInfo original, EncryptionInfo info);

        /// <summary>
        ///     Checks if the file <paramref name="fi"/> is already encrypted or not.
        /// </summary>
        /// <param name="fi">Targetted file</param>
        /// <returns></returns>
        bool IsEncrypted(FileSystemInfo target);
    }
}