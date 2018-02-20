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
using System.Collections;
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Common.DataStructures.Clustered;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered;
#endif
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Caching.DataReader
{
    internal class ReaderResultSetManager
    {
        Dictionary<string, HashVector> _readersList = new Dictionary<string, HashVector>(StringComparer.InvariantCultureIgnoreCase);
        Dictionary<string, ReaderResultSet> _readers = new Dictionary<string, ReaderResultSet>(StringComparer.InvariantCultureIgnoreCase);
        CacheRuntimeContext _context = null;
        object _syncRoot = new object();
        int _utcReaderExpiry = -1;
        long _readerExpiry = -1;
        long _initialreaderExpirySecond = 0;
        const int ConvertValue= 60000;

        public ReaderResultSetManager(CacheRuntimeContext context)
        {
            _context = context;
        }
        
        public string RegisterReader(string clientId, ReaderResultSet resultset)
        {
            string readerId = Guid.NewGuid().ToString();
            HashVector recordsets = new HashVector(StringComparer.InvariantCultureIgnoreCase);
            resultset.ClientID = clientId;
            resultset.LastAccessTime = DateTime.Now;
            lock (_syncRoot)
            {
                if (!string.IsNullOrEmpty(clientId))
                {
                    if (!_readersList.ContainsKey(clientId))
                        recordsets[readerId] = resultset;
                    else
                    {
                        recordsets = _readersList[clientId];
                        recordsets[readerId] = null;
                    }
                    _readersList[clientId] = recordsets;

                }
                if (!_readers.ContainsKey(readerId) && _context != null && _context.PerfStatsColl != null)
                {
                    _context.PerfStatsColl.IncrementRunningReaders();
                }
                _readers[readerId] = resultset;
            }

            return readerId;
        }
        
        public ReaderResultSet GetRecordSet(string readerId, int nextIndex, bool inproc,OperationContext context)
        {
            ReaderResultSet readerChunk = null;
            IRecordSet partialRecordSet = null;
            ReaderResultSet reader = null;
            RecordRow row = null;
            CacheEntry entry = null;
            CompressedValueEntry cmpEntry = null;
            int size = 0;
            try
            {
                if (!string.IsNullOrEmpty(readerId))
                {
                    if (_readers.ContainsKey(readerId))
                        reader = _readers[readerId];
                }
                if (reader != null)
                {
                    if (nextIndex != 0 && reader.RecordSet.SubsetInfo.LastAccessedRowID == nextIndex)
                        reader.RecordSet.RemoveRows(reader.RecordSet.SubsetInfo.StartIndex, nextIndex - reader.RecordSet.SubsetInfo.StartIndex);

                    if (reader.RecordSet.RowCount > 0)
                    {
                        readerChunk = new ReaderResultSet();
                        reader.LastAccessTime = DateTime.Now;
                        partialRecordSet = new RecordSet(reader.RecordSet.GetColumnMetaData());
                        int chunkSize = reader.ChunkSize;
                        reader.RecordSet.SubsetInfo.StartIndex = nextIndex;

                        int nextRowID = nextIndex;
                        while (size <= chunkSize)
                        {
                            row = reader.RecordSet.GetRow(nextRowID++);
                            if (row == null)
                                break;
                            row = row.Clone() as RecordRow;
                            if (reader.GetData && !reader.IsGrouped)
                            {
                                entry = _context.CacheImpl.Get(row.GetColumnValue(QueryKeyWords.KeyColumn), context);
                                row.IsSurrogate = entry != null && entry.IsSurrogate;
                        
                                if (entry != null && !entry.IsSurrogate)
                                {
                                    if (inproc) row.SetColumnValue(QueryKeyWords.ValueColumn, entry.Value);
                                    else
                                    {
                                        cmpEntry = new CompressedValueEntry();
                                        cmpEntry.Value = entry.Value;
                                        if (cmpEntry.Value is CallbackEntry)
                                            cmpEntry.Value = ((CallbackEntry)cmpEntry.Value).Value;

                                        cmpEntry.Flag = ((CacheEntry)entry).Flag;
                                        row.SetColumnValue(QueryKeyWords.ValueColumn, cmpEntry);                             
                                    }

                                    size += entry.Size;
                                }
                                
                                

                                if (entry != null)
                                {
                                    if (entry.IsSurrogate)
                                        size += 1024;

                                    partialRecordSet.AddRow(row);
                                    size += row.GetSize();
                                }

                            }
                            else
                            {
                                partialRecordSet.AddRow(row);
                                size += row.GetSize();                             
                            }
                        }

                        //Value column has been filled if getData is true
                        if (reader.GetData && !reader.IsGrouped)
                        {
                            reader.RecordSet.GetColumnMetaData()[QueryKeyWords.ValueColumn].IsFilled = true;
                        }

                        reader.RecordSet.SubsetInfo.LastAccessedRowID += partialRecordSet.RowCount;
                        readerChunk.RecordSet = partialRecordSet;
                        readerChunk.NextIndex = reader.RecordSet.SubsetInfo.LastAccessedRowID;
                       
                        if (!inproc)
                        {
                            readerChunk.NodeAddress = GetReciepent();
                        }

                        readerChunk.OrderByArguments = reader.OrderByArguments;
                        readerChunk.IsGrouped = reader.IsGrouped;
                        readerChunk.ReaderID = reader.ReaderID;
                    }
                    else
                        DisposeReader(reader.ReaderID);
                }
            
                return readerChunk;
            }
            catch (Exception ex)
            {
               if (ex is InvalidReaderException)
                {
                    DisposeReader(reader.ReaderID);
                }
                throw;
            }
        }

        private String GetReciepent() 
        {
            string ipAddress = String.Empty;
            int port = 0;
            if (_context.Render != null)
                ipAddress = _context.Render.IPAddress.ToString();//server address

#if !CLIENT
            if (_context.IsClusteredImpl)
            {
                ClusterCacheBase clusterCache = ((ClusterCacheBase)_context.CacheImpl);
               
                if (clusterCache.Cluster != null && clusterCache.Cluster.LocalAddress != null)
                {
                    Address localAddress = clusterCache.Cluster.LocalAddress;
                    port = localAddress.Port;

                    if (String.IsNullOrEmpty(ipAddress))
                    {
                       var clusterStat= clusterCache.Statistics as ClusterCacheStatistics;
                        if(clusterStat!=null)
                        {
                         ArrayList nodes = clusterStat.Nodes;
                            if (nodes != null)
                            {
                                foreach (NodeInfo node in nodes)
                                {
                                    if (node.RendererAddress != null && node.Address.IpAddress.Equals(localAddress.IpAddress))
                                    {
                                        ipAddress = node.RendererAddress.IpAddress.ToString();
                                    }
                                }
                            }
                        }                       
                    }
                }
            }
#endif

            return ipAddress + ":" + port;
        }

        public void DisposeReader(string readerId)
        {
            lock (_syncRoot)
            {
                if (_readers.ContainsKey(readerId))
                {
                    int count = _readers.Count;
                    ReaderResultSet resultset = _readers[readerId];
                    _readers.Remove(readerId);
                    if(resultset != null)
                    {
                        if (_readersList.Count > 0 && resultset.ClientID != null)
                        {
                            if (_readersList.ContainsKey(resultset.ClientID))
                            {
                                HashVector clientReaders = _readersList[resultset.ClientID];

                                if (clientReaders != null)
                                    clientReaders.Remove(readerId);
                            }
                        }
                    }
                    if (_context != null && _context.PerfStatsColl != null)
                    {
                        _context.PerfStatsColl.DecrementRunningReaders();
                    }
                }
            }
        }

        internal void DeadClients(ArrayList clients)
        {
            if (clients == null) return;
            lock (_syncRoot)
            {
                foreach (string client in clients)
                {
                    HashVector readers = new HashVector();
                    if (_readersList.ContainsKey(client))
                    {
                        readers = _readersList[client];
                        HashVector tempReaders = new HashVector(readers);
                        IDictionaryEnumerator enu = tempReaders.GetEnumerator();
                        while (enu.MoveNext())
                        {
                            if (_readers.ContainsKey(enu.Key.ToString()))
                            {
                                readers.Remove(enu.Key.ToString());
                                if (_context != null && _context.PerfStatsColl != null)
                                {
                                    _context.PerfStatsColl.DecrementRunningReaders();
                                }
                            }
                            _readersList.Remove(client);
                        }
                    }
                }
            }
        }

        internal void ExpireReader(long interval)
        {
            if (_readers.Count > 0 && ServiceConfiguration.ReaderExpiration > -1)
            {
                if(_utcReaderExpiry  <= -1 )
                    _utcReaderExpiry = AppUtil.DiffMinutes(DateTime.Now.AddMinutes(ServiceConfiguration.ReaderExpiration));
                if (_readerExpiry <= -1)
                    _readerExpiry = ServiceConfiguration.ReaderExpiration * ConvertValue;
               
                _initialreaderExpirySecond += interval;

                if (_initialreaderExpirySecond >= _readerExpiry)
                {
                    List<string> readerList = new List<string>();
                    lock (_syncRoot)
                    {
                        foreach (KeyValuePair<string, ReaderResultSet> entry in _readers)
                        {
                            if (AppUtil.DiffMinutes(entry.Value.LastAccessTime) < _utcReaderExpiry)
                            {
                                readerList.Add(entry.Key);
                            }
                        }
                    }
                    for (int i = 0; i < readerList.Count; i++)
                    {
                        DisposeReader(readerList[i]);
                    }
                    _initialreaderExpirySecond = 0;
                }
            }
        }
    }
}
