// ==++==
// 
//   Copyright (c). 2015. Microsoft Corporation.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Collections;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    public static class HashHelpers
    {
        public const int HashCollisionThreshold = 100;
        public const int MaxPrimeArrayLength = 2146435069;

#if FEATURE_RANDOMIZED_STRING_HASHING
		public static bool s_UseRandomizedStringHashing = string.UseRandomizedHashing();
#endif
        public static readonly int[] primes = new int[]
		{
			3,
			7,
			11,
			17,
			23,
			29,
			37,
			47,
			59,
			71,
			89,
			107,
			131,
			163,
			197,
			239,
			293,
			353,
			431,
			521,
			631,
			761,
			919,
			1103,
			1327,
			1597,
			1931,
			2333,
			2801,
			3371,
			4049,
			4861,
			5839,
			7013,
			8419,
			10103,
			12143,
			14591,
			17519,
			21023,
			25229,
			30293,
			36353,
			43627,
			52361,
			62851,
			75431,
			90523,
			108631,
			130363,
			156437,
			187751,
			225307,
			270371,
			324449,
			389357,
			467237,
			560689,
			672827,
			807403,
			968897,
			1162687,
			1395263,
			1674319,
			2009191,
			2411033,
			2893249,
			3471899,
			4166287,
			4999559,
			5999471,
			7199369
		};

#if !NETCORE
        private static ConditionalWeakTable<object, SerializationInfo> s_SerializationInfoTable; 
#endif
        private static RandomNumberGenerator rng;
        private static byte[] data;
        private static int currentIndex = 1024;
        private static readonly object lockObj = new object();
        private const int bufferSize = 1024;

#if !NETCORE
        internal static ConditionalWeakTable<object, SerializationInfo> SerializationInfoTable

        
        {
            get
            {
                if (HashHelpers.s_SerializationInfoTable == null)
                {
                    ConditionalWeakTable<object, SerializationInfo> value = new ConditionalWeakTable<object, SerializationInfo>();
                    Interlocked.CompareExchange<ConditionalWeakTable<object, SerializationInfo>>(ref HashHelpers.s_SerializationInfoTable, value, null); 
                }
                return HashHelpers.s_SerializationInfoTable;
            }
        }

#endif

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0)
            {
                int num = (int)Math.Sqrt((double)candidate);
                for (int i = 3; i <= num; i += 2)
                {
                    if (candidate % i == 0)
                    {
                        return false;
                    }
                }
                return true;
            }
            return candidate == 2;
        }
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int GetPrime(int min)
        {
            if (min < 0)
            {
                throw new ArgumentException(ResourceHelper.GetResourceString("Arg_HTCapacityOverflow"));
            }
            for (int i = 0; i < HashHelpers.primes.Length; i++)
            {
                int num = HashHelpers.primes[i];
                if (num >= min)
                {
                    return num;
                }
            }
            for (int j = min | 1; j < 2147483647; j += 2)
            {
                if (HashHelpers.IsPrime(j) && (j - 1) % 101 != 0)
                {
                    return j;
                }
            }
            return min;
        }
        public static int GetMinPrime()
        {
            return HashHelpers.primes[0];
        }
        public static int ExpandPrime(int oldSize)
        {
            int num = 2 * oldSize;
            if (num > 2146435069 && 2146435069 > oldSize)
            {
                return 2146435069;
            }
            return HashHelpers.GetPrime(num);
        }
        public static bool IsWellKnownEqualityComparer(object comparer)
        {
            return comparer == null || comparer == EqualityComparer<string>.Default || comparer is IWellKnownStringEqualityComparer;
        }
        public static IEqualityComparer GetRandomizedEqualityComparer(object comparer)
        {
            if (comparer == null)
            {
                return new RandomizedObjectEqualityComparer();
            }
            if (comparer == EqualityComparer<string>.Default)
            {
                return new RandomizedStringEqualityComparer();
            }
            IWellKnownStringEqualityComparer wellKnownStringEqualityComparer = comparer as IWellKnownStringEqualityComparer;
            if (wellKnownStringEqualityComparer != null)
            {
                return wellKnownStringEqualityComparer.GetRandomizedEqualityComparer();
            }
            return null;
        }
        public static object GetEqualityComparerForSerialization(object comparer)
        {
            if (comparer == null)
            {
                return null;
            }
            IWellKnownStringEqualityComparer wellKnownStringEqualityComparer = comparer as IWellKnownStringEqualityComparer;
            if (wellKnownStringEqualityComparer != null)
            {
                return wellKnownStringEqualityComparer.GetEqualityComparerForSerialization();
            }
            return comparer;
        }
        internal static long GetEntropy()
        {
            long result;
            lock (HashHelpers.lockObj)
            {
                if (HashHelpers.currentIndex == 1024)
                {
                    if (HashHelpers.rng == null)
                    {
                        HashHelpers.rng = RandomNumberGenerator.Create();
                        HashHelpers.data = new byte[1024];
                    }
                    HashHelpers.rng.GetBytes(HashHelpers.data);
                    HashHelpers.currentIndex = 0;
                }
                long num = BitConverter.ToInt64(HashHelpers.data, HashHelpers.currentIndex);
                HashHelpers.currentIndex += 8;
                result = num;
            }
            return result;
        }
    }

}
