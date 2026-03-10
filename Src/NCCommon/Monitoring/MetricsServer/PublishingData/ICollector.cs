using Alachisoft.NCache.Common.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public interface ICollector
    {
        string CouneterName
        {
            set;
            get;
        }

        string Name
        {
            set;
            get;
        }


        double LastValue
        {
            set;
            get;
        }

        int EventId
        {
            get;
        }


        double Collectstats();
       
    }
}
