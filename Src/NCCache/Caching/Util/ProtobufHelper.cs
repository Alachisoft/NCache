//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
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
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Caching.Util
{
    public sealed class ProtobufHelper
    {
      
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

        public static ExpirationHint GetExpirationHintObj(PoolManager poolManager, Alachisoft.NCache.Common.Protobuf.Dependency dependency, long absoluteExpiration, long slidingExpiration, bool resyncOnExpiration, string serializationContext)
        {
            ExpirationHint hint = null;
            //We expect Web.Cache to send in UTC DateTime, AbsoluteExpiration is dealt in UTC
            if (absoluteExpiration != Common.Util.Time.MaxUniversalTicks &&  absoluteExpiration != 0 && absoluteExpiration != 1 && absoluteExpiration != 2) 
                hint = FixedExpiration.Create(poolManager, new DateTime(absoluteExpiration, DateTimeKind.Utc));

            if (slidingExpiration != 0 && slidingExpiration != 1 && slidingExpiration != 2) 
                hint = IdleExpiration.Create (poolManager, new TimeSpan(slidingExpiration));

            ExpirationHint depHint = GetExpirationHintObj(poolManager, dependency, false, serializationContext);

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
                        hint = AggregateExpirationHint.Create(poolManager, hint, depHint);
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

        
        public static ExpirationHint GetExpirationHintObj(PoolManager poolManager, Alachisoft.NCache.Common.Protobuf.Dependency dependency, bool resyncOnExpiration, string serializationContext)
        {
            AggregateExpirationHint hints = AggregateExpirationHint.Create(poolManager);
            
            // POTeam
            int hintCount = hints.HintsWithoutClone.Count;
            if (hintCount == 0)
            {
                NCache.Util.MiscUtil.ReturnExpirationHintToPool(hints, poolManager);
                return null;
            }

            if (resyncOnExpiration) hints.SetBit(ExpirationHint.NEEDS_RESYNC);

            if (hintCount == 1)
            {
                return hints.Hints[0];
            }

            return hints;
        }
    }
}
