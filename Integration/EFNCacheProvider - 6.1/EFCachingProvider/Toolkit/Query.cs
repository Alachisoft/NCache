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
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Globalization;

namespace Alachisoft.NCache.Integrations.EntityFramework.Caching.Toolkit
{
   public class Query
    {
        private static Dictionary<string, Query> s_cachedQueries = new Dictionary<string,Query>();

        private string _queryText;
        private string _commandText;
        private List<string> _parameters = new List<string>();
        private List<string> _cacheableParameters = new List<string>();
        private const string BASE_PARAMETER = "param_";
        private const string PARAMETER_REPLACEMENT = "@";

        public static Query CreateQuery(DbCommand command,bool isStoredProcedure)
        {
            lock (s_cachedQueries)
            {
                if (s_cachedQueries.ContainsKey(command.CommandText))
                    return s_cachedQueries[command.CommandText];
            }

            Query query = new Query();
            int paramIndex = 0;
            string commandText = command.CommandText;
            List<string> parameterList = new List<string>();
            bool isFirst = true;
            

            if (commandText == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder(commandText.StripTabsAndNewlines());

            if (isStoredProcedure)
                sb.Append("(");

            
            foreach (DbParameter parameter in command.Parameters)
            {
                if (parameter.Direction != ParameterDirection.Input)
                {
                    // we only cache quries with input parameters
                    return null;
                }
                string parameterName = BASE_PARAMETER + paramIndex;
                paramIndex++;
                parameterList.Add(parameterName);

                if (isStoredProcedure)
                {
                    if (isFirst)
                    {
                        sb.Append(PARAMETER_REPLACEMENT + parameterName);
                        isFirst = false;
                    }
                    else
                        sb.Append("," + PARAMETER_REPLACEMENT + parameterName);
                }
                else
                    sb = sb.Replace("@" + parameter.ParameterName, PARAMETER_REPLACEMENT+parameterName);
            }

            if (isStoredProcedure)
                sb.Append(")");

            query._queryText = sb.ToString();
            query._parameters = parameterList;
            query._commandText = commandText;

            lock (s_cachedQueries)
            {
                if (!s_cachedQueries.ContainsKey(command.CommandText))
                    s_cachedQueries.Add(command.CommandText,query) ;
            }

            return query;
        }

        private Query()
        {
        }

        public string QueryText
        {
            get { return _queryText; }
        }

        public string[] Parameters
        {
            get { return _parameters.ToArray(); }
        }

        public string CommandText 
        {
            get { return _commandText; }
        }

        //protected string GetCacheKey()
        //{
        //    if (this.CommandText == null)
        //    {
        //        return null;
        //    }

        //    StringBuilder sb = new StringBuilder(this.CommandText.StripTabsAndNewlines());

        //    string cmdString = WrappedCommand.ToString();

        //    foreach (DbParameter parameter in Parameters)
        //    {
        //        if (parameter.Direction != ParameterDirection.Input)
        //        {
        //            // we only cache quries with input parameters
        //            return null;
        //        }
        //        sb = sb.Replace("@" + parameter.ParameterName, GetLiteralValue(parameter.Value));
        //    }

        //    return sb.ToString();
        //}


        public string GetCacheKey(List<string> cacheableParameters, DbParameterCollection parameters)
        {
            string cacheKey = _queryText;

            if (cacheableParameters != null && parameters != null)
            {
                if (cacheableParameters.Count != 0 && parameters.Count != 0)
                {
                    for (int i = 0; i < _parameters.Count; i++)
                    {
                        string parameter = _parameters[i];

                        if (cacheableParameters.Contains(parameter))
                        {
                            string value = GetLiteralValue(parameters[i].Value);
                            cacheKey = cacheKey.Replace(parameter, value);
                        }
                    }
                }
            }

            return cacheKey;
        }

        private static string GetLiteralValue(object value)
        {
            if (value is string)
            {
                return "'" + value.ToString().Replace("'", "''") + "'";
            }
            else
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        internal string GetParameterList()
        {
            if(_parameters!=null && _parameters.Count>0)
            {
                string parmList = "";
                for (int i = 0; i < _parameters.Count; i++)
                {
                    parmList += _parameters[i] + ",";
                }
                parmList = parmList.Remove(parmList.Length - 1);
                
                return parmList;
            }
            return "";
        }
    }
}
