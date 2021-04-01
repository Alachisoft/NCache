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
using Alachisoft.NCache.Common.Pooling.Lease;

namespace Alachisoft.NCache.Common.Pooling.ArrayPool
{
    internal class ArrayWrapper<T> : BookKeepingLease
    {
        #region ---------------------------- [ Fields ] ----------------------------

        private T[] _array;

        #endregion

        #region ------------------------- [ Properties ] ---------------------------

        public T[] Array
        {
            get
            {
                if (_array == null)
                    _array = new T[ArrayLength];

                return _array;
            }
            set
            {
                _array = value;
            }
        }

        public int ArrayLength
        {
            get; set;
        }

        #endregion

        #region ------------------------- [ Constructors ] -------------------------

        public ArrayWrapper(int length)
        {
            ArrayLength = length;
            _array = new T[length];
        }

        #endregion

        #region --------------------------- [ ILeasable ] --------------------------

        public override void ResetLeasable()
        {
            if (_array == null)
                return;

            System.Array.Clear(_array, 0, ArrayLength);
        }

        public override sealed void MarkInUse(int moduleRefId)
        {
            // This class is used only internally by ArrayPool and is not to be marked for use
            // thus. We don't calculate 'IsInUse' for this class therefore, it is returned to 
            // pool explicitly.
            throw new InvalidOperationException("There is no need to mark this ILeasable.");
        }

        public override sealed void MarkFree(int moduleRefId)
        {
            // This class is used only internally by ArrayPool and is not to be marked free 
            // thus. We don't calculate 'IsInUse' for this class therefore, it is returned 
            // to pool explicitly.
            throw new InvalidOperationException("There is no need to mark this ILeasable.");
        }

        public override sealed void ReturnLeasableToPool()
        {
            // We return this class to pool explicitly therefore, this method is not to be called.
            throw new InvalidOperationException("This ILeasable ought to be returned to pool explicitly.");
        }

        #endregion
    }
}
