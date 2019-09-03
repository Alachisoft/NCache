using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.CacheHealthAlerts;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Config.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Caching.CacheHealthAlerts
{
    internal class CacheResourcesCollector : AlertCollectorsBase
    {
        int eventID = 0;
        bool queueResource = false;

        internal CacheResourcesCollector(string counterName, string name,int id,ResourceAtribute attribute, CacheRuntimeContext cacheRuntimeContext, bool queueCounter = false) : base(attribute, cacheRuntimeContext)

        {
            CouneterName = counterName;
            Name = name;
            eventID = id;
            queueResource = queueCounter;

            SetThresholds();
        }

        public override int EventId
        {
            get
            {
                return eventID;
            }
        }


        public override double Collectstats()
        {
            try
            {
                if (Context != null && Context.PerfStatsColl != null)
                    return Context.PerfStatsColl.GetCounterValue(CouneterName);
            }
            catch (Exception ex)
            {

            }
            return 0.0;
        }

        public override bool LogLowEvent(double data)
        {
            bool toReturn = true;

            if (queueResource)
            {
                if (data == 0)
                    toReturn = false;
                return toReturn;
            }
            else
                return false; 
        }

        public override void SetThresholds()
        {
            if (queueResource)
            {
                if (MinThreshold < 1000)
                {
                    MinThreshold = 1000;
                }
            }
            else
            {
                if (MinThreshold < 50)
                    MinThreshold = 50;
            }

            base.SetThresholds();
        }
    }
}

