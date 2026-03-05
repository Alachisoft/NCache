using Alachisoft.NCache.Runtime.Enum;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Runtime.JSON
{
    [Obsolete("This API is deprecated and will be removed in a future release. This feature is being retired and will not be continued in future versions.", false)]
    public interface IJSONDocument : ICloneable, IComparable, IEnumerable<KeyValuePair<string, object>>, ICompactSerializable
    {
        object this[string attribute] { get; set; }

        /// <summary>
        /// 
        /// </summary>
        int Count { get; }
        /// <summary>
        /// 
        /// </summary>
        long Size { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void Add(string attribute, IJSONDocument value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void Add(string attribute, DateTime value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void Add(string attribute, Array value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void Add(string attribute, bool value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void Add(string attribute, double value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void Add(string attribute, short value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void Add(string attribute, int value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void Add(string attribute, long value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void Add(string attribute, float value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void Add(string attribute, string value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void Add(string attribute, object value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        bool Contains(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        ICollection<string> GetAttributes();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        JsonDataType GetAttributeDataType(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        short GetAsInt16(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        int GetAsInt32(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        long GetAsInt64(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        float GetAsFloat(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        double GetAsDouble(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        decimal GetAsDecimal(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        string GetString(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        bool GetBoolean(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        DateTime GetDateTime(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        IJSONDocument GetDocument(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        string GetAsString(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <returns></returns>
        T[] GetArray<T>(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <returns></returns>
        T Get<T>(string attribute);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGet(string attribute, out object value);
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGet<T>(string attribute, out T value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        void Remove(string attribute);
        /// <summary>
        /// 
        /// </summary>
        void Clear();
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Parse<T>();

    }
}
