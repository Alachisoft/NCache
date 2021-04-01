using System;

namespace Alachisoft.NCache.Runtime.JSON
{
    internal static class JsonUtil
    {
        public static T GetValueAs<T>(object value)
        {
            if (value == null)
                return (T)value;

            var valueType = value.GetType();

            if (typeof(T).IsAssignableFrom(valueType))
                return (T)value;

            return (T)Convert.ChangeType(value, typeof(T));

            // throw new InvalidCastException($"Value of type '{valueType.FullName}' cannot be casted to '{typeof(T).FullName}'.");
        }
    }
}
