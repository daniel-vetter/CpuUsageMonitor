using System;
using System.Windows.Forms;

namespace CpuUsageMonitor
{
    public partial class MainForm : Form
    {
        bool _allowClose = false;
        Monitor _monitor = new Monitor();

        public MainForm()
        {
            InitializeComponent();
        }

        private void OnBeendenClicked(object sender, EventArgs e)
        {
            _allowClose = true;
            Close();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_allowClose == false)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                _monitor.Stop();
            }
        }

        private void OnNotifyIconDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            labelStatus.Text = _monitor.IsRunning ? "Läuft" : "Gestoppt";
            labelRecordCount.Text = _monitor.RecordCount.ToString();
            labelMeasurements.Text = _monitor.MeasurementCount.ToString();
            labelFileSize.Text = ByteCountToReadable(_monitor.FileSize);
            buttonError.Visible = _monitor.Error != null;
        }

        private string ByteCountToReadable(long byteCount)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = byteCount;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024.0;
            }
            return String.Format("{0:0.##} {1}", len, sizes[order]);
        }

        private void OnMainFormLoaded(object sender, EventArgs e)
        {
            _monitor.Start();
        }

        private void OnErrorButtonClicked(object sender, EventArgs e)
        {
            MessageBox.Show(_monitor.Error.ToString());
        }

        private void OnExportButtonClicked(object sender, EventArgs e)
        {
            new ExportForm(_monitor).ShowDialog();
        }
    }
}
