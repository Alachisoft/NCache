//  Copyright (c) 2021 Alachisoft
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
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// Helper class to encrypt and decrypt connection string info
    /// </summary>
    public class Protector
    {
        public static string DecryptString(string txtString)
        {
            // If the variable is blank, return the input
            if (txtString.Equals(string.Empty))
            {
                return txtString;
            }

            // Create an instance of the encryption API
            // We assume the key has been encrypted on this machine and not by a user
            DataProtector dp = new DataProtector(Store.Machine);

            // Use the API to decrypt the connection string
            // API works with bytes so we need to convert to and from byte arrays
            byte[] decryptedData = dp.Decrypt(Convert.FromBase64String(txtString), null);

            // Return the decyrpted data to the string
            return Encoding.ASCII.GetString(decryptedData);
        }

        public static string EncryptString(string encryptedString)
        {
            // Create an instance of the encryption API
            // We assume the key has been encrypted on this machine and not by a user
            DataProtector dp = new DataProtector(Store.Machine);

            // Use the API to encrypt the connection string
            // API works with bytes so we need to convert to and from byte arrays
            byte[] dataBytes = Encoding.ASCII.GetBytes(encryptedString);
            byte[] encryptedBytes = dp.Encrypt(dataBytes, null);

            // Return the encyrpted data to the string
            return Convert.ToBase64String(encryptedBytes);
        }

        private enum Store { Machine = 1, User };

        /// <summary>
        /// The DSAPI wrapper
        /// To be released as part of the Microsoft Configuration Building Block
        /// </summary>
     
        private class DataProtector
        {
            #region Constants
            static private IntPtr NullPtr = ((IntPtr)((int)(0)));
            private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;
            private const int CRYPTPROTECT_LOCAL_MACHINE = 0x4;
            private Store store;
            #endregion

            #region P/Invoke structures
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct DATA_BLOB
            {
                public int cbData;
                public IntPtr pbData;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct CRYPTPROTECT_PROMPTSTRUCT
            {
                public int cbSize;
                public int dwPromptFlags;
                public IntPtr hwndApp;
                public String szPrompt;
            }
            #endregion

            #region External methods
            [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern bool CryptProtectData(
                ref DATA_BLOB pDataIn,
                String szDataDescr,
                ref DATA_BLOB pOptionalEntropy,
                IntPtr pvReserved,
                ref CRYPTPROTECT_PROMPTSTRUCT
                    pPromptStruct,
                int dwFlags,
                ref DATA_BLOB pDataOut);

            [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern bool CryptUnprotectData(
                ref DATA_BLOB pDataIn,
                String szDataDescr,
                ref DATA_BLOB pOptionalEntropy,
                IntPtr pvReserved,
                ref CRYPTPROTECT_PROMPTSTRUCT
                    pPromptStruct,
                int dwFlags,
                ref DATA_BLOB pDataOut);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            private unsafe static extern int FormatMessage(int dwFlags,
                ref IntPtr lpSource,
                int dwMessageId,
                int dwLanguageId,
                ref String lpBuffer,
                int nSize,
                IntPtr* Arguments);
            #endregion

            #region Constructor
            public DataProtector(Store tempStore)
            {
                store = tempStore;
            }
            #endregion

            #region Public methods
            public byte[] Encrypt(byte[] plainText, byte[] optionalEntropy)
            {
                bool retVal = false;

                DATA_BLOB plainTextBlob = new DATA_BLOB();
                DATA_BLOB cipherTextBlob = new DATA_BLOB();
                DATA_BLOB entropyBlob = new DATA_BLOB();

                CRYPTPROTECT_PROMPTSTRUCT prompt = new CRYPTPROTECT_PROMPTSTRUCT();
                InitPromptstruct(ref prompt);

                int dwFlags;
                try
                {
                    try
                    {
                        int bytesSize = plainText.Length;
                        plainTextBlob.pbData = Marshal.AllocHGlobal(bytesSize);
                        if (IntPtr.Zero == plainTextBlob.pbData)
                        {
                            throw new Exception("Unable to allocate plaintext buffer.");
                        }
                        plainTextBlob.cbData = bytesSize;
                        Marshal.Copy(plainText, 0, plainTextBlob.pbData, bytesSize);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Exception marshalling data. " + ex.Message);
                    }
                    if (Store.Machine == store)
                    {
                        //Using the machine store, should be providing entropy.
                        dwFlags = CRYPTPROTECT_LOCAL_MACHINE | CRYPTPROTECT_UI_FORBIDDEN;
                        //Check to see if the entropy is null
                        if (null == optionalEntropy)
                        {
                            //Allocate something
                            optionalEntropy = new byte[0];
                        }
                        try
                        {
                            int bytesSize = optionalEntropy.Length;
                            entropyBlob.pbData = Marshal.AllocHGlobal(optionalEntropy.Length);
                            if (IntPtr.Zero == entropyBlob.pbData)
                            {
                                throw new Exception("Unable to allocate entropy data buffer.");
                            }
                            Marshal.Copy(optionalEntropy, 0, entropyBlob.pbData, bytesSize);
                            entropyBlob.cbData = bytesSize;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Exception entropy marshalling data. " + ex.Message);
                        }
                    }
                    else
                    {
                        //Using the user store
                        dwFlags = CRYPTPROTECT_UI_FORBIDDEN;
                    }
                    retVal = CryptProtectData(ref plainTextBlob, "", ref entropyBlob,
                        IntPtr.Zero, ref prompt, dwFlags, ref cipherTextBlob);
                    if (false == retVal)
                    {
                        throw new Exception("Encryption failed. " + GetErrorMessage(Marshal.GetLastWin32Error()));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Exception encrypting. " + ex.Message);
                }
                byte[] cipherText = new byte[cipherTextBlob.cbData];
                Marshal.Copy(cipherTextBlob.pbData, cipherText, 0, cipherTextBlob.cbData);
                return cipherText;
            }

            public byte[] Decrypt(byte[] cipherText, byte[] optionalEntropy)
            {
                bool retVal = false;
                DATA_BLOB plainTextBlob = new DATA_BLOB();
                DATA_BLOB cipherBlob = new DATA_BLOB();
                CRYPTPROTECT_PROMPTSTRUCT prompt = new CRYPTPROTECT_PROMPTSTRUCT();
                InitPromptstruct(ref prompt);
                try
                {
                    try
                    {
                        int cipherTextSize = cipherText.Length;
                        cipherBlob.pbData = Marshal.AllocHGlobal(cipherTextSize);
                        if (IntPtr.Zero == cipherBlob.pbData)
                        {
                            throw new Exception("Unable to allocate cipherText buffer.");
                        }
                        cipherBlob.cbData = cipherTextSize;
                        Marshal.Copy(cipherText, 0, cipherBlob.pbData, cipherBlob.cbData);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Exception marshalling data. " + ex.Message);
                    }
                    DATA_BLOB entropyBlob = new DATA_BLOB();
                    int dwFlags;
                    if (Store.Machine == store)
                    {
                        //Using the machine store, should be providing entropy.
                        dwFlags = CRYPTPROTECT_LOCAL_MACHINE | CRYPTPROTECT_UI_FORBIDDEN;
                        //Check to see if the entropy is null
                        if (null == optionalEntropy)
                        {
                            //Allocate something
                            optionalEntropy = new byte[0];
                        }
                        try
                        {
                            int bytesSize = optionalEntropy.Length;
                            entropyBlob.pbData = Marshal.AllocHGlobal(bytesSize);
                            if (IntPtr.Zero == entropyBlob.pbData)
                            {
                                throw new Exception("Unable to allocate entropy buffer.");
                            }
                            entropyBlob.cbData = bytesSize;
                            Marshal.Copy(optionalEntropy, 0, entropyBlob.pbData, bytesSize);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Exception entropy marshalling data. " + ex.Message);
                        }
                    }
                    else
                    {
                        //Using the user store
                        dwFlags = CRYPTPROTECT_UI_FORBIDDEN;
                    }
                    retVal = CryptUnprotectData(ref cipherBlob, null, ref 
                        entropyBlob,
                        IntPtr.Zero, ref prompt, dwFlags,
                        ref plainTextBlob);
                    if (false == retVal)
                    {
                        throw new Exception("Decryption failed. " + GetErrorMessage(Marshal.GetLastWin32Error()));
                    }
                    //Free the blob and entropy.
                    if (IntPtr.Zero != cipherBlob.pbData)
                    {
                        Marshal.FreeHGlobal(cipherBlob.pbData);
                    }
                    if (IntPtr.Zero != entropyBlob.pbData)
                    {
                        Marshal.FreeHGlobal(entropyBlob.pbData);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Exception decrypting. " + ex.Message);
                }
                byte[] plainText = new byte[plainTextBlob.cbData];
                Marshal.Copy(plainTextBlob.pbData, plainText, 0, plainTextBlob.cbData);
                return plainText;
            }
            #endregion

            #region Private methods
            private void InitPromptstruct(ref CRYPTPROTECT_PROMPTSTRUCT ps)
            {
                ps.cbSize = Marshal.SizeOf(typeof(CRYPTPROTECT_PROMPTSTRUCT));
                ps.dwPromptFlags = 0;
                ps.hwndApp = NullPtr;
                ps.szPrompt = null;
            }

            private unsafe static String GetErrorMessage(int errorCode)
            {
                int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
                int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
                int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
                int messageSize = 255;
                String lpMsgBuf = "";
                int dwFlags = FORMAT_MESSAGE_ALLOCATE_BUFFER |
                              FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS;
                IntPtr ptrlpSource = new IntPtr();
                IntPtr prtArguments = new IntPtr();
                int retVal = FormatMessage(dwFlags, ref ptrlpSource, errorCode, 0,
                    ref lpMsgBuf, messageSize, &prtArguments);
                if (0 == retVal)
                {
                    throw new Exception("Failed to format message for error code " + errorCode + ". ");
                }
                return lpMsgBuf;
            }
            #endregion

        }
    }
}