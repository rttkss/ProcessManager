using System.Diagnostics;

namespace ProcessManager.Models
{
    public class ThreadInfo
    {
        public int Id { get; set; }
        public ThreadPriorityLevel Priority { get; set; }
        public System.Diagnostics.ThreadState State { get; set; }
        public string WaitReason { get; set; }
    }
}