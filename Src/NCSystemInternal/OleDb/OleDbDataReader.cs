using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace System.Data.OleDb
{
    //
    // Summary:
    //     Provides a way of reading a forward-only stream of data rows from a data source.
    //     This class cannot be inherited.
    //[DefaultMember("Item")]
    public sealed class OleDbDataReader : DbDataReader
    {
        //
        // Summary:
        //     Gets the value of the specified column in its native format given the column
        //     name.
        //
        // Parameters:
        //   name:
        //     The column name.
        //
        // Returns:
        //     The value of the specified column in its native format.
        //
        // Exceptions:
        //   T:System.IndexOutOfRangeException:
        //     No column with the specified name was found.
        public override object this[string name] { get { throw new NotImplementedException(); } }
        //
        // Summary:
        //     Gets the value of the specified column in its native format given the column
        //     ordinal.
        //
        // Parameters:
        //   index:
        //     The column ordinal.
        //
        // Returns:
        //     The value of the specified column in its native format.
        //
        // Exceptions:
        //   T:System.IndexOutOfRangeException:
        //     The index passed was outside the range of 0 through System.Data.IDataRecord.FieldCount.
        public override object this[int index] { get { throw new NotImplementedException(); } }

        //
        // Summary:
        //     Gets the number of fields in the System.Data.OleDb.OleDbDataReader that are not
        //     hidden.
        //
        // Returns:
        //     The number of fields that are not hidden.
        public override int VisibleFieldCount { get; }
        //
        // Summary:
        //     Gets a value that indicates whether the System.Data.OleDb.OleDbDataReader contains
        //     one or more rows.
        //
        // Returns:
        //     true if the System.Data.OleDb.OleDbDataReader contains one or more rows; otherwise
        //     false.
        public override bool HasRows { get; }
        //
        // Summary:
        //     Indicates whether the data reader is closed.
        //
        // Returns:
        //     true if the System.Data.OleDb.OleDbDataReader is closed; otherwise, false.
        public override bool IsClosed { get; }
        //
        // Summary:
        //     Gets the number of rows changed, inserted, or deleted by execution of the SQL
        //     statement.
        //
        // Returns:
        //     The number of rows changed, inserted, or deleted; 0 if no rows were affected
        //     or the statement failed; and -1 for SELECT statements.
        public override int RecordsAffected { get; }
        //
        // Summary:
        //     Gets the number of columns in the current row.
        //
        // Returns:
        //     When not positioned in a valid recordset, 0; otherwise the number of columns
        //     in the current record. The default is -1.
        //
        // Exceptions:
        //   T:System.NotSupportedException:
        //     There is no current connection to a data source.
        public override int FieldCount { get; }
        //
        // Summary:
        //     Gets a value that indicates the depth of nesting for the current row.
        //
        // Returns:
        //     The depth of nesting for the current row.
        public override int Depth { get; }

        //
        // Summary:
        //     Closes the System.Data.OleDb.OleDbDataReader object.
        public override void Close()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a Boolean.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the column.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        public override bool GetBoolean(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a byte.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the specified column as a byte.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        public override byte GetByte(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Reads a stream of bytes from the specified column offset into the buffer as an
        //     array starting at the given buffer offset.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        //   dataIndex:
        //     The index within the field from which to start the read operation.
        //
        //   buffer:
        //     The buffer into which to read the stream of bytes.
        //
        //   bufferIndex:
        //     The index within the buffer where the write operation is to start.
        //
        //   length:
        //     The maximum length to copy into the buffer.
        //
        // Returns:
        //     The actual number of bytes read.
        public override long GetBytes(int ordinal, long dataIndex, byte[] buffer, int bufferIndex, int length)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a character.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the specified column.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override char GetChar(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Reads a stream of characters from the specified column offset into the buffer
        //     as an array starting at the given buffer offset.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        //   dataIndex:
        //     The index within the row from which to start the read operation.
        //
        //   buffer:
        //     The buffer into which to copy data.
        //
        //   bufferIndex:
        //     The index within the buffer where the write operation is to start.
        //
        //   length:
        //     The number of characters to read.
        //
        // Returns:
        //     The actual number of characters read.
        public override long GetChars(int ordinal, long dataIndex, char[] buffer, int bufferIndex, int length)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Returns an System.Data.OleDb.OleDbDataReader object for the requested column
        //     ordinal.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     An System.Data.OleDb.OleDbDataReader object.
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public OleDbDataReader GetData(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the name of the source data type.
        //
        // Parameters:
        //   index:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The name of the back-end data type. For more information, see SQL Server data
        //     types or Access data types.
        public override string GetDataTypeName(int index)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a System.DateTime object.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the specified column.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        public override DateTime GetDateTime(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a System.Decimal object.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the specified column.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        public override decimal GetDecimal(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a double-precision floating-point number.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the specified column.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        public override double GetDouble(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Returns an System.Collections.IEnumerator that can be used to iterate through
        //     the rows in the data reader.
        //
        // Returns:
        //     An System.Collections.IEnumerator that can be used to iterate through the rows
        //     in the data reader.
        public override IEnumerator GetEnumerator()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the System.Type that is the data type of the object.
        //
        // Parameters:
        //   index:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The System.Type that is the data type of the object.
        public override Type GetFieldType(int index)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a single-precision floating-point number.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the specified column.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        public override float GetFloat(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a globally unique identifier (GUID).
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the specified column.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        public override Guid GetGuid(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a 16-bit signed integer.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the specified column.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        public override short GetInt16(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a 32-bit signed integer.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the specified column.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        public override int GetInt32(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a 64-bit signed integer.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the specified column.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        public override long GetInt64(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the name of the specified column.
        //
        // Parameters:
        //   index:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The name of the specified column.
        public override string GetName(int index)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the column ordinal, given the name of the column.
        //
        // Parameters:
        //   name:
        //     The name of the column.
        //
        // Returns:
        //     The zero-based column ordinal.
        //
        // Exceptions:
        //   T:System.IndexOutOfRangeException:
        //     The name specified is not a valid column name.
        public override int GetOrdinal(string name)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Returns a System.Data.DataTable that describes the column metadata of the System.Data.OleDb.OleDbDataReader.
        //
        // Returns:
        //     A System.Data.DataTable that describes the column metadata.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The System.Data.OleDb.OleDbDataReader is closed.
        public override DataTable GetSchemaTable()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a string.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the specified column.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        public override string GetString(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the specified column as a System.TimeSpan object.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value of the specified column.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The specified cast is not valid.
        public TimeSpan GetTimeSpan(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the value of the column at the specified ordinal in its native format.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     The value to return.
        public override object GetValue(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Populates an array of objects with the column values of the current row.
        //
        // Parameters:
        //   values:
        //     An array of System.Object into which to copy the attribute columns.
        //
        // Returns:
        //     The number of instances of System.Object in the array.
        public override int GetValues(object[] values)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets a value that indicates whether the column contains nonexistent or missing
        //     values.
        //
        // Parameters:
        //   ordinal:
        //     The zero-based column ordinal.
        //
        // Returns:
        //     true if the specified column value is equivalent to System.DBNull; otherwise,
        //     false.
        public override bool IsDBNull(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Advances the data reader to the next result, when reading the results of batch
        //     SQL statements.
        //
        // Returns:
        //     true if there are more result sets; otherwise, false.
        public override bool NextResult()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Advances the System.Data.OleDb.OleDbDataReader to the next record.
        //
        // Returns:
        //     true if there are more rows; otherwise, false.
        public override bool Read()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }

        protected override DbDataReader GetDbDataReader(int ordinal)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
    }
}
