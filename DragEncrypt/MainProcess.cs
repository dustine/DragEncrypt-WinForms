using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
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
        private byte[] hashedKey;

        public MainProcess(string fileLocation)
        {
            InitializeComponent();
            insertButton.Click += insertButton_Click;
            var fi = new FileInfo(fileLocation);
            if (IsEncrypted(fi))
            {
                insertButton.Text = "Decrypt";
                insertButton.Click +=
                    (o, ea) => { Task.Factory.StartNew(() => DecryptFile(fi)); };
            }
            else
            {
                insertButton.Text = "Encrypt";
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
                using (var encryptedFileStream = encryptedFileInfo.OpenText())
                {
                    var js = new JsonSerializer {CheckAdditionalContent = false };
                    encryptInfo = (EncryptInfo) js.Deserialize(encryptedFileStream, typeof (EncryptInfo));
                }
                // TODO: Find a less hackish way to find the char where to start the decrypt stream
                var encryptSize = JsonConvert.SerializeObject(encryptInfo).ToCharArray().Length;

                var newFileInfo = new FileInfo(encryptedFileInfo.FullName.Substring(0, 
                    encryptedFileInfo.FullName.Length-Settings.Default.Extension.Length));
                using (var tempFiles = new TempFileCollection())
                
                {
                    var zippedFileLocation = tempFiles.AddExtension("encrypt.tmp");
                    
                    // decrypt to temporary gzipped file
                    using(var encryptedFileStream = encryptedFileInfo.OpenRead())
                    using (var crypter = new AesManaged())
                    using (
                        var cs = new CryptoStream(encryptedFileStream, crypter.CreateDecryptor(),
                            CryptoStreamMode.Read))
                    using (var zippedFileStream = new FileStream(zippedFileLocation, FileMode.Create))
                    {
                        // load parameters
                        Array.Copy(hashedKey, crypter.Key, hashedKey.Length);
                        Array.Copy(encryptInfo.Iv, crypter.IV, encryptInfo.Iv.Length);
                        // move encrypted stream position after the header
                        encryptedFileStream.Position = encryptSize;
                        cs.CopyTo(zippedFileStream);
                    }

                    // unzip from the temporary file into the final permanent file
                    using (var newFileStream = newFileInfo.Open(FileMode.Create, FileAccess.Write))
                    using (var zipper = new GZipStream(newFileStream, CompressionMode.Decompress))
                    {
                        zipper.CopyTo(newFileStream);
                    }
                }
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

                using (var tempFiles = new TempFileCollection())
                using (var crypter = new AesManaged())
                {
                    var zippedFileLocation = tempFiles.AddExtension("encrypt.tmp");
                    Array.Copy(hashedKey, crypter.Key, hashedKey.Length);
                    //Debug.Assert(crypter.ValidKeySize(256));

                    // zip original file
                    using (var zippedFileStream = new FileStream(zippedFileLocation, FileMode.Create))
                    using (var zipper = new GZipStream(zippedFileStream, CompressionMode.Compress))
                    using (var originalFileStream = originalFileInfo.OpenRead())
                    {
                        originalFileStream.CopyTo(zipper);
                        // save encrypted file header
                    }

                    // add header to encrypted file with text stream
                    using (var newFileTextStream = newFileInfo.CreateText())
                    {
                        crypter.GenerateIV();
                        var info = new EncryptInfo(hash, crypter.IV);
                        newFileTextStream.Write(JsonConvert.SerializeObject(info));
                    }
                    //progressBar.BeginInvoke(new Action(() => { progressBar.Increment(5); }));
                    // encrypt zipped file into encrypted (final) file
                    using (var zippedResultFileStream = File.OpenRead(zippedFileLocation))
                    using (var cs = new CryptoStream(zippedResultFileStream, crypter.CreateEncryptor(), CryptoStreamMode.Read))
                    using (var newFileStream = newFileInfo.Open(FileMode.Append,FileAccess.Write))
                    {
                        cs.CopyTo(newFileStream);
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
            passwordBox.Enabled = false;
            showPasswordHoldButton.Enabled = false;
            insertButton.Enabled = false;

            // hash password
            using (var hasher = new SHA256Managed())
            {
                hashedKey = hasher.ComputeHash(Encoding.Unicode.GetBytes(passwordBox.Text));
                passwordBox.Text = null;
            }
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

        private class EncryptInfo
        {
            public string Hash { get; private set; }

            public string Version { get; private set; }

            public Dictionary<string, string> Methods { get; private set; }

            public byte[] Iv { get; private set; }

            [JsonConstructor]
            private EncryptInfo(string hash, string version, Dictionary<string, string> methods, byte[] iv)
            {
                Hash = hash;
                Version = version;
                Methods = methods;
                Iv = iv;
            }
            public EncryptInfo(string hash, byte[] iv)
            {
                Version = Application.ProductVersion;
                Methods = new Dictionary<string, string> {{"Hash", "SHA256"}, {"Encryption", "AES CBC"}};
                Iv = iv;
                Hash = hash;
            }
        }
    }
}