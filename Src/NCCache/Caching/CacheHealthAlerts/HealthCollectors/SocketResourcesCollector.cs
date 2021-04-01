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
    internal class SocketResourcesCollector : AlertCollectorsBase
    {
        int eventID = 0;
        bool clientConnectivity = false;

        internal SocketResourcesCollector(string counterName, string name,int id,ResourceAtribute attribute, CacheRuntimeContext cacheRuntimeContext, bool connectivity = false) : base(attribute, cacheRuntimeContext)

        {
            CouneterName = counterName;
            Name = name;
            eventID = id;
            clientConnectivity = connectivity;
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
                    return Context.Render.GetCounterValue(CouneterName);
            }
            catch
            {

            }
            return 0.0;
        }

        public override bool LogLowEvent(double data)
        {
            bool toReturn = true;

            if (clientConnectivity)
            {
                if (data == LastValue)
                    toReturn = false;

                LastValue = data;

                return toReturn;
            }
            else
                return base.LogLowEvent(data);

        }


        public override bool LogHighEvent()
        {
            if (clientConnectivity)
                return false;
            else
                return true;
        }

        public override void SetThresholds()
        {
            if (!clientConnectivity)
            {
                if (MinThreshold < 1000)
                    MinThreshold = 1000;
            }
            base.SetThresholds();
        }

    }
}

