using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                insertButton.Click += (o, ea) =>
                {
                    Task.Factory.StartNew(() => DecryptFile(fi), TaskCreationOptions.LongRunning);
                };
            }
            else
            {
                insertButton.Text = "Encrypt";
                insertButton.Click += (o, ea) =>
                {
                    Task.Factory.StartNew(() => EncryptFile(fi), TaskCreationOptions.LongRunning);
                };
            }
            
        }

        private static void Error(Exception e)
        {
            MessageBox.Show(e.ToString());
            Application.Exit();
        }

        private static bool IsEncrypted(FileSystemInfo fi)
        {
            return fi.Extension.Equals(Properties.Settings.Default.Extension, StringComparison.CurrentCultureIgnoreCase);
        }

        private void DecryptFile(FileInfo encryptedFileInfo)
        {
            throw new NotImplementedException();
        }

        private void EncryptFile(FileInfo originalFileInfo)
        {
            try
            {
                var originalHash = Hash(originalFileInfo);
                var newFileInfo = new FileInfo(originalFileInfo.FullName + Properties.Settings.Default.Extension);
                using (var fileStream = newFileInfo.Open(FileMode.Create))
                using (var crypter = new AesManaged())
                {
                    Array.Copy(hashedKey, crypter.Key, hashedKey.Length);
                    Debug.Assert(crypter.ValidKeySize(256));
                    
                }
            }
            catch (Exception e)
            {
                Error(e);
            }
        }

        private string Hash(FileInfo file)
        {
            using (var fileStream = file.Open(FileMode.Open))
            using (var hasher = new SHA256Managed())
            {
                var hash = hasher.ComputeHash(fileStream);
                var sb = new StringBuilder();
                foreach (var b in hash)
                {
                    sb.AppendFormat("{0:x2}", b);
                }

                progressBar.BeginInvoke(new Action(() =>
                {
                    progressBar.Increment((5));
                }));

                return sb.ToString();
            }
        }
        private void insertButton_Click(object sender, EventArgs e)
        {
            //Console.Beep();
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
            showPasswordHoldButton.Image = Properties.Resources.white_eye16;
            passwordBox.UseSystemPasswordChar = false;
            progressBar.ForeColor = SystemColors.WindowText;
        }

        private void showPasswordHoldButton_MouseUp(object sender, MouseEventArgs e)
        {
            showPasswordHoldButton.BackColor = SystemColors.Control;
            showPasswordHoldButton.Image = Properties.Resources.black_eye16;
            passwordBox.UseSystemPasswordChar = true;
        }
    }
}
