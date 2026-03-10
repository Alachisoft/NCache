using Alachisoft.NCache.Common.Monitoring;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common.Caching.Statistics.CustomCounters
{
    public class CounterCreationData
    {
        private CounterType counterType = CounterType.NumberOfItemCounter;
        private string counterName = String.Empty;
        private string counterHelp = String.Empty;

        private bool _isBaseCounter;

        public bool IsBaseCounter
        {
            get { return _isBaseCounter; }
            set { _isBaseCounter = value; }
        }



        public CounterCreationData(string counterName, string counterHelp, CounterType counterType, bool isBaseCounter = false)
        {
            CounterType = counterType;
            CounterName = counterName;
            CounterHelp = counterHelp;
            _isBaseCounter = isBaseCounter;
        }

        public CounterType CounterType
        {
            get
            {
                return counterType;
            }
            set
            {                
                counterType = value;
            }
        }

        public string CounterName
        {
            get
            {
                return counterName;
            }
            set
            {
                counterName = value;
            }
        }

        public string CounterHelp
        {
            get
            {
                return counterHelp;
            }
            set
            {
                counterHelp = value;
            }
        }
    }
}
