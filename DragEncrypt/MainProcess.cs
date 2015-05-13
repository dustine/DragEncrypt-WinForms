using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DragEncrypt.Properties;
using Newtonsoft.Json;

namespace DragEncrypt
{
    public partial class MainProcess : Form
    {
        private byte[] _hashedKey;

        public MainProcess(string fileLocation)
        {
            InitializeComponent();
            insertButton.Click += insertButton_Click;
            var fi = new FileInfo(fileLocation);
            if (IsEncrypted(fi))
            {
                insertButton.Text = Resources.MainProcess_MainProcess_Decrypt;
                insertButton.Click +=
                    (o, ea) => { Task.Factory.StartNew(() => DecryptFile(fi)); };
            }
            else
            {
                insertButton.Text = Resources.MainProcess_MainProcess_Encrypt;
                insertButton.Click +=
                    (o, ea) => { Task.Factory.StartNew(() => EncryptFile(fi)); };
            }
        }

        private static void Error(Exception e)
        {
            MessageBox.Show(e.ToString());
            Application.Exit();
        }

        /// <summary>
        /// Checks if the file fi is already encrypted or not
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        private static bool IsEncrypted(FileSystemInfo fi)
        {
            return fi.Extension.Equals(Settings.Default.Extension, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Tries to decrypt the file fi, using the private hashed key
        /// </summary>
        /// <param name="encryptedFileInfo"></param>
        private void DecryptFile(FileInfo encryptedFileInfo)
        {
            try
            {
                EncryptInfo encryptInfo;
                //Encoding headerEncoding;
                using (var encryptedFileStream = encryptedFileInfo.OpenText())
                {
                    var js = new JsonSerializer {CheckAdditionalContent = false };
                    encryptInfo = (EncryptInfo) js.Deserialize(encryptedFileStream, typeof (EncryptInfo));
                    //headerEncoding = encryptedFileStream.CurrentEncoding;
                }
                // TODO: Find a less hackish way to find the char where to start the decrypt stream
                //var encryptSize = headerEncoding.GetByteCount(JsonConvert.SerializeObject(encryptInfo));
                var encryptSize = encryptedFileInfo.Length - encryptInfo.EncryptedLength;

                var newFileInfo = new FileInfo(encryptedFileInfo.FullName.Substring(0, 
                    encryptedFileInfo.FullName.Length-Settings.Default.Extension.Length));
                using (var tempFiles = new TempFileInfoGenerator())
                {
                    var onlyEncryptedFileInfo = tempFiles.CreateFile();
                    var zippedFileInfo = tempFiles.CreateFile();
                    using (var onlyEncryptedFileStream = onlyEncryptedFileInfo.OpenWrite())
                    using (var encryptedFileStream = encryptedFileInfo.OpenRead())
                    {
                        encryptedFileStream.Seek(encryptSize, SeekOrigin.Begin);
                        encryptedFileStream.CopyTo(onlyEncryptedFileStream);
                    }

                    // decrypt to temporary gzipped file
                    //using (var onlyEncryptedFileStream = newFileInfo())
                    using (var onlyEncryptedFileStream = onlyEncryptedFileInfo.OpenRead())
                    using (var crypter = new AesManaged())
                    {
                        // load parameters
                        crypter.Key = _hashedKey;
                        crypter.IV = encryptInfo.Iv;

                        using (
                        var cs = new CryptoStream(onlyEncryptedFileStream, crypter.CreateDecryptor(),
                            CryptoStreamMode.Read))
                        using (var zippedFileStream = zippedFileInfo.OpenWrite())
                        {
                            // encryptedFileStream.Seek(encryptSize, SeekOrigin.Begin);
                            // move encrypted stream position after the header
                            cs.CopyTo(zippedFileStream);
                        }
                    }
                    
                    // unzip from the temporary file into the final permanent file
                    using (var zippedFileStream = zippedFileInfo.OpenRead())
                    using (var newFileStream = newFileInfo.Open(FileMode.Create, FileAccess.Write))
                    using (var zipper = new GZipStream(zippedFileStream, CompressionMode.Decompress))
                    {
                        zipper.CopyTo(newFileStream);
                    }
                }

                // check the hash of the final product
                var newHash = Hash(newFileInfo);
                if (newHash.Equals(encryptInfo.Hash, StringComparison.CurrentCultureIgnoreCase))
                    return;
                throw new CryptographicException("Result hash does not match initial hash");
            }
            catch (Exception e)
            {
                Error(e);
            }
        }

        /// <summary>
        /// Encrypts the given file, using the private hashed key 
        /// </summary>
        /// <param name="originalFileInfo"></param>
        private void EncryptFile(FileInfo originalFileInfo)
        {
            try
            {
                // hash original file
                var hash = Hash(originalFileInfo);
                var newFileInfo = new FileInfo(originalFileInfo.FullName + Settings.Default.Extension);

                using (var tempFiles = new TempFileInfoGenerator())
                using (var crypter = new AesManaged())
                {
                    var zippedFileInfo = tempFiles.CreateFile();
                    crypter.Key = _hashedKey;
                    crypter.GenerateIV();

                    //Debug.Assert(crypter.ValidKeySize(256));

                    // zip original file
                    using (var zippedFileStream = zippedFileInfo.OpenWrite())
                    using (var zipper = new GZipStream(zippedFileStream, CompressionMode.Compress))
                    using (var originalFileStream = originalFileInfo.OpenRead())
                    {
                        originalFileStream.CopyTo(zipper);
                        // save encrypted file header
                    }

                    //progressBar.BeginInvoke(new Action(() => { progressBar.Increment(5); }));
                    // encrypt zipped file into encrypted (final) file
                    var encryptedFileInfo = tempFiles.CreateFile();
                    using (var zippedResultFileStream = zippedFileInfo.OpenRead())
                    using (var cs = new CryptoStream(zippedResultFileStream, crypter.CreateEncryptor(), CryptoStreamMode.Read))
                    using (var encryptedStream = encryptedFileInfo.OpenWrite())
                    {
                        cs.CopyTo(encryptedStream);
                    }

                    var encryptedSize = encryptedFileInfo.Length;
                    // add header to encrypted file with text stream
                    using (var newFileTextStream = newFileInfo.CreateText())
                    {
                        var info = new EncryptInfo(encryptedSize, hash, crypter.IV);
                        newFileTextStream.Write(JsonConvert.SerializeObject(info));
                        //newFileTextStream.Flush();
                    }

                    // join the two files
                    using (var encryptedStream = encryptedFileInfo.OpenRead())
                    using (var newFileStream = newFileInfo.Open(FileMode.Append, FileAccess.Write))
                    {
                        encryptedStream.CopyTo(newFileStream);
                    }
                }
            }
            catch (Exception e)
            {
                Error(e);
            }
            Application.Exit();
        }

        /// <summary>
        /// Hashes the given file under SHA256
        /// </summary>
        /// <param name="file">The file to obtain the fash from</param>
        /// <returns>The hash, written a sequence of hexadecimal digits duplets</returns>
        private static string Hash(FileInfo file)
        {
            using (var fileStream = file.OpenRead())
            using (var hasher = new SHA256Managed())
            {
                var hash = hasher.ComputeHash(fileStream);
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
        }
        /// <summary>
        /// Common event handler for pressing the Password Insert button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void insertButton_Click(object sender, EventArgs e)
        {
            // hash password
            using (var hasher = new SHA256Managed())
            {
                _hashedKey = hasher.ComputeHash(Encoding.Unicode.GetBytes(passwordBox.Text));
                passwordBox.Text = null;
            }

            passwordBox.Enabled = false;
            showPasswordHoldButton.Enabled = false;
            insertButton.Enabled = false;
        }

        private void showPasswordHoldButton_MouseDown(object sender, MouseEventArgs e)
        {
            showPasswordHoldButton.BackColor = SystemColors.WindowText;
            showPasswordHoldButton.Image = Resources.white_eye16;
            passwordBox.UseSystemPasswordChar = false;
            progressBar.ForeColor = SystemColors.WindowText;
        }

        private void showPasswordHoldButton_MouseUp(object sender, MouseEventArgs e)
        {
            showPasswordHoldButton.BackColor = SystemColors.Control;
            showPasswordHoldButton.Image = Resources.black_eye16;
            passwordBox.UseSystemPasswordChar = true;
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private class EncryptInfo
        {
            public long EncryptedLength { get; private set; }

            public string Hash { get; private set; }

            public string Version { get; private set; }

            public Dictionary<string, string> Methods { get; private set; }

            public byte[] Iv { get; private set; }

            [JsonConstructor]
            // ReSharper disable once UnusedMember.Local
            private EncryptInfo(long encryptedLength, string hash, string version, Dictionary<string, string> methods, byte[] iv)
            {
                EncryptedLength = encryptedLength;
                Hash = hash;
                Version = version;
                Methods = methods;
                Iv = iv;
            }
            public EncryptInfo(long encryptedLength, string hash, byte[] iv)
            {
                EncryptedLength = encryptedLength;
                Version = Application.ProductVersion;
                Methods = new Dictionary<string, string> {{"Hash", "SHA256"}, {"Encryption", "AES CBC"}};
                Iv = iv;
                Hash = hash;
            }
        }
    }
}