using Alachisoft.NCache.Serialization.JSON.CustomConverters;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace Alachisoft.NCache.Serialization.JSON
{
    public class NCDataSetAndDataTableContractResolver : DefaultContractResolver
    {
        public NCDataSetAndDataTableContractResolver()
        {
            IgnoreSerializableInterface = true;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.DeclaringType.Equals(typeof(DataSet)) || property.DeclaringType.Equals(typeof(DataTable)))
            {
                if (property.PropertyName == NCDataSetUtil.TABLES || property.PropertyName == NCDataTableUtil.COLUMNS || property.PropertyName == NCDataTableUtil.ROWS)
                {
                    property.Writable = true;
                    property.ValueProvider = new PassThruValueProvider();
                }
            }

            return property;
        }

        protected override JsonContract CreateContract(Type objectType)
        {
            JsonContract contract = base.CreateContract(objectType);

            if (objectType == typeof(DataSet))
            {
                contract.Converter = new NCDataSetConverter();
            }

            if (objectType == typeof(DataTable))
            {
                contract.Converter = new NCDataTableConverter();
            }

            return contract;
        }
    }
}

