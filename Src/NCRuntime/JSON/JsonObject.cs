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
using System.Linq;
using Newtonsoft.Json;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Enum;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Runtime.ErrorHandling;

namespace Alachisoft.NCache.Runtime.JSON
{
    /// <summary>
    /// Class represents JObject in JSON standards
    /// </summary>
    [Serializable]
    [Obsolete("This API is deprecated and will be removed in a future release. This feature is being retired and will not be continued in future versions.", false)]
    public sealed class JsonObject : JsonValueBase, IJsonObject
    {
        #region ------------------------------ Fields ------------------------------

        private readonly IDictionary<string, JsonValueBase> _propertyMap;

        private string type;
        public string Type
        {
            get
            {
                return type;
            }
            
            set 
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(type));
                if (value.Equals(""))
                    throw new ArgumentException("Value cannot be empty", nameof(type));
                type = value;
            }
        }
        #endregion

        #region ---------------------------- Properties ----------------------------

        /// <summary>
        /// iterator over attributes in JSONObject
        /// </summary>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public JsonValueBase this[string attributeName]
        {
            get
            {
                return GetAttributeValue(attributeName);
            }
            set
            {
                if (value == default(JsonValueBase))
                    throw new NotSupportedException("Updating to null value is not supported with indexer.");
                else
                    AddOrUpdateAttribute(attributeName, value);
            }
        }

        /// <summary>
        /// Number of attributes in object
        /// </summary>
        public int Count
        {
            get
            {
                return _propertyMap == null ? 0 : _propertyMap.Count;
            }
        }

        /// <summary>
        /// Returns instance of this object
        /// </summary>
        public override object Value
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Size of the JSONObject
        /// </summary>
        protected internal override int Size
        {
            get
            {
                var size = base.Size;

                if (_propertyMap != default(IDictionary<string, JsonValueBase>))
                    size += (from property in _propertyMap select property).Sum(property =>
                        {
                            return property.Value.Size + GetAttributeNameSize(property.Key);
                        }
                    );

                return size;
            }
        }

        /// <summary>
        /// In memory size of the JSONObject
        /// </summary>
        protected internal override int InMemorySize
        {
            get
            {
                var inMemorySize = base.InMemorySize + 45;  // InMemorySize of base + .NET Dictionary overhead (45)

                if (_propertyMap != default(IDictionary<string, JsonValueBase>))
                    inMemorySize += (from property in _propertyMap select property).Sum(property =>
                        {
                            return property.Value.InMemorySize + GetAttributeNameInMemorySize(property.Key);
                        }
                    );

                return inMemorySize;
            }
        }

        #endregion

        #region --------------------------- Constructors ---------------------------

        /// <summary>
        /// Defafault constructor
        /// </summary>
        public JsonObject() : base(null, JsonDataType.Object)
        {
            _propertyMap = new Dictionary<string, JsonValueBase>();
        }

        /// <summary>
        /// Overloaded constructor which populates attributes by parsing given JSONObject string
        /// </summary>
        /// <param name="json">String representation of JSONObject</param>
        public JsonObject(string json) : this()
        {
            try
            {
                Parse(this, JObject.Parse(json));
            }
            catch (JsonReaderException jre)
            {
                throw new ArgumentException("Invalid JSON string provided.", nameof(json), jre);
            }
        }
        
        /// <summary>
        /// Overloaded constructor which populates attributes by parsing given JSONObject string and Type 
        /// </summary>
        /// <param name="json">String representation of JSONObject</param>
        public JsonObject(string json, string type) : this()
        {
            try
            {
                if (type == null)
                    throw new ArgumentNullException(nameof(type));
                if (type.Equals(""))
                    throw new ArgumentException("Value cannot be empty", nameof(type));
                this.Type = type;

                Parse(this, JObject.Parse(json));
            }
            catch (JsonReaderException jre)
            {
                throw new ArgumentException("Invalid JSON string provided.", nameof(json), jre);
            }
        }

        #endregion

        #region ----------------------------- Behavior -----------------------------

        /// <summary>
        /// Retruns collection of all the attribute names
        /// </summary>
        /// <returns></returns>
        public ICollection<string> GetAttributeNames()
        {
            return _propertyMap == null ? new string[] { } : _propertyMap.Keys;
        }

        /// <summary>
        /// Adds an attribute in the object
        /// </summary>
        /// <param name="attributeName">Name of the attribute</param>
        /// <param name="attributeValue">JSONValue as the attribute value</param>
        public void AddAttribute(string attributeName, JsonValue attributeValue)
        {
            AddAttribute(attributeName, attributeValue as JsonValueBase);
        }

        /// <summary>
        /// Adds an attribute in the object
        /// </summary>
        /// <param name="attributeName">Name of the attribute</param>
        /// <param name="attributeValue">JsonValueBase as the attribute value</param>
        public void AddAttribute(string attributeName, JsonValueBase attributeValue)
        {
            VerifyAttributeName(attributeName);

            if (_propertyMap.ContainsKey(attributeName))
                throw new OperationFailedException(ErrorCodes.Json.ATTRIBUTE_ALREADY_EXISTS, ErrorMessages.GetErrorMessage(ErrorCodes.Json.ATTRIBUTE_ALREADY_EXISTS));

            VerifyAttributeValue(attributeValue);

            _propertyMap.Add(attributeName, attributeValue);
        }

        /// <summary>
        /// Removes attribute from object on the basis of attribute name provided
        /// </summary>
        /// <param name="attributeName"></param>
        /// <returns>True if exists and removed succcessfully</returns>
        public bool RemoveAttribute(string attributeName)
        {
            VerifyAttributeName(attributeName);

            return _propertyMap.Remove(attributeName);
        }

        /// <summary>
        /// Gets attribute value identified by the attribute name
        /// </summary>
        /// <param name="attributeName"></param>
        /// <returns>Null if doesn't exist</returns>
        public JsonValueBase GetAttributeValue(string attributeName)
        {
            VerifyAttributeName(attributeName);

            var attributeValue = default(JsonValueBase);

            if (_propertyMap.TryGetValue(attributeName, out attributeValue))
                return attributeValue;

            return default(JsonValueBase);
        }

        /// <summary>
        /// Checks if the attribute exits 
        /// </summary>
        /// <param name="attributeName"></param>
        /// <returns>True if attribute exits</returns>
        public bool ContainsAttribute(string attributeName)
        {
            VerifyAttributeName(attributeName);
            return _propertyMap.ContainsKey(attributeName);
        }

        /// <summary>
        /// Removes all attributes from JSONObject
        /// </summary>
        public void Clear()
        {
            if (_propertyMap != default(IDictionary<string, JsonValueBase>))
                _propertyMap.Clear();
        }

        #region -------------------- IEnumerator Implementation --------------------

        /// <summary>
        /// Returns and Enumerator that iterates through JSONObject attributes
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, JsonValueBase>> GetEnumerator()
        {
            return _propertyMap?.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #endregion

        #region -------------------------- Helper Methods --------------------------

        private void VerifyAttributeName(string attributeName)
        {
            if (string.IsNullOrEmpty(attributeName))
                throw new ArgumentNullException(nameof(attributeName), "Value cannot be null or empty.");
        }

        private void VerifyAttributeValue(JsonValueBase attributeValue)
        {
            if (attributeValue == default(JsonValueBase))
                throw new ArgumentNullException("attributeValue");

            if (attributeValue == this)
                throw new OperationFailedException(ErrorCodes.Json.REFERENCE_TO_SELF, ErrorMessages.GetErrorMessage(ErrorCodes.Json.REFERENCE_TO_SELF, GetType().Name));

            attributeValue.InvalidateParentReference(new Dictionary<JsonValueBase, byte>()
                {
                    { this, 0}
                }
            );
        }

        private int GetAttributeNameSize(string attributeName)
        {
            // Each character for string in .NET is UTF-16 so, it takes 2 byte per character

            if (attributeName != default(string))
                return attributeName.Length * 2;

            return 0;
        }

        private int GetAttributeNameInMemorySize(string attributeName)
        {
            if (attributeName != default(string))
                return GetAttributeNameSize(attributeName) + 24;   // String size + .NET overhead

            return 0;
        }

        private void AddOrUpdateAttribute(string attributeName, JsonValueBase attributeValue)
        {
            VerifyAttributeName(attributeName);
            VerifyAttributeValue(attributeValue);

            _propertyMap[attributeName] = attributeValue;
        }

        private bool ShouldOmitAttribute(string attributeName)
        {
            if (string.IsNullOrEmpty(attributeName))
                return false;

            var shouldOmit = false;
            shouldOmit = shouldOmit || attributeName.Equals(JsonConstants.JsonExtraAttributeIdName);
            shouldOmit = shouldOmit || attributeName.Equals(JsonConstants.JsonExtraAttributeTypeName);

            return shouldOmit;
        }

        #endregion

        #region ---------------------------- Overrides -----------------------------

        /// <summary>
        /// Checks if an obj is equal to this instance of JSONObject
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var otherJsonObject = obj as JsonObject;

            if (otherJsonObject == default(JsonObject))
                return false;



            foreach (var entry in otherJsonObject)
            {
                var otherJsonObjectAttrName = entry.Key;
                var otherJsonObjectAttrValue = entry.Value;

                if (ShouldOmitAttribute(otherJsonObjectAttrName))
                    continue;

                var attrValueHere = this.GetAttributeValue(otherJsonObjectAttrName);

                // Probably no need but leave it here for some anomalous case
                if (attrValueHere == default(JsonValueBase))
                    return false;

                if (attrValueHere.DataType != otherJsonObjectAttrValue.DataType)
                    return false;

                if (!attrValueHere.Equals(otherJsonObjectAttrValue))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns JSONObject in string representation
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var jsonBuilder = new System.Text.StringBuilder();
            var attributeEnumerator = _propertyMap.GetEnumerator();

            jsonBuilder.Append('{');

            for (int i = 0; attributeEnumerator.MoveNext(); i++)
            {
                jsonBuilder
                    .Append(i != 0 ? ", " : " ")
                    .Append('"')
                    .Append(attributeEnumerator.Current.Key)
                    .Append('"')
                    .Append(' ')
                    .Append(':')
                    .Append(' ')
                    .Append(attributeEnumerator.Current.Value.ToString());
            }

            jsonBuilder.Append(jsonBuilder.Length > 1 ? " " : string.Empty);
            jsonBuilder.Append('}');

            return jsonBuilder.ToString();
        }

        #endregion
    }
}
