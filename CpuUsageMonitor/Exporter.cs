using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace CpuUsageMonitor
{
    internal class Exporter
    {
        public static void Export(string dbFile, string targetFile)
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
                    "GROUP BY Measurements.Id " +
                    "ORDER BY TimeStamp ASC";

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