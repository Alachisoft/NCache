using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Common.Util
{
    public class ConfigSetting
    {
        public DataType DataType { get; }
        public bool IsHotApplicable { get; }

        public ConfigSetting(DataType DataType, bool IsHotApplicable)
        {
            this.DataType = DataType;
            this.IsHotApplicable = IsHotApplicable;
        }
    }
}