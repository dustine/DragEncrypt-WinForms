using System;
using System.IO;

namespace DragEncrypt.Decryption
{
    /// <summary>
    /// The original encryption version, where the files created by it all have the 1.0.0.0 version.
    /// </summary>
    public class DecryptAlexia : IDecryptionAlgorithm
    {
        #region Singleton usage DecryptAlexia.Instance 

        static DecryptAlexia()
        {
        }

        private DecryptAlexia()
        {
            TargettedVersion = new Version(1,0,0);
        }

        public static DecryptAlexia Instance { get; } = new DecryptAlexia();

        #endregion

        public Version TargettedVersion { get; }

        public byte[] HashKey(EncryptionInfo info, string key)
        {
            throw new System.NotImplementedException();
        }

        public FileSystemInfo Encrypt(FileSystemInfo originalFile, string key)
        {
            throw new System.NotImplementedException();
        }

        public FileSystemInfo Encrypt(FileSystemInfo originalFile, string key, EncryptionInfo method)
        {
            throw new System.NotImplementedException();
        }
        
        public FileSystemInfo Decrypt(FileSystemInfo encryptedFile, string key)
        {
            throw new System.NotImplementedException();
        }
    }
}