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
using System.Runtime.InteropServices; 
using System.Security.Permissions;

using Alachisoft.NCache.Common.Interop;

namespace Alachisoft.NCache.Storage.Interop
{
    /// <summary>
	///	Memory mapped file helper class that provides the Win32 functions and 
	///	the conversion methods.
	/// </summary>
	//[CLSCompliant(false)]
	internal class Win32Mmf
	{
		
		/// <summary>
		///	Private constructor prevents class from getting created.
		/// </summary>
		private Win32Mmf()
		{}

		[DllImport("Kernel32.dll", SetLastError = true)]
		public static extern int GetFileSize(IntPtr hFile, out uint lpFileSizeHigh);

		[DllImport("Kernel32.dll", SetLastError = true)]
		public static extern bool GetFileSizeEx(IntPtr hFile, out ulong lpFileSize);

		[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern IntPtr CreateFileMapping(
			IntPtr hFile, // Handle to file
			IntPtr lpAttributes, // Security
			MemoryProtection flProtect, // protection
			uint dwMaximumSizeHigh, // High-order DWORD of size
			uint dwMaximumSizeLow, // Low-order DWORD of size
			string lpName // Object name
			);

		[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]     
		public static extern IntPtr CreateFile(
			string lpFileName, // File name
			Win32FileAccess dwDesiredAccess, // Access mode
			Win32FileShare dwShareMode, // Share mode
			IntPtr lpSecurityAttributes, // SD
			Win32FileMode dwCreationDisposition, // How to create
			Win32FileAttributes dwFlagsAndAttributes, // File attributes
			IntPtr hTemplateFile // Handle to template file
			);

		[DllImport("kernel32")]
		public static extern IntPtr OpenFileMapping(
			Win32FileMapAccess dwDesiredAccess, // Access mode
			bool isInheritHandle, // Inherit flag
			string lpName // Object name
			);

		[DllImport("kernel32")]
		public static extern IntPtr MapViewOfFile(
			IntPtr hFileMappingObject, // handle to file-mapping object
			Win32FileMapAccess dwDesiredAccess, // Access mode
			uint dwFileOffsetHigh, // High-order DWORD of offset
			uint dwFileOffsetLow, // Low-order DWORD of offset
			uint dwNumberOfBytesToMap // Number of bytes to map
			);

		[DllImport("kernel32")]
		public static extern bool FlushViewOfFile(
			IntPtr lpBaseAddress, // Starting address
			uint dwNumberOfBytesToFlush	// Number of bytes in range
			);

		[DllImport("kernel32")]
		public static extern bool UnmapViewOfFile(
			IntPtr lpBaseAddress // Starting address
			);

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool CloseHandle(IntPtr hFile);

		public static Win32FileMapAccess GetWin32FileMapAccess( 
			MemoryProtection protection )
		{
			switch ( protection )
			{
				case MemoryProtection.PageReadOnly:
					return Win32FileMapAccess.FILE_MAP_READ;
				case MemoryProtection.PageWriteCopy:
					return Win32FileMapAccess.FILE_MAP_WRITE;
				default:
					return Win32FileMapAccess.FILE_MAP_ALL_ACCESS;
			}
		}

		public static string GetWin32ErrorMessage( uint error )
		{
			StringBuilder buff = new StringBuilder( 1024 );
			uint len = Win32.FormatMessage(Constants.FORMAT_MESSAGE_FROM_SYSTEM,
				IntPtr.Zero, 
				error,
				0,
				buff,
				1024,
				IntPtr.Zero );
			return buff.ToString( 0, (int)len );
		}        
	}


	//[CLSCompliant(false)]
}
