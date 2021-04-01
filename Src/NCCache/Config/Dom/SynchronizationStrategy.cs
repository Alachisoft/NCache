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
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class SynchronizationStrategy : ICloneable, ICompactSerializable
    {

        private CallbackType _type = CallbackType.PullBasedCallback;
        private int _interval = 10;

        public SynchronizationStrategy()
        {
        }


        // Always polling
        [ConfigurationAttribute("strategy")]
        public string Strategy
        {
            get
            {
                switch (_type)
                {
                    case CallbackType.PullBasedCallback:
                        return "polling";
                    case CallbackType.PushBasedNotification:
                    default:
                        return "polling";
                }
            }
            set
            {
                switch (value)
                {
                    case "polling":
                        _type = CallbackType.PullBasedCallback;
                        break;
                    case "notification":
                    default:
                        _type = CallbackType.PullBasedCallback;
                        break;
                }
            }
        }

        public CallbackType CallbackType 
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        [ConfigurationAttribute("polling-interval", "sec")]
        public int Interval
        {
            get
            {
                return _interval;
            }
            set
            {
                _interval = value;
            }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _type = (CallbackType)reader.ReadInt32();
            _interval = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write((int)_type);
            writer.Write(_interval);
        }

        public object Clone()
        {
            return new SynchronizationStrategy() { _interval = this._interval, _type = this._type };
        }
    }
}
