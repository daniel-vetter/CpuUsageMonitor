using System;
using System.Collections.Generic;
using System.Linq;

namespace CpuUsageMonitor
{
    public class PerformanceReport
    {
        public PerformanceReport(PerformanceSnapshot firstSnapshot, PerformanceSnapshot secondSnapshot)
        {
            var sn2Index = secondSnapshot.ProcessList.Where(x => x.ProcessId != 0).ToDictionary(x => x.ProcessId);
            var reports = new List<ProcessReport>();
            foreach (var p1 in firstSnapshot.ProcessList)
            {
                if (!sn2Index.ContainsKey(p1.ProcessId))
                    continue;
                var p2 = sn2Index[p1.ProcessId];

                var cpuUsage = (p2.PercentProcessorTime - p1.PercentProcessorTime) /
                               (double) (p2.Timestamp - p1.Timestamp) / Environment.ProcessorCount * 100.0;

                var sd = new ProcessReport();
                sd.ProcessId = p2.ProcessId;
                sd.Name = p2.ProcessName;
                sd.PercentProcessorTime = cpuUsage;
                reports.Add(sd);
            }

            ProcessList = reports;
        }

        public List<ProcessReport> ProcessList { get; }
    }

    public class ProcessReport
    {
        public uint ProcessId { get; set; }
        public string Name { get; set; }
        public double PercentProcessorTime { get; set; }
    }
}