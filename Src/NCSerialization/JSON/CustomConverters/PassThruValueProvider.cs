using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Serialization.JSON
{
    public class PassThruValueProvider : IValueProvider
    {
        public object GetValue(object target)
        {
            return target;
        }

        public void SetValue(object target, object value)
        {
            return;
        }
    }
}

