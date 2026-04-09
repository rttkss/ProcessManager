using ProcessManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProcessManager.Services
{
    public class ProcessService
    {
        // WinAPI для родительского PID
        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            ref PROCESS_BASIC_INFORMATION processInformation,
            int processInformationLength,
            out int returnLength);


        public List<ProcessInfo> GetProcesses()
        {
            var list = new List<ProcessInfo>();
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    list.Add(new ProcessInfo
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        Priority = p.PriorityClass,
                        MemoryUsage = p.WorkingSet64,
                        ThreadCount = p.Threads.Count,
                        CpuTime = p.TotalProcessorTime,
                        StartTime = TryGetStartTime(p)
                    });
                }
                catch { /* нет доступа — пропускаем */ }
            }
            return list;
        }

        private DateTime? TryGetStartTime(Process p)
        {
            try { return p.StartTime; }
            catch { return null; }
        }

        public List<ThreadInfo> GetThreads(int processId)
        {
            var process = Process.GetProcessById(processId);
            var list = new List<ThreadInfo>();
            foreach (ProcessThread t in process.Threads)
            {
                list.Add(new ThreadInfo
                {
                    Id = t.Id,
                    Priority = t.PriorityLevel,
                    State = t.ThreadState,
                    WaitReason = t.ThreadState == System.Diagnostics.ThreadState.Wait
                                 ? t.WaitReason.ToString() : ""
                });
            }
            return list;
        }

        public void SetPriority(int processId, ProcessPriorityClass priority)
        {
            Process.GetProcessById(processId).PriorityClass = priority;
        }

        public void KillProcess(int processId)
        {
            Process.GetProcessById(processId).Kill();
        }

        // CPU Affinity 

        public IntPtr GetAffinity(int processId)
        {
            return Process.GetProcessById(processId).ProcessorAffinity;
        }

        public void SetAffinity(int processId, IntPtr mask)
        {
            Process.GetProcessById(processId).ProcessorAffinity = mask;
        }

        public int GetCoreCount()
        {
            return Environment.ProcessorCount;
        }

        // Process Tree

        public int GetParentPid(int processId)
        {
            try
            {
                var p = Process.GetProcessById(processId);
                var info = new PROCESS_BASIC_INFORMATION();
                NtQueryInformationProcess(p.Handle, 0, ref info, Marshal.SizeOf(info), out _);
                return info.InheritedFromUniqueProcessId.ToInt32();
            }
            catch { return -1; }
        }

        public Dictionary<int, List<ProcessInfo>> BuildParentChildMap(List<ProcessInfo> allProcs)
        {
            var allPids = new HashSet<int>();
            foreach (var pi in allProcs) allPids.Add(pi.Id);

            var map = new Dictionary<int, List<ProcessInfo>>();

            foreach (var pi in allProcs)
            {
                int parentPid = GetParentPid(pi.Id);
                if (parentPid <= 0 || !allPids.Contains(parentPid))
                    parentPid = 0;

                if (!map.ContainsKey(parentPid))
                    map[parentPid] = new List<ProcessInfo>();

                map[parentPid].Add(pi);
            }

            return map;
        }
    }
}