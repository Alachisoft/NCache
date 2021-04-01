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

namespace Alachisoft.NCache.SocketServer
{
    internal sealed class MappingTable
    {
        private Hashtable _mappingTable;
        private ArrayList _socketList;

        internal MappingTable()
        {
            _mappingTable = Hashtable.Synchronized(new Hashtable(25, 0.75f));
        }

        /// <summary>
        /// Add cacheId in a list against cacheId in table
        /// </summary>
        /// <param name="cacheId">The cacheId</param>
        /// <param name="cacheId">The cacheId to be stored</param>
        internal void Add(string cacheId, string socketId)
        {
            lock (this) // multiple threads can add cacheId to mappingTable.
            {
                if (_mappingTable.Contains(cacheId))
                {
                    _socketList = (ArrayList)_mappingTable[cacheId];
                    _socketList.Add(socketId);

                    _mappingTable[cacheId] = _socketList;
                }
                else
                {
                    _socketList = new ArrayList(10);
                    _socketList.Add(socketId);

                    _mappingTable.Add(cacheId, _socketList);
                }
            }
        }

        /// <summary>
        /// Get arraylist containing the cacheId's.
        /// </summary>
        /// <param name="cacheId">The cacheId</param>
        /// <returns>List of cacheId's</returns>
        internal ArrayList Get(string cacheId)
        {
            return (ArrayList)_mappingTable[cacheId];
        }

        /// <summary>
        /// Removes the cacheId from list, removes cacheId from table if there in no cacheId left
        /// </summary>
        /// <param name="cacheId">The cacheId</param>
        /// <param name="cacheId">The cacheId to be removed</param>
        internal void Remove(string cacheId, string socketId)
        {
            if (_mappingTable == null) return;

            if (_mappingTable.Contains(cacheId))
            {
                _socketList = (ArrayList)_mappingTable[cacheId];
                _socketList.Remove(socketId);

                if (_socketList.Count == 0)
                    _mappingTable.Remove(cacheId);
                else
                    _mappingTable[cacheId] = _socketList;
            }
        }

        /// <summary>
        /// Dispose the table object
        /// </summary>
        internal void Dispose()
        {
            if (_mappingTable == null) return;

            _mappingTable.Clear();
            _mappingTable = null;
        }
    }
}
