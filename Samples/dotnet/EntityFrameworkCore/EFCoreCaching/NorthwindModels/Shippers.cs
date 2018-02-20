using System;
using System.Collections.Generic;

namespace Alachisoft.NCache.Samples.NorthwindModels
{
    [Serializable]
    public partial class Shippers
    {
        public Shippers()
        {
            Orders = new HashSet<Orders>();
        }

        public int ShipperId { get; set; }
        public string CompanyName { get; set; }
        public string Phone { get; set; }

        public virtual ICollection<Orders> Orders { get; set; }
    }
}
