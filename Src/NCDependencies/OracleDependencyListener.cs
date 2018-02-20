// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
#if !NET20
using Oracle.ManagedDataAccess.Client;
#endif
using System.Collections;
using Alachisoft.NCache.Caching.AutoExpiration;
using System.Data;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Runtime.Dependencies;

namespace Alachisoft.NCache.RuntimeDependencies
{
#if !NET20
    /// <summary>
    /// Class that holds the instances of the OracleDependecy instances in it.
    /// It is also the event handler for the dependency.
    /// </summary>
    sealed class OracleDependencyListener : DependencyListener
    {
        private OracleDependency _oracleDep = null;
        private ArrayList _rowids = new ArrayList(1);
        IDictionary _cmdParams;
        private CommandType _cmdType;

        /// <summary>
        /// Initialize instance of oracle dependency listener
        /// </summary>
        /// <param name="key">key used to reference object</param>
        /// <param name="connString">connection string used to connect database</param>
        /// <param name="queryString">query string for which dataset is created to be monitored</param>
        /// <param name="context">current cache runtime context</param>
        /// <param name="hintType">expiration hint type</param>
        internal OracleDependencyListener(string key, string connString, string queryString,
            IDBConnectionPool connectionPool, IDependencyChangeListener dependencyListener, ILogger logger,
            ExpirationHintType hintType)
            : base(key, connString, queryString, connectionPool, dependencyListener, logger, hintType)
        {
        }

        internal OracleDependencyListener(string key, string connString, string queryString,
            IDBConnectionPool connectionPool, IDependencyChangeListener dependencyListener, ILogger logger,
            ExpirationHintType hintType, CommandType cmdType, IDictionary cmdParams)
            : base(key, connString, queryString, connectionPool, dependencyListener, logger, hintType)
        {
            _cmdParams = cmdParams;
            _cmdType = cmdType;
        }

        /// <summary>
        /// Static constructor to set port once.
        /// </summary>
        static OracleDependencyListener()
        {
            // database change notification in oracle 10g release 2 is only object based.
            // with introduction of 11g notifications can be query based (default) or object
            // based. incase of 10g we have to verify the change before taking any further actions.
            // --object based: this means that change notifications will be fired if any row is modified
            // in object (table). therefore we have to check rowid to confirm if the row changed
            // is the one for which the event is registered.
            // --query based: this means that change of only the row that is modified will be
            // notified if the change notification is registered for it. in other words change 
            // notifications will be fired on row level changes.
        }

        /// <summary>
        /// Initializes the oracle dependency instance. registers the change event handler for it.
        /// </summary>
        /// <returns>true if the dependency was successfully initialized.</returns>
        public override bool Initialize()
        {
            OracleCommand oracleCmd = null;
            OracleConnection connection = null;
            try
            {
                connection =
                    (OracleConnection) _connectionPool.AddToDbConnectionPool(_connString,
                        new OracleConnection(base._connString));
                oracleCmd = new OracleCommand(base._queryString, connection);
                oracleCmd.AddRowid = true;

                switch (_cmdType)
                {
                    case CommandType.Text:
                        oracleCmd.CommandType = CommandType.Text;
                        break;
                    case CommandType.StoredProcedure:
                        oracleCmd.CommandType = CommandType.StoredProcedure;
                        break;
                    default:
                        oracleCmd.CommandType = CommandType.Text;
                        break;
                }

                if (_cmdParams != null && _cmdParams.Count > 0)
                {
                    IDictionaryEnumerator ide = _cmdParams.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        OracleCommandParams param = (OracleCommandParams) ide.Value;
                        OracleParameter oracleParam =
                            new OracleParameter(ide.Key.ToString(), ConvertToOracleDbType(param.Type));
                        oracleParam.Direction = param.Direction;
                        oracleParam.Value = param.Value;
                        oracleCmd.Parameters.Add(oracleParam);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (oracleCmd != null)
            {
                try
                {
                    this.RegisterNotification(oracleCmd);
                    return true;
                }
                catch (Exception ex)
                {
                    NCacheLog.Error("NotificationBasedDependencyManager.OracleDependencyListener: " + ex.ToString());
                    connection.Close();
                    base._connectionPool.RemoveFromDbConnectionPool(_connString, true);
                    connection =
                        (OracleConnection) base._connectionPool.AddToDbConnectionPool(_connString,
                            new OracleConnection(base._connString));
                    oracleCmd = new OracleCommand(base._queryString, connection);

                    if (oracleCmd != null)
                    {
                        try
                        {
                            this.RegisterNotification(oracleCmd);
                            return true;
                        }
                        catch (Exception e)
                        {
                            _connectionPool.RemoveFromDbConnectionPool(_connString);
                            connection.Close();
                            throw e;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Register callback and associate query result set with the command
        /// </summary>
        /// <param name="command"></param>
        private void RegisterNotification(OracleCommand oraclelCmd)
        {
            oraclelCmd.AddRowid = true;
            OracleDataReader reader = null;
            try
            {
                // Create an OracleDependency instance and bind it to an OracleCommand
                // instance.
                // When an OracleDependency instance is bound to an OracleCommand
                // instance, an OracleNotificationRequest is created and is set in the
                // OracleCommand's Notification property. This indicates subsequent 
                // execution of command will register the notification.
                // By default, the notification request is using the Database Change
                // Notification.
                this._oracleDep = new OracleDependency(oraclelCmd, // command instance to which dependency is bound
                    false, // notify once. we have to set it to false to track database shutdown and startup.
                    // Oracle do not provide notification on restart like SQL do. we will remove the dependency
                    // once we receive the startup call
                    0, // amount of time in seconds that the registration remains active. 0 means 
                    // it'll stay active forever.
                    false); // Database performance is faster if the message is stored 
                // in an in-memory queue rather than in the database queue.

                this._oracleDep.OnChange +=
                    new Oracle.ManagedDataAccess.Client.OnChangeEventHandler(this.OnOracleDependencyChanged);

                // The notification registration is created and the query result sets 
                // associated with the command can be invalidated when there is a 
                // change.  When the first notification registration occurs, the 
                // notification listener is started.
                {
                    reader = oraclelCmd.ExecuteReader();
                    try
                    {
                        this._rowids.Clear();
                        // We go through all the records that were retrieved and save the rowid's
                        // That is only if user has included rowid in select statement
                        while (reader.Read())
                        {
                            this._rowids.Add(reader["rowid"]);
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        // thrown if user has not provided rowid as part of select query
                    }
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Called when dataset result is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnOracleDependencyChanged(object sender, OracleNotificationEventArgs eventArgs)
        {
            // Either there is no rowid retrieved, or there are rowids in list and one of it matches with
            // the rowid of row changed
            if ((this._rowids.Count == 0 || Contains(eventArgs.Details.Rows)) &&
                eventArgs.Info != OracleNotificationInfo.Startup && eventArgs.Info != OracleNotificationInfo.Shutdown)
            {
                base.OnDependencyChanged(
                    (eventArgs.Type == OracleNotificationType.Change ||
                     eventArgs.Type == OracleNotificationType.Query), // data set changed
                    false,
                    eventArgs.Info == OracleNotificationInfo.Error,
                    false); // some error occurred on server Oracle does not have invalid in Notification info
            }
        }

        private bool Contains(DataRowCollection rowColl)
        {
            foreach (DataRow row in rowColl)
            {
                if (_rowids.Contains(row["rowid"]))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Stop notification listening
        /// </summary>
        public override void Stop()
        {
            try
            {
                // As the notify once is set to false, we have to remove registered notification
                // so change notifications are no longer sent.
                OracleConnection connection = (OracleConnection) base._connectionPool.GetConnection(base._connString);
                if (connection != null)
                {
                    this._oracleDep.RemoveRegistration(connection);
                }

                this._rowids.Clear();
            }
            catch (InvalidOperationException)
            {
                // Notification registration is in an unregistered state
            }
            catch (Exception exc)
            {
                NCacheLog.Error("NotificationBasedDependencyManager.OracleDependencyListener", exc.ToString());
            }
            finally
            {
                base.Stop();
            }
        }

        private OracleDbType ConvertToOracleDbType(OracleCmdParamsType type)
        {
            switch (type)
            {
                case OracleCmdParamsType.BFile:
                    return OracleDbType.BFile;
                case OracleCmdParamsType.Blob:
                    return OracleDbType.Blob;
                case OracleCmdParamsType.Byte:
                    return OracleDbType.Byte;
                case OracleCmdParamsType.Char:
                    return OracleDbType.Char;
                case OracleCmdParamsType.Clob:
                    return OracleDbType.Clob;
                case OracleCmdParamsType.Date:
                    return OracleDbType.Date;
                case OracleCmdParamsType.Decimal:
                    return OracleDbType.Decimal;
                case OracleCmdParamsType.Double:
                    return OracleDbType.Double;
                case OracleCmdParamsType.Int16:
                    return OracleDbType.Int16;
                case OracleCmdParamsType.Int32:
                    return OracleDbType.Int32;
                case OracleCmdParamsType.Int64:
                    return OracleDbType.Int64;
                case OracleCmdParamsType.IntervalDS:
                    return OracleDbType.IntervalDS;
                case OracleCmdParamsType.IntervalYM:
                    return OracleDbType.IntervalYM;
                case OracleCmdParamsType.Long:
                    return OracleDbType.Long;
                case OracleCmdParamsType.LongRaw:
                    return OracleDbType.LongRaw;
                case OracleCmdParamsType.NChar:
                    return OracleDbType.NChar;
                case OracleCmdParamsType.NClob:
                    return OracleDbType.NClob;
                case OracleCmdParamsType.NVarchar2:
                    return OracleDbType.NVarchar2;
                case OracleCmdParamsType.Raw:
                    return OracleDbType.Raw;
                case OracleCmdParamsType.RefCursor:
                    return OracleDbType.RefCursor;
                case OracleCmdParamsType.Single:
                    return OracleDbType.Single;
                case OracleCmdParamsType.TimeStamp:
                    return OracleDbType.TimeStamp;
                case OracleCmdParamsType.TimeStampLTZ:
                    return OracleDbType.TimeStampLTZ;
                case OracleCmdParamsType.TimeStampTZ:
                    return OracleDbType.TimeStampTZ;
                case OracleCmdParamsType.Varchar2:
                    return OracleDbType.Varchar2;
                case OracleCmdParamsType.XmlType:
                    return OracleDbType.XmlType;
                default:
                    return OracleDbType.Char;
            }
        }

        /// <summary>
        /// returns a unique hashcode as this instance needs to 
        /// be placed in a hashtable.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base._cacheKey.GetHashCode();
        }

        /// <summary>
        /// compares two instances of this class for equality.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is OracleDependencyListener)
            {
                bool key, conn, query;
                key = base._cacheKey.Equals(((OracleDependencyListener) obj)._cacheKey);
                conn = base._connString.Equals(((OracleDependencyListener) obj)._connString);
                query = base._queryString.Equals(((OracleDependencyListener) obj)._queryString);
                if (key && conn && query)
                {
                    return true;
                }
            }

            return false;
        }
    }
#endif
}