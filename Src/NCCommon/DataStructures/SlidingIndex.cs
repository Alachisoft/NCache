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
using System.Collections.Generic;
using Alachisoft.NCache.Common.Stats;
using System.Collections;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class SlidingIndex<T>
    {
        const int MAX_OBSERVATION_INTERVAL = 300;
        const int MIN_OBSERVATION_INTERVAL = 1;

        private int _observationInterval = MIN_OBSERVATION_INTERVAL; //in seocnds;
        private long _entryIdSequence;
        private bool _checkDuplication;

        List<InstantaneousIndex<T>> _mainIndex = new List<InstantaneousIndex<T>>();

        public SlidingIndex(int interval)
            : this(interval, false)
        {
        }

        public SlidingIndex(int interval, bool checkDuplication)
        {
            if (interval > MIN_OBSERVATION_INTERVAL)
                _observationInterval = interval;
            _checkDuplication = checkDuplication;
            Clock.StartClock();
        }

        public int GetInterval()
        {
            return _observationInterval;
        }
        public bool AddToIndex(T indexValue)
        {
            lock (this)
            {
                long currentTime = Clock.CurrentTimeInSeconds;
                long entryId = _entryIdSequence++;
                InstantaneousIndex<T> indexEntry = null;

                if (_mainIndex.Count == 0)
                {
                    indexEntry = new InstantaneousIndex<T>();
                    indexEntry.EnableDuplicationCheck = _checkDuplication;
                    indexEntry.ClockTime = currentTime;
                    _mainIndex.Add(indexEntry);
                }
                else
                {
                    if (_checkDuplication && CheckForDuplication(indexValue)) return false;

                    ExpireOldEnteries(currentTime);

                    InstantaneousIndex<T> matchEntry = null;
                    foreach (InstantaneousIndex<T> entry in _mainIndex)
                    {
                        if (entry.ClockTime == currentTime)
                        {
                            matchEntry = entry;
                            break;
                        }
                    }

                    bool newEntry = false;
                    if (matchEntry != null)
                    {
                        indexEntry = matchEntry;
                    }
                    else
                    {
                        newEntry = true;
                        indexEntry = new InstantaneousIndex<T>();
                        indexEntry.EnableDuplicationCheck = _checkDuplication;
                        indexEntry.ClockTime = currentTime;
                        _mainIndex.Add(indexEntry);
                    }
                }
                indexEntry.AddEntry(entryId, indexValue);
            }
            return true;
        }

        private bool CheckForDuplication(T activity)
        {
            if (!_checkDuplication) return false;

            foreach (InstantaneousIndex<T> currentIndexEntry in _mainIndex)
            {
                if (currentIndexEntry.CheckDuplication(activity))
                    return true;
            }
            return false;
        }

        public IEnumerator<T> GetCurrentData()
        {
            lock (this)
            {
                IEnumerator<T> enumerator = new SlidingIndex<T>.Enumerator(this);
                return enumerator;
            }
        }

        public IEnumerator<T> GetCurrentData(ref long startTime)
        {
            lock (this)
            {
                IEnumerator<T> enumerator = new SlidingIndex<T>.Enumerator(this, ref startTime);
                return enumerator;
            }
        }
        private void ExpireOldEnteries(long currentTime)
        {
            lock (this)
            {

                List<InstantaneousIndex<T>> enteriesTobeRemoved = new List<InstantaneousIndex<T>>();
                foreach (InstantaneousIndex<T> currentIndexEntry in _mainIndex)
                {
                    long windowStartTime = currentTime - _observationInterval;
                    if (windowStartTime > 0 && currentIndexEntry.ClockTime < windowStartTime)
                    {
                        enteriesTobeRemoved.Add(currentIndexEntry);
                    }
                    else
                    {
                        break;
                    }
                }
                foreach (InstantaneousIndex<T> currentIndexEntry in enteriesTobeRemoved)
                {
                    _mainIndex.Remove(currentIndexEntry);
                }
            }
        }

        #region /                               ---- Enumerator ----                                    /

        class Enumerator : IEnumerator<T>
        {
            private List<InstantaneousIndex<T>> _index = new List<InstantaneousIndex<T>>();
            private List<InstantaneousIndex<T>>.Enumerator _enumerator;
            private IEnumerator<T> _subIndexEnumerator;
            private T _current;

            public Enumerator(SlidingIndex<T> slidingIndex)
            {
                foreach (InstantaneousIndex<T> indexEntry in slidingIndex._mainIndex)
                {
                    //for older enteries which are not supposed to change
                    if (indexEntry.ClockTime != Clock.CurrentTimeInSeconds)
                        _index.Add(indexEntry);
                    else
                    {
                        //index being modified currently
                        _index.Add(indexEntry.Clone() as InstantaneousIndex<T>);
                    }
                }

                _enumerator = _index.GetEnumerator();
            }


            public Enumerator(SlidingIndex<T> slidingIndex, ref long startTime)
            {
                long initialTime = startTime;
                foreach (InstantaneousIndex<T> indexEntry in slidingIndex._mainIndex)
                {
                    if (indexEntry.ClockTime > initialTime)
                    {
                        //for older enteries which are not supposed to change
                        if (indexEntry.ClockTime != Clock.CurrentTimeInSeconds)
                            _index.Add(indexEntry);
                        else
                        {
                            //index being modified currently
                            _index.Add(indexEntry.Clone() as InstantaneousIndex<T>);
                        }
                    }
                    startTime = indexEntry.ClockTime;
                }

                _enumerator = _index.GetEnumerator();
            }

            public T Current
            {
                get { return _current; }
            }

            public void Dispose()
            {

            }

            object System.Collections.IEnumerator.Current
            {
                get { return _current; }
            }

            public bool MoveNext()
            {
                do
                {
                    if (_subIndexEnumerator == null)
                    {
                        if (_enumerator.MoveNext())
                        {
                            InstantaneousIndex<T> indexEntry = _enumerator.Current;
                            _subIndexEnumerator = indexEntry.GetEnumerator();
                        }
                    }

                    if (_subIndexEnumerator != null)
                    {
                        if (_subIndexEnumerator.MoveNext())
                        {
                            _current = _subIndexEnumerator.Current;
                            return true;
                        }
                        else
                        {
                            _subIndexEnumerator = null;
                        }
                    }
                    else
                        return false;
                } while (true);
            }

            public void Reset()
            {
                _enumerator = _index.GetEnumerator();
                _subIndexEnumerator = null;
                _current = default(T);
            }
        }

        #endregion


        #region /                       ---- Instantaneous Index ---                            /

        class InstantaneousIndex<T> : ICloneable, IEnumerable<T>
        {
            private List<T> _activitesList = new List<T>();
            private Hashtable _table = new Hashtable();
            private bool _checkDuplication;
            private long _clockTime;
            private long _minActivityId;
            private long _maxActivityId;

            public long ClockTime
            {
                get
                {
                    return _clockTime;
                }
                set
                {

                    _clockTime = value;
                }
            }

            public long MinActivityId { get { return _minActivityId; } set { _minActivityId = value; } }

            public long MaxActivityId { get { return _maxActivityId; } set { _maxActivityId = value; } }

            public bool EnableDuplicationCheck { get { return _checkDuplication; } set { _checkDuplication = value; } }

            public void AddEntry(long entryId, T activity)
            {
                if (_checkDuplication) _table[activity] = entryId;

                _activitesList.Add(activity);
                if (_activitesList.Count == 1)
                    MinActivityId = entryId;

                MaxActivityId = entryId;
            }

            public bool CheckDuplication(T activity)
            {
                return _table.ContainsKey(activity);
            }

            public List<T> GetClientActivites(long lastEnteryId)
            {
                List<T> clientActivities = new List<T>();

                if (lastEnteryId < MinActivityId)
                {
                    return _activitesList;
                }
                else
                {
                    if (lastEnteryId < MaxActivityId)
                    {
                        int startingIndex = (int)(lastEnteryId - MinActivityId) + 1;
                        int length = (int)(MaxActivityId - lastEnteryId);
                        return _activitesList.GetRange(startingIndex, length);
                    }
                }
                return clientActivities;
            }

            public override bool Equals(object obj)
            {
                InstantaneousIndex<T> other = obj as InstantaneousIndex<T>;

                if (other != null && other.ClockTime == this.ClockTime)
                    return true;

                return false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public object Clone()
            {
                InstantaneousIndex<T> clone = new InstantaneousIndex<T>();
                clone._activitesList.AddRange(this._activitesList.GetRange(0, _activitesList.Count));
                return clone;
            }



            public IEnumerator<T> GetEnumerator()
            {
                return _activitesList.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _activitesList.GetEnumerator();
            }
        }

        #endregion
    }
}
