using System;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("Loan Tracker")]
[assembly: AssemblyDescription("Offline loan and repayment tracker")]
[assembly: AssemblyCompany("Loan Tracker")]
[assembly: AssemblyProduct("Loan Tracker")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace LoanTracker
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                DataStore store = DataStore.ForCurrentUser();
                Application.Run(new MainForm(store, store.Load()));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Loan Tracker could not start.\r\n\r\n" + ex.Message,
                    "Loan Tracker startup error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
