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
using Alachisoft.NCache.Storage.Interop;

namespace Alachisoft.NCache.Storage.Mmf
{
    internal class MmfFileView
    {
        private IntPtr _mvPtr;
        private uint viewLength;

        public MmfFileView(IntPtr view, uint length)
        {
            if (view == IntPtr.Zero)
                throw new ArgumentOutOfRangeException("_mvPtr");

            _mvPtr = view;
            viewLength = length;
        }

        /// <summary> Gets the length of the memory mapped file. </summary>
        public uint Length { get { return viewLength; } }
        /// <summary> Gets the view of the pointer. </summary>
        public IntPtr ViewPtr { get { return _mvPtr; } }

        /// <summary>
        ///	Close the memory mapped file.
        /// </summary>
        /// <remarks>
        ///	Close();
        /// </remarks>
        public void Close()
        {
            if (_mvPtr != IntPtr.Zero)
            {
                Win32Mmf.UnmapViewOfFile(_mvPtr);
            }
        }

        /// <summary>
        ///	Removes all the memory mapped files from the store.
        /// </summary>
        public void Flush()
        {
            if (_mvPtr != IntPtr.Zero)
            {
                Win32Mmf.FlushViewOfFile(_mvPtr, viewLength);
            }
        }

        /// <summary>
        ///	RawRead the number of bytes from the buffer.
        /// </summary>
        public bool CopyMemory(int srcOffset, int destOffset, int count)
        {
            byte[] buffer = new byte[count];

            if (!Read(buffer, srcOffset, count)) return false;
            if (!Write(buffer, destOffset, count)) return false;

            return true;
        }

        /// <summary>
        ///	RawRead the number of bytes from the buffer.
        /// </summary>
        public bool SwapMemory(int srcOffset, int destOffset, int srcLen, int destLen)
        {
            byte[] sbuffer = new byte[srcLen];
            byte[] dbuffer = new byte[destLen];

            if (!Read(sbuffer, srcOffset, srcLen)) return false;
            if (!Read(dbuffer, destOffset, destLen)) return false;

            Write(dbuffer, srcOffset, destLen);
            Write(sbuffer, destOffset, srcLen);

            return true;
        }

        #region /               -- Helper Read/Write methods --              /

        public byte ReadByte(int ofs) { return Marshal.ReadByte(_mvPtr, ofs); }
        public short ReadInt16(int ofs) { return Marshal.ReadInt16(_mvPtr, ofs); }
        public int ReadInt32(int ofs) { return Marshal.ReadInt32(_mvPtr, ofs); }
        public long ReadInt64(int ofs) { return Marshal.ReadInt64(_mvPtr, ofs); }
        public ushort ReadUInt16(int ofs) { return (ushort)Marshal.ReadInt16(_mvPtr, ofs); }
        public uint ReadUInt32(int ofs) { return (uint)Marshal.ReadInt32(_mvPtr, ofs); }
        public ulong ReadUInt64(int ofs) { return (ulong)Marshal.ReadInt64(_mvPtr, ofs); }

        public void WriteByte(int ofs, byte val) { Marshal.WriteByte(_mvPtr, ofs, val); }
        public void WriteInt16(int ofs, short val) { Marshal.WriteInt16(_mvPtr, ofs, val); }
        public void WriteInt32(int ofs, int val) { Marshal.WriteInt32(_mvPtr, ofs, val); }
        public void WriteInt64(int ofs, long val) { Marshal.WriteInt64(_mvPtr, ofs, val); }
        public void WriteUInt16(int ofs, ushort val) { Marshal.WriteInt16(_mvPtr, ofs, (short)val); }
        public void WriteUInt32(int ofs, uint val) { Marshal.WriteInt32(_mvPtr, ofs, (int)val); }
        public void WriteUInt64(int ofs, ulong val) { Marshal.WriteInt64(_mvPtr, ofs, (long)val); }

        /// <summary>
        ///	RawRead the number of bytes from the buffer.
        /// </summary>
        public byte[] Read(int offSet, int count)
        {
            if ((offSet < 0) || (offSet + count) > viewLength)
            {
                return null;
            }
            byte[] buffer = new byte[count];
            Marshal.Copy(new IntPtr(_mvPtr.ToInt64() + offSet), buffer, 0, count);
            return buffer;
        }
        public bool Read(byte[] buffer, int offSet, int count)
        {
            if ((offSet < 0) || (offSet + count) > viewLength)
            {
                return false;
            }
            Marshal.Copy(new IntPtr(_mvPtr.ToInt64() + offSet), buffer, 0, count);
            return true;
        }

        /// <summary>
        ///	RawWrite the binary data into the memory mapped file.
        /// </summary>
        public bool Write(byte[] buffer, int offSet)
        {
            return Write(buffer, offSet, buffer.Length);
        }
        public bool Write(byte[] buffer, int offSet, int count)
        {
            if ((offSet < 0) || (offSet + count) > viewLength)
            {
                return false;
            }
            Marshal.Copy(buffer, 0, new IntPtr(_mvPtr.ToInt64() + offSet), count);
            return true;
        }

        #endregion
    }
}