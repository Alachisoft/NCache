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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Protobuf;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#if JAVA
using Alachisoft.TayzGrid.Runtime.Dependencies;
#else
using Alachisoft.NCache.Runtime.Dependencies;
#endif 

using System.Collections;
#if JAVA
using Alachisoft.TayzGrid.Runtime.Exceptions;
#else
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Runtime.Caching;
#endif
#if JAVA
using Runtime = Alachisoft.TayzGrid.Runtime;
#else
using Runtime = Alachisoft.NCache.Runtime;
#endif
namespace Alachisoft.NCache.Common.Util
{
    public class DependencyHelper
    {
        public static CacheDependency GetCacheDependency(Dependency dependency)
        {
            CacheDependency cacheDependency = null;

            if (dependency == null) return null;

            if (dependency.keyDep.Count > 0)
            {
                for (int i = 0; i < dependency.keyDep.Count; i++)
                {
                    AddToDependency(ref cacheDependency,
                        new Runtime.Dependencies.KeyDependency(
                        dependency.keyDep[i].keys.ToArray(),
                        new DateTime(dependency.keyDep[i].startAfter)));
                }
            } 

            if (dependency.fileDep.Count > 0)
            {
                for (int i = 0; i < dependency.fileDep.Count; i++)
                {
                    AddToDependency(ref cacheDependency,
                                    new Runtime.Dependencies.FileDependency(
                                    dependency.fileDep[i].filePaths.ToArray(),
                                    new DateTime(dependency.fileDep[i].startAfter))); 
                }
            }

            if (dependency.oleDbDep.Count > 0)
            {
                for (int i = 0; i < dependency.oleDbDep.Count; i++)
                {
                    AddToDependency(ref cacheDependency,
                        new CacheDependency(null, null, DBDependencyFactory.CreateOleDbCacheDependency(
                        dependency.oleDbDep[i].connectionString,
                        dependency.oleDbDep[i].dbCacheKey)));
                }
            }

            if (dependency.sql7Dep.Count > 0)
            {
                for (int i = 0; i < dependency.sql7Dep.Count; i++)
                {
                    AddToDependency(ref cacheDependency,
                        new CacheDependency(null, null, DBDependencyFactory.CreateSqlCacheDependency(
                        dependency.sql7Dep[i].connectionString,
                        dependency.sql7Dep[i].dbCacheKey)));
                }
            }

            if (dependency.oracleDep.Count > 0)
            {
                for (int i = 0; i < dependency.oracleDep.Count; i++)
                {
                    Dictionary<string, OracleCmdParams> parameters = new Dictionary<string, OracleCmdParams>();
                    for (int pc = 0; pc < dependency.oracleDep[i].param.Count; pc++)
                    {
                        OracleCmdParams commandParam = new OracleCmdParams();

                        commandParam.Direction = (OracleParameterDirection)dependency.oracleDep[i].param[pc].cmdParam.direction;
                        commandParam.Type = (OracleCmdParamsType)dependency.oracleDep[i].param[pc].cmdParam.dbType;
                        commandParam.Value = dependency.oracleDep[i].param[pc].cmdParam.value;

                        parameters.Add(dependency.oracleDep[i].param[pc].key, commandParam);
                    }

                    OracleCacheDependency oraDep = new OracleCacheDependency(dependency.oracleDep[i].connectionString,
                        dependency.oracleDep[i].query,
                        (OracleCommandType)dependency.oracleDep[i].commandType,
                        parameters);

                    AddToDependency(ref cacheDependency, oraDep);
                }
            }


            if (dependency.yukonDep.Count > 0)
            {
                for (int i = 0; i < dependency.yukonDep.Count; i++)
                {
                    Dictionary<string, SqlCmdParams> parameters = new Dictionary<string, SqlCmdParams>();
                    for (int pc = 0; pc < dependency.yukonDep[i].param.Count; pc++)
                    {
                        YukonCommandParam yukonParam = dependency.yukonDep[i].param[pc].cmdParam;
                        SqlCmdParams commandParam = new SqlCmdParams();

                        commandParam.CompareInfo = (SqlCmpOptions)yukonParam.cmpOptions;
                        commandParam.Direction = (SqlParamDirection)yukonParam.direction;
                        commandParam.IsNullable = yukonParam.isNullable;
                        commandParam.LocaleID = yukonParam.localeId;
                        commandParam.Offset = yukonParam.offset;
                        commandParam.Precision = (byte)yukonParam.precision;
                        commandParam.Scale = (byte)yukonParam.scale;
                        commandParam.Size = yukonParam.size;
                        commandParam.SourceColumn = yukonParam.sourceColumn;
                        commandParam.SourceColumnNullMapping = yukonParam.sourceColumnNull;
                        commandParam.SqlValue = yukonParam.sqlValue;
                        commandParam.Type = (CmdParamsType)yukonParam.typeId;
                        commandParam.TypeName = yukonParam.typeName;
                        commandParam.UdtTypeName = yukonParam.udtTypeName;

                        if (!yukonParam.nullValueProvided)
                        {
                            string val = yukonParam.value as string;
                            if (val != null)
                            {
                                if (commandParam.Type == CmdParamsType.Binary || commandParam.Type == CmdParamsType.VarBinary || /*commandParam.Type == CmdParamsType.Image ||*/ commandParam.Type == CmdParamsType.Timestamp)
                                {
                                    System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                                    commandParam.Value = encoding.GetBytes(val);
                                }
                                else if (commandParam.Type == CmdParamsType.DateTime || commandParam.Type == CmdParamsType.DateTime2 || commandParam.Type == CmdParamsType.Date || commandParam.Type == CmdParamsType.SmallDateTime)
                                {
                                    commandParam.Value = new DateTime(Convert.ToInt64(val));
                                }
                                else if (commandParam.Type == CmdParamsType.Time)
                                {
                                    commandParam.Value = new TimeSpan(Convert.ToInt64(val));
                                }
                                else if (commandParam.Type == CmdParamsType.DateTimeOffset)
                                {
                                    string[] dateOffset = val.Split(new char[] { ',' });
                                    commandParam.Value = new DateTimeOffset(new DateTime(Convert.ToInt64(dateOffset[0])), TimeSpan.FromMinutes(Convert.ToInt32(dateOffset[1])));
                                }
                                else if (commandParam.Type == CmdParamsType.Money || commandParam.Type == CmdParamsType.SmallMoney || commandParam.Type == CmdParamsType.Decimal)
                                {
                                    commandParam.Value = Convert.ToDecimal(val);
                                }
                                else if (commandParam.Type == CmdParamsType.Int)
                                {
                                    commandParam.Value = Convert.ToInt32(val);
                                }
                                else if (commandParam.Type == CmdParamsType.BigInt)
                                {
                                    commandParam.Value = Convert.ToInt64(val);
                                }
                                else if (commandParam.Type == CmdParamsType.SmallInt)
                                {
                                    commandParam.Value = Convert.ToInt16(val);
                                }
                                else if (commandParam.Type == CmdParamsType.Real)
                                {
                                    commandParam.Value = Convert.ToSingle(val);
                                }
                                else if (commandParam.Type == CmdParamsType.UniqueIdentifier)
                                {
                                    commandParam.Value = new Guid(val);
                                }
                                else if (commandParam.Type == CmdParamsType.TinyInt)
                                {
                                    commandParam.Value = Convert.ToByte(val);
                                }
                                else if (commandParam.Type == CmdParamsType.Float)
                                {
                                    commandParam.Value = Convert.ToDouble(val);
                                }
                                else if (commandParam.Type == CmdParamsType.Bit)
                                {
                                    commandParam.Value = Convert.ToBoolean(val);
                                }
                                else
                                {
                                    commandParam.Value = val;
                                }
                            }
                        }

                        parameters.Add(dependency.yukonDep[i].param[pc].key, commandParam);
                    }

                    SqlCacheDependency yukonDep = new SqlCacheDependency(dependency.yukonDep[i].connectionString,
                        dependency.yukonDep[i].query,
                        (SqlCommandType)dependency.yukonDep[i].commandType,
                        parameters);

                    AddToDependency(ref cacheDependency, yukonDep);
                }
            }

            if (dependency.NosDep.Count > 0)
            {
                for (int i = 0; i < dependency.NosDep.Count; i++)
                {
                    Dictionary<string, object> parameters = new Dictionary<string, object>();
                    NosDbDependency nosDependency = dependency.NosDep[i];
                    for (int pc = 0; pc < nosDependency.param.Count; pc++)
                    {
                        parameters.Add(nosDependency.param[pc].key, nosDependency.param[pc].value);
                    }

                    NosDBDependency NosDep = new NosDBDependency(nosDependency.connectionString, nosDependency.query,
                        parameters, nosDependency.timeout);
                    AddToDependency(ref cacheDependency, NosDep);
                }
            }


            return cacheDependency;
        }        

        private static void AddToDependency(ref CacheDependency cacheDependency, CacheDependency newDependency)
        {
            if (cacheDependency != null)
            {
                cacheDependency.Dependencies.Add(newDependency);
            }
            else
            {
                cacheDependency = new CacheDependency(null, null, newDependency);
            }
        }

        public static Dependency GetProtoBufDependency(CacheDependency cacheDependency)
        {
            Dependency dependency = new Dependency();
            return GetProtoBufDependency(cacheDependency, dependency);
        }

        public static Dependency GetProtoBufDependency(CacheDependency cacheDependency, Dependency dependency)
        {
            if (cacheDependency is Runtime.Dependencies.FileDependency)
            {
                Runtime.Dependencies.FileDependency fileDependency = cacheDependency as Runtime.Dependencies.FileDependency;
                Protobuf.FileDependency protoDependency = new Protobuf.FileDependency();
                protoDependency.filePaths.AddRange(fileDependency.fileNames);
                protoDependency.startAfter = fileDependency.StartAfterTicks;

                dependency.fileDep.Add(protoDependency);

                return dependency;
            }
            else if (cacheDependency is Runtime.Dependencies.KeyDependency)
            {
                Runtime.Dependencies.KeyDependency keyDependency = cacheDependency as Runtime.Dependencies.KeyDependency;

                Alachisoft.NCache.Common.Protobuf.KeyDependency protoDependency = new Alachisoft.NCache.Common.Protobuf.KeyDependency();
                protoDependency.keys.AddRange(keyDependency.CacheKeys);
                protoDependency.startAfter = keyDependency.StartAfterTicks;

                dependency.keyDep.Add(protoDependency);

                return dependency;
            }
            else if (cacheDependency is Runtime.Dependencies.DBCacheDependency)
            {
                Runtime.Dependencies.DBCacheDependency dbDependency = cacheDependency as Runtime.Dependencies.DBCacheDependency;

                switch (dbDependency.Type)
                {
                    case Runtime.Dependencies.DBDependencyType.OleDbCacheDependency:
                        OleDbDependency oleDbDependency = new OleDbDependency();
                        oleDbDependency.connectionString = dbDependency.ConnectionString;
                        oleDbDependency.dbCacheKey = dbDependency.PrimaryKey;
                        dependency.oleDbDep.Add(oleDbDependency);

                        break;

                    case Runtime.Dependencies.DBDependencyType.SqlCacheDependency:
                        Sql7Dependency sqlDependency = new Sql7Dependency();
                        sqlDependency.connectionString = dbDependency.ConnectionString;
                        sqlDependency.dbCacheKey = dbDependency.PrimaryKey;
                        dependency.sql7Dep.Add(sqlDependency);

                        break;
                }

                return dependency;
            }
            else if (cacheDependency is Runtime.Dependencies.SqlCacheDependency)
            {
                Runtime.Dependencies.SqlCacheDependency sqlDependency = cacheDependency as Runtime.Dependencies.SqlCacheDependency;

                YukonDependency yukonDependency = new YukonDependency();
                yukonDependency.commandType = Convert.ToInt32(sqlDependency.CommandType);
                yukonDependency.connectionString = sqlDependency.ConnectionString;
                yukonDependency.query = sqlDependency.CommandText;

                if (sqlDependency.CommandParams != null)
                {
                    foreach (KeyValuePair<string, Runtime.Dependencies.SqlCmdParams> pair in sqlDependency.CommandParams)
                    {
                        Runtime.Dependencies.SqlCmdParams param = pair.Value;


                        YukonParam yukonParam = new YukonParam();
                        yukonParam.key = pair.Key;

                        YukonCommandParam yukonCmdParam = new YukonCommandParam();
                        yukonCmdParam.dbType = (int)param.SqlParamType;
                        yukonCmdParam.direction = (int)param.SqlParamDir;
                        yukonCmdParam.isNullable = param.IsNullable;
                        yukonCmdParam.localeId = param.LocaleID;
                        yukonCmdParam.offset = param.Offset;
                        yukonCmdParam.precision = param.Precision;
                        yukonCmdParam.scale = param.Scale;
                        yukonCmdParam.size = param.Size;
                        yukonCmdParam.sourceColumn = param.SourceColumn;
                        yukonCmdParam.sourceColumnNull = param.SourceColumnNullMapping;
                        yukonCmdParam.sqlValue = param.SqlValue != null ? param.SqlValue.ToString() : "";
                        yukonCmdParam.version = (int)param.SrcVersion;
                        yukonCmdParam.typeName = param.TypeName;
                        yukonCmdParam.typeId = (int)param.Type;
                        yukonCmdParam.udtTypeName = param.UdtTypeName;
                        yukonCmdParam.nullValueProvided = param.Value == null ? true : false;

                        if (!yukonCmdParam.nullValueProvided)
                        {
                            if (param.Type == CmdParamsType.Binary || param.Type == CmdParamsType.VarBinary /*|| param.Type == CmdParamsType.Image */|| param.Type == CmdParamsType.Timestamp)
                            {
                                byte[] val = param.Value as byte[];
                                if (val == null)
                                {
                                    throw new OperationFailedException("Expected 'System.Byte[]' value for parameter '" + param.SourceColumn + "'");
                                }
                                else
                                {
                                    System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                                    yukonCmdParam.value = encoding.GetString(val);
                                }
                            }
                            else if (param.Type == CmdParamsType.DateTime || param.Type == CmdParamsType.DateTime2 || param.Type == CmdParamsType.Date || param.Type == CmdParamsType.SmallDateTime)
                            {
                                try
                                {
                                    DateTime val = (DateTime)param.Value;
                                    yukonCmdParam.value = val.Ticks.ToString();
                                }
                                catch (InvalidCastException ex)
                                {
                                    throw new OperationFailedException("Expected 'System.DateTime' value for parameter type '" + param.Type + "'");
                                }
                            }
                            else if (param.Type == CmdParamsType.Time)
                            {
                                try
                                {
                                    TimeSpan val = (TimeSpan)param.Value;
                                    yukonCmdParam.value = val.Ticks.ToString();
                                }
                                catch (InvalidCastException ex)
                                {
                                    throw new OperationFailedException("Expected 'System.TimeSpan' value for parameter '" + param.Type + "'");
                                }
                            }
                            else if (param.Type == CmdParamsType.DateTimeOffset)
                            {
                                try
                                {
                                    DateTimeOffset val = (DateTimeOffset)param.Value;
                                    yukonCmdParam.value = String.Concat(val.Date.Ticks, ",", val.Offset.Minutes);
                                }
                                catch (InvalidCastException ex)
                                {
                                    throw new OperationFailedException("Expected 'System.DateTimeOffset' value for parameter '" + param.Type + "'");
                                }
                            }
                            else
                            {
                                yukonCmdParam.value = param.Value.ToString();
                            }
                        }
                        
                        yukonParam.cmdParam = yukonCmdParam;
                        yukonDependency.param.Add(yukonParam);
                    }
                }

                dependency.yukonDep.Add(yukonDependency);

                return dependency;
            }

            else if (cacheDependency is Runtime.Dependencies.OracleCacheDependency)
            {
                Runtime.Dependencies.OracleCacheDependency oracleDependency = cacheDependency as Runtime.Dependencies.OracleCacheDependency;

                OracleDependency protoDependency = new OracleDependency();
                protoDependency.commandType = (int)oracleDependency.CommandType;
                protoDependency.connectionString = oracleDependency.ConnectionString;
                protoDependency.query = oracleDependency.CommandText;

                if (oracleDependency.CommandParams != null)
                {
                    foreach (KeyValuePair<string, Runtime.Dependencies.OracleCmdParams> pair in oracleDependency.CommandParams)
                    {
                        Runtime.Dependencies.OracleCmdParams oracleCommandParams = pair.Value;

                        OracleParam param = new OracleParam();
                        param.key = pair.Key;

                        OracleCommandParam oracleCmdParam = new OracleCommandParam();
                        oracleCmdParam.dbType = (int)oracleCommandParams.Type;
                        oracleCmdParam.direction = (int)oracleCommandParams.Direction;
                        oracleCmdParam.value = oracleCommandParams.Value != null ? oracleCommandParams.Value.ToString() : "";

                        param.cmdParam = oracleCmdParam;

                        protoDependency.param.Add(param);
                    }
                }

                dependency.oracleDep.Add(protoDependency);

                return dependency;
            }


            else if (cacheDependency is Runtime.Dependencies.ExtensibleDependency)
            {
                Runtime.Dependencies.ExtensibleDependency extDependency = cacheDependency as Runtime.Dependencies.ExtensibleDependency;

                IFormatter formatter = new BinaryFormatter();
                MemoryStream stream = new MemoryStream();
                formatter.Serialize(stream, extDependency);

                Alachisoft.NCache.Common.Protobuf.ExtensibleDependency extensibleDependency = new Alachisoft.NCache.Common.Protobuf.ExtensibleDependency();
                extensibleDependency.data = stream.ToArray();

                dependency.xtDep.Add(extensibleDependency);

                return dependency;
            }
            else if (cacheDependency is NosDBDependency)
            {
                NosDBDependency sqlDependency = cacheDependency as NosDBDependency;

                NosDbDependency nosDependency = new NosDbDependency();
                nosDependency.timeout = Convert.ToInt32(sqlDependency.Timeout);
                nosDependency.connectionString = sqlDependency.ConnectionString;
                nosDependency.query = sqlDependency.CommandText;

                if (sqlDependency.Parameters != null)
                {
                    CommandHelper.PopulateValues(sqlDependency.Parameters, nosDependency.param);
                }
                dependency.NosDep.Add(nosDependency);

                return dependency;
            }
            else
            {
                foreach (Runtime.Dependencies.CacheDependency dep in cacheDependency.Dependencies)
                {
                    dependency = GetProtoBufDependency(dep, dependency);
                }

                return dependency;
            }
        }
    }

}
