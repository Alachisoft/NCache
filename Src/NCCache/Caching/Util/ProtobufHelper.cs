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
using System.Collections.Generic;

using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Caching.AutoExpiration;
using System.Data;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Alachisoft.NCache.Runtime.Exceptions;

using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Caching.Util
{
    public sealed class ProtobufHelper
    {
        public static Hashtable GetHashtableFromQueryInfoObj(QueryInfo queryInfo)
        {
            if (queryInfo == null)
            {
                return null;
            }

            Hashtable queryInfoTable = new Hashtable();

           
            int nullIndex = 0;

            string[] queryAttribValues=new string[queryInfo.attributes.Count];
            queryInfo.attributes.CopyTo(queryAttribValues);
            foreach (string attrib in queryAttribValues)
            {
                if (attrib == "NCNULL")
                {
                    queryInfo.attributes.Insert(nullIndex, null);
                    queryInfo.attributes.RemoveAt(nullIndex + 1);
                }
                nullIndex++;
            }



            ArrayList attributes = new ArrayList();
            foreach (string attrib in queryInfo.attributes)
            {
                attributes.Add(attrib);
            }

            queryInfoTable.Add(queryInfo.handleId, attributes);

            return queryInfoTable;
        }

        public static QueryInfo GetQueryInfoObj(Hashtable queryInfoDic)
        {
            if (queryInfoDic == null)
            {
                return null;
            }
            if (queryInfoDic.Count == 0)
            {
                return null;
            }

            QueryInfo queryInfo = new QueryInfo();

            IDictionaryEnumerator queryInfoEnum = queryInfoDic.GetEnumerator();
            while (queryInfoEnum.MoveNext())
            {
                queryInfo.handleId = (int)queryInfoEnum.Key;
                IEnumerator valuesEnum = ((ArrayList)queryInfoEnum.Value).GetEnumerator();
                
                while (valuesEnum.MoveNext())
                {
                    string value = null;
                    if (valuesEnum.Current != null)
                    {
                        if (valuesEnum.Current is DateTime)
                        {
                            value = ((DateTime)valuesEnum.Current).Ticks.ToString();
                        }
                        else
                        {
                            value = valuesEnum.Current.ToString();
                        }
                    }
                    else  //we need to send null values too as a special placeholder
                    {
                        value = "NCNULL";
                    }
                    queryInfo.attributes.Add(value);
                }
            }
            return queryInfo;
        }
        
        public static Hashtable GetHashtableFromTagInfoObj(TagInfo tagInfo)
        {
            if (tagInfo == null)
            {
                return null;
            }

            Hashtable tagInfoTable = new Hashtable();

            tagInfoTable["type"] = tagInfo.type;
            tagInfoTable["tags-list"] = new ArrayList(tagInfo.tags);

            return tagInfoTable;
        }

        public static TagInfo GetTagInfoObj(Hashtable tagInfoDic)
        {
            if (tagInfoDic == null)
            {
                return null;
            }
            if (tagInfoDic.Count == 0)
            {
                return null;
            }

            TagInfo tagInfo = new TagInfo();

            tagInfo.type = (string)tagInfoDic["type"];
            IEnumerator tagsEnum = ((ArrayList)tagInfoDic["tags-list"]).GetEnumerator();
            while (tagsEnum.MoveNext())
            {
                if (tagsEnum.Current != null)
                {
                    tagInfo.tags.Add(tagsEnum.Current.ToString());
                }
                else
                {
                    tagInfo.tags.Add(null);
                }
            }

            return tagInfo;
        }

        public static NamedTagInfo GetNamedTagInfoObj(Hashtable namedTagInfoDic, bool isDotNetClient)
        {
            NamedTagInfo tagInfo = new NamedTagInfo();
            tagInfo.type = (string)namedTagInfoDic["type"];
            foreach (DictionaryEntry entry in (Hashtable)namedTagInfoDic["named-tags-list"])
            {
                tagInfo.names.Add(entry.Key.ToString());
                if (isDotNetClient)
                {
                    tagInfo.types.Add(entry.Value.GetType().ToString());
                }

                else
                {
                    tagInfo.types.Add(JavaClrTypeMapping.ClrToJava(entry.Value.GetType().ToString()));
                }

                if (entry.Value.GetType() == typeof(DateTime))
                {
                    tagInfo.vals.Add(Convert.ToDateTime(entry.Value).Ticks.ToString());
                }
                else
                {
                    tagInfo.vals.Add(entry.Value.ToString());
                }
            }

            return tagInfo;
        }

        public static Hashtable GetHashtableFromNamedTagInfoObjFromDotNet(NamedTagInfo tagInfo)
        {
            if (tagInfo == null)
            {
                return null;
            }

            return GetHashtableFromNamedTagInfoObj(tagInfo);
        }

        private static Hashtable GetHashtableFromNamedTagInfoObj(NamedTagInfo tagInfo) 
        {
            Hashtable tagInfoTable = new Hashtable();
            tagInfoTable["type"] = tagInfo.type;

            Hashtable tagList = new Hashtable();
            for (int i = 0; i < tagInfo.names.Count; i++)
            {
                object obj = null;
                Type t1 = Type.GetType(tagInfo.types[i]);
                if (t1 == typeof(System.DateTime))
                {
                    obj = new DateTime(Convert.ToInt64(tagInfo.vals[i]));
                }
                else
                {
                    obj = Convert.ChangeType(tagInfo.vals[i], t1);
                }

                tagList.Add(tagInfo.names[i], obj);
            }

            tagInfoTable["named-tags-list"] = tagList;

            return tagInfoTable;
        }

        public static Hashtable GetHashtableFromNamedTagInfoObjFromJava(NamedTagInfo tagInfo)
        {
            if (tagInfo == null)
            {
                return null;
            }
            for (int i = 0; i < tagInfo.names.Count; i++)
            {
                tagInfo.types[i] = JavaClrTypeMapping.JavaToClr(tagInfo.types[i]);
            }
            return GetHashtableFromNamedTagInfoObj(tagInfo);
        }

        public static ExpirationHint GetExpirationHintObj(Alachisoft.NCache.Config.Dom.ExpirationPolicy policy, Alachisoft.NCache.Common.Protobuf.Dependency dependency, long absoluteExpiration, long slidingExpiration, bool resyncOnExpiration, string serializationContext)
        {
            ConvHelper.ValidateExpiration(policy);
            ExpirationHint hint = null;
            const long TICKS = 10000000;
            if ((absoluteExpiration == 1 || absoluteExpiration == 2) || (slidingExpiration == 1 || slidingExpiration == 2))
                if (policy.SlidingExpiration.LongerEnabled || policy.SlidingExpiration.DefaultEnabled || policy.AbsoluteExpiration.LongerEnabled || policy.AbsoluteExpiration.DefaultEnabled)
                {
                    #region In case user provides sliding expiration as an enum

                    if (absoluteExpiration == 0)
                    {
                        if (policy.SlidingExpiration.LongerEnabled && slidingExpiration == 2)
                            hint = new IdleExpiration(new TimeSpan(policy.SlidingExpiration.Longer * TICKS));
                        else if (policy.SlidingExpiration.DefaultEnabled && slidingExpiration == 1)
                            hint = new IdleExpiration(new TimeSpan(policy.SlidingExpiration.Default * TICKS));
                    }
                    #endregion

                    #region In case user provides absolute expiration as an enum

                    if (slidingExpiration == 0)
                    {
                        if (policy.AbsoluteExpiration.LongerEnabled && absoluteExpiration == 2)// If not enabled try to check if Longer Expiration is enabled
                            hint = new FixedExpiration(DateTime.Now.AddSeconds(policy.AbsoluteExpiration.Longer).ToUniversalTime());
                        else if (absoluteExpiration == 1 && policy.AbsoluteExpiration.DefaultEnabled)// check if Longer is enabled in config
                            hint = new FixedExpiration(DateTime.Now.AddSeconds(policy.AbsoluteExpiration.Default).ToUniversalTime());
                    }
                    #endregion
                }    
                else
                 {
                   absoluteExpiration = 0;// If user provides custom expiration but it is NOT enabled then item will stay in cache, forever.
                 }
            
            //We expect Web.Cache to send in UTC DateTime, AbsoluteExpiration is dealt in UTC
            if (absoluteExpiration != 0 && absoluteExpiration != 1 && absoluteExpiration != 2 && absoluteExpiration != DateTime.MaxValue.ToUniversalTime().Ticks) 
                hint = new FixedExpiration(new DateTime(absoluteExpiration, DateTimeKind.Utc));
            if (slidingExpiration != 0 && slidingExpiration != 1 && slidingExpiration != 2) 
                hint = new IdleExpiration(new TimeSpan(slidingExpiration));
            ExpirationHint depHint = GetExpirationHintObj(dependency, false, serializationContext);

            if (depHint != null)
            {
                if (hint != null)
                {
                    if (depHint is AggregateExpirationHint)
                    {
                        ((AggregateExpirationHint)depHint).Add(hint);
                        hint = depHint;
                    }
                    else
                    {
                        hint = new AggregateExpirationHint(hint, depHint);
                    }
                }
                else
                {
                    hint = depHint;
                }
            }

         
            if (hint != null && resyncOnExpiration)
            {
                hint.SetBit(ExpirationHint.NEEDS_RESYNC);
            }

            return hint;
        }

        
        public static ExpirationHint GetExpirationHintObj(Alachisoft.NCache.Common.Protobuf.Dependency dependency, bool resyncOnExpiration, string serializationContext)
        {
            AggregateExpirationHint hints = new AggregateExpirationHint();           

            if (dependency != null && dependency.keyDep != null && dependency.keyDep.Count > 0)
            {
                for (int i = 0; i < dependency.keyDep.Count; i++)
                {
                    Alachisoft.NCache.Caching.AutoExpiration.KeyDependency keyDep =
                        new Alachisoft.NCache.Caching.AutoExpiration.KeyDependency(dependency.keyDep[i].keys.ToArray(),
                        new DateTime(dependency.keyDep[i].startAfter));

                    hints.Add(keyDep);
                }
            }

            if (dependency != null && dependency.fileDep != null && dependency.fileDep.Count > 0)
            {
                for (int i = 0; i < dependency.fileDep.Count; i++)
                {
                    Alachisoft.NCache.Caching.AutoExpiration.FileDependency fileDep =
                        new Alachisoft.NCache.Caching.AutoExpiration.FileDependency(dependency.fileDep[i].filePaths.ToArray(),
                        new DateTime(dependency.fileDep[i].startAfter));

                    hints.Add(fileDep);
                }
            }

            if (dependency != null && dependency.oleDbDep != null && dependency.oleDbDep.Count > 0)
            {
                for (int i = 0; i < dependency.oleDbDep.Count; i++)
                {
                    OleDbCacheDependency oleDb = new OleDbCacheDependency(dependency.oleDbDep[i].connectionString,
                        dependency.oleDbDep[i].dbCacheKey);

                    hints.Add(oleDb);
                }
            }



            if (dependency != null && dependency.sql7Dep != null && dependency.sql7Dep.Count > 0)
            {
                for (int i = 0; i < dependency.sql7Dep.Count; i++)
                {
                    Sql7CacheDependency sql7Dep = new Sql7CacheDependency(dependency.sql7Dep[i].connectionString,
                        dependency.sql7Dep[i].dbCacheKey);

                    hints.Add(sql7Dep);
                }
            }


            if (dependency != null && dependency.xtDep != null && dependency.xtDep.Count > 0)
            {
                try
                {
                    for (int i = 0; i < dependency.xtDep.Count; i++)
                    {
                        IFormatter formater = new BinaryFormatter();
                        byte[] buffer = dependency.xtDep[i].data;
                        object obj = null;

                        using (MemoryStream stream = new MemoryStream(buffer))
                        {
                            obj = formater.Deserialize(stream);
                        }


                        Alachisoft.NCache.Caching.AutoExpiration.ExtensibleDependency xtDep =
                                    new Alachisoft.NCache.Caching.AutoExpiration.ExtensibleDependency(
                                    (Runtime.Dependencies.ExtensibleDependency)obj);


                        hints.Add(xtDep);
                    }
                }
                catch (SerializationException ex) 
                {
                    throw new OperationFailedException(ex.Message);
                }
            }

            if (dependency != null && dependency.oracleDep != null && dependency.oracleDep.Count > 0)
            {
                for (int i = 0; i < dependency.oracleDep.Count; i++)
                {
                    Hashtable parameters = new Hashtable();
                    for (int pc = 0; pc < dependency.oracleDep[i].param.Count; pc++)
                    {
                        OracleCommandParams commandParam = new OracleCommandParams((Runtime.Dependencies.OracleCmdParamsType)dependency.oracleDep[i].param[pc].cmdParam.dbType,
                            dependency.oracleDep[i].param[pc].cmdParam.value,
                            (Runtime.Dependencies.OracleParameterDirection)dependency.oracleDep[i].param[pc].cmdParam.direction);
                        parameters.Add(dependency.oracleDep[i].param[pc].key, commandParam);
                    }

                    OracleCacheDependency oraDep = new OracleCacheDependency(dependency.oracleDep[i].connectionString,
                        dependency.oracleDep[i].query,
                        (CommandType)dependency.oracleDep[i].commandType,
                        parameters);

                    hints.Add(oraDep);
                }
            }



            if (dependency != null && dependency.yukonDep != null && dependency.yukonDep.Count > 0)
            {
                for (int i = 0; i < dependency.yukonDep.Count; i++)
                {
                    Hashtable parameters = new Hashtable();
                    for (int pc = 0; pc < dependency.yukonDep[i].param.Count; pc++)
                    {
                        YukonCommandParam yukonParam = dependency.yukonDep[i].param[pc].cmdParam;
                        SqlCmdParams commandParam = new SqlCmdParams((SqlDbType)yukonParam.dbType, yukonParam.value);

                        commandParam.CmpInfo = (System.Data.SqlTypes.SqlCompareOptions)yukonParam.cmpOptions;
                        commandParam.Direction = (ParameterDirection)yukonParam.direction;
                        commandParam.IsNullable = yukonParam.isNullable;
                        commandParam.LocaleID = yukonParam.localeId;
                        commandParam.Offset = yukonParam.offset;
                        commandParam.Precision = (byte)yukonParam.precision;
                        commandParam.Scale = (byte)yukonParam.scale;
                        commandParam.ParamSize = yukonParam.size;
                        commandParam.SourceColumn = yukonParam.sourceColumn;
                        commandParam.SourceColumnNullMapping = yukonParam.sourceColumnNull;
                        commandParam.SqlValue = yukonParam.sqlValue;
                        commandParam.SrcVersion = (DataRowVersion)yukonParam.version;
                        commandParam.DbType = (SqlDbType)yukonParam.typeId;
                        commandParam.TypeName = yukonParam.typeName;
                        commandParam.UdtName = yukonParam.udtTypeName;

                        if (!yukonParam.nullValueProvided)
                        {
                            string val = yukonParam.value as string;
                            if (val != null)
                            {
                                if (commandParam.DbType == SqlDbType.Binary || commandParam.DbType == SqlDbType.VarBinary || commandParam.DbType == SqlDbType.Image || commandParam.DbType == SqlDbType.Timestamp)
                                {
                                    System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                                    commandParam.Value = encoding.GetBytes(val);
                                }
                                else if (commandParam.DbType == SqlDbType.DateTime || commandParam.DbType == SqlDbType.DateTime2 || commandParam.DbType == SqlDbType.Date || commandParam.DbType == SqlDbType.SmallDateTime)
                                {
                                    commandParam.Value = new DateTime(Convert.ToInt64(val));
                                }
                                else if (commandParam.DbType == SqlDbType.Time)
                                {
                                    commandParam.Value = new TimeSpan(Convert.ToInt64(val));
                                }
                                else if (commandParam.DbType == SqlDbType.DateTimeOffset)
                                {
                                    string[] dateOffset = val.Split(new char[] { ',' });
                                    commandParam.Value = new DateTimeOffset(new DateTime(Convert.ToInt64(dateOffset[0])), TimeSpan.FromMinutes(Convert.ToInt32(dateOffset[1])));
                                }
                                else if (commandParam.DbType == SqlDbType.Money || commandParam.DbType == SqlDbType.SmallMoney || commandParam.DbType == SqlDbType.Decimal)
                                {
                                    commandParam.Value = Convert.ToDecimal(val);
                                }
                                else if (commandParam.DbType == SqlDbType.Int)
                                {
                                    commandParam.Value = Convert.ToInt32(val);
                                }
                                else if (commandParam.DbType == SqlDbType.BigInt)
                                {
                                    commandParam.Value = Convert.ToInt64(val);
                                }
                                else if (commandParam.DbType == SqlDbType.SmallInt)
                                {
                                    commandParam.Value = Convert.ToInt16(val);
                                }
                                else if (commandParam.DbType == SqlDbType.Real)
                                {
                                    commandParam.Value = Convert.ToSingle(val);
                                }
                                else if (commandParam.DbType == SqlDbType.UniqueIdentifier)
                                {
                                    commandParam.Value = System.Data.SqlTypes.SqlGuid.Parse(val);
                                }
                                else if (commandParam.DbType == SqlDbType.TinyInt)
                                {
                                    commandParam.Value = Convert.ToByte(val);
                                }
                                else if (commandParam.DbType == SqlDbType.Float)
                                {
                                    commandParam.Value = Convert.ToDouble(val);
                                }
                                else if (commandParam.DbType == SqlDbType.Bit)
                                {
                                    commandParam.Value = Convert.ToBoolean(val);
                                }
                                else
                                {
                                    commandParam.Value = val;
                                }
                            }
                            else
                            {
                                commandParam.Value = DBNull.Value;
                            }
                        }
                        else
                        {
                            commandParam.Value = DBNull.Value;
                        }

                        parameters.Add(dependency.yukonDep[i].param[pc].key, commandParam);
                    }

                    SqlYukonCacheDependency yukonDep = new SqlYukonCacheDependency(dependency.yukonDep[i].connectionString,
                        dependency.yukonDep[i].query,
                        (CommandType)dependency.yukonDep[i].commandType,
                        parameters);

                    hints.Add(yukonDep);
                }
            }
            if (dependency != null && dependency.NosDep != null && dependency.NosDep.Count > 0)
            {
                for (int i = 0; i < dependency.NosDep.Count; i++)
                {
                    Hashtable parameters = new Hashtable();
                    for (int pc = 0; pc < dependency.NosDep[i].param.Count; pc++)
                    {
                       parameters.Add(dependency.NosDep[i].param[pc].key, dependency.NosDep[i].param[pc].value);
                    }
                    NosDBCacheDependency oraDep = new NosDBCacheDependency(dependency.NosDep[i].connectionString,
                        dependency.NosDep[i].query,
                        dependency.NosDep[i].timeout,
                        parameters);

                    hints.Add(oraDep);
                }
            }


            if (resyncOnExpiration) hints.SetBit(ExpirationHint.NEEDS_RESYNC);

            IList<ExpirationHint> expHints = hints.Hints;

            if (expHints.Count == 0)
            {
                return null;
            }

            if (expHints.Count == 1)
            {
                return expHints[0];
            }

            return hints;
        }


        public static Dependency GetDependencyObj(ExpirationHint internalHint)
        {
            if (internalHint == null)
            {
                return null;
            }

            Dependency dependency = new Dependency();

            if (internalHint is AggregateExpirationHint)
            {
                AggregateExpirationHint hint = (AggregateExpirationHint)internalHint;
                for (int i = 0; i < hint.Hints.Count; i++)
                {
                    AddHintToDependency(dependency, hint.Hints[i]);
                }
            }
            else
            {
                AddHintToDependency(dependency, internalHint);
            }

            return dependency;
        }

        private static void AddHintToDependency(Dependency dependency, ExpirationHint hint)
        {
            if (hint is Alachisoft.NCache.Caching.AutoExpiration.KeyDependency)
            {
                dependency.keyDep.Add(GetKeyDependency((Alachisoft.NCache.Caching.AutoExpiration.KeyDependency)hint));
            }
            else if (hint is Alachisoft.NCache.Caching.AutoExpiration.FileDependency)
            {
                dependency.fileDep.Add(GetFileDependency((Alachisoft.NCache.Caching.AutoExpiration.FileDependency)hint));
            }
            else if (hint is OleDbCacheDependency)
            {
                dependency.oleDbDep.Add(GetOleDbDependency((OleDbCacheDependency)hint));
            }
            else if (hint is Sql7CacheDependency)
            {
                dependency.sql7Dep.Add(GetSql7Dependency((Sql7CacheDependency)hint));
            }
            else if (hint is OracleCacheDependency)
            {
                dependency.oracleDep.Add(GetOracleDependency((OracleCacheDependency)hint));
            }
            else if (hint is SqlYukonCacheDependency)
            {
                dependency.yukonDep.Add(GetYukonDependency((SqlYukonCacheDependency)hint));
            }
            else if (hint is NosDBCacheDependency)
            {
                dependency.NosDep.Add(GetNosDBDependency((NosDBCacheDependency)hint));
            }


        }

        private static Alachisoft.NCache.Common.Protobuf.KeyDependency GetKeyDependency(Alachisoft.NCache.Caching.AutoExpiration.KeyDependency keyDep)
        {
            Common.Protobuf.KeyDependency protoKeyDep = new Common.Protobuf.KeyDependency();

            protoKeyDep.keys.AddRange(keyDep.CacheKeys);
            protoKeyDep.startAfter = keyDep.StartAfterTicks;

            return protoKeyDep;
        }

        private static Alachisoft.NCache.Common.Protobuf.FileDependency GetFileDependency(Alachisoft.NCache.Caching.AutoExpiration.FileDependency fileDep)
        {
            Alachisoft.NCache.Common.Protobuf.FileDependency protoFileDep
                            = new Alachisoft.NCache.Common.Protobuf.FileDependency();

            protoFileDep.filePaths.AddRange(fileDep.fileNames);
            protoFileDep.startAfter = fileDep.StartAfterTicks;

            return protoFileDep;
        }


        private static OleDbDependency GetOleDbDependency(OleDbCacheDependency oleDbDep)
        {
            OleDbDependency protoOleDb = new OleDbDependency();
            protoOleDb.connectionString = oleDbDep.ConnectionString;
            protoOleDb.dbCacheKey = oleDbDep.DbCacheKey;

            return protoOleDb;
        }



        private static Sql7Dependency GetSql7Dependency(Sql7CacheDependency sql7Dep)
        {
            Sql7Dependency protoSql7Dep = new Sql7Dependency();
            protoSql7Dep.connectionString = sql7Dep.ConnectionString;
            protoSql7Dep.dbCacheKey = sql7Dep.DbCacheKey;

            return protoSql7Dep;
        }

        private static Alachisoft.NCache.Common.Protobuf.OracleDependency GetOracleDependency(Alachisoft.NCache.Caching.AutoExpiration.OracleCacheDependency oracleDep)
        {
            Alachisoft.NCache.Common.Protobuf.OracleDependency protoOraDep = new Alachisoft.NCache.Common.Protobuf.OracleDependency();
            protoOraDep.commandType = (int)oracleDep.CommandType;
            protoOraDep.connectionString = oracleDep.ConnectionString;
            protoOraDep.query = oracleDep.QueryString;

            foreach (DictionaryEntry entry in oracleDep.CommandParams)
            {
                OracleCommandParams oracleCommandParams = (OracleCommandParams)entry.Value;

                OracleParam param = new OracleParam();
                param.key = (string)entry.Key;

                param.cmdParam = new OracleCommandParam();
                param.cmdParam.dbType = (int)oracleCommandParams.Type;
                param.cmdParam.direction = (int)oracleCommandParams.Direction;
                param.cmdParam.value = oracleCommandParams.Value.ToString();

                protoOraDep.param.Add(param);
            }

            return protoOraDep;
        }




        private static YukonDependency GetYukonDependency(SqlYukonCacheDependency yukonDep)
        {
            YukonDependency protoYukonDep = new YukonDependency();
            protoYukonDep.commandType = (int)yukonDep.CommandType;
            protoYukonDep.connectionString = yukonDep.ConnectionString;
            protoYukonDep.query = yukonDep.QueryString;

            foreach (DictionaryEntry entry in yukonDep.CommandParams)
            {
                SqlCmdParams yukonCommandParams = (SqlCmdParams)entry.Value;

                YukonParam param = new YukonParam();
                param.key = (string)entry.Key;

                param.cmdParam = new YukonCommandParam();
                param.cmdParam.cmpOptions = (int)yukonCommandParams.CmpInfo;
                param.cmdParam.direction = (int)yukonCommandParams.Direction;
                param.cmdParam.isNullable = yukonCommandParams.IsNullable;
                param.cmdParam.localeId = yukonCommandParams.LocaleID;
                param.cmdParam.offset = yukonCommandParams.Offset;
                param.cmdParam.precision = yukonCommandParams.Precision;
                param.cmdParam.scale = yukonCommandParams.Scale;
                param.cmdParam.size = yukonCommandParams.ParamSize;
                param.cmdParam.sourceColumn = yukonCommandParams.SourceColumn;
                param.cmdParam.sourceColumnNull = yukonCommandParams.SourceColumnNullMapping;
                param.cmdParam.sqlValue = yukonCommandParams.SqlValue.ToString();
                param.cmdParam.version = (int)yukonCommandParams.SrcVersion;
                param.cmdParam.typeId = (int)yukonCommandParams.DbType;
                param.cmdParam.typeName = yukonCommandParams.TypeName;
                param.cmdParam.udtTypeName = yukonCommandParams.UdtName;
                param.cmdParam.nullValueProvided = yukonCommandParams.Value == null;

                if (!param.cmdParam.nullValueProvided)
                {
                    if (yukonCommandParams.DbType == SqlDbType.Binary || yukonCommandParams.DbType == SqlDbType.VarBinary || yukonCommandParams.DbType == SqlDbType.Image || yukonCommandParams.DbType == SqlDbType.Timestamp)
                    {
                        byte[] val = yukonCommandParams.Value as byte[];
                        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                        param.cmdParam.value = encoding.GetString(val);
                    }
                    else if (yukonCommandParams.DbType == SqlDbType.DateTime || yukonCommandParams.DbType == SqlDbType.DateTime2 || yukonCommandParams.DbType == SqlDbType.Date || yukonCommandParams.DbType == SqlDbType.SmallDateTime)
                    {
                        DateTime val = (DateTime)yukonCommandParams.Value;
                        param.cmdParam.value = val.Ticks.ToString();
                    }
                    else if (yukonCommandParams.DbType == SqlDbType.Time)
                    {
                        TimeSpan val = (TimeSpan)yukonCommandParams.Value;
                        param.cmdParam.value = val.Ticks.ToString();
                    }
                    else if (yukonCommandParams.DbType == SqlDbType.DateTimeOffset)
                    {
                        DateTimeOffset val = (DateTimeOffset)yukonCommandParams.Value;
                        param.cmdParam.value = String.Concat(val.Date.Ticks, ",", val.Offset.Minutes);
                    }
                    else
                    {
                        param.cmdParam.value = yukonCommandParams.Value.ToString();
                    }
                }

                protoYukonDep.param.Add(param);
            }

            return protoYukonDep;
        }


        private static NosDbDependency GetNosDBDependency(NosDBCacheDependency NosDBDep)
        {
            NosDbDependency protoNosDB = new NosDbDependency();
            protoNosDB.timeout = NosDBDep.Timeout;
            protoNosDB.connectionString = NosDBDep.ConnectionString;
            protoNosDB.query = NosDBDep.QueryString;

            if (NosDBDep.CommandParams != null)
            {
                CommandHelper.PopulateValues(NosDBDep.CommandParams, protoNosDB.param);
            }
            return protoNosDB;
        }


    }
}
