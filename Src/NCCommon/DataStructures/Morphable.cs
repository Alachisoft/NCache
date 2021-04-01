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
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Common.DataStructures
{
    // Class: Morphable (Generic Type)
    // Author: Usman Shahid
    // A morphable is a collection that can be morphed between a plain(DeMorphed) 
    // and morphed(Clustered) view/state of the collection seamlessly. Note that the internal
    // state of a morphable can only be mutated through the view/state it is in (Morphed/DeMorphed).
    // If a morphable is in one view/state and different operations are performed on the other
    // view/state, the operations do not reflect on the internal data as the view/state the user
    // is operating on is a morphed clone of the internal view/state. The changes are needed to be
    // assigned to the appropriate view/state property. Enjoy.
    public class Morphable<T>: ISizable
    {
        private bool isMorphed = false;
        private T[] plainView = null;
        private ClusteredArray<T> clusteredView = null;
        private int morphedLengthThreshold = 80*1024;

        public Morphable()
        {
            plainView = new T[0];
        }

        public Morphable(T[] arr)
        {
            plainView = arr;
        }

        public Morphable(ClusteredArray<T> arr)
        {
            clusteredView = arr;
            morphedLengthThreshold = arr.LengthThreshold;
            isMorphed = true;
        }

        public Morphable(T[][] arr)
        {
            if (arr != null)
            {
                if (arr.Length > 1)
                {
                    morphedLengthThreshold = arr[0].Length;
                    clusteredView = new ClusteredArray<T>(morphedLengthThreshold, arr[0].Length*arr.Length);
                }
                else
                {
                    clusteredView = new ClusteredArray<T>(arr[0].Length);
                    morphedLengthThreshold = clusteredView.LengthThreshold;
                }
                int arrayposition = 0;
                foreach (T[] buffer in arr)
                {
                    clusteredView.CopyFrom(buffer, 0, arrayposition, buffer.Length);
                    arrayposition += buffer.Length;
                }
                isMorphed = true;
            }
        } 

        public bool IsMorphed
        {
            get { return isMorphed; }
        }

        public T[] PlainView
        {
            get { return isMorphed ? (T[])clusteredView : plainView; }
            set
            {
                if (!isMorphed)
                    plainView = value;
                else
                    clusteredView =  toClusteredArray(value);
            }
        }

        public void Morph()
        {
            if (!isMorphed)
            {
                clusteredView = toClusteredArray(plainView);
                isMorphed = true;
                plainView = null;
            }
        }

        public void DeMorph()
        {
            if (isMorphed)
            {
                plainView = (T[]) clusteredView;
                isMorphed = false;
                clusteredView = null;
            }
        }

        public void Alternate()
        {
            switch (isMorphed)
            {
                case true:
                    plainView = (T[]) clusteredView;
                    isMorphed = false;
                    clusteredView = null;
                    break;

                case false:
                    clusteredView = toClusteredArray(plainView);
                    isMorphed = true;
                    plainView = null;
                    break;
            }
        }

        public ClusteredArray<T> MorphedView
        {
            get { return isMorphed ? clusteredView : toClusteredArray(plainView); }
            set
            {
                if (isMorphed)
                    clusteredView = value;
                else
                    plainView = (T[]) value;
            }
        }

        public int Size
        {
            get { return isMorphed ? clusteredView.Length : plainView.Length; }
        }

        /// <summary>
        /// This property can control the cluster size of the Clustered View, before an object is morphed.
        /// It is only modifiable in the unmorphed state, and cannot be changed in the morphed state. 
        /// </summary>
        public int MorphedLengthThreshold
        {
            get { return morphedLengthThreshold; }
            set
            {
                if (!isMorphed)
                    morphedLengthThreshold = value;
            }
        }

        private ClusteredArray<T> toClusteredArray(T[] plainArray)
        {
            ClusteredArray<T> cc = new ClusteredArray<T>(morphedLengthThreshold, plainArray.Length);
            cc.CopyFrom(plainArray, 0, 0, plainArray.Length);
            return cc;
        } 

        public T this[int i]
        {
            get { return isMorphed ? clusteredView[i] : plainView[i]; }
            set
            {
                if (isMorphed)
                    clusteredView[i] = value;
                else
                    plainView[i] = value;
            }
        }

        public static explicit operator T[](Morphable<T> morphable)
        {
            return morphable.isMorphed ? (T[])morphable.clusteredView : morphable.plainView;
        }

        public static explicit operator Morphable<T>(T[] arr)
        {
            return arr != null ? new Morphable<T>(arr) : null;
        }

        public static explicit operator ClusteredArray<T>(Morphable<T> morphable)
        {
            return morphable.isMorphed ? morphable.clusteredView : morphable.toClusteredArray(morphable.plainView);
        }

        public static explicit operator Morphable<T>(ClusteredArray<T> arr)
        {

            return arr != null ? new Morphable<T>(arr) : null;
        }
        
        public int InMemorySize
        {
            get { return Size + MemoryUtil.NetOverHead; }
        }
    }
}
