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
using System.Windows.Forms.VisualStyles;
using DragEncrypt.Properties;
using Newtonsoft.Json;

namespace DragEncrypt
{
    public partial class MainProcess : Form
    {
        private readonly FileCryptographer _fileCryptographer;

        public MainProcess(string fileLocation)
        {
            InitializeComponent();
            submitButton.Click += insertButton_Click;
            var fi = new FileInfo(fileLocation);
            _fileCryptographer = new FileCryptographer();
            if (DragEncrypt.FileCryptographer.IsEncrypted(fi))
            {
                submitButton.Text = Resources.MainProcess_MainProcess_Decrypt;
                submitButton.Click +=
                    (o, ea) => { Task.Factory.StartNew(() => FileCryptographer.DecryptFile(fi)); };
            }
            else
            {
                submitButton.Text = Resources.MainProcess_MainProcess_Encrypt;
                submitButton.Click +=
                    (o, ea) => { Task.Factory.StartNew(() => FileCryptographer.EncryptFile(fi)); };
            }
        }

        public FileCryptographer FileCryptographer
        {
            get { return _fileCryptographer; }
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
                FileCryptographer.HashedKey = hasher.ComputeHash(Encoding.Unicode.GetBytes(passwordBox.Text));
                passwordBox.Text = null;
            }

            // lock interface
            passwordBox.Enabled = false;
            showPasswordHoldButton.Enabled = false;
            submitButton.Enabled = false;

            // hide button, show progress bar
            submitButton.Visible = false;
            progressBar.Visible = true;
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

        private void aboutLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            new AboutBox().ShowDialog();
        }
    }
}