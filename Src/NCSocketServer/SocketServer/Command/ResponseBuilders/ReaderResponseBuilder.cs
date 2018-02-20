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
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Caching;
using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.SocketServer.Command.ResponseBuilders
{
    class ReaderResponseBuilder : ResponseBuilderBase
    {
        private static Alachisoft.NCache.Caching.Cache _cache = null;

        internal static Alachisoft.NCache.Caching.Cache Cache
        {
            get
            {
                return _cache;
            }
            set
            {
                _cache = value;
            }
        }

        public static IList BuildExecuteReaderResponse(ClusteredList<Alachisoft.NCache.Common.DataReader.ReaderResultSet> resultSetList, int commandVersion, string RequestId, IList _serializedResponse, int commandId, Boolean isOldClient, out int resultCount)
        {
            resultCount = 0;
            if (resultSetList == null)
                return null;
            long requestId = Convert.ToInt64(RequestId);
            int sequenceId = 1;
            Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
            response.requestId = requestId;
            response.commandID = commandId;
            response.responseType = Common.Protobuf.Response.Type.EXECUTE_READER;

            ClusteredArrayList responseChunks = new ClusteredArrayList();

            foreach (Alachisoft.NCache.Common.DataReader.ReaderResultSet resultSet in resultSetList)
            {
                if (resultSet != null)
                {
                    responseChunks.AddRange(ToProtobufReaderResultSet(resultSet, isOldClient));
                    if (resultSet.RecordSet != null)
                    {
                        resultCount += resultSet.RecordSet.RowCount;
                    }
                }
            }

            if (responseChunks != null && responseChunks.Count > 0)
            {
                foreach (Alachisoft.NCache.Common.Protobuf.ReaderResultSet readerResult in responseChunks)
                {
                    response.sequenceId = sequenceId++;
                    response.numberOfChuncks = responseChunks.Count;
                    Alachisoft.NCache.Common.Protobuf.ExecuteReaderResponse executeReaderResponse = new Common.Protobuf.ExecuteReaderResponse();
                    executeReaderResponse.readerResultSets.Add(readerResult);
                    response.executeReaderResponse = executeReaderResponse;
                    _serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            else
            {
                Alachisoft.NCache.Common.Protobuf.ExecuteReaderResponse executeReaderResponse = new Common.Protobuf.ExecuteReaderResponse();
                response.executeReaderResponse = executeReaderResponse;
                _serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            return _serializedResponse;
        }

        public static IList BuildExecuteReaderCQResponse(List<Alachisoft.NCache.Common.DataReader.ReaderResultSet> resultSetList, int commandVersion, string RequestId, IList _serializedResponse, int commandId, Boolean isOldClient, out int resultCount)
        {
            resultCount = 0;
            if (resultSetList == null)
                return null;
            long requestId = Convert.ToInt64(RequestId);

            Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
            response.requestId = requestId;
            response.commandID = commandId;
            response.responseType = Common.Protobuf.Response.Type.EXECUTE_READER_CQ;

            ClusteredArrayList responseChunks = new ClusteredArrayList();

            foreach (Alachisoft.NCache.Common.DataReader.ReaderResultSet resultSet in resultSetList)
            {
                if (resultSet != null)
                {
                    responseChunks.AddRange(ToProtobufReaderResultSet(resultSet, isOldClient));
                    if (resultSet.RecordSet != null)
                    {
                        resultCount += resultSet.RecordSet.RowCount;
                    }
                }

            }

            int sequenceId = 1;
            if (responseChunks != null && responseChunks.Count > 0)
            {
                foreach (Alachisoft.NCache.Common.Protobuf.ReaderResultSet readerResult in responseChunks)
                {
                    response.sequenceId = sequenceId++;
                    response.numberOfChuncks = responseChunks.Count;
                    Alachisoft.NCache.Common.Protobuf.ExecuteReaderCQResponse executeReaderCQResponse = new Common.Protobuf.ExecuteReaderCQResponse();
                    executeReaderCQResponse.readerResultSets.Add(readerResult);
                    response.executeReaderCQResponse = executeReaderCQResponse;
                    _serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            else
            {
                Alachisoft.NCache.Common.Protobuf.ExecuteReaderCQResponse executeReaderCQResponse = new Common.Protobuf.ExecuteReaderCQResponse();
                response.executeReaderCQResponse = executeReaderCQResponse;
                _serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));

            }

            return _serializedResponse;
        }

        public static IList BuildReaderChunkResponse(ReaderResultSet resultSet, string RequestId, IList _serializedResponse, int commandId, Boolean isOldClient, out int count)
        {
            count = 0;
            long requestId = Convert.ToInt64(RequestId);
            Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
            response.requestId = requestId;
            response.commandID = commandId;
            response.responseType = Common.Protobuf.Response.Type.GET_READER_CHUNK;

            Alachisoft.NCache.Common.Protobuf.GetReaderChunkResponse getReaderChunkResponse =
                new Common.Protobuf.GetReaderChunkResponse();
            if (resultSet != null && resultSet.RecordSet != null)
            {
                count = resultSet.RecordSet.RowCount;
            }

            IList chunkedResponses = ToProtobufReaderResultSet(resultSet, isOldClient);

            if (chunkedResponses != null && chunkedResponses.Count > 0)
            {
                int sequence = 1;

                foreach (Alachisoft.NCache.Common.Protobuf.ReaderResultSet readerResult in chunkedResponses)
                {
                    response.sequenceId = sequence++;
                    response.numberOfChuncks = chunkedResponses.Count;
                    getReaderChunkResponse.readerResultSets = readerResult;
                    response.getReaderChunkResponse = getReaderChunkResponse;
                    _serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            else
            {
                response.getReaderChunkResponse = getReaderChunkResponse;
                _serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            return _serializedResponse;
        }

        private static string GetIP(string address)
        {
            string ip = address;
            string[] hostPort = address.Split(':');

            if (hostPort.Length > 0)
            {
                ip = hostPort[0];
            }

            return ip;
        }

        private static IList ToProtobufReaderResultSet(Common.DataReader.ReaderResultSet resultSet, Boolean isOldClient)
        {
            if (resultSet == null)
                return null;
            IList resultSetList = new ClusteredArrayList();

            if (isOldClient && !string.IsNullOrEmpty(resultSet.NodeAddress))
            {
                resultSet.NodeAddress = GetIP(resultSet.NodeAddress);
            }

            Common.Protobuf.ReaderResultSet resultSetProto = new Common.Protobuf.ReaderResultSet();
            resultSetProto.readerId = resultSet.ReaderID;
            resultSetProto.isGrouped = resultSet.IsGrouped;
            resultSetProto.nodeAddress = resultSet.NodeAddress;
            resultSetProto.nextIndex = resultSet.NextIndex;

            if (resultSet.OrderByArguments != null)
            {
                foreach (Common.Queries.OrderByArgument oba in resultSet.OrderByArguments)
                {
                    Common.Protobuf.OrderByArgument arg = new Common.Protobuf.OrderByArgument();
                    arg.attributeName = oba.AttributeName;
                    arg.order = (Common.Protobuf.Order)Convert.ToInt32(oba.Order);
                    resultSetProto.orderByArguments.Add(arg);
                }

            }
            Alachisoft.NCache.Common.Protobuf.RecordSet responseRecordSet = null;
            if (resultSet.RecordSet != null)
            {
                responseRecordSet = new Common.Protobuf.RecordSet();
                Alachisoft.NCache.Common.DataReader.IRecordSet recordSet = resultSet.RecordSet;

                Common.DataReader.ColumnCollection columns = recordSet.GetColumnMetaData();

                for (int i = 0; i < columns.Count; i++)
                {
                    if (columns[i].IsFilled)
                    {
                        Common.Protobuf.RecordColumn responseColumn = new Common.Protobuf.RecordColumn();
                        responseColumn.aggregateFunctionType = (Common.Protobuf.AggregateFunctionType)Convert.ToInt32(columns[i].AggregateFunctionType);
                        responseColumn.columnType = (Common.Protobuf.ColumnType)Convert.ToInt32(columns[i].ColumnType);
                        responseColumn.dataType = (Common.Protobuf.ColumnDataType)Convert.ToInt32(columns[i].DataType);
                        responseColumn.isFilled = columns[i].IsFilled;
                        responseColumn.isHidden = columns[i].IsHidden;
                        responseColumn.name = columns[i].ColumnName;
                        responseRecordSet.columns.Add(responseColumn);
                    }
                }

                int chunkSize = 0;

                for (int i = 0; i < recordSet.RowCount; i++)
                {
                    Common.DataReader.RecordRow row = recordSet.GetRow(i);
                    Common.Protobuf.RecordRow responseRow = new Common.Protobuf.RecordRow();

                    for (int j = 0; j < row.Columns.Count; j++)
                    {
                        if (!row.Columns[j].IsFilled)
                            continue;

                        Common.Protobuf.RecordSetValue rsValue = null;
                        if (row[j] != null)
                        {
                            rsValue = new Common.Protobuf.RecordSetValue();
                            switch (row.Columns[j].DataType)
                            {
                                case Common.Enum.ColumnDataType.AverageResult:
                                    Alachisoft.NCache.Common.Queries.AverageResult avgResult = (Alachisoft.NCache.Common.Queries.AverageResult)row[j];
                                    Common.Protobuf.AverageResult ar = new Common.Protobuf.AverageResult();
                                    ar.sum = avgResult.Sum.ToString();
                                    ar.count = avgResult.Count.ToString();
                                    rsValue.avgResult = ar;

                                    chunkSize += ar.sum.Length + ar.count.Length;

                                    break;
                                case Common.Enum.ColumnDataType.CompressedValueEntry:
                                    Alachisoft.NCache.Common.Protobuf.Value value = new Alachisoft.NCache.Common.Protobuf.Value();
                                    if (row[j] != null)
                                    {
                                        object actualCachedItem = ((CompressedValueEntry)row[j]).Value;
                                        Common.BitSet flag = ((CompressedValueEntry)row[j]).Flag;

                                        UserBinaryObject ubObject = null;
                                        if (actualCachedItem is UserBinaryObject)
                                            ubObject = (UserBinaryObject)actualCachedItem;
                                        else
                                            ubObject = (UserBinaryObject)Cache.SocketServerDataService.GetClientData(actualCachedItem, ref flag, Common.Util.LanguageContext.DOTNET);

                                        chunkSize += ubObject.Size;
                                        value.data.AddRange(((UserBinaryObject)ubObject).DataList);
                                        rsValue.binaryObject = value;
                                        rsValue.flag = flag.Data;
                                    }
                                    break;
                                default:
                                    rsValue.stringValue = Common.DataReader.RecordSet.GetString(row[j], row.Columns[j].DataType);
                                    chunkSize += rsValue.stringValue != null ? rsValue.stringValue.Length : 0;
                                    break;
                            }
                        }
                        responseRow.values.Add(rsValue);
                    }
                    // Also some logic to check if chunk is greater than threshold size
                    // Right now, it is going to make chunks for each row.
                    responseRecordSet.rows.Add(responseRow);
                    resultSetProto.recordSet = responseRecordSet;

                    if (chunkSize > Alachisoft.NCache.Common.Util.ServiceConfiguration.ResponseDataSize || i == (recordSet.RowCount - 1))
                    {
                        resultSetList.Add(resultSetProto);
                        chunkSize = 0;

                        if (i < (recordSet.RowCount - 1))
                        {
                            resultSetProto = new Common.Protobuf.ReaderResultSet();
                            responseRecordSet = new Common.Protobuf.RecordSet();
                            resultSetProto.readerId = resultSet.ReaderID;
                            resultSetProto.nodeAddress = resultSet.NodeAddress;
                        }
                    }
                }
            }
            return resultSetList;
        }
    }
}
