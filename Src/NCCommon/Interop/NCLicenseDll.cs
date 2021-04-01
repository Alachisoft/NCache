//  Copyright (c) 2018 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System.Text;
using System.Runtime.InteropServices;
#if NETCORE
using Alachisoft.NCache.Common.Licensing.NetCore;
using Alachisoft.NCache.Licensing.NetCore.DOM;
using System.Net.NetworkInformation;
using System.Linq;
#endif


namespace Alachisoft.NCache.Common.Util
{
    /// <summary>
    /// Utility class to help with interop tasks.
    /// </summary>
#if NETCORE
    public class UnManagedNCLicense
#else
    public class NCLicenseDll
#endif
    {

        internal const string DLL_LICENSE = "nclicense";
        /// <summary>
        /// Declare the structure, which is the parameter of ReadEvaluationData. 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct EvaluationData
        {
            public short Year;
            public short Month;
            public short Days;
            public short ExtensionVal;
            public short Extensions;
            public short Period;
            public short ActivationStatus;
            public short Res3;
        };

        public enum ActivationStatus
        {
            EVAL = 80,
            ACTIVATED,
            DEACTIVATED
        }


        /// <summary>
        /// Returns the number of processors on the system.
        /// </summary>   
        [DllImport(DLL_LICENSE, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetNumProcessors();
        /// <summary>
        /// Returns the total number of cores available in the system.
        /// </summary>
        [DllImport(DLL_LICENSE, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetNumCores();

        /// <summary>
        /// Returns 0 or 1, If VM based OS found returns 1 else 0
        /// </summary> 
        [DllImport(DLL_LICENSE, CallingConvention = CallingConvention.Cdecl)]
        public static extern int IsEmulatedOS();

        /// <summary>
        /// Returns a list of mac addresses found on the system.
        /// </summary>
        [DllImport(DLL_LICENSE, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetAdaptersAddressList(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder list);

        [DllImport(DLL_LICENSE, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReadActivationCode(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder code, short prodId);
        
        [DllImport(DLL_LICENSE, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReadInstallCode(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder code, short prodId);

#if NETCORE
        [DllImport(DLL_LICENSE, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ReadEvaluationData(
            int version,
            ref NCLicenseDll.EvaluationData time, short prodId);
#else
        [DllImport(DLL_LICENSE, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ReadEvaluationData(
            int version,
            ref EvaluationData time, short prodId);
#endif

        [DllImport(DLL_LICENSE, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetRegVal(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder RegVal, [MarshalAs(UnmanagedType.LPStr)] StringBuilder section
, [MarshalAs(UnmanagedType.LPStr)] StringBuilder key, [MarshalAs(UnmanagedType.LPStr)]StringBuilder defaultVal, short prodId);

        [DllImport(DLL_LICENSE, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SetRegVal(
            [MarshalAs(UnmanagedType.LPStr)] StringBuilder section
             , [MarshalAs(UnmanagedType.LPStr)]StringBuilder key, [MarshalAs(UnmanagedType.LPStr)]StringBuilder newVal, short prodId);

        [DllImport(DLL_LICENSE, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SetRegValInt(
            [MarshalAs(UnmanagedType.LPStr)] StringBuilder section
              , [MarshalAs(UnmanagedType.LPStr)] StringBuilder key, long newVal, short prodId);

        [DllImport(DLL_LICENSE, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetRegKeys(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder RegVal, [MarshalAs(UnmanagedType.LPStr)] StringBuilder section
                 , [MarshalAs(UnmanagedType.LPStr)] StringBuilder key, [MarshalAs(UnmanagedType.LPStr)]StringBuilder defaultVal, short prodId);
    }
}
