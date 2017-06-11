using System;
using System.Collections.Generic;
using System.Management;

namespace CpuUsageMonitor
{
    public class PerformanceSnapshot
    {
        public List<ProcessSnapshot> ProcessList { get; } = new List<ProcessSnapshot>();

        public PerformanceSnapshot()
        {
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PerfRawData_PerfProc_Process"))
            {
                foreach (var queryObj in searcher.Get())
                {
                    try
                    {
                        var processName = queryObj["Name"].ToString();
                        if (processName.Contains("#"))
                            processName = processName.Substring(0, processName.IndexOf("#", StringComparison.Ordinal));

                        var pss = new ProcessSnapshot();
                        pss.PercentProcessorTime = (ulong)queryObj["PercentProcessorTime"];
                        pss.ProcessId = (uint)queryObj["IDProcess"];
                        pss.ProcessName = processName;
                        pss.Timestamp = (ulong)queryObj["Timestamp_Sys100NS"];
                        ProcessList.Add(pss);
                    }
                    finally
                    {
                        queryObj.Dispose();
                    }
                }
            }
        }
    }

    public class ProcessSnapshot
    {
        public uint ProcessId { get; set; }
        public string ProcessName { get; set; }
        public ulong Timestamp { get; set; }
        public ulong PercentProcessorTime { get; set; }
    }
}
