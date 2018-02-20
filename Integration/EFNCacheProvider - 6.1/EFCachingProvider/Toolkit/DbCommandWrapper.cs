// Copyright (c) Microsoft Corporation.  All rights reserved.

using Alachisoft.NCache.Integrations.EntityFramework.Caching;
using System;
using System.Data;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Common.CommandTrees;

namespace Alachisoft.NCache.Integrations.EntityFramework.Toolkit
{
    /// <summary>
    /// DbCommand wrapper base class.
    /// </summary>
    public class DbCommandWrapper : System.Data.Common.DbCommand
    {
        private System.Data.Common.DbCommand wrappedCommand;
        private System.Data.Common.DbConnection connection;
        private EFCachingCommandDefinition definition;
        private readonly DbCommandTree _commandTree;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbCommandWrapper"/> class.
        /// </summary>
        /// <param name="wrappedCommand">The wrapped command.</param>
        /// <param name="definition">The definition.</param>
        public DbCommandWrapper(System.Data.Common.DbCommand wrappedCommand, EFCachingCommandDefinition definition)
        {
            this.wrappedCommand = wrappedCommand;
            this.definition = definition;
        }

        /// <summary>
        /// Gets the command definition.
        /// </summary>
        /// <value>The command definition.</value>
        public EFCachingCommandDefinition Definition
        {
            get { return this.definition; }
        }

        /// <summary>
        /// Gets or sets the text command to run against the data source.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The text command to execute. The default value is an empty string ("").
        /// </returns>
        public override string CommandText
        {
            get { return this.wrappedCommand.CommandText; }
            set { this.wrappedCommand.CommandText = value; }
        }

        /// <summary>
        /// Gets or sets the wait time before terminating the attempt to execute a command and generating an error.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The time in seconds to wait for the command to execute.
        /// </returns>
        public override int CommandTimeout
        {
            get { return this.wrappedCommand.CommandTimeout; }
            set { this.wrappedCommand.CommandTimeout = value; }
        }

        /// <summary>
        /// Indicates or specifies how the <see cref="P:System.Data.Common.DbCommand.CommandText"/> property is interpreted.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// One of the <see cref="T:System.Data.CommandType"/> values. The default is Text.
        /// </returns>
        public override CommandType CommandType
        {
            get { return this.wrappedCommand.CommandType; }
            set { this.wrappedCommand.CommandType = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the command object should be visible in a customized interface control.
        /// </summary>
        /// <value></value>
        /// <returns>true, if the command object should be visible in a control; otherwise false. The default is true.
        /// </returns>
        public override bool DesignTimeVisible
        {
            get { return false; }
            set { }
        }

        /// <summary>
        /// Gets or sets how command results are applied to the <see cref="T:System.Data.DataRow"/> when used by the Update method of a <see cref="T:System.Data.Common.DbDataAdapter"/>.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// One of the <see cref="T:System.Data.UpdateRowSource"/> values. The default is Both unless the command is automatically generated. Then the default is None.
        /// </returns>
        public override UpdateRowSource UpdatedRowSource
        {
            get { return this.wrappedCommand.UpdatedRowSource; }
            set { this.wrappedCommand.UpdatedRowSource = value; }
        }

        /// <summary>
        /// Gets the wrapped command.
        /// </summary>
        /// <value>The wrapped command.</value>
        public System.Data.Common.DbCommand WrappedCommand
        {
            get { return this.wrappedCommand; }
        }

        /// <summary>
        /// Gets or sets the <see cref="T:System.Data.Common.DbConnection"/> used by this <see cref="T:System.Data.Common.DbCommand"/>.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The connection to the data source.
        /// </returns>
        protected override System.Data.Common.DbConnection DbConnection
        {
            get
            {
                return this.connection;
            }

            set
            {
                //DbConnectionWrapper conn = value as DbConnectionWrapper;
                System.Data.Common.DbConnection conn = value;
                if (conn != null)
                {
                    this.connection = conn;
                    this.wrappedCommand.Connection = conn;
                }
                else
                {
                    this.connection = null;
                    this.wrappedCommand.Connection = null;
                }
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="T:System.Data.Common.DbParameter"/> objects.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The parameters of the SQL statement or stored procedure.
        /// </returns>
        protected override System.Data.Common.DbParameterCollection DbParameterCollection
        {
            get { return this.wrappedCommand.Parameters; }
        }

        /// <summary>
        /// Gets or sets the <see cref="P:System.Data.Common.DbCommand.DbTransaction"/> within which this <see cref="T:System.Data.Common.DbCommand"/> object executes.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The transaction within which a Command object of a .NET Framework data provider executes. The default value is a null reference (Nothing in Visual Basic).
        /// </returns>
        protected override System.Data.Common.DbTransaction DbTransaction
        {
            get { return this.wrappedCommand.Transaction; }
            set { this.wrappedCommand.Transaction = value; }
        }

        /// <summary>
        /// Attempts to cancels the execution of a <see cref="T:System.Data.Common.DbCommand"/>.
        /// </summary>
        public override void Cancel()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <returns>The number of rows affected.</returns>
        public override int ExecuteNonQuery()
        {
            return this.wrappedCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        /// <returns>
        /// The first column of the first row in the result set.
        /// </returns>
        public override object ExecuteScalar()
        {
            return this.wrappedCommand.ExecuteScalar();
        }        

        /// <summary>
        /// Creates a prepared (or compiled) version of the command on the data source.
        /// </summary>
        public override void Prepare()
        {
            this.wrappedCommand.Prepare();
        }

        /// <summary>
        /// Creates a new instance of a <see cref="T:System.Data.Common.DbParameter"/> object.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Data.Common.DbParameter"/> object.
        /// </returns>
        protected override System.Data.Common.DbParameter CreateDbParameter()
        {
            return this.wrappedCommand.CreateParameter();
        }

        /// <summary>
        /// Executes the command text against the connection.
        /// </summary>
        /// <param name="behavior">An instance of <see cref="T:System.Data.CommandBehavior"/>.</param>
        /// <returns>
        /// A <see cref="T:System.Data.Common.DbDataReader"/>.
        /// </returns>
        protected override System.Data.Common.DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return this.wrappedCommand.ExecuteReader(behavior);
        }

        public DbCommandTree CommandTree
        {
            get {
                return _commandTree;
            }
        }
    }
}
