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
using System.Collections;

namespace Alachisoft.NCache.Caching.Util
{
    /// <summary>
    /// Enumerator that provides single enumration over multiple containers.
    /// </summary>
    internal class AggregateEnumerator: IDictionaryEnumerator
    {
        /// <summary> list of enumerators. </summary>
        private IDictionaryEnumerator[]		_enums = null;

        /// <summary> index of the current enumerator. </summary>
        private int	_currId = 0;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="first">first enumerator.</param>
        /// <param name="second">second enumerator.</param>
        public AggregateEnumerator(params IDictionaryEnumerator[] enums)
        {
            _enums = enums;
            ((IEnumerator)this).Reset();
        }

        #region	/                 --- IEnumerator ---           /

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element 
        /// in the collection.
        /// </summary>
        void IEnumerator.Reset()
        {
            _currId = 0;
            _enums[_currId].Reset();
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; 
        /// false if the enumerator has passed the end of the collection.</returns>
        bool IEnumerator.MoveNext()
        {
            bool result = _enums[_currId].MoveNext();
            if(!result && _currId < _enums.Length - 1)
            {
                _enums[++_currId].Reset();
                result = _enums[_currId].MoveNext();
            }
            return result;
        }
		
        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        object IEnumerator.Current 
        {
            get { return _enums[_currId].Current; }
        }

        #endregion

        #region	/                 --- IDictionaryEnumerator ---           /

        /// <summary>
        /// gets both the key and the value of the current dictionary entry.
        /// </summary>
        DictionaryEntry IDictionaryEnumerator.Entry 
        {	
            get { return _enums[_currId].Entry; }
        }

        /// <summary>
        /// gets the key of the current dictionary entry.
        /// </summary>
        object IDictionaryEnumerator.Key 
        {
            get { return _enums[_currId].Key; }
        }

        /// <summary>
        /// gets the value of the current dictionary entry.
        /// </summary>
        object IDictionaryEnumerator.Value 
        {
            get { return _enums[_currId].Value; }
        }

        #endregion
    }
}