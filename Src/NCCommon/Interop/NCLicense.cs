//using Alachisoft.NCache.Licensing.Crypto;
//using Alachisoft.NCache.Licensing.NetCore.RegistryUtil;
//using Alachisoft.NCache.Runtime.Exceptions;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;

//namespace Alachisoft.NCache.Common.Util
//{
//    public class NCLicense
//    {
//        private static uint g_DataOffset = 0x1008;
//        public struct EvaluationData
//        {
//            public short Year;
//            public short Month;
//            public short Days;
//            public short ExtensionVal;
//            public short Extensions;
//            public short Period;
//            public short ActivationStatus;
//            public short Res3;
//        };

//        public enum ActivationStatus
//        {
//            EVAL = 80,
//            ACTIVATED,
//            DEACTIVATED,
//            UNREGISTERED
//        }
//        public static int ReadEvaluationData(int version, ref NCLicense.EvaluationData evaluationData, short productId)
//        {
//            //String[] systemFolder = GetStubPath();

//            byte[] data = null;

//            int fileRetryCount = 0;
//            for (int i = 0; i < systemFolder.Length; i++)
//            {
//                //int dwSize = 0;
//                bool fileExists = File.Exists(systemFolder[i]);
//                if (fileExists)
//                {
//                    FileStream fs = File.OpenRead(systemFolder[i]);
//                    data = new byte[(int)fs.Length];
//                    fs.Read(data, 0, (int)fs.Length);
//                    fs.Close();
//                    if (data != null)
//                        break;
//                }
//                fileRetryCount++;
//            }
//            if (fileRetryCount == systemFolder.Length || data == null)
//            {
//                evaluationData.ActivationStatus = (int)ActivationStatus.UNREGISTERED;
//                return -1;
//            }
//            //byte[] temp = new byte[data.Length - g_DataOffset];
//            //Buffer.BlockCopy(data, (int)g_DataOffset, temp, 0, (int)(data.Length - g_DataOffset));
//            NCCryptoCode.EncryptDecryptBytes(data, data.Length - (int)g_DataOffset, (int)g_DataOffset);
//            evaluationData = GetInstallTime(data, version, productId);
//            if (!IsValidVersionMark(evaluationData, productId))
//                return -1;

//            //evaluationData.Extensions = GetExtensionsUsed();

//            //if (evaluationData.Extensions < 0)    //TODO: Licensing (These values need to be zero by them. This is not correct method)
//            //    evaluationData.Extensions = 0;
//            //if (evaluationData.Res3 < 0)
//            //    evaluationData.Res3 = 0;
//            return 0;
//        }

//        public static bool IsValidVersionMark(NCLicense.EvaluationData evaluationDt, short productId)
//        {
//            if (evaluationDt.Year < 2005 || evaluationDt.Year > 3500) return false;
//            if (evaluationDt.Month > 12 || evaluationDt.Days > 31) return false;
//            return true;
//        }
//        public static byte[] StructureToByteArray(object obj)
//        {
//            int len = Marshal.SizeOf(obj);

//            byte[] arr = new byte[len];

//            IntPtr ptr = Marshal.AllocHGlobal(len);

//            Marshal.StructureToPtr(obj, ptr, true);

//            Marshal.Copy(ptr, arr, 0, len);

//            Marshal.FreeHGlobal(ptr);

//            return arr;
//        }
//        public static NCLicense.EvaluationData GetInstallTime(byte[] data, long version, short prodId)
//        {
//            NCLicense.EvaluationData time = new NCLicense.EvaluationData();

//            int evaluationDataSize = 0;
//            unsafe
//            {
//                evaluationDataSize = sizeof(NCLicense.EvaluationData);
//            }
//            byte[] temp = new byte[evaluationDataSize];
//            Buffer.BlockCopy(data, (int)(g_DataOffset + version * evaluationDataSize), temp, 0, evaluationDataSize);

//            return ByteArrayToStructure<NCLicense.EvaluationData>(temp);
//        }

//        public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
//        {
//            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
//            T stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
//            handle.Free();
//            return stuff;
//        }
//        public static string[] GetStubPath()
//        {
//            String[] systemFolder = null;
//            try
//            {
//#if NETCORE
//                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//#endif
//                {
//                    String sys32Folder = Environment.GetEnvironmentVariable("WINDIR") + "\\system32";
//                    systemFolder = new String[] { sys32Folder };
//                    for (int i = 0; i < systemFolder.Length; i++)
//                    {
//                        systemFolder[i] += Path.DirectorySeparatorChar + RegUtil.WinStub;
//                    }
//                }
//#if NETCORE
//                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//                {
//                    systemFolder = new String[]{
//                        "//usr//lib//" + RegUtil.LinuxStub,
//                        "//usr//bin//" + RegUtil.LinuxStub
//                    };
//                }
//#endif
//            }
//            catch (Exception e)
//            {
//                throw new RegistrationException("Your copy of NCache Open Source is not registered. You can get free registration key from http://www.alachisoft.com/activate/RequestKey.php?Edition=NC-OSS-50-4x&Version=5.0&Source=Register-NCache");

//            }
//            return systemFolder;
//        }

//    }
//}
