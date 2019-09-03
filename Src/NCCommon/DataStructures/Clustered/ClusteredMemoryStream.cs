﻿// ==++==
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
** Class:  ClusteredMemoryStream
** 
** <OWNER>[....]</OWNER>
**
**
** Purpose: A Stream whose backing store is clustered memory.  Great
** for temporary storage without creating a temp file. 
**
**
===========================================================*/

using System;
using System.Collections;
using System.Runtime;
using System.Runtime.InteropServices;
#if DEBUG
using System.Diagnostics.Contracts;
#endif
using System.IO;

#if FEATURE_ASYNC_IO
using System.Threading;
using System.Threading.Tasks;
using System.Security.Permissions;
#endif

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    // A MemoryStream represents a Stream in memory (ie, it has no backing store).
    // This stream may reduce the need for temporary buffers and files in 
    // an application.  
    // 
    // There are two ways to create a MemoryStream.  You can initialize one
    // from an unsigned byte array, or you can create an empty one.  Empty 
    // memory streams are resizable, while ones created with a byte array provide
    // a stream "view" of the data.
    [Serializable]
    [ComVisible(true)]
    public class ClusteredMemoryStream : Stream
    {
        private ClusteredArray<byte> _buffer;    // Either allocated internally or externally.
        private int _origin;       // For user-provided arrays, start at this origin
        private int _position;     // read/write head.
#if DEBUG
        [ContractPublicPropertyName("Length")]
#endif
        private int _length;       // Number of bytes within the memory stream
        private int _capacity;     // length of usable portion of buffer for stream
        // Note that _capacity == _buffer.Length for non-user-provided byte[]'s

        private bool _expandable;  // User-provided buffers aren't expandable.
        private bool _writable;    // Can user write to this stream?
        private bool _exposable;   // Whether the array can be returned to the user.
        private bool _isOpen;      // Is this stream open or closed?

#if FEATURE_ASYNC_IO
        [NonSerialized]
        private Task<int> _lastReadTask; // The last successful task returned from ReadAsync
#endif

        // <

        private const int MemStreamMaxLength = Int32.MaxValue;

        public ClusteredMemoryStream()
            : this(0)
        {
        }

        public ClusteredMemoryStream(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", ResourceHelper.GetResourceString("ArgumentOutOfRange_NegativeCapacity"));
            }
#if DEBUG
            Contract.EndContractBlock();
#endif
            _buffer = new ClusteredArray<byte>(capacity);
            _capacity = capacity;
            _expandable = true;
            _writable = true;
            _exposable = true;
            _origin = 0;      // Must be 0 for byte[]'s created by MemoryStream
            _isOpen = true;
        }

#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public ClusteredMemoryStream(byte[] buffer)
            : this(buffer, true)
        {
        }

        public ClusteredMemoryStream(ClusteredArray<byte> buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer", ResourceHelper.GetResourceString("ArgumentNull_Buffer"));
#if DEBUG
            Contract.EndContractBlock();
#endif
            _buffer = buffer;
            _length = _capacity = buffer.Length;
            _writable = true;
            _exposable = false;
            _origin = 0;
            _isOpen = true;
        }

        public ClusteredMemoryStream(byte[] buffer, bool writable)
        {
            if (buffer == null) throw new ArgumentNullException("buffer", ResourceHelper.GetResourceString("ArgumentNull_Buffer"));
#if DEBUG
            Contract.EndContractBlock();
#endif
            _buffer = new ClusteredArray<byte>(buffer.Length);
            _buffer.CopyFrom(buffer, 0, 0, buffer.Length);
            _length = _capacity = buffer.Length;
            _writable = writable;
            _exposable = false;
            _origin = 0;
            _isOpen = true;
        }

        public ClusteredMemoryStream(byte[][] buffer)
            : this(buffer, true)
        { }

        public ClusteredMemoryStream(byte[][] buffer, bool writable)
        {
            int position = 0;
            foreach (byte[] bytes in buffer)
            {
                _buffer.CopyFrom(bytes, 0, position, bytes.Length);
                position += bytes.Length;
            }
            _length = _capacity = position;
            _writable = writable;
            _expandable = false;
            _origin = 0;
            _isOpen = true;
        }

        public ClusteredMemoryStream(IList buffers, bool writable)
        {
            int position = 0;
            _buffer = new ClusteredArray<byte>(0);
            foreach (byte[] bytes in buffers)
            {
                _buffer.CopyFrom(bytes, 0, position, bytes.Length);
                position += bytes.Length;
            }
            _length = _capacity = position;
            _writable = writable;
            _expandable = false;
            _origin = 0;
            _isOpen = true;
        }

        public ClusteredMemoryStream(byte[] buffer, int index, int count)
            : this(buffer, index, count, true, false)
        {
        }

        public ClusteredMemoryStream(byte[] buffer, int index, int count, bool writable)
            : this(buffer, index, count, writable, false)
        {
        }

        //Changelog: Used ClusteredArray's internal CopyFrom method.
        public ClusteredMemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", ResourceHelper.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(ResourceHelper.GetResourceString("Argument_InvalidOffLen"));
#if DEBUG
            Contract.EndContractBlock();
#endif
            _buffer = new ClusteredArray<byte>(buffer.Length);
            _buffer.CopyFrom(buffer, 0, 0, buffer.Length);
            _origin = _position = index;
            _length = _capacity = index + count;
            _writable = writable;
            _exposable = publiclyVisible;  // Can GetBuffer return the array?
            _expandable = false;
            _isOpen = true;
        }

        public override bool CanRead
        {
#if DEBUG
            [Pure]
#endif
            get { return _isOpen; }
        }

        public override bool CanSeek
        {
#if DEBUG
            [Pure]
#endif
            get { return _isOpen; }
        }

        public override bool CanWrite
        {
#if DEBUG
            [Pure]
#endif
            get { return _writable; }
        }

        private void EnsureWriteable()
        {
            // Previously, instead of calling CanWrite we just checked the _writable field directly, and some code
            // took a dependency on that behavior.
#if FEATURE_CORECLR
            if (IsAppEarlierThanSl4) {
                if (!_writable) __Error.WriteNotSupported();
            } else {
                if (!CanWrite) __Error.WriteNotSupported();
            }
#else
            if (!CanWrite) __Error.WriteNotSupported();
#endif
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _isOpen = false;
                    _writable = false;
                    _expandable = false;
                    // Don't set buffer to null - allow GetBuffer & ToArray to work.
#if FEATURE_ASYNC_IO
                    _lastReadTask = null;
#endif
                }
            }
            finally
            {
                // Call base.Close() to cleanup async IO resources
                base.Dispose(disposing);
            }
        }

        // returns a bool saying whether we allocated a new array.
        private bool EnsureCapacity(int value)
        {
            // Check for overflow
            if (value < 0)
                throw new IOException(ResourceHelper.GetResourceString("IO.IO_StreamTooLong"));
            if (value > _capacity)
            {
                int newCapacity = value;
                if (newCapacity < 256)
                    newCapacity = 256;
                if (newCapacity < _capacity * 2)
                    newCapacity = _capacity * 2;
                Capacity = newCapacity;
                return true;
            }
            return false;
        }

        public override void Flush()
        {
        }

#if FEATURE_ASYNC_IO
        [HostProtection(ExternalThreading=true)]
        [ComVisible(false)]
        public override Task FlushAsync(CancellationToken cancellationToken) {

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation(cancellationToken);

            try {

                Flush();
                return Task.CompletedTask;
        
            } catch(Exception ex) {

                return Task.FromException(ex);
            }
        }
#endif // FEATURE_ASYNC_IO


        //Changelog: Used ClusteredArray's internal CopyTo method.
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public virtual ClusteredArray<byte> GetBuffer()
        {
            if (!_exposable)
                throw new UnauthorizedAccessException(ResourceHelper.GetResourceString("UnauthorizedAccess_MemStreamBuffer"));
            return (ClusteredArray<byte>)_buffer.Clone();
        }

        public ClusteredArrayList GetInternalBuffer()
        {
            return _buffer.ToInternalList(Length);
        }


        //Added for clustered array support



        // -------------- PERF: Internal functions for fast direct access of MemoryStream buffer (cf. BinaryReader for usage) ---------------

        // PERF: Internal sibling of GetBuffer, always returns a buffer (cf. GetBuffer())
        //Changelog: Used ClusteredArray's internal CopyTo method.
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        internal byte[] InternalGetBuffer()
        {
            byte[] bytes = new byte[_length];
            _buffer.CopyTo(bytes, 0, 0, _length);
            return bytes;
        }

        // PERF: Get origin and length - used in ResourceWriter.
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        internal void InternalGetOriginAndLength(out int origin, out int length)
        {
            if (!_isOpen) __Error.StreamIsClosed();
            origin = _origin;
            length = _length;
        }

        // PERF: True cursor position, we don't need _origin for direct access
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        internal int InternalGetPosition()
        {
            if (!_isOpen) __Error.StreamIsClosed();
            return _position;
        }

        // PERF: Takes out Int32 as fast as possible
        internal int InternalReadInt32()
        {
            if (!_isOpen)
                __Error.StreamIsClosed();

            int pos = (_position += 4); // use temp to avoid ----
            if (pos > _length)
            {
                _position = _length;
                __Error.EndOfFile();
            }
            return (int)(_buffer[pos - 4] | _buffer[pos - 3] << 8 | _buffer[pos - 2] << 16 | _buffer[pos - 1] << 24);
        }

        // PERF: Get actual length of bytes available for read; do sanity checks; shift position - i.e. everything except actual copying bytes
        internal int InternalEmulateRead(int count)
        {
            if (!_isOpen) __Error.StreamIsClosed();

            int n = _length - _position;
            if (n > count) n = count;
            if (n < 0) n = 0;

#if DEBUG
            Contract.Assert(_position + n >= 0, "_position + n >= 0");  // len is less than 2^31 -1.
#endif
            _position += n;
            return n;
        }

        // Gets & sets the capacity (number of bytes allocated) for this stream.
        // The capacity cannot be set to a value less than the current length
        // of the stream.
        // 
        public virtual int Capacity
        {
            get
            {
                if (!_isOpen) __Error.StreamIsClosed();
                return _capacity - _origin;
            }
            set
            {
                // Only update the capacity if the MS is expandable and the value is different than the current capacity.
                // Special behavior if the MS isn't expandable: we don't throw if value is the same as the current capacity
#if !FEATURE_CORECLR
                if (value < Length) throw new ArgumentOutOfRangeException("value", ResourceHelper.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
#endif
#if DEBUG
                Contract.Ensures(_capacity - _origin == value);
                Contract.EndContractBlock();
#endif
#if FEATURE_CORECLR              
                if (IsAppEarlierThanSl4) {
                    if (value < _length) throw new ArgumentOutOfRangeException("value", ResourceHelper.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                } else {
                    if (value < Length) throw new ArgumentOutOfRangeException("value", ResourceHelper.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                }
#endif

                if (!_isOpen) __Error.StreamIsClosed();
                if (!_expandable && (value != Capacity)) __Error.MemoryStreamNotExpandable();

                // MemoryStream has this invariant: _origin > 0 => !expandable (see ctors)
                if (_expandable && value != _capacity)
                {
                    if (value > 0)
                    {
                        _buffer.Resize(value);
                    }
                    else
                    {
                        _buffer = null;
                    }
                    _capacity = value;
                }
            }
        }

        public override long Length
        {
#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            get
            {
                if (!_isOpen) __Error.StreamIsClosed();
                return _length - _origin;
            }
        }

        public override long Position
        {
#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            get
            {
                if (!_isOpen) __Error.StreamIsClosed();
                return _position - _origin;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
#if DEBUG
                Contract.Ensures(Position == value);
                Contract.EndContractBlock();
#endif
                if (!_isOpen) __Error.StreamIsClosed();

                if (value > MemStreamMaxLength)
                    throw new ArgumentOutOfRangeException("value", ResourceHelper.GetResourceString("ArgumentOutOfRange_StreamLength"));
                _position = _origin + (int)value;
            }
        }

#if FEATURE_CORECLR
        private static bool IsAppEarlierThanSl4 {
            get {
                return CompatibilitySwitches.IsAppEarlierThanSilverlight4;
            }
        }
#endif

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", ResourceHelper.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(ResourceHelper.GetResourceString("Argument_InvalidOffLen"));
#if DEBUG
            Contract.EndContractBlock();
#endif
            if (!_isOpen) __Error.StreamIsClosed();

            int n = _length - _position;
            if (n > count) n = count;
            if (n <= 0)
                return 0;
#if DEBUG
            Contract.Assert(_position + n >= 0, "_position + n >= 0");  // len is less than 2^31 -1.
#endif
            if (n <= 8)
            {
                int byteCount = n;
                while (--byteCount >= 0)
                    buffer[offset + byteCount] = _buffer[_position + byteCount];
            }
            else
                _buffer.CopyTo(buffer, offset, _position, n);
            _position += n;

            return n;
        }

#if FEATURE_ASYNC_IO
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer==null)
                throw new ArgumentNullException("buffer", ResourceHelper.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(ResourceHelper.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock(); // contract validation copied from Read(...)

            // If cancellation was requested, bail early
            if (cancellationToken.IsCancellationRequested) 
                return Task.FromCancellation<int>(cancellationToken);

            try
            {
                int n = Read(buffer, offset, count);
                var t = _lastReadTask;
                Contract.Assert(t == null || t.Status == TaskStatus.RanToCompletion, 
                    "Expected that a stored last task completed successfully");
                return (t != null && t.Result == n) ? t : (_lastReadTask = Task.FromResult<int>(n));
            }
            catch (OperationCanceledException oce)
            {
                return Task.FromCancellation<int>(oce);
            }
            catch (Exception exception)
            {
                return Task.FromException<int>(exception);
            }
        }
#endif //FEATURE_ASYNC_IO


        public override int ReadByte()
        {
            if (!_isOpen) __Error.StreamIsClosed();

            if (_position >= _length) return -1;

            return _buffer[_position++];
        }


#if FEATURE_ASYNC_IO
        public override Task CopyToAsync(Stream destination, Int32 bufferSize, CancellationToken cancellationToken) {

            // This implementation offers beter performance compared to the base class version.

            // The parameter checks must be in [....] with the base version:
            if (destination == null)
                throw new ArgumentNullException("destination");
            
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedPosNum"));

            if (!CanRead && !CanWrite)
                throw new ObjectDisposedException(null, ResourceHelper.GetResourceString("ObjectDisposed_StreamClosed"));

            if (!destination.CanRead && !destination.CanWrite)
                throw new ObjectDisposedException("destination", ResourceHelper.GetResourceString("ObjectDisposed_StreamClosed"));

            if (!CanRead)
                throw new NotSupportedException(ResourceHelper.GetResourceString("NotSupported_UnreadableStream"));

            if (!destination.CanWrite)
                throw new NotSupportedException(ResourceHelper.GetResourceString("NotSupported_UnwritableStream"));

            Contract.EndContractBlock();

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() or Write() which a subclass might have overriden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read/Write) when we are not sure.
            if (this.GetType() != typeof(MemoryStream))
                return base.CopyToAsync(destination, bufferSize, cancellationToken);

            // If cancelled - return fast:
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation(cancellationToken);
           
            // Avoid copying data from this buffer into a temp buffer:
            //   (require that InternalEmulateRead does not throw,
            //    otherwise it needs to be wrapped into try-catch-Task.FromException like memStrDest.Write below)

            Int32 pos = _position;
            Int32 n = InternalEmulateRead(_length - _position);

            // If destination is not a memory stream, write there asynchronously:
            MemoryStream memStrDest = destination as MemoryStream;
            if (memStrDest == null)                 
                return destination.WriteAsync(_buffer, pos, n, cancellationToken);
           
            try {

                // If destination is a MemoryStream, CopyTo synchronously:
                memStrDest.Write(_buffer, pos, n);
                return Task.CompletedTask;

            } catch(Exception ex) {
                return Task.FromException(ex);
            }
        }
#endif //FEATURE_ASYNC_IO


        public override long Seek(long offset, SeekOrigin loc)
        {
            if (!_isOpen) __Error.StreamIsClosed();

            if (offset > MemStreamMaxLength)
                throw new ArgumentOutOfRangeException("offset", ResourceHelper.GetResourceString("ArgumentOutOfRange_StreamLength"));
            switch (loc)
            {
                case SeekOrigin.Begin:
                    {
                        int tempPosition = unchecked(_origin + (int)offset);
                        if (offset < 0 || tempPosition < _origin)
                            throw new IOException(ResourceHelper.GetResourceString("IO.IO_SeekBeforeBegin"));
                        _position = tempPosition;
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        int tempPosition = unchecked(_position + (int)offset);
                        if (unchecked(_position + offset) < _origin || tempPosition < _origin)
                            throw new IOException(ResourceHelper.GetResourceString("IO.IO_SeekBeforeBegin"));
                        _position = tempPosition;
                        break;
                    }
                case SeekOrigin.End:
                    {
                        int tempPosition = unchecked(_length + (int)offset);
                        if (unchecked(_length + offset) < _origin || tempPosition < _origin)
                            throw new IOException(ResourceHelper.GetResourceString("IO.IO_SeekBeforeBegin"));
                        _position = tempPosition;
                        break;
                    }
                default:
                    throw new ArgumentException(ResourceHelper.GetResourceString("Argument_InvalidSeekOrigin"));
            }
#if DEBUG
            Contract.Assert(_position >= 0, "_position >= 0");
#endif
            return _position;
        }

        // Sets the length of the stream to a given value.  The new
        // value must be nonnegative and less than the space remaining in
        // the array, Int32.MaxValue - origin
        // Origin is 0 in all cases other than a MemoryStream created on
        // top of an existing array and a specific starting offset was passed 
        // into the MemoryStream constructor.  The upper bounds prevents any 
        // situations where a stream may be created on top of an array then 
        // the stream is made longer than the maximum possible length of the 
        // array (Int32.MaxValue).
        // 
        public override void SetLength(long value)
        {
            if (value < 0 || value > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("value", ResourceHelper.GetResourceString("ArgumentOutOfRange_StreamLength"));
            }
#if DEBUG
            Contract.Ensures(_length - _origin == value);
            Contract.EndContractBlock();
#endif
            EnsureWriteable();

            // Origin wasn't publicly exposed above.
#if DEBUG
            Contract.Assert(MemStreamMaxLength == Int32.MaxValue);  // Check parameter validation logic in this method if this fails.
#endif
            if (value > (Int32.MaxValue - _origin))
            {
                throw new ArgumentOutOfRangeException("value", ResourceHelper.GetResourceString("ArgumentOutOfRange_StreamLength"));
            }

            int newLength = _origin + (int)value;
            bool allocatedNewArray = EnsureCapacity(newLength);
            if (!allocatedNewArray && newLength > _length)
                ClusteredArray<byte>.Clear(_buffer, _length, newLength - _length);
            _length = newLength;
            if (_position > newLength) _position = newLength;

        }

        //Changelog: Used ClusteredArray's internal CopyTo method.
        public virtual byte[] ToArray()
        {
            byte[] copy = new byte[_length - _origin];
            _buffer.CopyTo(copy, 0, _origin, _length - _origin);
            return copy;
        }

        //Changelog: Used ClusteredArray's internal CopyFrom method.
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", ResourceHelper.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(ResourceHelper.GetResourceString("Argument_InvalidOffLen"));
#if DEBUG
            Contract.EndContractBlock();
#endif
            if (!_isOpen) __Error.StreamIsClosed();
            EnsureWriteable();

            int i = _position + count;
            // Check for overflow
            if (i < 0)
                throw new IOException(ResourceHelper.GetResourceString("IO.IO_StreamTooLong"));

            if (i > _length)
            {
                bool mustZero = _position > _length;
                if (i > _capacity)
                {
                    bool allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray)
                        mustZero = false;
                }
                if (mustZero)
                    ClusteredArray<byte>.Clear(_buffer, _length, i - _length);
                _length = i;
            }
            if (count <= 8)
            {
                int byteCount = count;
                while (--byteCount >= 0)
                    _buffer[_position + byteCount] = buffer[offset + byteCount];
            }
            else
                _buffer.CopyFrom(buffer, offset, _position, count);
            _position = i;

        }

#if FEATURE_ASYNC_IO
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", ResourceHelper.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", ResourceHelper.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(ResourceHelper.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock(); // contract validation copied from Write(...)

            // If cancellation is already requested, bail early
            if (cancellationToken.IsCancellationRequested) 
                return Task.FromCancellation(cancellationToken);

            try
            {
                Write(buffer, offset, count);
                return Task.CompletedTask;
            }
            catch (OperationCanceledException oce)
            {
                return Task.FromCancellation<VoidTaskResult>(oce);
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }
#endif // FEATURE_ASYNC_IO

        public override void WriteByte(byte value)
        {
            if (!_isOpen) __Error.StreamIsClosed();
            EnsureWriteable();

            if (_position >= _length)
            {
                int newLength = _position + 1;
                bool mustZero = _position > _length;
                if (newLength >= _capacity)
                {
                    bool allocatedNewArray = EnsureCapacity(newLength);
                    if (allocatedNewArray)
                        mustZero = false;
                }
                if (mustZero)
                    ClusteredArray<byte>.Clear(_buffer, _length, _position - _length);
                _length = newLength;
            }
            _buffer[_position++] = value;

        }

        // Writes this MemoryStream to another stream.
        //Changelog: Used ClusteredArray's internal CopyTo method.
        public virtual void WriteTo(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream", ResourceHelper.GetResourceString("ArgumentNull_Stream"));
#if DEBUG
            Contract.EndContractBlock();
#endif
            if (!_isOpen) __Error.StreamIsClosed();
            byte[] bytes = new byte[_length - _origin];
            _buffer.CopyTo(bytes, 0, _origin, _length - _origin);
            stream.Write(bytes, 0, bytes.Length);
        }

#if CONTRACTS_FULL
        [ContractInvariantMethod]
        private void ObjectInvariantMS() {
            Contract.Invariant(_origin >= 0);
            Contract.Invariant(_origin <= _position);
            Contract.Invariant(_length <= _capacity);
            // equivalent to _origin > 0 => !expandable, and using fact that _origin is non-negative.
            Contract.Invariant(_origin == 0 || !_expandable);
        }
#endif
    }
}
