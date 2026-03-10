using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public class CounterIDMap
    {

        private List<CounterMetadata> counterMetadata = new List<CounterMetadata>();
        private List<string> counterNames = new List<string>();
        private Dictionary<short, string> counterIDMap = new Dictionary<short, string>();


        public List<short> CounterIds { get; set; }
        public string Version { get; set; }
        public string Category { get; set; }
      

        public void AssignAndAddCounters(List<CounterMetadata> counters)
        {
            // sort by name
            // assign ids

            int counterId = 0;
            List<short> ids = new List<short>();

            foreach (var counterMeta in counters)
            {
                if(!counterNames.Contains(counterMeta.Name))
                    counterNames.Add(counterMeta.Name);
            }

            foreach (var name in counterNames)
            {
                if (!counterIDMap.ContainsKey((short)counterId))
                {
                    counterIDMap.Add((short)counterId, name);
                }
                if (!ids.Contains((short)counterId))
                    ids.Add((short)counterId);
                counterId++;
            }
            CounterIds = ids;
        }

        public short GetCounerID(string counter)
        {
            if (counterIDMap != null)
            {
                if (counterIDMap.Values.Contains(counter))
                    return counterIDMap.FirstOrDefault(x => x.Value == counter).Key;
            }
            return -10;
        }

        public string GetCounterName(short id)
        {

            string name = String.Empty;
            if (counterIDMap != null)
            {
                if (counterIDMap.TryGetValue(id, out name))
                    return name;
            }

            return name;

        }


        public IDictionary<string, double> ConvertToNameMap(CounterDataCollection collection)
        {

            Dictionary<string, double> nameMap = new Dictionary<string, double>();
            if(counterIDMap !=null)
            {
                foreach (var val in counterIDMap)
                {
                    nameMap.Add(val.Value, val.Key);
                }

            }

            return nameMap;
        }
        public void Clear()
        {
            counterNames.Clear();
            counterIDMap.Clear();
            CounterIds.Clear();
        }

    }
}
