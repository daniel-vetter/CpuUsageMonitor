using System;
using System.Threading;
using System.Windows.Forms;

namespace CpuUsageMonitor
{
    internal static class Program
    {
        private static readonly Mutex Mutex = new Mutex(true, "{8F6212C4-B22A-45fd-ADDF-72F04E6BDE8F}");

        /// <summary>
        ///     Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            {
                if (Mutex.WaitOne(TimeSpan.Zero, true))
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new MainForm());
                    Mutex.ReleaseMutex();
                }
                else
                {
                    MessageBox.Show(@"Anwendung läuft bereits (Siehe SysTray)", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}