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
using System.Text;
using System.Security.Cryptography;

namespace Alachisoft.NCache.Common
{
    public class EncryptionUtil
    {
        private static TripleDESCryptoServiceProvider s_des;
        private static string s_key = "A41'D3a##asd[1-a;d zs[s`";
        static object _lock = new object();

        private static string s_iv = "KKNWLCZU";

        public static Byte[] ConvertStringToByteArray(String s)
        {
            return (new ASCIIEncoding()).GetBytes(s);
        }

        public static byte[] Encrypt(string PlainText)
        {
            try
            {
                if (String.IsNullOrEmpty(PlainText))
                    return null;
                lock (_lock)
                {
                    s_des = new TripleDESCryptoServiceProvider();
                    int i = s_des.BlockSize;
                    int j = s_des.KeySize;
                    byte[] k = ConvertStringToByteArray(s_key);
                    byte[] IV = ConvertStringToByteArray(s_iv);

                    byte[] input = ConvertStringToByteArray(PlainText);

                    s_des.IV = IV;
                    s_des.Key = k;
                    s_des.Padding = PaddingMode.PKCS7;
                    s_des.Mode = CipherMode.CBC;
                    byte[] ret = s_des.CreateEncryptor().TransformFinalBlock(input, 0, input.Length);
                    return ret;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
           
        }

        public static string Decrypt(byte[] CypherText)
        {
            try
            {
                if (CypherText == null || CypherText.Length == 0)
                    return null;
                lock (_lock)
                {
                    s_des = new TripleDESCryptoServiceProvider();
                    byte[] k = ConvertStringToByteArray(s_key);
                    byte[] IV = ConvertStringToByteArray(s_iv);

                    s_des.IV = IV;
                    s_des.Key = k;
                    s_des.Padding = PaddingMode.PKCS7;
                    s_des.Mode = CipherMode.CBC;
                    return Encoding.ASCII.GetString(s_des.CreateDecryptor().TransformFinalBlock(CypherText, 0, CypherText.Length));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
           
        }

        private static Random random = new Random((int)DateTime.Now.Ticks);
        
        private static string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }


        /// <summary>
        /// Encrypt user provided key with the default key stored; This key is obfuscated
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>encrypted string</returns>
        public static string EncryptKey(string key)
        {
            try
            {

                byte[] data = ASCIIEncoding.ASCII.GetBytes(key);
                s_des = new TripleDESCryptoServiceProvider();
                int i = s_des.BlockSize;
                int j = s_des.KeySize;
                byte[] k = ConvertStringToByteArray(s_key);
                byte[] IV = ConvertStringToByteArray(s_iv);
                s_des.IV = IV;
                s_des.Key = k;
                s_des.Padding = PaddingMode.PKCS7;
                s_des.Mode = CipherMode.CBC;
                byte[] ret = s_des.CreateEncryptor().TransformFinalBlock(data, 0, data.Length);
                return Convert.ToBase64String(ret, Base64FormattingOptions.None);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string DecryptKey(string encodedkey)
        {
            try
            {
                byte[] data = Convert.FromBase64String(encodedkey);
                s_des = new TripleDESCryptoServiceProvider();
                byte[] k = ConvertStringToByteArray(s_key);
                byte[] IV = ConvertStringToByteArray(s_iv);
                s_des.IV = IV;
                s_des.Key = k;
                s_des.Padding = PaddingMode.PKCS7;
                s_des.Mode = CipherMode.CBC;

                byte[] ret = s_des.CreateDecryptor().TransformFinalBlock(data, 0, data.Length);
                return ASCIIEncoding.ASCII.GetString(ret);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string DefaultUserName
        {
            get { return EncryptKey(RandomString(10)); }
        }

        public static string DefaultPassword
        {
            get { return EncryptKey(RandomString(10)); }
        }
    }
}
