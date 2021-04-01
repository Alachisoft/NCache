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

namespace Alachisoft.NCache.Client
{
    internal class NodeRequestStatus
    {
        private HashSet<long> requestSet;
        private HashSet<long> acknowledgedRequestSet;
        private long lastRequestId;
        private long lastAcknowledgeId;
        private readonly object syncRoot = new object();

        public NodeRequestStatus()
        {
            requestSet = new HashSet<long>();
            acknowledgedRequestSet = new HashSet<long>();
        }

        public void RegisterRequest(long id)
        {
            if (!requestSet.Contains(id))
                requestSet.Add(id);
            lastRequestId = id;
        }

        public long LastAcknowledged
        {
            get
            {
                if (acknowledgedRequestSet.Count != 0)
                {
                    return GetMinValue(acknowledgedRequestSet);
                }
                return -1;
            }
        }

        public object SyncRoot
        {
            get { return syncRoot; }
        }

        public void Acknowledge(long Id)
        {
            if (requestSet.Contains(Id))
            {
                requestSet.Remove(Id);
                acknowledgedRequestSet.Add(Id);
                lastAcknowledgeId = Id;
            }
        }

        public void Clean()
        {
            long minimum = requestSet.Count != 0 ? GetMinValue(requestSet) : lastRequestId;
            lock (acknowledgedRequestSet)
            {
                long lastAcknowledged = acknowledgedRequestSet.Count != 0
                    ? GetMaxValue(acknowledgedRequestSet)
                    : lastAcknowledgeId;
                long[] ids = GetArray(acknowledgedRequestSet);
                for (int i = 0; i < ids.Length; i++)
                {
                    if (ids[i] < minimum)
                        acknowledgedRequestSet.Remove(ids[i]);
                }
                if (!acknowledgedRequestSet.Contains(lastAcknowledged))
                    acknowledgedRequestSet.Add(lastAcknowledged);
            }
        }

        private long GetMinValue(HashSet<long> collection)
        {
            long minVal = long.MaxValue;
            IEnumerator<long> ie = collection.GetEnumerator();
            while (ie.MoveNext())
            {
                if (minVal > ie.Current)
                    minVal = ie.Current;
            }
            return minVal == long.MaxValue ? -1 : minVal;
        }

        private long GetMaxValue(HashSet<long> collection)
        {
            long maxVal = long.MinValue;
            IEnumerator<long> ie = collection.GetEnumerator();
            while (ie.MoveNext())
            {
                if (maxVal < ie.Current)
                    maxVal = ie.Current;
            }
            return maxVal;
        }

        private long[] GetArray(HashSet<long> collection)
        {
            long[] arrayCollection = new long[acknowledgedRequestSet.Count];
            collection.CopyTo(arrayCollection, 0);
            return arrayCollection;
        }
    }
}
