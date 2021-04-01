//  Copyright (c) 2021 Alachisoft
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
using Alachisoft.NCache.Common.Caching;

namespace Alachisoft.NCache.Common.Pooling.Util
{
    internal static class BucketingUtil
    {
        #region ---------------------------- [ Fields ] ----------------------------

        private static readonly int[] _bucketLengths = new[]
        {
            00100, 00200, 00300, 00500, 00750, 01000, 02000, 03000,
            04000, 05000, 06000, 07000, 08000, 09000, 10000, 11000,
            12000, 13000, 14000, 15000, 16000, 17000, 18000, 19000,
            20000, 22000, 24000, 26000, 28000, 30000, 32000, 34000,
            36000, 38000, 40000, 44000, 48000, 52000, 56000, 60000,
            66000, 72000, 78000, UserBinaryObject.LARGE_OBJECT_SIZE,
        };

        #endregion

        #region ------------------------- [ Properties ] ---------------------------

        public static int TotalBuckets
        {
            get => _bucketLengths.Length;
        }

        #endregion

        #region ----------------------- [ Utility Methods ] ------------------------

        public static int GetBucket(int arrayLength)
        {
            return NearSearch(arrayLength);
        }

        public static int GetLength(int bucketId)
        {
            var totalBuckets = TotalBuckets;

            if (bucketId < 0 || bucketId >= totalBuckets)
                throw new ArgumentException("Invalid bucket ID specified.", nameof(bucketId));

            return _bucketLengths[bucketId];
        }

        public static bool IsValidLength(int length)
        {
            return IsValidLength(GetBucket(length), length);
        }

        public static bool IsValidLength(int bucketId, int length)
        {
            return GetLength(bucketId) == length;
        }

        #endregion

        #region ------------------------ [ Helper Methods ] ------------------------

        private static int NearSearch(int nearTerm)
        {
            int left = 0, right = _bucketLengths.Length - 1, middle = -1;

            while (left <= right)
            {
                middle = left + (right - left) / 2;

                if (_bucketLengths[middle] == nearTerm)
                    return middle;

                // If current node is greater than search term
                if (_bucketLengths[middle] > nearTerm)
                    // Move to the left portion of the array
                    right = middle - 1;

                else
                    // Move to the right portion of the array
                    left = middle + 1;
            }

            var nearest = middle;

            if (_bucketLengths[middle] < nearTerm)
            {
                for (int i = middle; i < _bucketLengths.Length; i++)
                {
                    if (nearTerm - _bucketLengths[i] <= 0)
                    {
                        nearest = i;
                        break;
                    }
                }
            }
            else if (_bucketLengths[middle] > nearTerm)
            {
                for (int i = middle; i >= 0; i--)
                {
                    if (_bucketLengths[i] - nearTerm <= 0)
                    {
                        nearest = i + 1;
                        break;
                    }
                }
            }
            return nearest;
        }

        #endregion
    }
}
