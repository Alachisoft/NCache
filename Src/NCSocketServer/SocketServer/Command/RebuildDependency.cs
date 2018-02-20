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
using System.Collections;
using System.Data;
using System.IO;
using System.Data.SqlTypes;

using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching;


namespace Alachisoft.NCache.SocketServer.Command
{
    internal sealed class RebuildDependency
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="expHint"></param>
        /// <param name="beginQuoteIndex"></param>
        /// <param name="endQuoteIndex"></param>
        public ExpirationHint CreateDependencyFromString(ref string command,ref byte[] data, string cacheId, ref int beginQuoteIndex, ref int endQuoteIndex, bool isBulkOps,object items, ref int currentXtDep)
        {
            bool isInner = false;
            string interimCommand = null;
            int interimBeginIndex = 0, interimEndIndex = 0;

            ExpirationHint expirationHint = null;            
            AggregateExpirationHint aggregateExpiration = null;
            
            do
            {
                beginQuoteIndex += interimEndIndex;

                UpdateDelimIndexes(command, '\r', ref beginQuoteIndex, ref endQuoteIndex);
                if (endQuoteIndex < 0) break;

                interimCommand = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1).Remove(0, 1);
                
                if (interimCommand == string.Empty) break;
                if (interimCommand.StartsWith("\""))
                {
                    endQuoteIndex = beginQuoteIndex;
                    break;
                }

                interimBeginIndex = interimEndIndex = 0;

                if (interimCommand.StartsWith("INNER") && !isInner)
                {
                    isInner = true;
                    aggregateExpiration = new AggregateExpirationHint();
                }
                else if (interimCommand.StartsWith("FILEDEPENDENCY")

                    || interimCommand.StartsWith("KEYDEPENDENCY")

                    )
                {
                    string value = null;
                    DateTime startAfter;
                    ArrayList list = new ArrayList();

                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    while (true)
                    {
                        UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);

                        value = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);
                        int valueBeginIndex = 0, valueEndIndex = 0;

                        if (value.Equals("STARTAFTER"))
                        {
                            UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                            startAfter = new DateTime(Convert.ToInt64(interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1)));

                            interimBeginIndex += valueBeginIndex; interimEndIndex += valueEndIndex;

                            break;
                        }                        
                        else
                        {
                            list.Add(value);
                        }
                    }

                    if (interimCommand.StartsWith("KEYDEPENDENCY"))
                        expirationHint = new KeyDependency((string[])list.ToArray(typeof(string)), startAfter);
                    else

                        expirationHint = new FileDependency((string[])list.ToArray(typeof(string)), startAfter);
                }
                else if (interimCommand.StartsWith("EXTDEPENDENCY"))
                {
                    if (!isBulkOps)
                    {
                        UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                        UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                        int extensibleDepStartIndex = Convert.ToInt32(interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));

                        UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                        int extensibleDepEndIndex = Convert.ToInt32(interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));

                        byte[] value = null;
                        byte[] extensibleDependency = new byte[extensibleDepEndIndex - extensibleDepStartIndex];
                        value = new byte[extensibleDepStartIndex];

                        MemoryStream mStream = new MemoryStream(data);
                        mStream.Read(value, 0, extensibleDepStartIndex);
                        mStream.Read(extensibleDependency, 0, extensibleDepEndIndex - extensibleDepStartIndex);
                        mStream.Close();

                        data = value;

                        expirationHint = (ExtensibleDependency)Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(extensibleDependency, cacheId);
                    }
                    else
                    {
                        ArrayList userItems=items as ArrayList;
                        UserBinaryObject userBinObj = userItems[currentXtDep++] as UserBinaryObject;
                        if (userBinObj != null)
                        {
                            expirationHint = (ExtensibleDependency)Serialization.Formatters.CompactBinaryFormatter.FromByteBuffer(userBinObj.GetFullObject(), cacheId);
                        }
                    }
                }
                else if (interimCommand.StartsWith("SQL7DEPENDENCY") 

                    || interimCommand.StartsWith("OLEDBDEPENDENCY")

                    )
                {
                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    string connectionString = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);

                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    string cacheKey = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);


                    if (interimCommand.StartsWith("OLEDBDEPENDENCY"))
                        expirationHint = new OleDbCacheDependency(connectionString, cacheKey);
                    else

                        expirationHint = new Sql7CacheDependency(connectionString, cacheKey);
                }
                else if (interimCommand.StartsWith("YUKONDEPENDENCY"))
                {
                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    string connectionString = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);

                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    string queryString = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);

                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    string commandType = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);
                    CommandType cmdType = (CommandType)Convert.ToInt32(commandType);

                    UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                    string cmdParamId = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);


                    if (interimCommand.StartsWith("ORACLEDEPENDENCY"))
                    {
                        Hashtable cmdParams = new Hashtable();
                        if (cmdParamId != string.Empty)
                        {
                            while (true)
                            {
                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                if (interimEndIndex == -1)
                                    break;
                                string key = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                string type = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);
                                Runtime.Dependencies.OracleCmdParamsType oracleType = (Runtime.Dependencies.OracleCmdParamsType)Convert.ToInt32(type);

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                object value = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                string direction = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);
                                Runtime.Dependencies.OracleParameterDirection oracleParamDirection = (Runtime.Dependencies.OracleParameterDirection)Convert.ToInt32(direction);


                                OracleCommandParams oracleParams = new OracleCommandParams(oracleType, value, oracleParamDirection);
                                cmdParams.Add(key, oracleParams);
                            }
                        }
                        if (cmdParams.Count > 0)
                            expirationHint = new OracleCacheDependency(connectionString, queryString, cmdType, cmdParams);
                        else
                            expirationHint = new OracleCacheDependency(connectionString, queryString, cmdType, new Hashtable());
                    }
                    else
                    {
                        Hashtable cmdParams = new Hashtable();
                        if (cmdParamId != string.Empty)
                        {
                            while (true)
                            {
                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                if (interimEndIndex == -1)
                                    break;
                                string key = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                string type = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);
                                SqlDbType sqlType = (SqlDbType)Convert.ToInt32(type);

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                string direction = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);
                                ParameterDirection paramDirection = (ParameterDirection)Convert.ToInt32(direction);

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                string dbtype = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);
                                DbType dbType = (DbType)Convert.ToInt32(dbtype);

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                string cmpOptions = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);
                                SqlCompareOptions compareOptions = (SqlCompareOptions)Convert.ToInt32(cmpOptions);

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                string srcVer = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);
                                DataRowVersion srcVersion = (DataRowVersion)Convert.ToInt32(srcVer);

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                object value = interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1);
                                if (value.ToString() == "#")
                                    value = null;

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                bool isNullable = Convert.ToBoolean(interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                int localeId = Convert.ToInt32(interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                int offset = Convert.ToInt32(interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                byte precision = Convert.ToByte(interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                byte scale = Convert.ToByte(interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                int size = Convert.ToInt32(interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                string sourceColumn = (interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));
                                if (sourceColumn == "#")
                                    sourceColumn = "";

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                bool sourceColumnNullMapping = Convert.ToBoolean(interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                object sqlValue = (interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));
                                if (sqlValue.ToString() == "#")
                                    sqlValue = null;

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                string typeName = (interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));
                                if (typeName == "#")
                                    typeName = "";

                                UpdateDelimIndexes(interimCommand, '"', ref interimBeginIndex, ref interimEndIndex);
                                string udtTypeName = (interimCommand.Substring(interimBeginIndex + 1, interimEndIndex - interimBeginIndex - 1));
                                if (udtTypeName == "#")
                                    udtTypeName = "";

                                SqlCmdParams sqlParams = new SqlCmdParams(sqlType, value);
                                sqlParams.CmpInfo = compareOptions;
                                sqlParams.Direction = paramDirection;
                                sqlParams.IsNullable = isNullable;
                                sqlParams.LocaleID = localeId;
                                sqlParams.Offset = offset;
                                sqlParams.Precision = precision;
                                sqlParams.Scale = scale;
                                sqlParams.ParamSize = size;
                                sqlParams.SourceColumn = sourceColumn;
                                sqlParams.SourceColumnNullMapping = sourceColumnNullMapping;
                                sqlParams.SqlValue = sqlValue;
                                sqlParams.SrcVersion = srcVersion;
                                sqlParams.TypeName = typeName;
                                sqlParams.UdtName = udtTypeName;
                                cmdParams.Add(key, sqlParams);
                            }
                        }
                        
                        if (cmdParams.Count > 0)
                            expirationHint = new SqlYukonCacheDependency(connectionString, queryString, cmdType, cmdParams);
                        else
                            expirationHint = new SqlYukonCacheDependency(connectionString, queryString, cmdType, new Hashtable());
                    }
                }

                if (interimCommand != "INNER" && isInner && expirationHint != null) aggregateExpiration.Add(expirationHint);
            } while (endQuoteIndex > -1);

            return aggregateExpiration == null ? expirationHint : aggregateExpiration;
        }

        /// <summary>
        /// Make actual expiration hint from the Keys and file list provided.
        /// </summary>
        /// <param name="expHint">expiration hint</param>
        /// <param name="keysList">list of keys in the dependecy string</param>
        /// <param name="filesList">list of files in the dependency string</param>
        /// <param name="startsAfter">time at which the expiration begins</param>
        /// <returns>constructed expiration hint</returns>
        public ExpirationHint MakeExpirationHint(ExpirationHint expHint, ArrayList keysList, ArrayList filesList, DateTime startsAfter)
        {
            string[] keys = new string[keysList.Count];
            string[] files = new string[filesList.Count];

            for (int i = 0; i < keysList.Count; i++)
                keys[i] = (string)keysList[i];
            for (int i = 0; i < filesList.Count; i++)
                files[i] = (string)filesList[i];

            if (expHint == null)
            {
                if (keys.Length == 0)

                    return files.Length == 0 ?
                        null :
                        new AggregateExpirationHint(new FileDependency(files, startsAfter));

                else
                    return files.Length == 0 ?
                        new AggregateExpirationHint(new KeyDependency(keys, startsAfter)) :
                        new AggregateExpirationHint(new KeyDependency(keys, startsAfter), new FileDependency(files, startsAfter));
            }
            else
            {
                if (keys.Length == 0)
                {

                    return files.Length == 0 ?
                        new AggregateExpirationHint(expHint) :
                        new AggregateExpirationHint(expHint, new FileDependency(files, startsAfter));
                }
                else
                {
                    return files.Length == 0 ?
                        new AggregateExpirationHint(expHint, new KeyDependency(keys, startsAfter)) :
                        new AggregateExpirationHint(expHint, new KeyDependency(keys, startsAfter), new FileDependency(files, startsAfter));
                }
            }
        }

        /// <summary>
        /// Update the indexes passed to the next and current delimiter
        /// </summary>
        /// <param name="command">source string</param>
        /// <param name="delim">dlimiter</param>
        /// <param name="beginQuoteIndex">current delimiter index</param>
        /// <param name="endQuoteIndex">next delimiters index</param>
        private void UpdateDelimIndexes(string command, char delim, ref int beginQuoteIndex, ref int endQuoteIndex)
        {
            beginQuoteIndex = endQuoteIndex;
            endQuoteIndex = command.IndexOf(delim, beginQuoteIndex + 1);
        }
    }
}
