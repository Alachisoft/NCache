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
using System.Threading;

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public class ClientActivity :ICloneable, Runtime.Serialization.ICompactSerializable
    {
        private ArrayList _activities = new ArrayList();
		[CLSCompliant(false)]
        public DateTime _startTime;
		[CLSCompliant(false)]
        public DateTime _endTime;
		[CLSCompliant(false)]
        public Thread _thread;

        public ClientActivity()
        {
            _startTime = DateTime.Now;
        }

        public void LogActivity(string method, string log)
        {
            MethodActivity mActivity = new MethodActivity(method, log);
            lock (this)
            {
                _activities.Add(mActivity);
            }
        }

        public DateTime StartTime
        {
            get { return _startTime; }
        }

        public DateTime EndTime
        {
            get { return _endTime; }
        }

        public ArrayList Activities
        {
            get { return _activities; }
        }
        public void Clear()
        {
            lock (this)
            {
                _activities.Clear();
            }
        }
        
        public void StartActivity()
        {
        }

        public void StopActivity()
        {
            _endTime = DateTime.Now;
        }


        #region ICloneable Members

        public object Clone()
        {
            ClientActivity clone = new ClientActivity();
            lock (this)
            {
                clone._startTime = _startTime;
                clone._endTime = _endTime;
                clone._activities = _activities.Clone() as ArrayList;
            }
            return clone;
        }

        #endregion

        #region  ICompact Serializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _activities = (ArrayList)reader.ReadObject();
            _startTime = reader.ReadDateTime();
            _endTime = reader.ReadDateTime();
            _thread = (Thread)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_activities);
            writer.Write(_startTime);
            writer.Write(_endTime);
            writer.WriteObject(_thread);
        } 
        #endregion
    }
}
