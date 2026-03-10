using Alachisoft.NCache.Runtime.JSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Alachisoft.NCache.Serialization.JSON
{
    public class SearchableAttributesContractResolver<T> : Newtonsoft.Json.Serialization.DefaultContractResolver
                                        where T : Attribute
    {
        Type _AttributeToIgnore = null;

        public SearchableAttributesContractResolver()
        {
            _AttributeToIgnore = typeof(T);
        }
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            // list the properties to ignore
            var objProperties =type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var toIgnoreFields = objProperties
                    .Where(x => x.GetCustomAttributes(true).All(a=>a.GetType()!=typeof(T))).ToList();


            // Build the properties list
            var properties = base.CreateProperties(type, memberSerialization);

            if(toIgnoreFields.Count== objProperties.Length)
            {
                return properties;
            }
            // only serialize properties that are not ignored
            properties = properties
                .Where(p => toIgnoreFields.All(info => info.Name != p.UnderlyingName))
                .ToList();

            return properties;
        }

    }
    
}
