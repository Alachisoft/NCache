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
using System.Collections.Generic;

namespace Alachisoft.NCache.Common
{
    public class TopicConstant
    {
        public const string DeliveryOption = "DeliveryOption";
        public const string NotifyOption = "NotifyFailure";
        public const string ExpirationTime = "ExpirationTimeValue";
        public const string TopicTag = "CHA$#+%*$>";
        public const string TopicName = "CHA$#+%*$>";
        public const char TopicSeperator = '>';
        //Event Topics
      //  public const string GeneralEventsTopic = "$GeneralEvents$";
        public const string ItemLevelEventsTopic = "$ItemLevelEvents$";
        //public const string CQEventsTopic = "$ContinuousQueryEvents$";
        public const string CollectionEventsTopic = "$CollectionEvents$";

        //We must add any new event related topic defined here in this global list
        public static List<string> AllEventTopics = new List<string>(){ ItemLevelEventsTopic,CollectionEventsTopic};
    }
}