using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Text;

namespace System.Data.OleDb
{
    //
    // Summary:
    //     Represents an open connection to a data source.
    [DefaultEvent("InfoMessage")]
    public sealed class OleDbConnection : DbConnection, ICloneable, IDbConnection, IDisposable
    {
        //
        // Summary:
        //     Initializes a new instance of the System.Data.OleDb.OleDbConnection class.
        public OleDbConnection()
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.Data.OleDb.OleDbConnection class with
        //     the specified connection string.
        //
        // Parameters:
        //   connectionString:
        //     The connection used to open the database.
        public OleDbConnection(string connectionString)
        {
            //TODO: ALACHISOFT
        }

        //
        // Summary:
        //     Gets a string that contains the version of the server to which the client is
        //     connected.
        //
        // Returns:
        //     The version of the connected server.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The connection is closed.
        //[ResDescriptionAttribute("OleDbConnection_ServerVersion")]
        public override string ServerVersion { get; }
        //
        // Summary:
        //     Gets the name of the OLE DB provider specified in the "Provider= " clause of
        //     the connection string.
        //
        // Returns:
        //     The name of the provider as specified in the "Provider= " clause of the connection
        //     string. The default value is an empty string.
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[ResCategoryAttribute("DataCategory_Data")]
        //[ResDescriptionAttribute("OleDbConnection_Provider")]
        public string Provider { get; }
        //
        // Summary:
        //     Gets the server name or file name of the data source.
        //
        // Returns:
        //     The server name or file name of the data source. The default value is an empty
        //     string.
        [Browsable(true)]
        //[ResDescriptionAttribute("OleDbConnection_DataSource")]
        public override string DataSource { get; }
        //
        // Summary:
        //     Gets the name of the current database or the database to be used after a connection
        //     is opened.
        //
        // Returns:
        //     The name of the current database or the name of the database to be used after
        //     a connection is opened. The default value is an empty string.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[ResDescriptionAttribute("OleDbConnection_Database")]
        public override string Database { get; }
        //
        // Summary:
        //     Gets the time to wait while trying to establish a connection before terminating
        //     the attempt and generating an error.
        //
        // Returns:
        //     The time in seconds to wait for a connection to open. The default value is 15
        //     seconds.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The value set is less than 0.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[ResDescriptionAttribute("OleDbConnection_ConnectionTimeout")]
        public override int ConnectionTimeout { get; }
        //
        // Summary:
        //     Gets or sets the string used to open a database.
        //
        // Returns:
        //     The OLE DB provider connection string that includes the data source name, and
        //     other parameters needed to establish the initial connection. The default value
        //     is an empty string.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     An invalid connection string argument has been supplied or a required connection
        //     string argument has not been supplied.
        [DefaultValue("")]
        [Editor("Microsoft.VSDesigner.Data.ADO.Design.OleDbConnectionStringEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [RecommendedAsConfigurable(true)]
        [RefreshProperties(RefreshProperties.All)]
        //[ResCategoryAttribute("DataCategory_Data")]
        //[ResDescriptionAttribute("OleDbConnection_ConnectionString")]
        [SettingsBindable(true)]
        public override string ConnectionString { get; set; }
        //
        // Summary:
        //     Gets the current state of the connection.
        //
        // Returns:
        //     A bitwise combination of the System.Data.ConnectionState values. The default
        //     is Closed.
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[ResDescriptionAttribute("DbConnection_State")]
        public override ConnectionState State { get; }

        //
        // Summary:
        //     Occurs when the provider sends a warning or an informational message.
        //[ResCategoryAttribute("DataCategory_InfoMessage")]
        //[ResDescriptionAttribute("DbConnection_InfoMessage")]
        //public event OleDbInfoMessageEventHandler InfoMessage;

        //
        // Summary:
        //     Indicates that the System.Data.OleDb.OleDbConnection object pool can be released
        //     when the last underlying connection is released.
        public static void ReleaseObjectPool()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Starts a database transaction with the current System.Data.IsolationLevel value.
        //
        // Returns:
        //     An object representing the new transaction.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     Parallel transactions are not supported.
        //public OleDbTransaction BeginTransaction();
        //
        // Summary:
        //     Starts a database transaction with the specified isolation level.
        //
        // Parameters:
        //   isolationLevel:
        //     The isolation level under which the transaction should run.
        //
        // Returns:
        //     An object representing the new transaction.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     Parallel transactions are not supported.
        //public OleDbTransaction BeginTransaction(IsolationLevel isolationLevel);
        //
        // Summary:
        //     Changes the current database for an open System.Data.OleDb.OleDbConnection.
        //
        // Parameters:
        //   value:
        //     The database name.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The database name is not valid.
        //
        //   T:System.InvalidOperationException:
        //     The connection is not open.
        //
        //   T:System.Data.OleDb.OleDbException:
        //     Cannot change the database.
        public override void ChangeDatabase(string value)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        //
        // Summary:
        //     Closes the connection to the data source.
        public override void Close()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Creates and returns an System.Data.OleDb.OleDbCommand object associated with
        //     the System.Data.OleDb.OleDbConnection.
        //
        // Returns:
        //     An System.Data.OleDb.OleDbCommand object.
        public OleDbCommand CreateCommand()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Enlists in the specified transaction as a distributed transaction.
        //
        // Parameters:
        //   transaction:
        //     A reference to an existing System.EnterpriseServices.ITransaction in which to
        //     enlist.
        //public void EnlistDistributedTransaction(System.EnterpriseServices.ITransaction transaction);
        //
        // Summary:
        //     Enlists in the specified transaction as a distributed transaction.
        //
        // Parameters:
        //   transaction:
        //     A reference to an existing System.Transactions.Transaction in which to enlist.
        public override void EnlistTransaction(System.Transactions.Transaction transaction)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Returns schema information from a data source as indicated by a GUID, and after
        //     it applies the specified restrictions.
        //
        // Parameters:
        //   schema:
        //     One of the System.Data.OleDb.OleDbSchemaGuid values that specifies the schema
        //     table to return.
        //
        //   restrictions:
        //     An System.Object array of restriction values. These are applied in the order
        //     of the restriction columns. That is, the first restriction value applies to the
        //     first restriction column, the second restriction value applies to the second
        //     restriction column, and so on.
        //
        // Returns:
        //     A System.Data.DataTable that contains the requested schema information.
        //
        // Exceptions:
        //   T:System.Data.OleDb.OleDbException:
        //     The specified set of restrictions is invalid.
        //
        //   T:System.InvalidOperationException:
        //     The System.Data.OleDb.OleDbConnection is closed.
        //
        //   T:System.ArgumentException:
        //     The specified schema rowset is not supported by the OLE DB provider.-or- The
        //     schema parameter contains a value of System.Data.OleDb.OleDbSchemaGuid.DbInfoLiterals
        //     and the restrictions parameter contains one or more restrictions.
        public DataTable GetOleDbSchemaTable(Guid schema, object[] restrictions)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Returns schema information for the data source of this System.Data.OleDb.OleDbConnection
        //     using the specified string for the schema name and the specified string array
        //     for the restriction values.
        //
        // Parameters:
        //   collectionName:
        //     Specifies the name of the schema to return.
        //
        //   restrictionValues:
        //     Specifies a set of restriction values for the requested schema.
        //
        // Returns:
        //     A System.Data.DataTable that contains schema information.
        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Returns schema information for the data source of this System.Data.OleDb.OleDbConnection
        //     using the specified string for the schema name.
        //
        // Parameters:
        //   collectionName:
        //     Specifies the name of the schema to return.
        //
        // Returns:
        //     A System.Data.DataTable that contains schema information.
        public override DataTable GetSchema(string collectionName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Returns schema information for the data source of this System.Data.OleDb.OleDbConnection.
        //
        // Returns:
        //     A System.Data.DataTable that contains schema information.
        public override DataTable GetSchema()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Opens a database connection with the property settings specified by the System.Data.OleDb.OleDbConnection.ConnectionString.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The connection is already open.
        //
        //   T:System.Data.OleDb.OleDbException:
        //     A connection-level error occurred while opening the connection.
        public override void Open()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Updates the System.Data.OleDb.OleDbConnection.State property of the System.Data.OleDb.OleDbConnection
        //     object.
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void ResetState()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        protected override DbCommand CreateDbCommand()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        protected override void Dispose(bool disposing)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
    }
}
