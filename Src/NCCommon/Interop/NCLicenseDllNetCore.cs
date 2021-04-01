using Alachisoft.NCache.Common.Licensing.NetCore;
using Alachisoft.NCache.Licensing.NetCore.Crypto;
using Alachisoft.NCache.Licensing.NetCore.DOM;
using Alachisoft.NCache.Licensing.NetCore.RegistryUtil;
using Alachisoft.NCache.Runtime.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace Alachisoft.NCache.Common.Util
{
    public class NCLicenseDll
    {
        private static uint g_DataOffset = 0x1008;

        private static int _numOfCores = -1;
        private static int _numOfProcessors = -1;
        private static string _installCode = null;

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
        public static int GetNumProcessors()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return UnManagedNCLicense.GetNumProcessors();
            if (_numOfProcessors <= 0)
            {
                _numOfProcessors = MachineInfo.PhysicalCores;
            }

            return _numOfProcessors;
        }

        /// <summary>
        /// Returns the total number of cores available in the system.
        /// </summary>
        public static int GetNumCores()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return UnManagedNCLicense.GetNumCores();
            if (_numOfCores <= 0)
            {
                _numOfCores = MachineInfo.TotalAvailableCores;
            }

            return _numOfCores;
        }

        /// <summary>
        /// Returns 0 or 1, If VM based OS found returns 1 else 0
        /// </summary>
        public static int IsEmulatedOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return UnManagedNCLicense.IsEmulatedOS();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a list of mac addresses found on the system.
        /// </summary>
        public static int GetAdaptersAddressList(StringBuilder list)
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return UnManagedNCLicense.GetAdaptersAddressList(list);
            }
            catch (DllNotFoundException)
            {
                // Eating this exception on purpose
                // because of Nano Server
            }

            return GetAdaptersAddressListManaged(list);
        }

        /// <summary>
        /// Returns a list of mac addresses found on the system.
        /// </summary>
        public static int GetAdaptersAddressListManaged(StringBuilder list)
        {
            var macAddresses = (from nic in NetworkInterface.GetAllNetworkInterfaces()
                                where nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                                select nic.GetPhysicalAddress().ToString().Replace("-", string.Empty).ToLower());

            int i = 0;
            foreach (var macAddress in macAddresses)
            {
                if (!string.IsNullOrEmpty(macAddress))
                {
                    if (i != 0)
                        list.Append(":");
                    list.Append(macAddress);
                    i++;
                }
            }
            return i;
        }
        public static void ReadActivationCode(StringBuilder code, short prodId)
        {
            //            if (RegUtil.LicenseProperties == null || RegUtil.LicenseProperties.UserInfo == null)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                UnManagedNCLicense.ReadActivationCode(code, prodId);
            else
            {
                RegUtil.LoadRegistry();
                NCCryptoCode.Decode(RegUtil.LicenseProperties.UserInfo.AuthCode, code);
            }
        }

        public static void ReadInstallCode(StringBuilder s, short productId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                UnManagedNCLicense.ReadInstallCode(s, productId);
            }
            else
            {
                if (!string.IsNullOrEmpty(_installCode))
                {
                    s.Append(_installCode);
                    return;
                }
                RegUtil.LoadRegistry();
                var installCode = RegUtil.GetInstallCode();
                NCCryptoCode.Decode(installCode, s);
                _installCode = s.ToString();
            }
        }

        public static int ReadEvaluationData(int version, ref NCLicenseDll.EvaluationData evaluationData, short productId)
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return UnManagedNCLicense.ReadEvaluationData(version, ref evaluationData, productId);
            }
            catch (DllNotFoundException)
            {
                // Eating this exception on purpose
                // because of Nano Server
            }

            return ReadEvaluationDataManaged(version, ref evaluationData, productId);
        }

        public static int ReadEvaluationDataManaged(int version, ref NCLicenseDll.EvaluationData evaluationData, short productId)
        {
            String[] systemFolder = GetStubPath();

            byte[] data = null;

            int fileRetryCount = 0;
            for (int i = 0; i < systemFolder.Length; i++)
            {
                //int dwSize = 0;
                bool fileExists = File.Exists(systemFolder[i]);
                if (fileExists)
                {
                    FileStream fs = File.OpenRead(systemFolder[i]);
                    data = new byte[(int)fs.Length];
                    fs.Read(data, 0, (int)fs.Length);
                    fs.Close();
                    if (data != null)
                        break;
                }
                fileRetryCount++;
            }
            if (fileRetryCount == systemFolder.Length || data == null)
                return -1;
            //byte[] temp = new byte[data.Length - g_DataOffset];
            //Buffer.BlockCopy(data, (int)g_DataOffset, temp, 0, (int)(data.Length - g_DataOffset));
            NCCryptoCode.EncryptDecryptBytes(data, data.Length - (int)g_DataOffset, (int)g_DataOffset);
            evaluationData = GetInstallTime(data, version, productId);
            if (!IsValidVersionMark(evaluationData, productId))
                return -1;
            //evaluationData.Hour = Misc::GetExtensionsUsed(prodId); //TODO: Licensing
            if (evaluationData.Extensions < 0)    //TODO: Licensing (These values need to be zero by them. This is not correct method)
                evaluationData.Extensions = 0;
            if (evaluationData.Res3 < 0)
                evaluationData.Res3 = 0;
            return 0;
        }


        public static void GetRegVal(StringBuilder RegVal, StringBuilder section, StringBuilder key, StringBuilder defaultVal, short prodId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                UnManagedNCLicense.GetRegVal(RegVal, section, key, defaultVal, prodId);
            else
                throw new NotImplementedException();
        }

        public static bool SetRegVal(StringBuilder section, StringBuilder key, StringBuilder newVal, short prodId)
        {
            throw new NotImplementedException();
        }

        public static bool SetRegValInt(StringBuilder section, StringBuilder key, long newVal, short prodId)
        {
            throw new NotImplementedException();
        }

        public static void GetRegKeys(StringBuilder RegVal, StringBuilder section, StringBuilder key, StringBuilder defaultVal, short prodId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                UnManagedNCLicense.GetRegKeys(RegVal, section, key, defaultVal, prodId);
            else
                throw new NotImplementedException();
        }

        public static bool IsValidVersionMark(NCLicenseDll.EvaluationData evaluationDt, short productId)
        {
            if (evaluationDt.Year < 2005 || evaluationDt.Year > 3500) return false;
            if (evaluationDt.Month > 12 || evaluationDt.Days > 31) return false;
            return true;
        }

        public static NCLicenseDll.EvaluationData GetInstallTime(byte[] data, long version, short prodId)
        {
            NCLicenseDll.EvaluationData time = new NCLicenseDll.EvaluationData();

            int evaluationDataSize = 0;
            unsafe
            {
                evaluationDataSize = sizeof(NCLicenseDll.EvaluationData);
            }
            byte[] temp = new byte[evaluationDataSize];
            Buffer.BlockCopy(data, (int)(g_DataOffset + version * evaluationDataSize), temp, 0, evaluationDataSize);

            return ByteArrayToStructure<NCLicenseDll.EvaluationData>(temp);
        }

        public static string[] GetStubPath()
        {
            String[] systemFolder = null;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    String sys32Folder = Environment.GetEnvironmentVariable("WINDIR") + "\\system32";
                    systemFolder = new String[] { sys32Folder };
                    for (int i = 0; i < systemFolder.Length; i++)
                    {
                        systemFolder[i] += Path.DirectorySeparatorChar + RegUtil.WinStub;
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    systemFolder = new String[]{
                    "//usr//lib//" + RegUtil.LinuxStub,
                    "//usr//bin//" + RegUtil.LinuxStub
                };
                }
            }
            catch (Exception e)
            {
                throw new LicensingException("Unable to find file containing evaluation data");

            }
            return systemFolder;
        }

        public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return stuff;
        }

        public static byte[] StructureToByteArray(object obj)
        {
            int len = Marshal.SizeOf(obj);

            byte[] arr = new byte[len];

            IntPtr ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);

            Marshal.Copy(ptr, arr, 0, len);

            Marshal.FreeHGlobal(ptr);

            return arr;
        }
    }
}
