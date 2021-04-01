using Alachisoft.NCache.Common.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Alachisoft.NCache.Common
{
    public class SeralizationHelper : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            Type typeToDeserialize = null;

            if(typeName == "Alachisoft.NCache.Caching.SmallUserBinaryObject")
            {
                return typeof(SmallUserBinaryObject);
            }

            if (typeName == "Alachisoft.NCache.Caching.LargeUserBinaryObject")
            {
                return typeof(LargeUserBinaryObject);
            }

            if(assemblyName.Contains("Alachisoft"))
            {
                var split = assemblyName.Split(new char[] { ' ' });
                var currentSplit = Assembly.GetExecutingAssembly().FullName.Split(new char[] { ' ' });
                split[1] = currentSplit[1];
                assemblyName = string.Concat(split);
            }

            typeToDeserialize =  Type.GetType(string.Format("{0}, {1}", typeName, assemblyName));

            return typeToDeserialize; 

        }
    }
}
