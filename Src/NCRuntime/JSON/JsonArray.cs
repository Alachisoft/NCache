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

namespace Alachisoft.NCache.Runtime.JSON
{
    /// <summary>
    /// Class represnts JArray in JSON standards. Inner implementation of IJSONArray
    /// </summary>
    [Serializable]
    [Obsolete("This API is deprecated and will be removed in a future release. This feature is being retired and will not be continued in future versions.", false)]
    public sealed class JsonArray : JsonValueBase, IJsonArray
    {
        #region ------------------------------ Fields ------------------------------

        private readonly IList<JsonValueBase> _jsonItems;

        #endregion

        #region ---------------------------- Properties ----------------------------

        /// <summary>
        /// Indexer for the JSON Arary
        /// </summary>
        /// <param name="index"></param>
        /// <returns>JSON value on that index</returns>
        public JsonValueBase this[int index]
        {
            get
            {
                VerifyIndex(index);
                return _jsonItems[index];
            }
            set
            {
                if (value == default(JsonValueBase))
                    throw new NotSupportedException("Updating to null value is not supported with indexer.");

                if (index > -1 && index < Count)
                    UpdateValue(index, value);

                else
                    AddAt(index, value);
            }
        }

        /// <summary>
        /// Total items in array
        /// </summary>
        public int Count
        {
            get
            {
                return _jsonItems == null ? 0 : _jsonItems.Count;
            }
        }

        bool ICollection<JsonValueBase>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// JSONArray instance
        /// </summary>
        public override object Value
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Size if the JSONArray
        /// </summary>
        protected internal override int Size
        {
            get
            {
                var size = base.Size;

                if (_jsonItems != default(IList<JsonValueBase>))
                    size += (from item in _jsonItems select item.Size).Sum();

                return size;
            }
        }

        /// <summary>
        /// In memory size if the JSONArray
        /// </summary>
        protected internal override int InMemorySize
        {
            get
            {
                var inMemorySize = base.InMemorySize + 8;   // InMemorySize of base + .NET list overhead (8)

                if (_jsonItems != default(IList<JsonValueBase>))
                    inMemorySize += (from item in _jsonItems select item.InMemorySize).Sum();

                return inMemorySize;
            }
        }

        #endregion

        #region --------------------------- Constructors ---------------------------

        /// <summary>
        /// Default constructor
        /// </summary>
        public JsonArray() : base(null, JsonDataType.Array)
        {
            _jsonItems = new List<JsonValueBase>();
        }

        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="json">JSONArray object in string format</param>
        public JsonArray(string json) : this()
        {
            try
            {
                Parse(this, JArray.Parse(json));
            }
            catch (JsonReaderException jre)
            {
                throw new ArgumentException("Invalid JSON string provided.", nameof(json), jre);
            }
        }

        #endregion

        #region ----------------------------- Behavior -----------------------------

        /// <summary>
        /// Adds JSONValue item to array
        /// </summary>
        /// <param name="item">JSONValue to be added</param>
        public void Add(JsonValue item)
        {
            Add(item as JsonValueBase);
        }

        /// <summary>
        /// Adds JSONValueBase item to array
        /// </summary>
        /// <param name="item">JSONValueBase to be added</param>
        public void Add(JsonValueBase item)
        {
            VerifyJsonItem(item);
            _jsonItems?.Add(item);
        }

        /// <summary>
        /// Copies items from provided array starting from a particular array index
        /// </summary>
        /// <param name="array">Array from which the items have to be copied</param>
        /// <param name="arrayIndex">Starting index of the array from which to start copying</param>
        public void CopyTo(JsonValueBase[] array, int arrayIndex)
        {
            _jsonItems?.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes JSONValue item from array
        /// </summary>
        /// <param name="item">item to be removed</param>
        /// <returns>True if item exists and is removed successfully</returns>
        public bool Remove(JsonValue item)
        {
            return Remove(item as JsonValueBase);
        }

        /// <summary>
        /// Removes JsonValueBase item from array
        /// </summary>
        /// <param name="item">item to be removed</param>
        /// <returns>True if item exists and is removed successfully</returns>
        public bool Remove(JsonValueBase item)
        {
            if (_jsonItems == null)
                return false;

            VerifyJsonItem(item);
            return _jsonItems.Remove(item);
        }

        /// <summary>
        /// Checks if a JSONValue item exits in array
        /// </summary>
        /// <param name="item">Item to be found</param>
        /// <returns>True if exits</returns>
        public bool Contains(JsonValue item)
        {
            return Contains(item as JsonValueBase);
        }

        /// <summary>
        /// Checks if a JsonValueBase item exits in array
        /// </summary>
        /// <param name="item">Item to be found</param>
        /// <returns>True if exits</returns>
        public bool Contains(JsonValueBase item)
        {
            if (_jsonItems == null)
                return false;

            VerifyJsonItem(item);
            return _jsonItems.Contains(item);
        }

        /// <summary>
        /// Clears all array items and brings count to 0 
        /// </summary>
        public void Clear()
        {
            _jsonItems?.Clear();
        }

        #region -------------------- IEnumerator Implementation --------------------

        /// <summary>
        /// Returns and Enumerator that iterates through JSONArray items
        /// </summary>
        /// <returns></returns>
        public IEnumerator<JsonValueBase> GetEnumerator()
        {
            return _jsonItems?.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #endregion

        #region -------------------------- Helper Methods --------------------------

        private void VerifyIndex(int index)
        {
            if (index < 0 || index > Count)
                throw new IndexOutOfRangeException();
        }

        private void VerifyJsonItem(JsonValueBase jsonItem)
        {
            if (jsonItem == default(JsonValueBase))
                throw new ArgumentNullException("jsonItem");

            if (jsonItem == this)
                throw new ArgumentException($"{GetType().Name} cannot contain an item that is a reference to self.");

            jsonItem.InvalidateParentReference(new Dictionary<JsonValueBase, byte>()
                {
                    { this, 0}
                }
            );
        }

        private void UpdateValue(int index, JsonValueBase jsonItem)
        {
            VerifyIndex(index);
            VerifyJsonItem(jsonItem);
            _jsonItems[index] = jsonItem;
        }

        private void AddAt(int index, JsonValueBase jsonItem)
        {
            VerifyJsonItem(jsonItem);

            if (index < 0)
                throw new IndexOutOfRangeException();

            if (index < Count)
            {
                var existingItem = _jsonItems?[index] as JsonNull;

                if (existingItem == default(JsonValueBase))
                    throw new InvalidOperationException("A value already exists at the specified index.");

                UpdateValue(index, jsonItem);
            }
            else
            {
                var aheadFactor = index - Count;

                for (int i = 0; i < aheadFactor; i++)
                    Add(new JsonNull());

                Add(jsonItem);
            }
        }

        #endregion

        #region ---------------------------- Overrides -----------------------------

        /// <summary>
        /// Checks if an obj is equal to this instance of JSONArray
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var otherJsonArray = obj as JsonArray;

            if (otherJsonArray == default(JsonArray))
                return false;

            if (otherJsonArray.Count != Count)
                return false;

            var currentEnumerator = GetEnumerator();
            var otherEnumerator = otherJsonArray.GetEnumerator();

            while (currentEnumerator.MoveNext())
            {
                if (!otherEnumerator.MoveNext())
                    return false;

                var jsonValueOther = otherEnumerator.Current;
                var jsonValueCurrent = currentEnumerator.Current;

                if (jsonValueCurrent.DataType != jsonValueOther.DataType)
                    return false;

                if (!jsonValueCurrent.Equals(jsonValueOther))
                    return false;
            }

            if (otherEnumerator.MoveNext())
                return false;

            return true;
        }

        /// <summary>
        /// Returns JSONArray in string representation
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var jsonBuilder = new System.Text.StringBuilder();
            var itemEnumerator = _jsonItems.GetEnumerator();

            jsonBuilder.Append('[');

            for (int i = 0; itemEnumerator.MoveNext(); i++)
            {
                jsonBuilder
                    .Append(i != 0 ? ", " : " ")
                    .Append(itemEnumerator.Current.ToString());
            }

            jsonBuilder.Append(jsonBuilder.Length > 1 ? " " : string.Empty);
            jsonBuilder.Append(']');

            return jsonBuilder.ToString();
        }

        #endregion
    }
}
