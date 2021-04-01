using Alachisoft.NCache.Common.Caching;

namespace Alachisoft.NCache.Client
{
    internal class ValueEmissary
    {
        public string Key { get; set; }
        public object Data { get; set; }
        public EntryType Type { get; set; }
    }
}
