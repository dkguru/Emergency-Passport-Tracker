using Emergency_Passport_Tracker;
using System;
using System.Windows.Forms;

namespace Emergency_Passport_Tracker
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}