using System;

namespace ProcessManager.Utilities
{
    public static class AffinityHelper
    {
        public static bool IsCoreEnabled(IntPtr mask, int coreIndex)
        {
            long v = mask.ToInt64();
            return (v & (1L << coreIndex)) != 0;
        }

        public static IntPtr SetCoreMask(bool[] cores)
        {
            long mask = 0;
            for (int i = 0; i < cores.Length; i++)
                if (cores[i]) mask |= (1L << i);
            return new IntPtr(mask);
        }

        public static string ToBinaryString(IntPtr mask, int coreCount)
        {
            long v = mask.ToInt64();
            string b = Convert.ToString(v, 2).PadLeft(coreCount, '0');
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < b.Length; i += 4)
                parts.Add(b.Substring(i, Math.Min(4, b.Length - i)));
            return string.Join(" ", parts);
        }

        public static string ToHexString(IntPtr mask)
        {
            return "0x" + mask.ToInt64().ToString("X");
        }
    }
}