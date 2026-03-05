using Alachisoft.NCache.Common.DataStructures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    class MonitorableEntitiesDataStore
    {
        SlidingIndex<MutableKeyValuePair<IntervalCounterDataCollection,bool>> _statsIndex;
        SlidingIndex<object> _logIndex;
        SlidingIndex<EventData> _eventsIndex;
        SlidingIndex<ClusterHealthData> _clusterHealthIndex;

        public MonitorableEntitiesDataStore(int interval)
        {
             _statsIndex = new SlidingIndex<MutableKeyValuePair<IntervalCounterDataCollection,bool>>(interval);
             _logIndex = new  SlidingIndex<object>(interval);
             _eventsIndex = new SlidingIndex<EventData>(interval);
             _clusterHealthIndex = new SlidingIndex<ClusterHealthData>(interval);
        }

        public void AddCounterData(IntervalCounterDataCollection CounterDataCollection)
        {
            _statsIndex.AddToIndex(new MutableKeyValuePair<IntervalCounterDataCollection, bool>() { Key =CounterDataCollection, Value= true });
        }

        public List<IntervalCounterDataCollection> GetStatsData(ref long startTime)
        {
            lock(_statsIndex)
            {
                IEnumerator<MutableKeyValuePair<IntervalCounterDataCollection, bool>> en = _statsIndex.GetCurrentData(ref startTime, getReplicatData: false);
                List<IntervalCounterDataCollection> list = new List<IntervalCounterDataCollection>();
                while (en.MoveNext())
                {
                    if (en.Current.Value)
                    {
                        var current = (IntervalCounterDataCollection)en.Current.Key;
                        list.Add(current);
                        en.Current.Value = false;
                    }
                }

                return list;
            }
        }
        
        public void AddToLogIndex (object obj)
        {
            _logIndex.AddToIndex(obj);
            
        }

        public void AddToAPILogIndex(object obj)
        {

        }

        

    }
}
