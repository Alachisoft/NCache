using Alachisoft.NCache.Config.Dom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Caching.CacheHealthAlerts
{
    public class AlertCollectorsBase
    {

        ResourceAtribute resouerceAttribute;
        CacheRuntimeContext context;
        int eventID = 0;
        string counterName;
        string name;
        double lastValue = -1;

        internal AlertCollectorsBase(ResourceAtribute attribute, CacheRuntimeContext cacheRuntimeContext)
        {
            resouerceAttribute = attribute;
            context = cacheRuntimeContext;
        }

        internal CacheRuntimeContext Context
        {
            get
            {
                return context;
            }
        }

        public string CouneterName
        {
          internal  set
            {
               counterName= value;
            }
            get
            {
                return counterName;
            }
        }

        public string Name
        {
            internal set
            {
                name = value;
            }
            get
            {
                return name;
            }
        }


        public double LastValue
        {
            internal set
            {
                lastValue = value;
            }
            get
            {
                return lastValue;
            }
        }

        


        public long MaxThreshold
        {
            set
            {
                resouerceAttribute.MaxThreshold = value;
            }
            get
            {
                return resouerceAttribute.MaxThreshold;
            }
        }

        public long MinThreshold
        {
            set
            {
                resouerceAttribute.MinThreshold = value;
            }
            get
            {
                return resouerceAttribute.MinThreshold;
            }
        }

        public int Duration
        {
            get
            {
                if (resouerceAttribute.Duration < 5) // minimum value for duration is 5;
                    return 5;
                if (resouerceAttribute.Duration > 15) // maximum value for duration is 5;
                    return 15;
                else
                    return resouerceAttribute.Duration;
            }
        }

        public virtual int EventId
        {
            get
            {
                return eventID;
            }
        }

        public virtual double Collectstats()
        {
            return 0.0;
        }

        internal string GetProcessInstancename()
        {
            int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
            PerformanceCounterCategory cat = new PerformanceCounterCategory("Process");
            string[] instances = cat.GetInstanceNames();
            try
            {
                foreach (string instance in instances)
                {
                    if (!instance.ToLower().StartsWith("alachisoft.ncache.cachehost"))
                        continue;

                    using (PerformanceCounter cnt = new PerformanceCounter("Process",
                         "ID Process", instance, true))
                    {
                        int val = (int)cnt.RawValue;
                        if (val == pid)
                        {
                            return instance;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
            return null;
        }

  
        public virtual bool LogLowEvent (double data)
        {
            return true;
        }

        public virtual bool LogHighEvent()
        {
            return true;
        }

        public virtual void SetThresholds ()
        {
            if (MinThreshold < 0)
                MinThreshold = 0;

            if (MaxThreshold == MinThreshold && MinThreshold == 0)
            {
                MaxThreshold = 50;
            }
            else if (MaxThreshold <= MinThreshold)
            {
                MaxThreshold = MinThreshold + MinThreshold / 2;
            }
        }

    }
}
