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
using System.Text;

using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Caching.AutoExpiration;
using System.Data;
using Runtime = Alachisoft.NCache.Runtime;
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

            // putting back null values
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
                    else  // we need to send null values too as a special placeholder
                    {
                        value = "NCNULL";
                    }
                    queryInfo.attributes.Add(value);
                }
            }
            return queryInfo;
        }
        

        public static ExpirationHint GetExpirationHintObj(long absoluteExpiration, long slidingExpiration, string serializationContext)
        {
            ExpirationHint hint = null;
            //We expect Web.Cache to send in UTC DateTime, AbsoluteExpiration is dealt in UTC
            if (absoluteExpiration != 0 && absoluteExpiration != DateTime.MaxValue.ToUniversalTime().Ticks) hint = new FixedExpiration(new DateTime(absoluteExpiration, DateTimeKind.Utc));
            if (slidingExpiration != 0) hint = new IdleExpiration(new TimeSpan(slidingExpiration));


            return hint;
        }
    }

}
