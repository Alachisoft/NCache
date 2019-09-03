// ==++==
// 
//   Copyright (c). 2015. Microsoft Corporation.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ==--==
/*============================================================
**
** Class:  __Error
**
**
** Purpose: Centralized error methods. Used for translating 
** Win32 HRESULTs into meaningful error strings & exceptions.
**
**
===========================================================*/

using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    public static class __Error
    {

        public static void EndOfFile()
        {
            throw new EndOfStreamException(ResourceHelper.GetResourceString("IO.EOF_ReadBeyondEOF"));
        }
        public static void StreamIsClosed()
        {
            throw new ObjectDisposedException(null, ResourceHelper.GetResourceString("ObjectDisposed_StreamClosed"));
        }
        public static void MemoryStreamNotExpandable()
        {
            throw new NotSupportedException(ResourceHelper.GetResourceString("NotSupported_MemStreamNotExpandable"));
        }
        public static void WriteNotSupported()
        {
            throw new NotSupportedException(ResourceHelper.GetResourceString("NotSupported_UnwritableStream"));
        }

        internal static void SeekNotSupported()
        {
            throw new NotSupportedException(ResourceHelper.GetResourceString("NotSupported_UnseekableStream"));
        }

        internal static void ReadNotSupported()
        {
            throw new NotSupportedException(ResourceHelper.GetResourceString("NotSupported_UnreadableStream"));
        }
    }
}
