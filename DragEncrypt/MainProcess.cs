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
        private readonly FileCryptographer _fileCryptographer = new FileCryptographer();
        private FileInfo _encryptedFileInfo;

        private FileInfo EncryptedFileInfo
        {
            get { return _encryptedFileInfo; }
            set
            {
                _encryptedFileInfo = value;
                if (_encryptedFileInfo == null)
                {
                    submitButton.Enabled = false;
                    filePathLabel.Text = "";
                }
                else
                {
                    submitButton.Enabled = true;
                    filePathLabel.Text = _encryptedFileInfo.FullName;
                    ChangeSubmitButtonText();
                }
            }
        }

        private void ChangeSubmitButtonText()
        {
            submitButton.Text = FileCryptographer.IsEncrypted(EncryptedFileInfo) ? Resources.MainProcess_MainProcess_Decrypt : Resources.MainProcess_MainProcess_Encrypt;
        }

        public MainProcess(string fileLocation)
        {
            InitializeComponent();
            if (String.IsNullOrWhiteSpace(fileLocation))
            {
                EncryptedFileInfo = PickTargetFile();
            }
        }
        private static FileInfo PickTargetFile()
        {
            string fileLocation;
            using (var openFile = new OpenFileDialog()
            {
                Multiselect = false,
                CheckPathExists = true,
                CheckFileExists = true,
                Title = Resources.Program_Main_Select_Target_File
            })
            {
                openFile.ShowDialog();
                fileLocation = openFile.FileName;
            }
            return String.IsNullOrWhiteSpace(fileLocation) ? null : new FileInfo(fileLocation);
        }

        private void aboutLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            new AboutBox().ShowDialog();
        }

        private void changeFileButton_Click(object sender, EventArgs e)
        {
            EncryptedFileInfo = PickTargetFile();
        }

        private void HidePassword()
        {
            showPasswordHoldButton.BackColor = SystemColors.Control;
            showPasswordHoldButton.Image = Resources.black_eye16;
            passwordBox.UseSystemPasswordChar = true;
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
                _fileCryptographer.HashedKey = hasher.ComputeHash(Encoding.Unicode.GetBytes(passwordBox.Text));
                passwordBox.Text = null;
            }

            // lock interface
            mainTableLayoutPanel.Enabled = false;

            // hide button, show progress bar
            submitButton.Visible = false;
            progressBar.Visible = true;

            // start decryption/encryption
            if (FileCryptographer.IsEncrypted(EncryptedFileInfo))
            {
                Task.Factory.StartNew(() => _fileCryptographer.DecryptFile(EncryptedFileInfo));
            }
            else
            {
                Task.Factory.StartNew(() => _fileCryptographer.EncryptFile(EncryptedFileInfo));
            }
        }

        private void ShowPassword()
        {
            showPasswordHoldButton.BackColor = SystemColors.WindowText;
            showPasswordHoldButton.Image = Resources.white_eye16;
            passwordBox.UseSystemPasswordChar = false;
        }

        private void showPasswordHoldButton_KeyDown(object sender, KeyEventArgs e)
        {
            ShowPassword();
        }

        private void showPasswordHoldButton_KeyUp(object sender, KeyEventArgs e)
        {
            HidePassword();
        }

        private void showPasswordHoldButton_MouseDown(object sender, MouseEventArgs e)
        {
            ShowPassword();
        }
        private void showPasswordHoldButton_MouseUp(object sender, MouseEventArgs e)
        {
            HidePassword();
        }
    }
}