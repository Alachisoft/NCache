// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Data;
using System.Data.Common;
using Alachisoft.NCache.Integrations.EntityFramework.CacheEntry;

namespace Alachisoft.NCache.Integrations.EntityFramework
{
    /// <summary>
    /// Implementation of the <see cref="DbDataReader"/> which reads the results of another data reader
    /// and stores a copy in the cache.
    /// </summary>
    internal class EFCachingDataReaderCacheWriter : EFCachingDataReaderBase
    {
        private DbQueryResults queryResult;
        private DbDataReader wrappedReader;        
        private Action<DbQueryResults> addToCache;

        /// <summary>
        /// Initializes a new instance of the EFCachingDataReaderCacheWriter class.
        /// </summary>
        /// <param name="wrappedReader">The wrapped reader.</param>
        /// <param name="addToCache">The delegate used to add the result to the cache when the reader finishes.</param>
        /// <param name="behavior">An instance of System.Data.CommandBehavior.</param>
        public EFCachingDataReaderCacheWriter(DbDataReader wrappedReader, Action<DbQueryResults> addToCache, CommandBehavior behavior)
        {
            this.wrappedReader = wrappedReader;
            this.addToCache = addToCache;
            base.behavior = behavior;

            this.queryResult = new DbQueryResults(this.wrappedReader)
            {
                FieldCount = this.wrappedReader.FieldCount,
                VisibleFieldCount = this.wrappedReader.VisibleFieldCount
            };
        }        

        /// <summary>
        /// Gets a value that indicates whether this <see cref="T:System.Data.Common.DbDataReader"/> contains one or more rows.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Data.Common.DbDataReader"/> contains one or more rows; otherwise false.
        /// </returns>
        public override bool HasRows
        {
            get { return this.wrappedReader.HasRows; }
        }

        /// <summary>
        /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The number of rows changed, inserted, or deleted. -1 for SELECT statements; 0 if no rows were affected or the statement failed.
        /// </returns>
        public override int RecordsAffected
        {
            get { return this.wrappedReader.RecordsAffected; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Data.Common.DbDataReader"/> is closed.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Data.Common.DbDataReader"/> is closed; otherwise false.
        /// </returns>
        public override bool IsClosed
        {
            get { return this.wrappedReader.IsClosed; }
        }

        /// <summary>
        /// Gets a value indicating the depth of nesting for the current row.
        /// </summary>
        /// <returns>
        /// The depth of nesting for the current row.
        /// </returns>
        public override int Depth
        {
            get { return this.wrappedReader.Depth; }
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
            get { return this.wrappedReader.FieldCount; }
        }

        /// <summary>
        /// Gets the number of fields in the System.Data.Common.DbDataReader that are
        /// not hidden.
        /// </summary>
        public override int VisibleFieldCount
        {
            get { return this.wrappedReader.VisibleFieldCount; }
        }

        /// <summary>
        /// Gets name of the data type of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>
        /// A string representing the name of the data type.
        /// </returns>
        public override string GetDataTypeName(int ordinal)
        {
            return this.wrappedReader.GetDataTypeName(ordinal);
        }

        /// <summary>
        /// Gets the data type of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The data type of the specified column.</returns>
        public override Type GetFieldType(int ordinal)
        {
            return this.wrappedReader.GetFieldType(ordinal);
        }

        /// <summary>
        /// Gets the name of the column, given the zero-based column ordinal.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The name of the specified column.</returns>
        public override string GetName(int ordinal)
        {
            return this.wrappedReader.GetName(ordinal);
        }

        /// <summary>
        /// Gets the column ordinal given the name of the column.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The zero-based column ordinal.</returns>
        /// <exception cref="T:System.IndexOutOfRangeException">
        /// The name specified is not a valid column name.
        /// </exception>
        public override int GetOrdinal(string name)
        {
            return this.wrappedReader.GetOrdinal(name);
        }

        /// <summary>
        /// Returns a <see cref="T:System.Data.DataTable"/> that describes the column metadata of the <see cref="T:System.Data.Common.DbDataReader"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Data.DataTable"/> that describes the column metadata.
        /// </returns>
        public override DataTable GetSchemaTable()
        {
            return this.wrappedReader.GetSchemaTable();
        }

        /// <summary>
        /// Advances the reader to the next result when reading the results of a batch of statements.
        /// </summary>
        /// <returns>
        /// true if there are more result sets; otherwise false.
        /// </returns>
        public override bool NextResult()
        {
            if (this.wrappedReader.NextResult())
            {
                this.queryResult = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Closes the <see cref="T:System.Data.Common.DbDataReader"/> object.
        /// </summary>
        public override void Close()
        {
            if (this.queryResult != null && this.queryResult.RowsCount > 0)
            {
                ///Reader not yet closed
                if (!this.wrappedReader.IsClosed)
                {
                    ///Complete result has been fetched from database.
                    if (!this.wrappedReader.Read())
                    {
                        this.addToCache(this.queryResult);
                        this.queryResult.Dispose();
                        this.queryResult = null;
                    }
                    else ///There was still some data to be read when the reader is closed
                    ///We shall not cache incomplete result
                    {
                        this.queryResult.Dispose();
                        this.queryResult = null;
                    }
                }
            }
            this.wrappedReader.Close();
        }

        /// <summary>
        /// Advances the reader to the next record in a result set.
        /// </summary>
        /// <returns>
        /// true if there are more rows; otherwise false.
        /// </returns>
        public override bool Read()
        {
            if (this.wrappedReader.Read())
            {
                object[] values = new object[this.wrappedReader.FieldCount];
                this.wrappedReader.GetValues(values);
                SetValues(values);

                if (this.queryResult != null)
                {
                    this.queryResult.Add(new DbRow()
                    {
                        Depth = this.wrappedReader.Depth,
                        Values = values
                    });
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
