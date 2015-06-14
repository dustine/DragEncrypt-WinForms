using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DragEncrypt.Properties;

namespace DragEncrypt
{
    public partial class MainProcess : Form
    {
        private string _targetFileLocation;

        public MainProcess(string fileLocation)
        {
            InitializeComponent();
            Icon = Resources.DrawEncrypt;
            deleteFileCheckBox.Checked = Settings.Default.SafelyDeleteFiles;
            TargetFileLocation = fileLocation;
        }

        private string TargetFileLocation
        {
            get{ return _targetFileLocation;}
            set
            {
                if (value == null)
                {
                    if (TargetFileInfo != null) return;
                    UnknownFunctionFeatures();
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(value) && FileCryptographer.CanProcess(value))
                    {
                        TargetFileInfo = new FileInfo(value);
                        SwapEncryptDecryptFeatures();
                    }
                }
                _targetFileLocation = TargetFileInfo?.FullName;
            }
        }

        private FileInfo TargetFileInfo { get; set; }
            //set
        //    {
        //        if (value == null)
        //        {
        //            if (TargetFileInfo != null) return;
        //            UnknownFunctionFeatures();
        //        }
        //        else
        //        {
        //            TargetFileInfo = value;
        //            SwapEncryptDecryptFeatures();
        //        }
        //    }
        //}

        private void UnknownFunctionFeatures()
        {
            submitButton.Enabled = false;
            optionsGroupBox.Enabled = false;
            filePathLabel.Text = "";
        }

        private void SwapEncryptDecryptFeatures()
        {
            submitButton.Enabled = true;
            optionsGroupBox.Enabled = true;
            filePathLabel.Text = _targetFileLocation;

            if (FileCryptographer.IsEncrypted(TargetFileInfo))
            {
                // options
                deleteFileCheckBox.Enabled = false;
                // submit button text
                submitButton.Text = Resources.MainProcess_MainProcess_Decrypt;
            }
            else
            {
                deleteFileCheckBox.Enabled = true;
                submitButton.Text = Resources.MainProcess_MainProcess_Encrypt;
            }
        }

        private static FileInfo PickTargetFile()
        {
            string fileLocation;
            using (var openFile = new OpenFileDialog
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
            return string.IsNullOrWhiteSpace(fileLocation) ? null : new FileInfo(fileLocation);
        }

        private void aboutLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            new AboutBox().ShowDialog();
        }

        private void changeFileButton_Click(object sender, EventArgs e)
        {
            TargetFileInfo = PickTargetFile();
        }

        private void HidePassword()
        {
            showPasswordHoldButton.BackColor = SystemColors.Control;
            showPasswordHoldButton.Image = Resources.black_eye16;
            passwordBox.UseSystemPasswordChar = true;
        }

        /// <summary>
        ///     Common event handler for pressing the Password Insert button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void insertButton_Click(object sender, EventArgs e)
        {
            //// hash password
            //using (var hasher = new SHA256CryptoServiceProvider())
            //{
            //    _fileCryptographer.HashedKey = hasher.ComputeHash(Encoding.Unicode.GetBytes(passwordBox.Text));
            //    
            //}

            // lock interface
            mainTableLayoutPanel.Enabled = false;

            // hide button, show progress bar
            submitButton.Visible = false;
            progressBar.Visible = true;

            // start decryption/encryption
            var ts = TaskScheduler.FromCurrentSynchronizationContext();
            var key = passwordBox.Text;
            passwordBox.Text = null;

            if (FileCryptographer.IsEncrypted(TargetFileInfo))
            {
                Task.Factory.StartNew(
                    () =>
                        FileCryptographer.Decrypt(TargetFileInfo, key))
                    .ContinueWith(task =>
                    {
                        if (task.Exception != null) task.Exception.Handle(Error);
                        key = null;
                        Close();
                    }, ts);
            }
            else
            {
                Task.Factory.StartNew(
                    () =>
                        FileCryptographer.Encrypt(TargetFileInfo, key, deleteFileCheckBox.Checked))
                    .ContinueWith(task =>
                    {
                        if (task.Exception != null) task.Exception.Handle(Error);
                        key = null;
                        Close();
                    }, ts);
            }
        }

        private void ShowPassword()
        {
            showPasswordHoldButton.BackColor = SystemColors.WindowText;
            showPasswordHoldButton.Image = Resources.white_eye16;
            passwordBox.UseSystemPasswordChar = false;
        }

        private void showPasswordHoldButton_Click(object sender, EventArgs e)
        {
            ShowPassword();
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

        private static bool Error(Exception e)
        {
            MessageBox.Show(e.ToString());
            return true;
        }

        private void deleteFileCheckBox_CheckStateChanged(object sender, EventArgs e)
        {
            Settings.Default.SafelyDeleteFiles = deleteFileCheckBox.Checked;
            Settings.Default.Save();
        }

        private void MainProcess_DragEnter(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void MainProcess_DragDrop(object sender, DragEventArgs e)
        {
            var file = ((string[])e.Data.GetData(DataFormats.FileDrop)).First();
            TargetFileInfo = new FileInfo(file);
        }
    }
}