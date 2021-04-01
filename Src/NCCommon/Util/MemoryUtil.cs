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
using System.Collections;

namespace Alachisoft.NCache.Common
{
    public class MemoryUtil 
    {
        public const int KB = 1024;
        //24 Bytes overhead for every .net class in x64
        public const int NetOverHead = 24;
        public const int NetHashtableOverHead = 45;
        public const int NetListOverHead = 8;
        public const int NetClassOverHead = 16;
        public const int NetIntSize = 4;
        public const int NetEnumSize = 4;
        public const int NetByteSize = 1;
        public const int NetShortSize = 2;
        public const int NetStringCharSize = 16;
        public const int NetLongSize = 8;
        public const int NetDateTimeSize = 8;
        public const int NetReferenceSize = 8;

        #region  Dot Net Primitive Tyes String Constants
        public const String Net_bool = "bool";
        public const String Net_System_Boolean = "System.Boolean";
        public const String Net_char = "char";
        public const String Net_System_Char = "System.Char";
        public const String Net_string = "string";
        public const String Net_System_String = "System.String";
        public const String Net_float = "float";
        public const String Net_System_Single = "System.Single";
        public const String Net_double = "double";
        public const String Net_System_Double = "System.Double";
        public const String Net_short = "short";
        public const String Net_ushort = "ushort";
        public const String Net_System_Int16 = "System.Int16";
        public const String Net_System_UInt16 = "System.UInt16";
        public const String Net_int = "int";
        public const String Net_System_Int32 = "System.Int32";
        public const String Net_uint = "uint";
        public const String Net_System_UInt32 = "System.UInt32";
        public const String Net_long = "long";
        public const String Net_System_Int64 = "System.Int64";
        public const String Net_ulong = "ulong";
        public const String Net_SystemUInt64 = "System.UInt64";
        public const String Net_byte = "byte";
        public const String Net_System_Byte = "System.Byte";
        public const String Net_sbyte = "sbyte";
        public const String Net_System_SByte = "System.SByte";
        public const String Net_System_Object = "System.Object";
        public const String Net_System_DateTime = "System.DateTime";
        
        public const String Net_decimal = "decimal";        
        public const String Net_System_Decimal = "System.Decimal";

        #endregion

        #region Java Primitive Types String Constants

        public const String Java_Lang_Boolean = "java.lang.Boolean";// True/false value
        public const String Java_Lang_Character = "java.lang.Character";// Unicode character (16 bit;
        public const String Java_Lang_String = "java.lang.String";// Unicode String
        public const String Java_Lang_Float = "java.lang.Float";// IEEE 32-bit float
        public const String Java_Lang_Double = "java.lang.Double";// IEEE 64-bit float
        public const String Java_Lang_Short = "java.lang.Short";// Signed 16-bit integer
        public const String Java_Lang_Integer = "java.lang.Integer";// Signed 32-bit integer
        public const String Java_Lang_Long = "java.lang.Long";// Signed 32-bit integer
        public const String Java_Lang_Byte = "java.lang.Byte";// Unsigned 8-bit integer
        public const String Java_Lang_Object = "java.lang.Object";// Base class for all objects            
        public const String Java_Util_Date = "java.util.Date";// Dates will always be serialized (passed by value); according to .NET Remoting
        public const String Java_Match_BigDecimal = "java.math.BigDecimal";// Will always be serialized (passed by value); according to .NET Remoting           
        
        #endregion

        /// <summary>
        /// Hashcode algorithm returning same hash code for both 32bit and 64 bit apps. 
        /// Used for data distribution under por/partitioned topologies.
        /// </summary>
        /// <param name="strArg"></param>
        /// <returns></returns>
        public static int GetStringSize(object key)
        {
            if (key != null && key is String)
            {
                //size of .net charater is 2 bytes so multiply by 2 length of key, 24 bytes are extra overhead(header) of each instance
                return (2 * ((String)key).Length) + Common.MemoryUtil.NetOverHead;
            }
            return 0;
        }

        /// <summary>
        /// Returns memory occupied in bytes by string instance without .NET overhead.
        /// </summary>
        /// <param name="arg">String value whose size without .NET overhead is to be determined.</param>
        /// <returns>Integer representing size of string instance without .NET overhead. 0 value corresponds to null or non-string argument.</returns>
        public static int GetStringSizeWithoutNetOverhead(object arg)
        {
            if (arg != null && arg is String)
            {
                //size of .net charater is 2 bytes so multiply by 2 length of key
                return (2 * ((String)arg).Length);
            }
            return 0;
        }

        /// <summary>
        /// Hashcode algorithm returning same hash code for both 32bit and 64 bit apps. 
        /// Used for data distribution under por/partitioned topologies.
        /// </summary>
        /// <param name="strArg"></param>
        /// <returns></returns>
        public static int GetStringSize(object[] keys)
        {
            int totalSize = 0;
            if (keys != null)
            {
                foreach (object key in keys)
                {
                    //size of .net charater is 2 bytes so multiply by 2 length of key, 24 bytes are extra overhead(header) of each instance
                    totalSize += (2 * ((String)key).Length) + Common.MemoryUtil.NetOverHead;
                }
            }
            return totalSize;
        }


        /// <summary>        
        /// Used to get DataType Size for provided AttributeSize.
        /// </summary>
        /// <param name="strArg"></param>
        /// <returns></returns>
        public static int GetTypeSize(AttributeTypeSize type) 
        {
            switch(type)
            {
                case AttributeTypeSize.Byte1:
                    return 1;

                case AttributeTypeSize.Byte2:
                    return 2;

                case AttributeTypeSize.Byte4:
                    return 4;

                case AttributeTypeSize.Byte8:
                    return 8;

                case AttributeTypeSize.Byte16:
                    return 16;
            }
            return 0;
        }

        public static AttributeTypeSize GetAttributeTypeSize(String type)
        {
            switch (type)
            {
                case Net_bool:
                case Net_byte:
                case Net_System_Byte:
                case Net_sbyte:
                case Net_System_SByte:
                case Net_System_Boolean:

                case Java_Lang_Boolean:
                case Java_Lang_Byte: return AttributeTypeSize.Byte1;
                
                case Net_char:
                case Net_short:
                case Net_ushort:
                case Net_System_Int16:
                case Net_System_UInt16:
                case Net_System_Char:

                case Java_Lang_Character:
                case Java_Lang_Float:
                case Java_Lang_Short: return AttributeTypeSize.Byte2;
                
                case Net_float:
                case Net_int:
                case Net_System_Int32:
                case Net_uint:
                case Net_System_UInt32:
                case Net_System_Single:

                case Java_Lang_Integer: return AttributeTypeSize.Byte4;

                case Net_double:
                case Net_System_Double:                             
                case Net_long:
                case Net_System_Int64:
                case Net_ulong:
                case Net_System_DateTime:
                case Net_SystemUInt64:

                case Java_Lang_Double:
                case Java_Lang_Long:
                case Java_Util_Date: return AttributeTypeSize.Byte8;

                case Net_decimal:
                case Net_System_Decimal:
                case Java_Match_BigDecimal: return AttributeTypeSize.Byte16;
            }

            return AttributeTypeSize.Variable ;
        }


        public static Type GetDataType(string typeName)
        {
            switch (typeName)
            {
                case Net_string: return typeof(string);
                case Net_System_String: return typeof(System.String);
                case Java_Lang_String: return typeof(string);
                case Net_bool: return typeof(bool);
                case Net_byte: return typeof(byte);
                case Net_System_Byte: return typeof(Byte);
                case Net_sbyte: return typeof(sbyte);
                case Net_System_SByte: return typeof(SByte);
                case Net_System_Boolean: return typeof(Boolean);

                case Java_Lang_Boolean: return typeof(Boolean);
                case Java_Lang_Byte: return typeof(Byte);

                case Net_char: return typeof(char);
                case Net_short: return typeof(short);
                case Net_ushort: return typeof(ushort);
                case Net_System_Int16: return typeof(Int16);
                case Net_System_UInt16: return typeof(UInt16);
                case Net_System_Char: return typeof(Char);

                case Java_Lang_Character: return typeof(Char);
                case Java_Lang_Float: return typeof(float);
                case Java_Lang_Short: return typeof(short);

                case Net_float: return typeof(float);
                case Net_int: return typeof(int);
                case Net_System_Int32: return typeof(Int32);
                case Net_uint: return typeof(uint);
                case Net_System_UInt32: return typeof(UInt32);
                case Net_System_Single: return typeof(Single);

                case Java_Lang_Integer: return typeof(int);

                case Net_double: return typeof(double);
                case Net_System_Double: return typeof(Double);
                case Net_long: return typeof(long);
                case Net_System_Int64: return typeof(Int64);
                case Net_ulong: return typeof(ulong);
                case Net_System_DateTime: return typeof(DateTime);
                case Net_SystemUInt64: return typeof(UInt64);

                case Java_Lang_Double: return typeof(Double);
                case Java_Lang_Long: return typeof(long);
                case Java_Util_Date: return typeof(DateTime);

                case Net_decimal: return typeof(decimal);
                case Net_System_Decimal: return typeof(Decimal);
                case Java_Match_BigDecimal: return typeof(Decimal);
                default: return null;

            }
        }

        public static int GetInMemoryInstanceSize(int actualDataBytes) 
        {
            int temp = MemoryUtil.NetClassOverHead;
            ushort remainder = (ushort)(actualDataBytes & 7);
            if (remainder != 0)
                remainder = (ushort)(8 - remainder);

            temp += actualDataBytes + remainder;
            return temp;
        }

        public static long GetInMemoryInstanceSize(long actualDataBytes)
        {
            long temp = MemoryUtil.NetClassOverHead;
            ushort remainder = (ushort)(actualDataBytes & 7);
            if (remainder != 0)
                remainder = (ushort)(8 - remainder);

            temp += actualDataBytes + remainder;
            return temp;           
        }

        public static ArraySegment<TReturn>[] GetArraySegments<TReturn>(IList list)
        {
            ArraySegment<TReturn>[] segments = new ArraySegment<TReturn>[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                TReturn[] array = (TReturn[])list[i];
                segments[i] = new ArraySegment<TReturn>(array);
            }
            return segments;
        }

        /// <summary>
        /// Returns .Net's LOH safe generic collection count...
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static int GetSafeCollectionCount<T>(long length)
        {
            Type genericType = typeof (T);
            int sizeOfReference;

            if (genericType.IsValueType){
                sizeOfReference = System.Runtime.InteropServices.Marshal.SizeOf(genericType);
            }
            else
            {
                sizeOfReference = IntPtr.Size;
            }

            int safeLength = (81920 / sizeOfReference);

            return ((length > safeLength) ? safeLength : (int)length);
        }

        /// <summary>
        /// Returns .Net's LOH safe generic collection count...
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static int GetSafeByteCollectionCount(long length)
        {
            return ((length > 81920) ? 81920 : (int)length);
        }
    }
}