using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Threading;

namespace CpuUsageMonitor
{
    public class Monitor
    {
        private CancellationTokenSource _cancel;
        private Thread _thread;

        public bool IsRunning
        {
            get
            {
                var thread = _thread;
                if (thread == null)
                    return false;
                return thread.IsAlive;
            }
        }

        public long MeasurementCount { get; private set; }
        public long FileSize { get; private set; }
        public long RecordCount { get; private set; }
        public Exception Error { get; private set; }
        public string DbFile { get; set; }

        public void Start()
        {
            DbFile = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName, "data.db");

            if (!File.Exists(DbFile))
            {
                var connection = new SQLiteConnection("Data Source=" + DbFile);
                connection.Open();
                var cmd = new SQLiteCommand(connection);
                cmd.CommandText = "CREATE TABLE ProcessDetails (" +
                                  "    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                  "    MeasurementId INTERGER NOT NULL, " +
                                  "    ProcessId NUMBER NOT NULL, " +
                                  "    ProcessName TEXT NOT NULL, " +
                                  "    ProcessorTime INTEGER NOT NULL" +
                                  ");";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE Measurements (" +
                                  "    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                  "    TimeStamp DATETIME NOT NULL" +
                                  ");";
                cmd.ExecuteNonQuery();
                connection.Close();
            }
            else
            {
                var connection = new SQLiteConnection("Data Source=" + DbFile);
                connection.Open();
                var cmd = new SQLiteCommand(connection);
                cmd.CommandText = "SELECT COUNT(*) FROM Measurements";
                MeasurementCount = (long)cmd.ExecuteScalar();
                cmd.CommandText = "SELECT COUNT(*) FROM ProcessDetails";
                RecordCount = (long)cmd.ExecuteScalar();
                connection.Close();
            }

            _cancel = new CancellationTokenSource();
            _thread = new Thread(WorkProc);
            _thread.Start();
        }

        private void WorkProc()
        {
            while (!_cancel.IsCancellationRequested)
            {
                try
                {
                    Measure();
                }
                catch (Exception e)
                {
                    Error = e;
                }
                _cancel.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
            }
        }

        private void Measure()
        {
            var connection = new SQLiteConnection("Data Source=" + DbFile);
            connection.Open();

            try
            {
                using (var transaction = connection.BeginTransaction())
                {
                    var cmd = new SQLiteCommand(connection);
                    cmd.CommandText = "INSERT INTO Measurements (TimeStamp) VALUES (@TimeStamp)";
                    cmd.Parameters.AddWithValue("@TimeStamp", DateTime.Now);
                    cmd.ExecuteNonQuery();
                    var measurementId = connection.LastInsertRowId;

                    var snapshot1 = new PerformanceSnapshot();
                    Thread.Sleep(1000);
                    var snapshot2 = new PerformanceSnapshot();
                    var report = new PerformanceReport(snapshot1, snapshot2);

                    foreach (var process in report.ProcessList)
                    {
                        cmd = new SQLiteCommand(connection);
                        cmd.CommandText =
                            "INSERT INTO ProcessDetails (MeasurementId, ProcessId, ProcessName, ProcessorTime) VALUES (@MeasurementId, @ProcessId, @ProcessName, @ProcessorTime)";
                        cmd.Parameters.AddWithValue("@MeasurementId", measurementId);
                        cmd.Parameters.AddWithValue("@ProcessId", process.ProcessId);
                        cmd.Parameters.AddWithValue("@ProcessName", process.Name);
                        cmd.Parameters.AddWithValue("@ProcessorTime", process.PercentProcessorTime);
                        cmd.ExecuteNonQuery();
                        RecordCount++;
                    }

                    MeasurementCount++;
                    transaction.Commit();
                }
            }
            finally
            {
                connection.Close();
                connection.Dispose();
            }
            
            FileSize = new FileInfo(DbFile).Length;
        }

        public void Stop()
        {
            _cancel.Cancel();
            _thread.Join();
        }
    }
}