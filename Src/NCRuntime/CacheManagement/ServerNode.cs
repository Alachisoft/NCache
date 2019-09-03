//  Copyright (c) 2018 Alachisoft
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
using System.Collections.Generic;
using System.Text;
namespace Alachisoft.NCache.Runtime.CacheManagement
{   
    /// <summary>
    /// ServerNode contains information about a single node in server
    /// </summary>
    [Serializable]
    public class ServerNode
    {
        #region Private Members

        private string _serverIP;
        private int _port;
        private bool _isReplica=false;        
        
        #endregion

        #region Public Properties

        /// <summary>
        ///IP Address of server node
        /// </summary>
        public string ServerIP
        {
            set { _serverIP = value; }
            get { return _serverIP; }                  
        }

        /// <summary>
        /// Port of server node
        /// </summary>
        public int Port 
        {
            set { _port = value; }
            get { return _port; }
        }

        /// <summary>
        /// Is server a replica node.
        /// </summary>
        public Boolean IsReplica
        {
            set { _isReplica = value; }
            get { return _isReplica; }
        }

        #endregion
    }
}
