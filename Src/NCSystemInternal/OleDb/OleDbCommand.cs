using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Text;

namespace System.Data.OleDb
{
    //
    // Summary:
    //     Represents an SQL statement or stored procedure to execute against a data source.
    [DefaultEvent("RecordsAffected")]
    [Designer("Microsoft.VSDesigner.Data.VS.OleDbCommandDesigner, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [ToolboxItem(true)]
    public sealed class OleDbCommand : DbCommand, ICloneable, IDbCommand, IDisposable
    {
        //
        // Summary:
        //     Initializes a new instance of the System.Data.OleDb.OleDbCommand class.
        public OleDbCommand()
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.Data.OleDb.OleDbCommand class with the
        //     text of the query.
        //
        // Parameters:
        //   cmdText:
        //     The text of the query.
        public OleDbCommand(string cmdText)
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.Data.OleDb.OleDbCommand class with the
        //     text of the query and an System.Data.OleDb.OleDbConnection.
        //
        // Parameters:
        //   cmdText:
        //     The text of the query.
        //
        //   connection:
        //     An System.Data.OleDb.OleDbConnection that represents the connection to a data
        //     source.
        public OleDbCommand(string cmdText, OleDbConnection connection)
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.Data.OleDb.OleDbCommand class with the
        //     text of the query, an System.Data.OleDb.OleDbConnection, and the System.Data.OleDb.OleDbCommand.Transaction.
        //
        // Parameters:
        //   cmdText:
        //     The text of the query.
        //
        //   connection:
        //     An System.Data.OleDb.OleDbConnection that represents the connection to a data
        //     source.
        //
        //   transaction:
        //     The transaction in which the System.Data.OleDb.OleDbCommand executes.
        //public OleDbCommand(string cmdText, OleDbConnection connection, OleDbTransaction transaction)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}

        //
        // Summary:
        //     Gets the System.Data.OleDb.OleDbParameterCollection.
        //
        // Returns:
        //     The parameters of the SQL statement or stored procedure. The default is an empty
        //     collection.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        //[ResCategoryAttribute("DataCategory_Data")]
        //[ResDescriptionAttribute("DbCommand_Parameters")]
        //public OleDbParameterCollection Parameters { get; }
        //
        // Summary:
        //     Gets or sets a value that indicates whether the command object should be visible
        //     in a customized Windows Forms Designer control.
        //
        // Returns:
        //     A value that indicates whether the command object should be visible in a control.
        //     The default is true.
        [Browsable(false)]
        [DefaultValue(true)]
        [DesignOnly(true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool DesignTimeVisible { get; set; }
        //
        // Summary:
        //     Gets or sets the System.Data.OleDb.OleDbConnection used by this instance of the
        //     System.Data.OleDb.OleDbCommand.
        //
        // Returns:
        //     The connection to a data source. The default value is null.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The System.Data.OleDb.OleDbCommand.Connection property was changed while a transaction
        //     was in progress.
        [DefaultValue(null)]
        [Editor("Microsoft.VSDesigner.Data.Design.DbConnectionEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        //[ResCategoryAttribute("DataCategory_Data")]
        //[ResDescriptionAttribute("DbCommand_Connection")]
        public OleDbConnection Connection { get; set; }
        //
        // Summary:
        //     Gets or sets a value that indicates how the System.Data.OleDb.OleDbCommand.CommandText
        //     property is interpreted.
        //
        // Returns:
        //     One of the System.Data.OleDb.OleDbCommand.CommandType values. The default is
        //     Text.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The value was not a valid System.Data.OleDb.OleDbCommand.CommandType.
        [DefaultValue(CommandType.Text)]
        [RefreshProperties(RefreshProperties.All)]
        //[ResCategoryAttribute("DataCategory_Data")]
        //[ResDescriptionAttribute("DbCommand_CommandType")]
        public override CommandType CommandType { get; set; }
        //
        // Summary:
        //     Gets or sets the wait time before terminating an attempt to execute a command
        //     and generating an error.
        //
        // Returns:
        //     The time (in seconds) to wait for the command to execute. The default is 30 seconds.
        //[ResCategoryAttribute("DataCategory_Data")]
        //[ResDescriptionAttribute("DbCommand_CommandTimeout")]
        public override int CommandTimeout { get; set; }
        //
        // Summary:
        //     Gets or sets the SQL statement or stored procedure to execute at the data source.
        //
        // Returns:
        //     The SQL statement or stored procedure to execute. The default value is an empty
        //     string.
        [DefaultValue("")]
        [Editor("Microsoft.VSDesigner.Data.ADO.Design.OleDbCommandTextEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [RefreshProperties(RefreshProperties.All)]
        //[ResCategoryAttribute("DataCategory_Data")]
        //[ResDescriptionAttribute("DbCommand_CommandText")]
        public override string CommandText { get; set; }
        //
        // Summary:
        //     Gets or sets the System.Data.OleDb.OleDbTransaction within which the System.Data.OleDb.OleDbCommand
        //     executes.
        //
        // Returns:
        //     The System.Data.OleDb.OleDbTransaction. The default value is null.
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[ResDescriptionAttribute("DbCommand_Transaction")]
        //public OleDbTransaction Transaction { get; set; }
        //
        // Summary:
        //     Gets or sets how command results are applied to the System.Data.DataRow when
        //     used by the Update method of the System.Data.OleDb.OleDbDataAdapter.
        //
        // Returns:
        //     One of the System.Data.UpdateRowSource values.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The value entered was not one of the System.Data.UpdateRowSource values.
        [DefaultValue(UpdateRowSource.Both)]
        //[ResCategoryAttribute("DataCategory_Update")]
        //[ResDescriptionAttribute("DbCommand_UpdatedRowSource")]
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; }
        protected override DbTransaction DbTransaction { get; set; }

        //
        // Summary:
        //     Tries to cancel the execution of an System.Data.OleDb.OleDbCommand.
        public override void Cancel()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Creates a new System.Data.OleDb.OleDbCommand object that is a copy of the current
        //     instance.
        //
        // Returns:
        //     A new System.Data.OleDb.OleDbCommand object that is a copy of this instance.
        public OleDbCommand Clone()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Creates a new instance of an System.Data.OleDb.OleDbParameter object.
        //
        // Returns:
        //     An System.Data.OleDb.OleDbParameter object.
        //public OleDbParameter CreateParameter()
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
        //
        // Summary:
        //     Executes an SQL statement against the System.Data.OleDb.OleDbCommand.Connection
        //     and returns the number of rows affected.
        //
        // Returns:
        //     The number of rows affected.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The connection does not exist.-or- The connection is not open.-or- Cannot execute
        //     a command within a transaction context that differs from the context in which
        //     the connection was originally enlisted.
        public override int ExecuteNonQuery()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Sends the System.Data.OleDb.OleDbCommand.CommandText to the System.Data.OleDb.OleDbCommand.Connection
        //     and builds an System.Data.OleDb.OleDbDataReader.
        //
        // Returns:
        //     An System.Data.OleDb.OleDbDataReader object.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     Cannot execute a command within a transaction context that differs from the context
        //     in which the connection was originally enlisted.
        public OleDbDataReader ExecuteReader()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Sends the System.Data.OleDb.OleDbCommand.CommandText to the System.Data.OleDb.OleDbCommand.Connection,
        //     and builds an System.Data.OleDb.OleDbDataReader using one of the System.Data.CommandBehavior
        //     values.
        //
        // Parameters:
        //   behavior:
        //     One of the System.Data.CommandBehavior values.
        //
        // Returns:
        //     An System.Data.OleDb.OleDbDataReader object.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     Cannot execute a command within a transaction context that differs from the context
        //     in which the connection was originally enlisted.
        public OleDbDataReader ExecuteReader(CommandBehavior behavior)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Executes the query, and returns the first column of the first row in the result
        //     set returned by the query. Additional columns or rows are ignored.
        //
        // Returns:
        //     The first column of the first row in the result set, or a null reference if the
        //     result set is empty.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     Cannot execute a command within a transaction context that differs from the context
        //     in which the connection was originally enlisted.
        public override object ExecuteScalar()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Creates a prepared (or compiled) version of the command on the data source.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The System.Data.OleDb.OleDbCommand.Connection is not set.-or- The System.Data.OleDb.OleDbCommand.Connection
        //     is not open.
        public override void Prepare()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Resets the System.Data.OleDb.OleDbCommand.CommandTimeout property to the default
        //     value.
        public void ResetCommandTimeout()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        protected override DbParameter CreateDbParameter()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        protected override void Dispose(bool disposing)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }

        object ICloneable.Clone()
        {
            throw new NotImplementedException();
        }
    }
}
