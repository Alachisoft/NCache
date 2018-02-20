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
using System.Collections.Generic;
using System.Collections;
using System.Data;

using Alachisoft.NCache.Runtime.Dependencies;

using Alachisoft.NCache.Config.Dom;
namespace Alachisoft.NCache.Caching.AutoExpiration
{
    public class DependencyHelper
    {
        internal static DateTime AbsoluteDefaultExpiration = DateTime.MinValue.AddYears(1);
        internal static DateTime AbsoluteLongerExpiration = DateTime.MinValue.AddYears(2);

        internal static TimeSpan SlidingDefaultExpiration = TimeSpan.MinValue.Add(new TimeSpan(0, 0, 1));
        internal static TimeSpan SlidingLongerExpiration = TimeSpan.MinValue.Add(new TimeSpan(0, 0, 2));

        List<Runtime.Dependencies.CacheDependency> aggregateList = new List<CacheDependency>();
        private static long TICKS = 10000000;
        public CacheDependency GetActualCacheDependency(ExpirationHint hint, ref DateTime absoluteExpiration, ref TimeSpan slidingExpiration)
        {
            if (hint != null)
            {
                if (hint is AggregateExpirationHint)
                {
                    AggregateExpirationHint aggregate = (AggregateExpirationHint)hint;
                    IList<ExpirationHint> hints = aggregate.Hints;
                    if (hints != null && hints.Count > 0)
                    {
                        foreach (ExpirationHint h in hints)
                        {
                            CacheDependency cacheDep = GetActualCacheDependency(h, ref absoluteExpiration, ref slidingExpiration);
                            if (cacheDep != null && !(cacheDep is Runtime.Dependencies.AggregateCacheDependency))
                                aggregateList.Add(cacheDep);
                        }
                        if (aggregateList.Count >= 2)
                        {
                            Runtime.Dependencies.AggregateCacheDependency aggDep = new Runtime.Dependencies.AggregateCacheDependency();
                            aggDep.Add(aggregateList.ToArray());
                            return aggDep;
                        }
                        else
                        {
                            return aggregateList[0];
                        }
                    }
                }
                else if (hint is FixedExpiration)
                {
                    absoluteExpiration = ((FixedExpiration)hint).AbsoluteTime;
                }
                else if (hint is IdleExpiration)
                {
                    slidingExpiration = ((IdleExpiration)hint).SlidingTime;
                }
                else
                {
                    return GetNonAggregateDependencies(hint);
                }
            }
                return null;
        }
        private CacheDependency GetNonAggregateDependencies(ExpirationHint hint)
        {
            if (hint is KeyDependency)
            {
                KeyDependency keyDep = (KeyDependency)hint;

                return new Runtime.Dependencies.KeyDependency(keyDep.CacheKeys, new DateTime(keyDep.StartAfterTicks));
            }
            else if (hint is FileDependency)
            {
                FileDependency fileDep = (FileDependency)hint;
                return new Runtime.Dependencies.FileDependency(fileDep.fileNames, new DateTime(fileDep.StartAfterTicks));
            }

            else if (hint is OleDbCacheDependency)
            {
                OleDbCacheDependency oleDbDep = (OleDbCacheDependency)hint;
                return DBDependencyFactory.CreateOleDbCacheDependency(oleDbDep.ConnectionString, oleDbDep.DbCacheKey);
            }
            else if (hint is Sql7CacheDependency)
            {
                Sql7CacheDependency sql7Dep = (Sql7CacheDependency)hint;
                return DBDependencyFactory.CreateSqlCacheDependency(sql7Dep.ConnectionString, sql7Dep.DbCacheKey);
            }

            else if (hint is OracleCacheDependency)
            {
                OracleCacheDependency oracleDep = (OracleCacheDependency)hint;

                Dictionary<string, OracleCmdParams> parameters = new Dictionary<string, OracleCmdParams>();
                IDictionaryEnumerator enu = oracleDep.CommandParams.GetEnumerator();
                while (enu.MoveNext())
                {
                    string key = enu.Key.ToString();
                    OracleCommandParams cmdParam = (OracleCommandParams)enu.Value;

                    OracleCmdParams commandParam = new OracleCmdParams();

                    commandParam.Direction = (OracleParameterDirection)cmdParam.Direction;
                    commandParam.Type = (OracleCmdParamsType)cmdParam.Type;
                    commandParam.Value = cmdParam.Value;

                    parameters.Add(key, commandParam);
                }

                Runtime.Dependencies.OracleCacheDependency oraDep = new Runtime.Dependencies.OracleCacheDependency(oracleDep.ConnectionString,
                oracleDep.QueryString,
                    (OracleCommandType)oracleDep.CommandType,
                    parameters);
                return oraDep;
            }

            else if (hint is SqlYukonCacheDependency)
            {
                SqlYukonCacheDependency yukonDep = (SqlYukonCacheDependency)hint;

                Dictionary<string, Runtime.Dependencies.SqlCmdParams> parameters = new Dictionary<string, Runtime.Dependencies.SqlCmdParams>();
                IDictionaryEnumerator enu = yukonDep.CommandParams.GetEnumerator();

                while (enu.MoveNext())
                {
                    string key = enu.Key.ToString();
                    SqlCmdParams param = (SqlCmdParams)enu.Value;
                    Runtime.Dependencies.SqlCmdParams yukonCmdParam = new Runtime.Dependencies.SqlCmdParams();

                    yukonCmdParam.SqlParamDir = param.Direction;
                    yukonCmdParam.Type = (CmdParamsType)param.DbType;
                    yukonCmdParam.IsNullable = param.IsNullable;
                    yukonCmdParam.LocaleID = param.LocaleID;
                    yukonCmdParam.Offset = param.Offset;
                    yukonCmdParam.Precision = param.Precision;
                    yukonCmdParam.Scale = param.Scale;
                    yukonCmdParam.Size = param.ParamSize;
                    yukonCmdParam.SourceColumn = param.SourceColumn;
                    yukonCmdParam.SourceColumnNullMapping = param.SourceColumnNullMapping;
                    yukonCmdParam.SqlValue = param.SqlValue;
                    yukonCmdParam.SourceVersion = (SqlDataRowVersion)(int)param.SrcVersion;
                    yukonCmdParam.TypeName = param.TypeName;
                    yukonCmdParam.UdtTypeName = param.UdtName;
                    yukonCmdParam.Value = param.Value;

                    parameters.Add(key, yukonCmdParam);
                }

                Runtime.Dependencies.SqlCacheDependency sqlDep = new Runtime.Dependencies.SqlCacheDependency(yukonDep.ConnectionString,
                     yukonDep.QueryString,
                     (SqlCommandType)yukonDep.CommandType,
                     parameters);
                return sqlDep;
            }
            if (hint is NosDBCacheDependency)
            {
                NosDBCacheDependency nosDbDep = (NosDBCacheDependency)hint;

                Dictionary<string, object> parameters = new Dictionary<string, object>();
                IDictionaryEnumerator enu = nosDbDep.CommandParams.GetEnumerator();
                while (enu.MoveNext())
                {
                    string key = enu.Key.ToString();
                    parameters.Add(key, enu.Value);
                }
                NosDBDependency oraDep = new NosDBDependency(nosDbDep.ConnectionString, nosDbDep.QueryString, parameters, nosDbDep.Timeout);
                return oraDep;
            }
            return null;
        }
 
        public static CacheDependency GetCacheDependency(ExpirationHint hint, ref DateTime absoluteExpiration, ref TimeSpan slidingExpiration)
        {
            CacheDependency cacheDependency = null;
          
			if (hint != null)
			{
				if (hint is AggregateExpirationHint)
				{
					AggregateExpirationHint aggregate = (AggregateExpirationHint)hint;
                    IList<ExpirationHint> hints = aggregate.Hints;
					if (hints != null && hints.Count > 0)
					{
						foreach (ExpirationHint h in hints)
						{
							AddToDependency(ref cacheDependency, GetCacheDependency(h, ref absoluteExpiration, ref slidingExpiration));
						}
					}
				}
               
				else if (hint is FixedExpiration)
				{
					absoluteExpiration = ((FixedExpiration)hint).AbsoluteTime;
				}
				else if (hint is IdleExpiration)
				{
					slidingExpiration = ((IdleExpiration)hint).SlidingTime;
				}

				else if (hint is KeyDependency)
				{
					KeyDependency keyDep = (KeyDependency)hint;

					AddToDependency(ref cacheDependency,                        
						new Runtime.Dependencies.KeyDependency(keyDep.CacheKeys, new DateTime(keyDep.StartAfterTicks)));
				}

				else if (hint is FileDependency)
				{
					FileDependency fileDep = (FileDependency)hint;

					AddToDependency(ref cacheDependency, new Runtime.Dependencies.FileDependency(fileDep.fileNames, new DateTime(fileDep.StartAfterTicks)));

				}


				else if (hint is OleDbCacheDependency)
				{
					OleDbCacheDependency oleDbDep = (OleDbCacheDependency)hint;

					AddToDependency(ref cacheDependency,
						new CacheDependency(null, null, DBDependencyFactory.CreateOleDbCacheDependency(
							oleDbDep.ConnectionString, oleDbDep.DbCacheKey)));
				}




				else if (hint is Sql7CacheDependency)
				{
					Sql7CacheDependency sql7Dep = (Sql7CacheDependency)hint;
					AddToDependency(ref cacheDependency,
						new CacheDependency(null, null, DBDependencyFactory.CreateSqlCacheDependency(
						sql7Dep.ConnectionString, sql7Dep.DbCacheKey)));
				}




                else if (hint is OracleCacheDependency)
                {
                    OracleCacheDependency oracleDep = (OracleCacheDependency)hint;

                    Dictionary<string, OracleCmdParams> parameters = new Dictionary<string, OracleCmdParams>();
                    IDictionaryEnumerator enu = oracleDep.CommandParams.GetEnumerator();
                    while (enu.MoveNext())
                    {
                        string key = enu.Key.ToString();
                        OracleCommandParams cmdParam = (OracleCommandParams)enu.Value;

                        OracleCmdParams commandParam = new OracleCmdParams();

                        commandParam.Direction = (OracleParameterDirection)cmdParam.Direction;
                        commandParam.Type = (OracleCmdParamsType)cmdParam.Type;
                        commandParam.Value = cmdParam.Value;

                        parameters.Add(key, commandParam);
                    }

                    Runtime.Dependencies.OracleCacheDependency oraDep = new Runtime.Dependencies.OracleCacheDependency(oracleDep.ConnectionString,
                    oracleDep.QueryString,
                        (OracleCommandType)oracleDep.CommandType,
                        parameters);

                    AddToDependency(ref cacheDependency, oraDep);
                }


                else if (hint is SqlYukonCacheDependency)
                {
                    SqlYukonCacheDependency yukonDep = (SqlYukonCacheDependency)hint;

                    Dictionary<string, Runtime.Dependencies.SqlCmdParams> parameters = new Dictionary<string, Runtime.Dependencies.SqlCmdParams>();
                    IDictionaryEnumerator enu = yukonDep.CommandParams.GetEnumerator();

                    while (enu.MoveNext())
                    {
                        string key = enu.Key.ToString();
                        SqlCmdParams param = (SqlCmdParams)enu.Value;
                        Runtime.Dependencies.SqlCmdParams yukonCmdParam = new Runtime.Dependencies.SqlCmdParams();

                        yukonCmdParam.SqlParamDir = param.Direction;
                        yukonCmdParam.Type = (CmdParamsType)param.DbType;
                        yukonCmdParam.IsNullable = param.IsNullable;
                        yukonCmdParam.LocaleID = param.LocaleID;
                        yukonCmdParam.Offset = param.Offset;
                        yukonCmdParam.Precision = param.Precision;
                        yukonCmdParam.Scale = param.Scale;
                        yukonCmdParam.Size = param.ParamSize;
                        yukonCmdParam.SourceColumn = param.SourceColumn;
                        yukonCmdParam.SourceColumnNullMapping = param.SourceColumnNullMapping;
                        yukonCmdParam.SqlValue = param.SqlValue;
                        yukonCmdParam.SourceVersion = (SqlDataRowVersion)(int)param.SrcVersion;
                        yukonCmdParam.TypeName = param.TypeName;
                        yukonCmdParam.UdtTypeName = param.UdtName;
                        yukonCmdParam.Value = param.Value;

                        parameters.Add(key, yukonCmdParam);
                    }

                    Runtime.Dependencies.SqlCacheDependency sqlDep = new Runtime.Dependencies.SqlCacheDependency(yukonDep.ConnectionString,
                        yukonDep.QueryString,
                        (SqlCommandType)yukonDep.CommandType,
                        parameters);

                    AddToDependency(ref cacheDependency, sqlDep);
                }
                else if (hint is NosDBCacheDependency)
                {
                    NosDBCacheDependency nosDbDep = (NosDBCacheDependency)hint;

                    Dictionary<string, object> parameters = new Dictionary<string, object>();
                    IDictionaryEnumerator enu = nosDbDep.CommandParams.GetEnumerator();
                    while (enu.MoveNext())
                    {
                        string key = enu.Key.ToString();
                        parameters.Add(key, enu.Value);
                    }
                    NosDBDependency nosDb = new NosDBDependency(nosDbDep.ConnectionString, nosDbDep.QueryString, parameters, nosDbDep.Timeout);
                    AddToDependency(ref cacheDependency, nosDb);
                }


            }

            return cacheDependency;
		}

		private static void AddToDependency(ref CacheDependency cacheDependency, CacheDependency newDependency)
		{
			if (newDependency != null)
			{
                if (cacheDependency == null)
                    cacheDependency = new CacheDependency();

				cacheDependency.Dependencies.Add(newDependency);
			}
		}

        public static ExpirationHint GetExpirationHint(ExpirationPolicy  policy, CacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            ExpirationHint hint = GetExpirationHint(policy,absoluteExpiration, slidingExpiration);

            if (hint == null)
                return GetExpirationHint(dependency);
            
            ExpirationHint hint2 = GetExpirationHint(dependency);

            if (hint2 == null)
                return hint;


            AggregateExpirationHint aggregateHint = null;

            if (hint2 is AggregateExpirationHint)
            {
                aggregateHint = hint2 as AggregateExpirationHint;
                aggregateHint.Add(hint);
                
                return aggregateHint;
            }

            aggregateHint = new AggregateExpirationHint();
            aggregateHint.Add(hint);
            aggregateHint.Add(hint2);
            
            return aggregateHint;
        }

        public static ExpirationHint GetExpirationHint(CacheDependency dependency)
        {
            AggregateExpirationHint aggregateHint = new AggregateExpirationHint();
            return GetExpirationHint(dependency, aggregateHint);
        }

        private static ExpirationHint GetExpirationHint(ExpirationPolicy policy, DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
          ExpirationHint hint = null;
          if (policy.SlidingExpiration.LongerEnabled || policy.SlidingExpiration.DefaultEnabled || policy.AbsoluteExpiration.LongerEnabled || policy.AbsoluteExpiration.DefaultEnabled)
          {
            if ((absoluteExpiration == AbsoluteDefaultExpiration.ToUniversalTime() || absoluteExpiration == AbsoluteLongerExpiration.ToUniversalTime() || (slidingExpiration == SlidingDefaultExpiration || slidingExpiration == SlidingLongerExpiration)))
              {
                  #region In case user provides sliding expiration as an enum

                  if (absoluteExpiration == DateTime.MaxValue)
                  {
                      if (slidingExpiration == SlidingLongerExpiration && policy.SlidingExpiration.LongerEnabled)
                          hint = new IdleExpiration(new TimeSpan(policy.SlidingExpiration.Longer * TICKS));
                      else if (slidingExpiration == SlidingDefaultExpiration && policy.SlidingExpiration.DefaultEnabled)
                          hint = new IdleExpiration(new TimeSpan(policy.SlidingExpiration.Default * TICKS));
                  }
                  #endregion


                  #region In case user provides absolute expiration as an enum

                  if (slidingExpiration == TimeSpan.Zero)
                  {
                      if (absoluteExpiration == AbsoluteLongerExpiration.ToUniversalTime() && policy.AbsoluteExpiration.LongerEnabled)// check if Longer is enabled in config
                          hint = new FixedExpiration(DateTime.Now.AddSeconds(policy.AbsoluteExpiration.Longer).ToUniversalTime());
                      else if (absoluteExpiration == AbsoluteDefaultExpiration.ToUniversalTime() && policy.AbsoluteExpiration.DefaultEnabled)
                          hint = new FixedExpiration(DateTime.Now.AddSeconds(policy.AbsoluteExpiration.Default).ToUniversalTime());
                  }
                  #endregion

                  // If no expirations were enabled we have to assign the default expiration
                  

              }
              else
              {
                  if (hint == null)
                      hint = new FixedExpiration(DateTime.Now.AddSeconds(policy.AbsoluteExpiration.Default).ToUniversalTime());
              }
          }
          


            if (DateTime.MaxValue.ToUniversalTime().Equals(absoluteExpiration) && TimeSpan.Zero.Equals(slidingExpiration))
                return null;

            if (DateTime.MaxValue.ToUniversalTime().Equals(absoluteExpiration))
                hint = new IdleExpiration(slidingExpiration);
            else
            {
                absoluteExpiration = absoluteExpiration.ToUniversalTime();
                hint = new FixedExpiration(absoluteExpiration);
            }

            return hint;
        }

        private static ExpirationHint GetExpirationHint(CacheDependency cacheDependency, AggregateExpirationHint aggregateHint)
        {
            if (cacheDependency == null)
            {
                return null;
            }

            ExpirationHint hint = null;
            
            if (cacheDependency is Runtime.Dependencies.KeyDependency)
            {
                Runtime.Dependencies.KeyDependency keyDependency = cacheDependency as Runtime.Dependencies.KeyDependency;
                hint = new KeyDependency(keyDependency.CacheKeys, new DateTime(keyDependency.StartAfterTicks));

                aggregateHint.Add(hint);
            }


            else if (cacheDependency is Runtime.Dependencies.FileDependency)
            {
                Runtime.Dependencies.FileDependency fileDependency = cacheDependency as Runtime.Dependencies.FileDependency;

                hint = new FileDependency(fileDependency.fileNames, new DateTime(fileDependency.StartAfterTicks));

                aggregateHint.Add(hint);
            }

            else if (cacheDependency is Runtime.Dependencies.DBCacheDependency)
            {
                Runtime.Dependencies.DBCacheDependency dbDependency = cacheDependency as Runtime.Dependencies.DBCacheDependency;

                switch (dbDependency.Type)
                {
                    case Runtime.Dependencies.DBDependencyType.OleDbCacheDependency:
                        hint = new OleDbCacheDependency(dbDependency.ConnectionString, dbDependency.PrimaryKey);
                        break;

                    case Runtime.Dependencies.DBDependencyType.SqlCacheDependency:
                        hint = new Sql7CacheDependency(dbDependency.ConnectionString, dbDependency.PrimaryKey);
                        break;
                }

                aggregateHint.Add(hint);
            }


            else if (cacheDependency is Runtime.Dependencies.OracleCacheDependency)
            {
                Runtime.Dependencies.OracleCacheDependency oracleDependency = cacheDependency as Runtime.Dependencies.OracleCacheDependency;

                Hashtable parameters = new Hashtable();
                if (oracleDependency.CommandParams != null)
                {
                    foreach (KeyValuePair<string, Runtime.Dependencies.OracleCmdParams> pair in oracleDependency.CommandParams)
                    {
                        Runtime.Dependencies.OracleCmdParams oracleCommandParams = pair.Value;
                        OracleCommandParams commandParam = new OracleCommandParams((OracleCmdParamsType)oracleCommandParams.Type, oracleCommandParams.Value, oracleCommandParams.Direction);
                        parameters.Add(pair.Key, commandParam);
                    }
                }

                hint = new OracleCacheDependency(oracleDependency.ConnectionString, oracleDependency.CommandText, oracleDependency.CommandType, parameters);
                aggregateHint.Add(hint);
            }



            else if (cacheDependency is Runtime.Dependencies.SqlCacheDependency)
            {
                Runtime.Dependencies.SqlCacheDependency sqlDependency = cacheDependency as Runtime.Dependencies.SqlCacheDependency;

                Hashtable parameters = new Hashtable();

                if (sqlDependency.CommandParams != null)
                {
                    foreach (KeyValuePair<string, Runtime.Dependencies.SqlCmdParams> pair in sqlDependency.CommandParams)
                    {
                        Runtime.Dependencies.SqlCmdParams param = pair.Value;

                        Alachisoft.NCache.Caching.AutoExpiration.SqlCmdParams commandParam = new SqlCmdParams();
                        commandParam.CmpInfo = param.SqlCmpInfo;
                        commandParam.Direction = param.SqlParamDir;
                        commandParam.IsNullable = param.IsNullable;
                        commandParam.LocaleID = param.LocaleID;
                        commandParam.Offset = param.Offset;
                        commandParam.Precision = param.Precision;
                        commandParam.Scale = param.Scale;
                        commandParam.ParamSize = param.Size;
                        commandParam.SourceColumn = param.SourceColumn;
                        commandParam.SourceColumnNullMapping = param.SourceColumnNullMapping;
                        commandParam.SqlValue = param.SqlValue;
                        commandParam.SrcVersion = param.SrcVersion;
                        commandParam.DbType = (SqlDbType)param.Type;
                        commandParam.TypeName = param.TypeName;
                        commandParam.UdtName = param.UdtTypeName;
                        commandParam.Value = param.Value;
                        parameters.Add(pair.Key, commandParam);
                    }
                }

                hint = new SqlYukonCacheDependency(sqlDependency.ConnectionString, sqlDependency.CommandText, sqlDependency.CommandType, parameters);
                aggregateHint.Add(hint);
            }


            else if (cacheDependency is Runtime.Dependencies.ExtensibleDependency)
            {
                hint = new ExtensibleDependency(cacheDependency as Runtime.Dependencies.ExtensibleDependency);

                aggregateHint.Add(hint);
            }
            else if (cacheDependency is NosDBDependency)
            {
                NosDBDependency nosDbDependency = cacheDependency as NosDBDependency;
                hint = new NosDBCacheDependency(nosDbDependency.ConnectionString, nosDbDependency.CommandText, nosDbDependency.Timeout, nosDbDependency.Parameters);
                aggregateHint.Add(hint);
            }
            else
            {
                foreach (CacheDependency dep in cacheDependency.Dependencies)
                {
                    hint = GetExpirationHint(dep, aggregateHint);
                }

            }
            IList<ExpirationHint> hints = aggregateHint.Hints;
            if (hints.Count == 0)
                return null;

            if (hints.Count == 1)
                return hints[0];

            return aggregateHint;
        }
    }
}
