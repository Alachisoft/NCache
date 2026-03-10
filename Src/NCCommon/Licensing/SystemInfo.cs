using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Alachisoft.NCache.Common.Licensing
{

    public static class SystemInfo
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            public ushort wProcessorArchitecture;
            public ushort wReserved;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public IntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort wProcessorLevel;
            public ushort wProcessorRevision;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESSOR_RELATIONSHIP
        {
            public byte Flags;
            public byte EfficiencyClass;
            public byte Reserved1;
            public byte Reserved2;
            public ushort GroupCount;
            public ushort Reserved3;
            public ushort Reserved4;
            public ushort Reserved5;
            public IntPtr GroupMask;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct NUMA_NODE_RELATIONSHIP
        {
            public uint NodeNumber;
            public IntPtr GroupMask;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct CACHE_RELATIONSHIP
        {
            public byte Level;
            public byte Associativity;
            public ushort LineSize;
            public uint CacheSize;
            public ProcessorCacheType Type;
            public IntPtr GroupMask;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct GROUP_RELATIONSHIP
        {
            public ushort MaximumGroupCount;
            public ushort ActiveGroupCount;
            public IntPtr GroupInfo;
        }
        [StructLayout(LayoutKind.Explicit)]
        private struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_UNION
        {
            [FieldOffset(0)]
            public PROCESSOR_RELATIONSHIP Processor;
            [FieldOffset(0)]
            public NUMA_NODE_RELATIONSHIP NumaNode;
            [FieldOffset(0)]
            public CACHE_RELATIONSHIP Cache;
            [FieldOffset(0)]
            public GROUP_RELATIONSHIP Group;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
        {
            public LogicalProcessorRelationship Relationship;
            public int Size;
            public SYSTEM_LOGICAL_PROCESSOR_INFORMATION_UNION Info;
        }
        private enum LogicalProcessorRelationship
        {
            ProcessorCore,
            NumaNode,
            Cache,
            ProcessorPackage,
            Group
        }
        private enum ProcessorCacheType
        {
            Unified,
            Instruction,
            Data,
            Trace
        }
        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetLogicalProcessorInformationEx(
            LogicalProcessorRelationship relationship,
            IntPtr buffer,
            ref int returnedLength);
        public static int GetNumProcessors()
        {
            int returnLength = 0;
            GetLogicalProcessorInformationEx(LogicalProcessorRelationship.ProcessorPackage, IntPtr.Zero, ref returnLength);
            IntPtr ptr = Marshal.AllocHGlobal(returnLength);
            try
            {
                if (!GetLogicalProcessorInformationEx(LogicalProcessorRelationship.ProcessorPackage, ptr, ref returnLength))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
                int packageCount = 0;
                IntPtr current = ptr;
                while (current.ToInt64() < ptr.ToInt64() + returnLength)
                {
                    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX info = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(current);
                    if (info.Relationship == LogicalProcessorRelationship.ProcessorPackage)
                    {
                        packageCount++;
                    }
                    current = IntPtr.Add(current, info.Size);
                }
                return packageCount;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        public static int GetNumCores()
        {
            int returnLength = 0;
            GetLogicalProcessorInformationEx(LogicalProcessorRelationship.ProcessorCore, IntPtr.Zero, ref returnLength);
            IntPtr ptr = Marshal.AllocHGlobal(returnLength);
            try
            {
                if (!GetLogicalProcessorInformationEx(LogicalProcessorRelationship.ProcessorCore, ptr, ref returnLength))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
                int coreCount = 0;
                IntPtr current = ptr;
                while (current.ToInt64() < ptr.ToInt64() + returnLength)
                {
                    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX info = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(current);
                    if (info.Relationship == LogicalProcessorRelationship.ProcessorCore)
                    {
                        coreCount++;
                    }
                    current = IntPtr.Add(current, info.Size);
                }
                return coreCount;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}

