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
using Alachisoft.NCache.Common.Caching.Statistics.CustomCounters;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Management.Statistics
{
    public class ClientCustomCounters : ICompactSerializable
    {
        public double _pcClientRequestsPerSec;
        /// <summary> performance counter for cache responses per second by the client. </summary>
        public double _pcClientResponsesPerSec;
        /// <summary> performance counter for cache requests per second by all the clients. </summary>
        public double _pcTotalClientRequestsPerSec;
        /// <summary> performance counter for cache responses per second by the all clients. </summary>
        public double _pcTotalClientResponsesPerSec;


        /// <summary> performance counter for Cache get operations per second. </summary>
        public double _pcGetPerSec;
        /// <summary> performance counter for Cache add operations per second. </summary>
        public double _pcAddPerSec;
        /// <summary> performance counter for Cache update operations per second. </summary>
        public double _pcUpdPerSec;
        /// <summary> performance counter for Cache remove operations per second. </summary>
        public double _pcDelPerSec;
        /// <summary> performance counter read operations per second </summary>
        public double _pcReadOperationsPerSec;
        /// <summary> performance counter write operations per second </summary>
        public double _pcWriteOperationsPerSec;
        /// <summary> performance counter for Mirror Queue size. </summary>
        public double _pcReqrQueueSize;

        /// <summary> performance counter for Cache avg. per milli-second time of get operations. </summary>
        public double _pcMsecPerGetAvg;
        /// <summary> performance counter for Cache avg. per milli-second time of add operations. </summary>
        public double _pcMsecPerAddAvg;
        /// <summary> performance counter for Cache avg. per milli-second time of update operations. </summary>
        public double _pcMsecPerUpdAvg;
        /// <summary> performance counter for Cache avg. per milli-second time of remove operations. </summary>
        public double _pcMsecPerDelAvg;
        /// <summary> performance counter for Cache max. per milli-second time of get operations. </summary>
        public double _pcMsecPerGetBase;
        /// <summary> performance counter for Cache max. per milli-second time of add operations. </summary>
        public double _pcMsecPerAddBase;
        /// <summary> performance counter for Cache max. per milli-second time of update operations. </summary>
        public double _pcMsecPerUpdBase;
        /// <summary> performance counter for Cache max. per milli-second time of remove operations. </summary>
        public double _pcMsecPerDelBase;
        /// <summary> performance counter for Cache avg. per milli-second time of Event operations. </summary>
        public double _pcMsecPerEventAvg;
        /// <summary> performance counter for Cache max. per milli-second time of Event operations. </summary>
        public double _pcMsecPerEventBase;
        /// <summary> performance counter for events processed per second. </summary>
        public double _pcEventProcesedPerSec;
        /// <summary> performance counter for events Triggered/Received per second. </summary>
        public double _pcEventTriggeredPerSec;

        //Bulk Counters
        public double _pcMsecPerAddBulkAvg;
        public double _pcMsecPerAddBulkBase;

        public double _pcMsecPerUpdBulkAvg;
        public double _pcMsecPerUpdBulkBase;

        public double _pcMsecPerGetBulkAvg;
        public double _pcMsecPerGetBulkBase;

        public double _pcMsecPerDelBulkAvg;
        public double _pcMsecPerDelBulkBase;



        /// <summary> performance counter for</summary>
        public double _pcAvgItemSize;
        /// <summary> base performance counter for</summary>
        public double _pcAvgItemSizeBase;
        /// <summary> performance counter for</summary>

        public double _pcMsecPerSerializationAvg;
        public double _pcMsecPerSerializationAvgBase;

        public double _pcMsecPerDeserializationAvg;
        public double _pcMsecPerDeserializationAvgBase;

        //Polling counters
        /// <summary>
        /// Count of Poll requests made either after client interval or server notification
        /// </summary>
        public double _pollRequestsSent;
        /// <summary>
        /// Resultant Updates from last poll request
        /// </summary>
        public double _pollLastUpdates;
        /// <summary>
        /// Resultant Removes from last poll request
        /// </summary>
        public double _pollLastRemoves;

        public double _pcMsecPerMessagePublishAvg;
        public double _pcMsecPerMessagePublishBase;
        public double _pcMessagePublishPerSec;
        public double _pcMessageDeliverPerSec;
      
        public ClientCustomCounters()
        {
        }

        public void Deserialize(CompactReader reader)
        {
            _pcAddPerSec = reader.ReadDouble();
            _pcAddPerSec = reader.ReadDouble();
            _pcGetPerSec = reader.ReadDouble();
            _pcUpdPerSec = reader.ReadDouble();
            _pcDelPerSec = reader.ReadDouble();
            _pcEventTriggeredPerSec = reader.ReadDouble();
            _pcEventProcesedPerSec = reader.ReadDouble();
            _pcReadOperationsPerSec = reader.ReadDouble();
            _pcWriteOperationsPerSec = reader.ReadDouble();
            _pcMsecPerAddBulkAvg = reader.ReadDouble();
            _pcMsecPerAddBulkBase = reader.ReadDouble();
            _pcMsecPerGetBulkAvg = reader.ReadDouble();
            _pcMsecPerGetBulkBase = reader.ReadDouble();
            _pcMsecPerUpdBulkAvg = reader.ReadDouble();
            _pcMsecPerUpdBulkBase = reader.ReadDouble();
            _pcMsecPerDelBulkAvg = reader.ReadDouble();
            _pcMsecPerDelBulkBase = reader.ReadDouble();
            _pcMsecPerGetAvg = reader.ReadDouble();
            _pcMsecPerGetBase = reader.ReadDouble();
            _pcMsecPerAddAvg = reader.ReadDouble();
            _pcMsecPerAddBase = reader.ReadDouble();
            _pcMsecPerUpdAvg = reader.ReadDouble();
            _pcMsecPerUpdBase = reader.ReadDouble();
            _pcMsecPerDelAvg = reader.ReadDouble();
            _pcMsecPerDelBase =  reader.ReadDouble();
            _pcReqrQueueSize = reader.ReadDouble();
            _pcAvgItemSize = reader.ReadDouble();
            _pcAvgItemSizeBase = reader.ReadDouble();
            _pcMsecPerEventAvg = reader.ReadDouble();
            _pcMsecPerEventBase = reader.ReadDouble();
            _pcMsecPerSerializationAvg = reader.ReadDouble();
            _pcMsecPerSerializationAvgBase = reader.ReadDouble();
            _pcMsecPerDeserializationAvg = reader.ReadDouble();
            _pcMsecPerDeserializationAvgBase = reader.ReadDouble();
            _pollRequestsSent = reader.ReadDouble();
            _pollLastUpdates = reader.ReadDouble();
            _pollLastRemoves = reader.ReadDouble();
            _pcMsecPerMessagePublishAvg = reader.ReadDouble();
            _pcMsecPerMessagePublishBase = reader.ReadDouble();
            _pcMessagePublishPerSec = reader.ReadDouble();
            _pcMessageDeliverPerSec = reader.ReadDouble();
            _pcClientRequestsPerSec = reader.ReadDouble();
            _pcClientResponsesPerSec = reader.ReadDouble();
            _pcTotalClientRequestsPerSec = reader.ReadDouble();
            _pcTotalClientResponsesPerSec = reader.ReadDouble();

        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_pcAddPerSec);
            writer.Write(_pcAddPerSec);
            writer.Write(_pcGetPerSec);
            writer.Write(_pcUpdPerSec);
            writer.Write(_pcDelPerSec);
            writer.Write(_pcEventTriggeredPerSec);
            writer.Write(_pcEventProcesedPerSec);
            writer.Write(_pcReadOperationsPerSec);
            writer.Write(_pcWriteOperationsPerSec);
            writer.Write(_pcMsecPerAddBulkAvg);
            writer.Write(_pcMsecPerAddBulkBase);
            writer.Write(_pcMsecPerGetBulkAvg);
            writer.Write(_pcMsecPerGetBulkBase);
            writer.Write(_pcMsecPerUpdBulkAvg);
            writer.Write(_pcMsecPerUpdBulkBase);
            writer.Write(_pcMsecPerDelBulkAvg);
            writer.Write(_pcMsecPerDelBulkBase);
            writer.Write(_pcMsecPerGetAvg);
            writer.Write(_pcMsecPerGetBase);
            writer.Write(_pcMsecPerAddAvg);
            writer.Write(_pcMsecPerAddBase);
            writer.Write(_pcMsecPerUpdAvg);
            writer.Write(_pcMsecPerUpdBase);
            writer.Write(_pcMsecPerDelAvg);
            writer.Write(_pcMsecPerDelBase);
            writer.Write(_pcReqrQueueSize);
            writer.Write(_pcAvgItemSize);
            writer.Write(_pcAvgItemSizeBase);
            writer.Write(_pcMsecPerEventAvg);
            writer.Write(_pcMsecPerEventBase);
            writer.Write(_pcMsecPerSerializationAvg);
            writer.Write(_pcMsecPerSerializationAvgBase);
            writer.Write(_pcMsecPerDeserializationAvg);
            writer.Write(_pcMsecPerDeserializationAvgBase);
            writer.Write(_pollRequestsSent);
            writer.Write(_pollLastUpdates);
            writer.Write(_pollLastRemoves);
            writer.Write(_pcMsecPerMessagePublishAvg);
            writer.Write(_pcMsecPerMessagePublishBase);
            writer.Write(_pcMessagePublishPerSec);
            writer.Write(_pcMessageDeliverPerSec);
            writer.Write(_pcClientRequestsPerSec);
            writer.Write(_pcClientResponsesPerSec);
            writer.Write(_pcTotalClientRequestsPerSec);
            writer.Write(_pcTotalClientResponsesPerSec);
        }
    }
}
