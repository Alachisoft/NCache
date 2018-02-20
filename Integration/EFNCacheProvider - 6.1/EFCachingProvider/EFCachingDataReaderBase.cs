// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Data.Common;
using System.Data;
using Alachisoft.NCache.Integrations.EntityFramework.CacheEntry;

namespace Alachisoft.NCache.Integrations.EntityFramework
{
    /// <summary>
    /// Base class for caching data readers.
    /// </summary>
    internal abstract class EFCachingDataReaderBase : DbDataReader
    {
        protected CommandBehavior behavior;
        private object[] values;

        /// <summary>
        /// Initializes a new instance of the EFCachingDataReaderBase class.
        /// </summary>
        protected EFCachingDataReaderBase()
        {
            this.values = new object[0];
        }

        /// <summary>
        /// Gets the number of columns in the current row.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The number of columns in the current row.
        /// </returns>
        public override int FieldCount
        {
            get { return this.values.Length; }
        }

        /// <summary>
        /// Gets the value of a column with the specified name.
        /// </summary>
        /// <value>Value of the column.</value>
        /// <param name="name">Column name.</param>
        public override object this[string name]
        {
            get { return this.GetValue(this.GetOrdinal(name)); }
        }

        /// <summary>
        /// Gets the value with the specified ordinal.
        /// </summary>
        /// <value>The value at the specified ordinal.</value>
        /// <param name="ordinal">Column ordinal.</param>
        public override object this[int ordinal]
        {
            get { return this.GetValue(ordinal); }
        }

        /// <summary>
        /// Gets the value of the specified column as a Boolean.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override bool GetBoolean(int ordinal)
        {
            return (bool)this.GetValue(ordinal);
        }

        /// <summary>
        /// Gets the value of the specified column as a byte.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override byte GetByte(int ordinal)
        {
            return (byte)this.GetValue(ordinal);
        }

        /// <summary>
        /// Reads a stream of bytes from the specified column, starting at location indicated by <paramref name="dataOffset"/>, into the buffer, starting at the location indicated by <paramref name="bufferOffset"/>.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <param name="dataOffset">The index within the row from which to begin the read operation.</param>
        /// <param name="buffer">The buffer into which to copy the data.</param>
        /// <param name="bufferOffset">The index with the buffer to which the data will be copied.</param>
        /// <param name="length">The maximum number of characters to read.</param>
        /// <returns>The actual number of bytes read.</returns>
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            return 0;           
        }

        /// <summary>
        /// Gets the value of the specified column as a single character.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override char GetChar(int ordinal)
        {
            return (char)this.GetValue(ordinal);
        }

        /// <summary>
        /// Reads a stream of characters from the specified column, starting at location indicated by <paramref name="dataIndex"/>, into the buffer, starting at the location indicated by <paramref name="bufferIndex"/>.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <param name="dataOffset">The index within the row from which to begin the read operation.</param>
        /// <param name="buffer">The buffer into which to copy the data.</param>
        /// <param name="bufferOffset">The index with the buffer to which the data will be copied.</param>
        /// <param name="length">The maximum number of characters to read.</param>
        /// <returns>The actual number of characters read.</returns>
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            return 0;
        }

        /// <summary>
        /// Gets the value of the specified column as a <see cref="T:System.DateTime"/> object.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override DateTime GetDateTime(int ordinal)
        {
            return (DateTime)this.GetValue(ordinal);
        }

        /// <summary>
        /// Gets the value of the specified column as a <see cref="T:System.Decimal"/> object.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override decimal GetDecimal(int ordinal)
        {
            return (decimal)this.GetValue(ordinal);
        }        

        /// <summary>
        /// Gets the value of the specified column as a double-precision floating point number.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override double GetDouble(int ordinal)
        {
            return (double)this.GetValue(ordinal);
        }

        /// <summary>
        /// Returns an <see cref="T:System.Collections.IEnumerator"/> that can be used to iterate through the rows in the data reader.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> that can be used to iterate through the rows in the data reader.
        /// </returns>
        public override System.Collections.IEnumerator GetEnumerator()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the value of the specified column as a single-precision floating point number.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override float GetFloat(int ordinal)
        {
            return (float)this.GetValue(ordinal);
        }

        /// <summary>
        /// Gets the value of the specified column as a globally-unique identifier (GUID).
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override Guid GetGuid(int ordinal)
        {
            return (Guid)this.GetValue(ordinal);
        }

        /// <summary>
        /// Gets the value of the specified column as a 16-bit signed integer.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override short GetInt16(int ordinal)
        {
            return (short)this.GetValue(ordinal);
        }

        /// <summary>
        /// Gets the value of the specified column as a 32-bit signed integer.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override int GetInt32(int ordinal)
        {
            return (int)this.GetValue(ordinal);
        }

        /// <summary>
        /// Gets the value of the specified column as a 64-bit signed integer.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override long GetInt64(int ordinal)
        {
            return (long)this.GetValue(ordinal);
        }

        /// <summary>
        /// Gets the value of the specified column as an instance of <see cref="T:System.String"/>.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override string GetString(int ordinal)
        {
            return (string)this.GetValue(ordinal);
        }

        /// <summary>
        /// Gets the value of the specified column as an instance of <see cref="T:System.Object"/>.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override object GetValue(int ordinal)
        {
            return this.values[ordinal];
        }

        /// <summary>
        /// Gets all attribute columns in the collection for the current row.
        /// </summary>
        /// <param name="result">An array of <see cref="T:System.Object"/> into which to copy the attribute columns.</param>
        /// <returns>
        /// The number of instances of <see cref="T:System.Object"/> in the array.
        /// </returns>
        public override int GetValues(object[] result)
        {
            Array.Copy(this.values, result, this.values.Length);
            return this.values.Length;
        }

        /// <summary>
        /// Gets a value that indicates whether the column contains nonexistent or missing values.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>
        /// true if the specified column is equivalent to <see cref="T:System.DBNull"/>; otherwise false.
        /// </returns>
        public override bool IsDBNull(int ordinal)
        {
            return this.values[ordinal] is DBNull;
        }

        /// <summary>
        /// Sets the collection of values that the reader should return.
        /// </summary>
        /// <param name="rowValues">The values.</param>
        protected void SetValues(object[] rowValues)
        {
            this.values = rowValues;
        }

        /// <summary>
        /// See if the CommandBehavior flag is set for specified behavior
        /// </summary>
        /// <param name="behavior">CommandBehavior to check</param>
        /// <returns>True if the behavior is set, false otherwise</returns>
        protected bool IsCommandBehaviour(CommandBehavior behavior)
        {
            return (behavior == (behavior & this.behavior));
        }
    }
}
