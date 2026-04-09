using System;
using System.Diagnostics;

namespace ProcessManager.Models
{
    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ProcessPriorityClass Priority { get; set; }
        public long MemoryUsage { get; set; }
        public int ThreadCount { get; set; }
        public TimeSpan CpuTime { get; set; }
        public DateTime? StartTime { get; set; }

        // Display helpers
        public string MemoryUsageMb =>
            (MemoryUsage / 1024.0 / 1024.0).ToString("0.0") + " MB";

        public string CpuTimeStr =>
            CpuTime == TimeSpan.Zero ? "—" :
            $"{(int)CpuTime.TotalHours:D2}:{CpuTime.Minutes:D2}:{CpuTime.Seconds:D2}";

        public string StartTimeStr =>
            StartTime.HasValue ? StartTime.Value.ToString("HH:mm:ss") : "—";

        /// <summary>Подсвечиваем высокоприоритетные процессы</summary>
        public bool IsHighPriority =>
            Priority == ProcessPriorityClass.High ||
            Priority == ProcessPriorityClass.RealTime;
    }
}