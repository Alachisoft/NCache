// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Alachisoft.NCache.Common.Enum;
#if NET40
using System.Numerics;
#endif

namespace Alachisoft.NCache.Web.Aggregation
{
    internal static class Calculation
    {
        private static object mutex = new object();

        internal static Object Sum(ICollection value, DataType dataType)
        {
            int integerResult = 0;
            Double doubleRessult = 0.0d;
            float floatResult = 0.0f;
            Decimal decimalResult = new Decimal(0);
#if NET40
            BigInteger bigIntegerResult = BigInteger.Zero;
#endif
            long longResult = 0L;
            short shortResult = 0;
            if (value != null)
            {
                {
                    IEnumerator itr = value.GetEnumerator();
                    while (itr.MoveNext())
                    {
                        switch (dataType)
                        {
                            case DataType.DOUBLE:
                                doubleRessult += (Double)itr.Current;
                                break;
                            case DataType.FLOAT:
                                floatResult += (float)itr.Current;
                                break;
                            case DataType.DECIMAL:
                                decimalResult = Decimal.Add(decimalResult, (decimal)itr.Current);
                                break;

                            case DataType.INTEGER:
                                integerResult += (int)itr.Current;
                                break;
#if NET40
                            case DataType.BIGINTEGER:
                                BigInteger current;
                                current = BigInteger.Parse(itr.Current.ToString());
                                bigIntegerResult = BigInteger.Add(bigIntegerResult, current);
                                break;
#endif
                            case DataType.LONG:
                                longResult += (long)itr.Current;
                                break;
                            case DataType.SHORT:
                                {
                                    shortResult += (short)itr.Current;
                                    break;
                                }
                        }
                    }
                }
                switch (dataType)
                {
                    case DataType.DOUBLE:
                        return (double)doubleRessult;
                    case DataType.FLOAT:
                        return (float)floatResult;
                    case DataType.DECIMAL:
                        return decimalResult;

                    case DataType.INTEGER:
                        return (int)integerResult;
#if NET40
                    case DataType.BIGINTEGER:
                        return bigIntegerResult;
#endif
                    case DataType.LONG:
                        return (long)longResult;

                    case DataType.SHORT:
                        return (short)shortResult;
                }
            }

            return null;
        }

        internal static Object Avg(ICollection value, DataType dataType, bool calculate)
        {
            int integerResult = 0;
#if NET40
            BigInteger bigIntegerResult = BigInteger.Zero;
#endif
            Double doubleRessult = 0.000d;
            float floatResult = 0.000f;
            Decimal decimalResult = new Decimal(0.000);

            long longResult = 0L;
            short shortResult = 0;

            if (value != null)
            {
                int size = value.Count;
                IEnumerator itr = value.GetEnumerator();
                while (itr.MoveNext())
                {
                    object valueToAdd = null;
                    object current = itr.Current;
                    if (current is DictionaryEntry)
                    {
                        DictionaryEntry entry = (DictionaryEntry)itr.Current;
                        valueToAdd = entry.Key;
                        size += (int)entry.Value;
                    }
                    else
                    {
                        valueToAdd = current;
                    }

                    switch (dataType)
                    {
                        case DataType.DOUBLE:
                            doubleRessult += (Double)valueToAdd;
                            break;
                        case DataType.FLOAT:
                            floatResult += (float)valueToAdd;
                            break;
                        case DataType.DECIMAL:
                            decimalResult = Decimal.Add(decimalResult, (decimal)valueToAdd);
                            break;

                        case DataType.INTEGER:
                            integerResult += (int)valueToAdd;
                            break;
#if NET40
                        case DataType.BIGINTEGER:
                            BigInteger curr;
                            if (valueToAdd is BigInteger)
                                curr = (BigInteger)valueToAdd;
                            else
                                curr = BigInteger.Parse(valueToAdd.ToString());

                            bigIntegerResult = BigInteger.Add(bigIntegerResult, curr);
                            break;
#endif
                        case DataType.LONG:
                            longResult += (long)valueToAdd;
                            break;
                        case DataType.SHORT:
                            shortResult += (short)valueToAdd;
                            break;
                    }
                }

                if (calculate)
                {
                    switch (dataType)
                    {
                        case DataType.DOUBLE:
                            return (double)(doubleRessult / size);
                        case DataType.FLOAT:
                            return (float)(floatResult / size);
                        case DataType.DECIMAL:
                            return (decimal)(decimalResult / size);
                        case DataType.INTEGER:
                            return (int)(integerResult / size);
#if NET40
                        case DataType.BIGINTEGER:
                            return bigIntegerResult / (BigInteger)size;
#endif
                        case DataType.LONG:
                            return (long)(longResult / size);
                        case DataType.SHORT:
                            {
                                return (short)(shortResult / size);
                            }
                    }
                }
                else
                {
                    if (dataType == DataType.DOUBLE)
                        return new DictionaryEntry(doubleRessult, size);
                    else if (dataType == DataType.FLOAT)
                        return new DictionaryEntry(floatResult, size);
                    else if (dataType == DataType.DECIMAL)
                        return new DictionaryEntry(decimalResult, size);
                    else if (dataType == DataType.INTEGER)
                        return new DictionaryEntry(integerResult, size);
                    else if (dataType == DataType.LONG)
                        return new DictionaryEntry(longResult, size);
                    else if (dataType == DataType.SHORT)
                        return new DictionaryEntry(shortResult, size);
#if NET40
                    else if (dataType == DataType.BIGINTEGER)
                        return new DictionaryEntry(bigIntegerResult, size);
#endif
                }
            }

            return null;
        }

        internal static Object Min(ICollection value, DataType type)
        {
            if (value != null)
            {
                return Calculation.MinInternal(value, type);
            }

            return null;
        }

        internal static Object Max(ICollection value, DataType type)
        {
            if (value != null)
            {
                return Calculation.MaxInternal(value, type);
            }

            return null;
        }

        internal static long Count(ICollection value)
        {
            if (value != null)
            {
                return (long)value.Count;
            }

            return 0;
        }

        internal static long CountAll(ICollection value)
        {
            long count = 0;

            if (value != null)
            {
                IEnumerator itr = value.GetEnumerator();
                while (itr.MoveNext())
                {
                    count += (long)itr.Current;
                }
            }

            return count;
        }

        internal static ArrayList DistinctValue(ICollection value)
        {
            if (value != null)
            {
                HashSet<object> hashset = new HashSet<object>();
                IEnumerator enumerator = value.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    hashset.Add(enumerator.Current);
                }
#if NET40
                return new ArrayList(hashset.ToList());
#else
                    object[] arr = new object[hashset.Count];
                    hashset.CopyTo(arr);
                    return new ArrayList(arr);
#endif
            }

            return null;
        }

        internal static ArrayList DistinctAllValue(ICollection value)
        {
            HashSet<object> hashSet = new HashSet<object>();
            if (value != null)
            {
                IEnumerator itr = value.GetEnumerator();
                while (itr.MoveNext())
                {
                    IEnumerator internalEnumerator = ((ArrayList)itr.Current).GetEnumerator();
                    while (internalEnumerator.MoveNext())
                    {
                        hashSet.Add(internalEnumerator.Current);
                    }
                }
#if NET40
                return new ArrayList(hashSet.ToList());
#else
                    object[] arr = new object[hashSet.Count];
                    hashSet.CopyTo(arr);
                    return new ArrayList(arr);
#endif
            }

            return null;
        }

        private static object MinInternal(ICollection value, DataType type)
        {
            if (value != null)
            {
                IEnumerator enumerator = value.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    object previousMin = enumerator.Current;
                    while (enumerator.MoveNext())
                    {
                        int comparisonResult = -1;
                        comparisonResult = Comparer.Default.Compare(previousMin, enumerator.Current);
                        if (comparisonResult > 0)
                            previousMin = enumerator.Current;
                    }

                    return previousMin;
                }
            }

            return null;
        }

        private static object MaxInternal(ICollection value, DataType type)
        {
            if (value != null)
            {
                IEnumerator enumerator = value.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    object previousMax = enumerator.Current;
                    while (enumerator.MoveNext())
                    {
                        int comparisonResult = -1;
                        comparisonResult = Comparer.Default.Compare(previousMax, enumerator.Current);
                        if (comparisonResult < 0)
                            previousMax = enumerator.Current;
                    }

                    return previousMax;
                }
            }

            return null;
        }
    }
}
