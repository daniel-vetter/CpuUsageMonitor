using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CpuUsageMonitor
{
    public partial class ExportForm : Form
    {
        private readonly Monitor _monitor;

        public ExportForm(Monitor monitor)
        {
            _monitor = monitor;
            InitializeComponent();
        }

        private void Export_Load(object sender, EventArgs e)
        {
            dateTimePickerStart.Value = DateTime.Now.Date;
            dateTimePickerEnd.Value = DateTime.Now.Date;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            var dlg = new SaveFileDialog();
            dlg.Filter = @"CSV Datei (*.csv)|*.csv";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ExportWorkProc(_monitor.DbFile, dlg.FileName, dateTimePickerStart.Value, dateTimePickerEnd.Value);
                Close();
            }
        }

        public void ExportWorkProc(string dbFile, string targetFile, DateTime startDay, DateTime endDay)
        {
            using (var connection = new SQLiteConnection("Data Source=" + dbFile))
            {
                connection.Open();

                var processNames = new List<string>();
                SQLiteCommand cmd = new SQLiteCommand(connection);
                cmd.CommandText = "SELECT DISTINCT ProcessName FROM ProcessDetails ORDER BY ProcessName COLLATE NOCASE";
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        processNames.Add(reader.GetString(0));


                var columns = string.Join(", ", processNames.Select(x => $"SUM(CASE WHEN ProcessName = '{x}' THEN ProcessorTime ELSE 0 END) as '{x}'").ToArray());

                cmd.CommandText =
                    "SELECT TimeStamp, " +
                    "SUM(ProcessorTime), " +
                    columns +
                    "FROM Measurements " +
                    "INNER JOIN ProcessDetails ON Measurements.Id = ProcessDetails.MeasurementId " +
                    "WHERE TimeStamp >= @startDay AND TimeStamp < @endDay " + 
                    "GROUP BY Measurements.Id " +
                    "ORDER BY TimeStamp ASC";

                cmd.Parameters.AddWithValue("@startDay", startDay.Date);
                cmd.Parameters.AddWithValue("@endDay", endDay.Date.AddDays(1).Date);

                if (File.Exists(targetFile))
                    File.Delete(targetFile);
                using (var file = File.CreateText(targetFile))
                using (var reader = cmd.ExecuteReader())
                {
                    file.WriteLine(string.Join("\t", Enumerable.Range(0, reader.FieldCount).Select(x => reader.GetName(x)).ToArray()));

                    while (reader.Read())
                    {
                        var sb = new StringBuilder();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            object value = reader.GetValue(i);
                            string valStr = value.ToString();
                            sb.Append(valStr);
                            if (i != reader.FieldCount - 1)
                                sb.Append("\t");
                        }
                        file.WriteLine(sb.ToString());
                    }
                }

                connection.Close();
                connection.Dispose();
            }
        }
    }
}
