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

        private static bool IsEncrypted(FileSystemInfo fi)
        {
            return fi.Extension.Equals(Settings.Default.Extension, StringComparison.CurrentCultureIgnoreCase);
        }

        private void DecryptFile(FileInfo encryptedFileInfo)
        {
            throw new NotImplementedException();
        }

        private async void EncryptFile(FileInfo originalFileInfo)
        {
            try
            {
                var hashTask = Task<String>.Factory.StartNew(() =>
                {
                    var result = Hash(originalFileInfo);
                    progressBar.BeginInvoke(new Action(() => { progressBar.Increment(10); }));
                    return result;
                });

                var newFileInfo = new FileInfo(originalFileInfo.FullName + Settings.Default.Extension);
                using (var tempFiles = new TempFileCollection())
                using (var crypter = new AesManaged())
                {
                    var zippedFileLocation = tempFiles.AddExtension("encrypt.tmp");

                    using (var zippedFileStream = new FileStream(zippedFileLocation, FileMode.Create))
                    using (var zipper = new GZipStream(zippedFileStream, CompressionMode.Compress))
                    using (var originalFileStream = originalFileInfo.OpenRead())
                    using (var newFileTextStream = newFileInfo.CreateText())
                    {
                        var zipTask = originalFileStream.CopyToAsync(zipper);
                        Array.Copy(hashedKey, crypter.Key, hashedKey.Length);
                        //Debug.Assert(crypter.ValidKeySize(256));
                        crypter.GenerateIV();
                        var info = new EncryptInfo(await hashTask, crypter.IV);
                        newFileTextStream.Write(JsonConvert.SerializeObject(info));
                        await zipTask;
                    }
                    //MessageBox.Show(newFileInfo.Length.ToString
                    progressBar.BeginInvoke(new Action(() => { progressBar.Increment(5); }));

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

        private string Hash(FileInfo file)
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

        private void insertButton_Click(object sender, EventArgs e)
        {
            passwordBox.Enabled = false;
            showPasswordHoldButton.Enabled = false;
            insertButton.Enabled = false;

            //hashPassword
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
            private readonly string _version = Application.ProductVersion;
            public string Hash { get; private set; }

            public string Version
            {
                get { return _version; }
            }

            public Dictionary<string, string> Methods
            {
                get { return _methods; }
            }

            public byte[] Iv { get; private set; }

            private readonly Dictionary<string,string> _methods = new Dictionary<string, string>()
            {
                {"Hash","SHA256"},
                {"Encryption","AES CBC"}
            };

            public EncryptInfo(string hash, byte[] iv)
            {
                Iv = iv;
                Hash = hash;
            }
        }
    }
}