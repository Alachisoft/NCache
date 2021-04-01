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
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring.APILogging
{
    [Serializable]
    public class APIRuntimeLogItem
    {
        private string _clientId = null;
        private string _clientNode = null;
        private TimeSpan _executionTime;
        private Hashtable _parameters;
        private string _exceptionMessage = null;
        private DateTime _generatedTime;
        private string _instanceId;
        private int _methodOverload;
        private string _methodName;
        private string _class;

        public string ClientID
        {
            get { return _clientId; }
            set { _clientId = value; }
        }

        public string ClientNode
        {
            get { return _clientNode; }
            set { _clientNode = value; }
        }

        public TimeSpan ExecutionTime
        {
            get { return _executionTime; }
            set { _executionTime = value; }
        }

        public Hashtable RuntimeParameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }
        public APIRuntimeLogItem(string exceptionMessage)
        {
            this.ExceptionMessage = exceptionMessage;
        }

        public DateTime GeneratedTime
        {
            get { return _generatedTime; }
            set { _generatedTime = value; }
        }

        public string InstanceID
        {
            get { return _instanceId; }
            set { _instanceId = value; }
        }

        public string MethodName
        {
            get { return _methodName; }
            set { _methodName = value; }
        }

        public int MethodOverload
        {
            get { return _methodOverload; }
            set { _methodOverload = value; }
        }

        public string ExceptionMessage
        {
            get { return _exceptionMessage; }
            set
            {
                _exceptionMessage = value;
            }
        }

        public string Class
        {
            get { return _class; }
            set { _class = value; }
        }
    }
}
