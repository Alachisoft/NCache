using System;

namespace Lextm.SharpSnmpLib.Mib
{
    class DisplayHint
    {
        private enum NumType {
            Dec,
            Hex,
            Oct,
            Bin,
            Str
        }

        private readonly string _str;
        private readonly NumType _type;
        private readonly int _decimalPoints;

        public DisplayHint(string str)
        {
            _str = str;
            if (str.StartsWith("d"))
            {
                _type = NumType.Dec;
                if (str.StartsWith("d-"))
                {
                    _decimalPoints = Convert.ToInt32(str.Substring(2));
                }
            }
            else if (str.StartsWith("o"))
            {
                _type = NumType.Oct;
            }
            else if (str.StartsWith("h"))
            {
                _type = NumType.Hex;
            }
            else if (str.StartsWith("b"))
            {
                _type = NumType.Bin;
            }
            else
            {
                _type = NumType.Str;
                foreach (char c in str)
                {
                    // TODO: 
                }
            }

        }

        public override string ToString()
        {
            return _str;
        }

        internal object Decode(int i)
        {
            switch (_type)
            {
                case NumType.Dec:
                    if (_decimalPoints == 0)
                    {
                        return i;
                    }

                    return i / Math.Pow(10.0, _decimalPoints);
                case NumType.Hex:
                    return Convert.ToString(i, 16);
                case NumType.Oct:
                    return Convert.ToString(i, 8);
                case NumType.Bin:
                    return Convert.ToString(i, 2);
                default:
                    return null;
            }
        }
    }
}
