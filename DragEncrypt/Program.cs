using System;
using System.Diagnostics;
using System.Windows.Forms;
using DragEncrypt.Properties;

namespace DragEncrypt
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainProcess(args != null && args.Length <= 0? "" : args[0]));
        }
    }
}
