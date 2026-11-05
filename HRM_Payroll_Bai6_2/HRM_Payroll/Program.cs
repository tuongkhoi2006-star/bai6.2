using System;
using System.Windows.Forms;
using HRM_Payroll.GUI;

namespace HRM_Payroll
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
