using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace DragEncrypt
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    public class EncryptionInfo
    {
        //public long EncryptedLength { get; set; }

        public string Version { get; set; }

        public Type HashAlgorithm { get; set; }

        public Type EncryptionAlgorithm { get; set; }

        public int KeySize { get; set; }

        public int BlockSize { get; set; }

        public string OriginalHash { get; set; }

        public byte[] Iv { get; set; }

        public EncryptionInfo()
        {
            Version = Application.ProductVersion;
            HashAlgorithm = typeof(SHA256CryptoServiceProvider);
            EncryptionAlgorithm = typeof (AesCryptoServiceProvider);
            KeySize = 256;
            BlockSize = 128;
        }
    }
}