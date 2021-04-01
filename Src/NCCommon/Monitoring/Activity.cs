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

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public class Activity: Runtime.Serialization.ICompactSerializable
    {
		[CLSCompliant(false)]
        protected DateTime _time;
		[CLSCompliant(false)]
        protected string _log;

        public Activity(string log)
        {
            _time = DateTime.Now;
            _log = log;
        }

        public DateTime Time
        {
            get { return _time; }
        }

        public string Log
        {
            get { return _log; }
            set { _log = value; }
        }


        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _time = reader.ReadDateTime();
            _log = (string)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_time);
            writer.WriteObject(_log);
        }
    }
}
