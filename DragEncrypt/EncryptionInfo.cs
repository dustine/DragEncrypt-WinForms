using System;

namespace DragEncrypt
{
    public class EncryptionInfo
    {
        public string Version { get; set; }
        public Type HashAlgorithm { get; set; }
        public Type EncryptionAlgorithm { get; set; }
        public int KeySize { get; set; }
        public int BlockSize { get; set; }
        public int SaltSize { get; set; }
        public string OriginalHash { get; set; }
        public byte[] Salt { get; set; }
        public byte[] Iv { get; set; }
    }
}