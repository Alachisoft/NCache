// Byte related function helper.
// Copyright (C) 2008-2010 Malcolm Crowe, Lex Li, and other contributors.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/1
 * Time: 12:31
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Description of ByteTool.
    /// </summary>
    public static class ByteTool
    {
        /// <summary>
        /// Converts decimal string to bytes.
        /// </summary>
        /// <param name="description">The decimal string.</param>
        /// <returns></returns>
        public static byte[] ConvertDecimal(string description)
        {
            if (description == null)
            {
                throw new ArgumentNullException("description");
            }
            
            var result = new List<byte>();
            var content = description.Trim().Split(new[] { ' ' });
            foreach (var part in content)
            {
                #if CF
                result.Add(byte.Parse(part, NumberStyles.Integer, CultureInfo.InvariantCulture));
                #else
                byte temp;
                if (byte.TryParse(part, out temp))
                {
                    result.Add(temp);
                }
                #endif
            }

            return result.ToArray();
        }
        
        /// <summary>
        /// Converts the byte string.
        /// </summary>
        /// <param name="description">The HEX string.</param>
        /// <returns></returns>
        public static byte[] Convert(IEnumerable<char> description)
        {
            if (description == null)
            {
                throw new ArgumentNullException("description");
            }

            var result = new List<byte>();
            var buffer = new StringBuilder(2);
            foreach (var c in description.Where(c => !char.IsWhiteSpace(c)))
            {
                if (!char.IsLetterOrDigit(c))
                {
                    throw new ArgumentException("illegal character found", "description");
                }
                
                buffer.Append(c);
                if (buffer.Length != 2)
                {
                    continue;
                }
                #if CF
                result.Add(byte.Parse(buffer.ToString(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                #else
                byte temp;
                if (byte.TryParse(buffer.ToString(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out temp))
                {
                    result.Add(temp);
                }
                #endif
                buffer.Length = 0;
            }
            
            if (buffer.Length != 0)
            {
                throw new ArgumentException("not a complete byte string", "description");
            }
            
            return result.ToArray();
        }

        /// <summary>
        /// Converts the byte string.
        /// </summary>
        /// <param name="buffer">The bytes.</param>
        /// <returns></returns>
        public static string Convert(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            return BitConverter.ToString(buffer).Replace('-', ' ');
        }

        internal static byte[] ParseItems(params ISnmpData[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            using (var result = new MemoryStream())
            {
                foreach (var item in items)
                {
                    if (item == null)
                    {
                        throw new ArgumentException("item in the collection cannot be null", "items");
                    }

                    item.AppendBytesTo(result);
                }
                
                return result.ToArray();
            }
        }

        internal static byte[] ParseItems(IEnumerable<ISnmpData> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            using (var result = new MemoryStream())
            {
                foreach (var item in items)
                {
                    item.AppendBytesTo(result);
                }
                
                return result.ToArray();
            }
        }

        internal static byte[] GetRawBytes(IEnumerable<byte> orig, bool negative)
        {
            if (orig == null)
            {
                throw new ArgumentNullException("orig");
            }

            byte flag;
            byte sign;
            if (negative)
            {
                flag = 0xff;
                sign = 0x80;
            }
            else
            {
                flag = 0x0;
                sign = 0x0;
            }

            var list = new List<byte>(orig);
            while (list.Count > 1)
            {
                if (list[list.Count - 1] == flag)
                {
                    list.RemoveAt(list.Count - 1);
                }
                else
                {
                    break;
                }
            }

            // if sign bit is not correct, add an extra byte
            if ((list[list.Count - 1] & 0x80) != sign)
            {
                list.Add(flag);
            }

            list.Reverse();
            return list.ToArray();
        }

        internal static Sequence PackMessage(byte[] length, VersionCode version, ISegment header, ISegment parameters, ISnmpData data)
        {
            if (header == null)
            {
                throw new ArgumentNullException("header");
            }

            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            var items = new[] 
            {
                new Integer32((int)version),
                header.GetData(version),
                parameters.GetData(version),
                data
            };
            return new Sequence(length, items);
        }

        internal static byte[] WritePayloadLength(this int length) // excluding initial octet
        {
            if (length < 0)
            {
                throw new ArgumentException("length cannot be negative", "length");
            }

            var stream = new MemoryStream();

            if (length < 127)
            {
                stream.WriteByte((byte)length);
                return stream.ToArray();
            }
            
            var c = new byte[16];
            var j = 0;
            while (length > 0)
            {
                c[j++] = (byte)(length & 0xff);
                length = length >> 8;
            }
            
            stream.WriteByte((byte)(0x80 | j));
            while (j > 0)
            {
                int x = c[--j];
                stream.WriteByte((byte)x);
            }

            return stream.ToArray();
        }
    }
}
