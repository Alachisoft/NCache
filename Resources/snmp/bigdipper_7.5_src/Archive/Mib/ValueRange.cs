using System;

namespace Lextm.SharpSnmpLib.Mib
{
    internal class ValueRange
    {
        private readonly Int64 _start;
        private readonly Int64? _end;

        public ValueRange(Symbol first, Symbol second)
        {
            Int64 value = 0;

            if (first.ToString().StartsWith("'"))
            {
                if (first.ToString().EndsWith("B", true, System.Globalization.CultureInfo.InvariantCulture))
                {
                    // Binary number
                    string num = first.ToString().TrimStart(new[] { '\'' }).TrimEnd(new[] { 'b', 'B', '\'' });
                    try
                    {
                        value = Convert.ToInt64(num, 2);
                    }
                    catch (Exception)
                    {
                        first.Throw(string.Format("invalid sub-typing; error parsing start of range: {0}", num));
                    }
                }
                else if (first.ToString().EndsWith("H", true, System.Globalization.CultureInfo.InvariantCulture))
                {
                    // Hex number
                    string num = first.ToString().TrimStart(new[] { '\'' }).TrimEnd(new[] { 'h', 'H', '\'' });
                    try
                    {
                        value = Convert.ToInt64(num, 16);
                    }
                    catch (Exception)
                    {
                        first.Throw(string.Format("invalid sub-typing; error parsing start of range: {0}", num));
                    }
                }
                else
                {
                    first.Throw("invalid sub-typing; error parsing start of range: {0}");
                }
            }
            else
            {
                first.Assert(Int64.TryParse(first.ToString(), out value), "invalid sub-typing; error parsing start of range");
            }

            _start = value;

            if (second != null)
            {
                if (second.ToString().StartsWith("'"))
                {
                    if (second.ToString().EndsWith("B", true, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        // Binary number
                        string num = second.ToString().TrimStart(new[] { '\'' }).TrimEnd(new[] { 'b', 'B', '\'' });
                        try
                        {
                            value = Convert.ToInt64(num, 2);
                        }
                        catch (Exception)
                        {
                            second.Throw(string.Format("invalid sub-typing; error parsing end of range: {0}", num));
                        }
                    }
                    else if (second.ToString().EndsWith("H", true, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        // Hex number
                        string num = second.ToString().TrimStart(new[] { '\'' }).TrimEnd(new[] { 'h', 'H', '\'' });
                        try
                        {
                            value = Convert.ToInt64(num, 16);
                        }
                        catch (Exception)
                        {
                            second.Throw(string.Format("invalid sub-typing; error parsing end of range: {0}", num));
                        }
                    }
                    else
                    {
                        second.Throw("invalid sub-typing; error parsing end of range");
                    }
                }
                else if (second.ToString() == "MAX")
                {
                    value = Int64.MaxValue;
                }
                else
                {
                    second.Assert(Int64.TryParse(second.ToString(), out value), "invalid sub-typing; error parsing end of range");
                }
                _end = value;
                second.Assert(_start <= _end,
                                string.Format(
                                    "illegal sub-typing; start of range {0} must be less than end of range {1}",
                                    _start,
                                    _end));
            }
        }

        public Int64 Start
        {
            get { return _start; }
        }

        public Int64? End
        {
            get { return _end; }
        }

        internal bool Contains(Int64 p)
        {
            return _end == null ? p == _start : _start <= p && p <= _end;
        }
    }
}
