using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TinehEncryption
{
    public partial class MainProcess : Form
    {
        public MainProcess(string fileLocation)
        {
            InitializeComponent();
            var fi = new FileInfo(fileLocation);
            if (IsEncrypted(fi))
            {

            }
            else
            {
                
            }
            Task.Factory.StartNew(() => EncryptFile(fileLocation), TaskCreationOptions.LongRunning);
        }

        private static bool IsEncrypted(FileSystemInfo fi)
        {
            return fi.Extension.Equals(".encrypted", StringComparison.CurrentCultureIgnoreCase);
        }

        private void EncryptFile(string fileLocation)
        {
            try
            {
                var originalFile = new FileInfo(fileLocation);
                var originalHash = Hash(originalFile);
                
            }
            catch (Exception e)
            {
                Error(e);
            }
        }

        private string Hash(FileInfo file)
        {
            using (var fs = file.Open(FileMode.Open))
            using (var hasher = new SHA256Managed())
            {
                var hash = hasher.ComputeHash(fs);
                var sb = new StringBuilder();
                foreach (var b in hash)
                {
                    sb.AppendFormat("{0:X2}", b);
                }

                progressBar.BeginInvoke(new Action(() =>
                {
                    progressBar.Increment((5));
                }));

                return sb.ToString();
            }
        }

        private static void Error(Exception e)
        {
            MessageBox.Show(e.ToString());
            Application.Exit();
        }

        private void insertButton_Click(object sender, EventArgs e)
        {
            Console.Beep();
        }
    }
}
