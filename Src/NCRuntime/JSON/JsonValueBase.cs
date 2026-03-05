//  Copyright (c) 2026 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License

using System;
using Newtonsoft.Json;
using System.Numerics;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Enum;

namespace Alachisoft.NCache.Runtime.JSON
{
    /// <summary>
    /// abstract class which acts as the base class for all JSON based types in NCache
    /// </summary>
    [Serializable]
    [Obsolete("This API is deprecated and will be removed in a future release. This feature is being retired and will not be continued in future versions.", false)]
    public abstract class JsonValueBase
    {
        #region ---------------------------- Properties ----------------------------

        /// <summary>
        /// Size of the object
        /// </summary>
        protected internal virtual int Size
        {
            get; private set;
        }

        /// <summary>
        /// In memory size of the object
        /// </summary>
        protected internal virtual int InMemorySize
        {
            get; private set;
        }

        /// <summary>
        /// Type of the JSON object
        /// </summary>
        public virtual JsonDataType DataType
        {
            get; private set;
        }

        /// <summary>
        /// Value of the object
        /// </summary>
        public virtual object Value
        {
            get; private set;
        }

        #endregion

        #region --------------------------- Constructors ---------------------------

        /// <summary>
        /// Default constructor
        /// </summary>
        protected JsonValueBase() : this(null, JsonDataType.Null)
        {
        }

        /// <summary>
        /// Overloaded constructor which cass the value to JSONDataType provided
        /// </summary>
        /// <param name="value"></param>
        /// <param name="dataType"></param>
        protected JsonValueBase(object value, JsonDataType dataType)
        {
            Value = value;
            DataType = dataType;

            var size = 0;
            var inMemorySize = 0;

            CalculateSize(value, out size, out inMemorySize);

            Size = size + 9;                            // Size of value + size of two integer properties (8) + size of byte type enum (1)
            InMemorySize = inMemorySize + 9 + 24;       // InMemorySize of value + size of two integer properties (8) 
                                                        // + size of byte type enum (1) + .NET overhead for this class (24)
        }

        #endregion

        #region -------------------------- Helper Methods --------------------------

        private void CalculateSize(object value, out int size, out int inMemorySize)
        {
            size = 0;
            inMemorySize = 0;

            if (value == null)
                return;

            inMemorySize += 24;      // .NET overhead cuz of boxing

            #region ----- Primitive Type Size -----

            if (value.GetType().IsPrimitive || value.GetType() == typeof(DateTime))
            {
                var typeCodeSize = GetTypeSize(
                    Type.GetTypeCode(value.GetType())
                );

                size += typeCodeSize;
                inMemorySize += typeCodeSize;

                return;
            }

            #endregion

            #region --------- String Size ---------

            var possiblyString = value as string;

            if (possiblyString != default(string))
            {
                size += possiblyString.Length * 2;           // String size
                inMemorySize += possiblyString.Length * 2;   // String size (.NET overhead considered already)

                return;
            }

            #endregion
        }

        private int GetTypeSize(TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.SByte:
                    return 1;

                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return 2;

                case TypeCode.Decimal:
                case TypeCode.UInt32:
                case TypeCode.Int32:
                    return 4;

                case TypeCode.Double:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.UInt64:
                case TypeCode.DateTime:
                    return 8;

                default:
                    return 0;
            }
        }

        #endregion

        #region ------------------------ Public Helper APIs ------------------------

        /// <summary>
        /// Parses string representation of json object and returns the parsed object
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static JsonValueBase Parse(string json)
        {
            if (json == null)
                return null;

            var jsonToken = default(JToken);

            try
            {
                jsonToken = JToken.Parse(json);
            }
            catch (JsonReaderException jre)
            {
                throw new ArgumentException("Invalid JSON string provided.", nameof(json), jre);
            }
            return Parse(jsonToken);
        }

        private static JsonValueBase Parse(JToken jsonToken)
        {
            if (jsonToken == null)
                throw new ArgumentNullException(nameof(jsonToken));

            switch (jsonToken.Type)
            {
                case JTokenType.Null:
                    return new JsonNull();

                case JTokenType.Array:
                    return Parse(null, jsonToken as JArray);

                case JTokenType.Object:
                    return Parse(null, jsonToken as JObject);

                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Integer:
                case JTokenType.Boolean:
                    return Parse(jsonToken as JValue);

                case JTokenType.Date:
                    return Parse(jsonToken as JValue);

                case JTokenType.Raw:
                case JTokenType.Uri:
                case JTokenType.Guid:
                case JTokenType.Bytes:
                case JTokenType.Comment:
                case JTokenType.TimeSpan:
                    throw new NotSupportedException($"Token '{jsonToken.Type}' is not supported for conversion.");

                case JTokenType.Undefined:
                    throw new NotSupportedException("Undefined token encountered during conversion.");

                case JTokenType.None:
                case JTokenType.Property:
                case JTokenType.Constructor:
                default:
                    throw new NotSupportedException($"Invalid token '{jsonToken.Type}' encountered during conversion.");
            }
        }

        internal static JsonValue Parse(JValue jsonValue)
        {
            switch (jsonValue.Type)
            {
                case JTokenType.Float:
                    return (double)jsonValue.Value;

                case JTokenType.Boolean:
                    return (bool)jsonValue.Value;

                case JTokenType.String:
                    return (JsonValue)(string)jsonValue.Value;

                case JTokenType.Integer:
                    {
                        var value = jsonValue.Value;

                        if (value is BigInteger bigValue)
                        {
                            if (bigValue.Sign == -1 || bigValue > ulong.MaxValue)
                            {
                                return (decimal)bigValue;
                            }
                            return (ulong)(BigInteger)value;
                        }
                        return (long)value;
                    }

                case JTokenType.Date:
                    {
                        return (DateTime)jsonValue.Value;
                    }
                default:
                    throw new NotSupportedException($"Token '{jsonValue.Type}' is not supported for conversion.");
            }
        }

        internal static JsonObject Parse(JsonObject jsonObject, JObject jObject)
        {
            if (jObject == null)
                throw new ArgumentNullException(nameof(jObject));

            if (jsonObject == null)
                jsonObject = new JsonObject();

            foreach (KeyValuePair<string, JToken> attribute in jObject)
                jsonObject.AddAttribute(attribute.Key, Parse(attribute.Value));

            return jsonObject;
        }

        internal static JsonArray Parse(JsonArray jsonArray, JArray jArray)
        {
            if (jArray == null)
                throw new ArgumentNullException(nameof(jArray));

            if (jsonArray == null)
                jsonArray = new JsonArray();

            foreach (JToken jsonElement in jArray)
                jsonArray.Add(Parse(jsonElement));

            return jsonArray;
        }

        #endregion

        #region ---------------------------- Operators -----------------------------

        public static implicit operator JsonValueBase(bool value)
        {
            return (JsonValue)value;
        }

        public static implicit operator JsonValueBase(byte value)
        {
            return (JsonValue)value;
        }

        public static implicit operator JsonValueBase(sbyte value)
        {
            return (JsonValue)value;
        }

        public static implicit operator JsonValueBase(short value)
        {
            return (JsonValue)value;
        }

        public static implicit operator JsonValueBase(ushort value)
        {
            return (JsonValue)value;
        }

         public static implicit operator JsonValueBase(int value)
        {
            return (JsonValue)value;
        }

        public static implicit operator JsonValueBase(uint value)
        {
            return (JsonValue)value;
        }

        public static implicit operator JsonValueBase(long value)
        {
            return (JsonValue)value;
        }

       public static implicit operator JsonValueBase(ulong value)
        {
            return (JsonValue)value;
        }


        public static implicit operator JsonValueBase(float value)
        {
            return (JsonValue)value;
        }

        public static implicit operator JsonValueBase(double value)
        {
            return (JsonValue)value;
        }

        public static implicit operator JsonValueBase(decimal value)
        {
            return (JsonValue)value;
        }


        public static implicit operator JsonValueBase(DateTime value)
        {
            return (JsonValue)value;
        }

        public static implicit operator JsonValueBase(string value)
        {
            if (value == default(string))
                new JsonNull();

            return (JsonValue)value;
        }


        #endregion
    }
}
