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
using System.IO;
using System.Reflection;
using System.Resources;
using System.Security;
using System.Text;
using System.Security.Permissions;

using Alachisoft.NCache.Storage.Mmf;
using Alachisoft.NCache.Common.Interop;
using Alachisoft.NCache.Storage.Interop;

namespace Alachisoft.NCache.Storage.Mmf
{
    /// <summary>
	///	This class provides methods to create, provide file
	///	access, write, read and remove a memory mapped file
	///	from the store.
	/// </summary>
	internal class MmfFile : IDisposable
	{
		private static readonly IntPtr INVALID_HANDLE = new IntPtr(-1);

		private IntPtr				_hFile;
		private IntPtr				_mmHandle;
		private ulong				_mmLength;
		private MemoryProtection	_protection;

		private string objectName = Guid.NewGuid().ToString();

		private MmfFile(IntPtr hFile, ulong maxLength, MemoryProtection protection)
		{
			if (hFile == IntPtr.Zero)
				throw new ArgumentOutOfRangeException("hFile");
			if (protection < MemoryProtection.PageNoAccess || protection > MemoryProtection.SecReserve)
				throw new ArgumentOutOfRangeException("protection");

			_hFile = hFile;
			_mmLength = maxLength;
			_protection = protection;
		}


		/// <summary> Gets the maximum length of the file. </summary>
		public ulong MaxLength { get { return _mmLength; } }
		/// <summary> Gets the maximum length of the file. </summary>
		public bool IsPageFile{ get { return _hFile == INVALID_HANDLE; } }
		public ulong FileSize
		{
			get
			{
				ulong length;
				if (Win32Mmf.GetFileSizeEx(_hFile, out length) == false)
				{
					throw new IOException(Win32Mmf.GetWin32ErrorMessage(Win32.GetLastError()));
				}
				return length;
			}
		}

		/// <summary>
		///	Dispose the instance of the memory mapped
		///	file.
		/// </summary>
		public void Dispose()
		{
			Close();
		}

		/// <summary>
		///	Close the memory mapped file.
		/// </summary>
		/// <remarks>
		///	Close();
		/// </remarks>
		public void Close()
		{
			CloseMapHandle();
			if (_hFile != IntPtr.Zero)
			{
				Win32Mmf.CloseHandle(_hFile);
			}
			System.GC.SuppressFinalize(this);
		}

		/// <summary>
		///	Close the map handle of the memory map.
		/// </summary>
		/// <remarks>
		///	CloseMapHandle();
		/// </remarks>
		public void CloseMapHandle()
		{
			if (_mmHandle != IntPtr.Zero)
			{
				Win32Mmf.CloseHandle(_mmHandle);
			}
		}

		/// <summary>
		///	View the length and the offset of the memory
		///	mapped file.
		/// </summary>
		/// <remarks>
		///	MapView( offSet, count );
		/// </remarks>
		/// <param name="offSet">
		///	The start position
		/// </param>
		/// <param name="count">
		///	The length of the binary stream
		/// </param>
		public MmfFileView MapView(ulong offSet, uint count)
		{
			if(offSet < 0)
				throw new ArgumentOutOfRangeException("offSet");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count");

 			IntPtr mapViewPointer = Win32Mmf.MapViewOfFile(
				_mmHandle,
				Win32Mmf.GetWin32FileMapAccess( _protection ),
				(uint)(offSet >> 32),
				(uint)offSet & 0xffffffff,
				(uint)count);

			if( mapViewPointer == IntPtr.Zero )
			{
				uint error = Win32.GetLastError();
				if (error == Constants.ERROR_NOT_ENOUGH_MEMORY)
				{
					throw new OutOfMemoryException();
				}
				throw new IOException(Win32Mmf.GetWin32ErrorMessage(error));
			}
            return new MmfFileView(mapViewPointer, count);
		}

		public void UnMapView(MmfFileView view)
        {
            try
            {
				Win32Mmf.UnmapViewOfFile(view.ViewPtr);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="viewSize"></param>
		public int GetMaxViews(uint viewSize)
		{
			ulong maxSize = MaxLength;
			int totalViews = (int)(maxSize / viewSize);

			if (maxSize % viewSize > 0)
			{
				totalViews++;
			}

			return totalViews;
		}

		/// <summary>
		///	Set the maximum length of the file.
		/// </summary>
		/// <remarks>
		///	SetMaxLength( maxLength );
		/// </remarks>
		/// <param name="maxLength">
		///	Specifies the length to be set
		/// </param>
		public void SetMaxLength(ulong maxLength)
		{
			CloseMapHandle();
			_mmLength = maxLength;
			_mmHandle = Win32Mmf.CreateFileMapping(
				_hFile,
				IntPtr.Zero,       
				_protection,
				(uint)(_mmLength >> 32),
				(uint)_mmLength & 0xffffffff,
				objectName);

			if( _mmHandle == IntPtr.Zero )
			{
				uint error = Win32.GetLastError();
				if (error == Constants.ERROR_NOT_ENOUGH_MEMORY)
				{
					throw new OutOfMemoryException();
				}
				if (error == Constants.ERROR_DISK_FULL)
				{
					throw new OutOfMemoryException();
				}
                if (error == Constants.ERROR_COMMITMENT_LIMIT)
                {
                    throw new OutOfMemoryException("Limited Virtual Memory. Your system has no paging file, or the paging file is too small.");
                }
                Trace.error("MmfFile.SetMaxLength() Error Number is ::" + error, Win32Mmf.GetWin32ErrorMessage(error));
                throw new IOException(Win32Mmf.GetWin32ErrorMessage(error));
			}
		}

		public static MmfFile Create(string name, bool resetContents)
		{
			return Create(name, 0, resetContents);
		}

		public static MmfFile Create(string name, ulong maxLength, bool resetContents)
		{
			if (name == null)
				return Create(INVALID_HANDLE, maxLength);

			IntPtr hFile = Win32Mmf.CreateFile(name,
				Win32FileAccess.GENERIC_READ | Win32FileAccess.GENERIC_WRITE,
				Win32FileShare.FILE_SHARE_READ,
				IntPtr.Zero,
				resetContents ? Win32FileMode.CREATE_ALWAYS : Win32FileMode.OPEN_ALWAYS,
				Win32FileAttributes.FILE_ATTRIBUTE_NORMAL,
				IntPtr.Zero);
			if (hFile == IntPtr.Zero)
			{
				throw new IOException(Win32Mmf.GetWin32ErrorMessage(Win32.GetLastError()));
			}

			if (maxLength <= 0)
			{
				if (Win32Mmf.GetFileSizeEx(hFile, out maxLength) == false)
				{
					throw new IOException(Win32Mmf.GetWin32ErrorMessage(Win32.GetLastError()));
				}
			}

			return Create(hFile, maxLength);
		}

		public static MmfFile Create(IntPtr hFile, ulong maxLength)
		{
			if (maxLength < 0)
				throw new ArgumentOutOfRangeException("maxLength");

			MmfFile file = new MmfFile(hFile, maxLength, MemoryProtection.PageReadWrite);
			if(!file.IsPageFile)
				file.SetMaxLength(Math.Max(maxLength, file.FileSize));
			else
				file.SetMaxLength(maxLength);
			return file;
		}
	}
}
