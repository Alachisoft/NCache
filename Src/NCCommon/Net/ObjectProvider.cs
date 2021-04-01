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
using System.Collections;

namespace Alachisoft.NCache.Common
{
    public abstract class ObjectProvider
    {
        private Hashtable _available = new Hashtable();
        private Hashtable _rented = new Hashtable();
        private ArrayList _availableRentIds = new ArrayList();
        [CLSCompliant(false)]
        protected int _initialSize = 30;
        [CLSCompliant(false)]
        protected Type _objectType;
        private object _sync = new object();
        private int _rentid = 1;

        private ArrayList list = new ArrayList();

        public ObjectProvider() 
        {
            Initialize();
        }
        public ObjectProvider(int initialSize)
        {
            _initialSize = initialSize;
            Initialize();
        }
        public void Initialize()
        {
            IRentableObject obj = null;

            lock (_sync)
            {
                for (int i = 0; i < _initialSize; i++)
                {
                    obj = CreateObject();
                    if (obj != null)
                    {
                        ResetObject(obj);
                        list.Add(obj);
                    }
                }
            }
        }
     
        public IRentableObject RentAnObject()
        {
            IRentableObject obj  = null;
            lock (_sync)
            {
                if (_available.Count > 0)
                {
                    obj = (IRentableObject)_available[_availableRentIds[0]];
                    _available.Remove(obj.RentId);
                    _availableRentIds.Remove(obj.RentId);
                    _rented.Add(obj.RentId, obj);
                }
                else
                {
                    obj = (IRentableObject)CreateObject();
                    obj.RentId = _rentid++;
                    if (obj != null)
                    {
                        _rented.Add(obj.RentId,obj);
                    }
                }
            }
           
            return obj;
        }

        public void SubmittObject(IRentableObject obj)
        {
            lock (_sync)
            {
                {
                    if (_rented.Contains(obj.RentId))
                    {
                        _rented.Remove(obj.RentId);
                        ResetObject(obj);
                        _available.Add(obj.RentId,obj);
                        _availableRentIds.Add(obj.RentId);
                    }
                }
            }
        }

        protected abstract IRentableObject CreateObject();
        protected abstract void ResetObject(object obj);
        public abstract Type ObjectType { get;}
        public abstract string Name { get; }
        public int TotalObjects
        {
            get
            {
                return _rented.Count + _available.Count;
            }
        }

        public int AvailableCount
        {
            get
            {
                return _available.Count;
            }
        }

        public int RentCount
        {
            get
            {
                return _rented.Count;
            }
        }

        public int InitialSize
        {
            get
            {
                return _initialSize;
            }
            set
            {
                _initialSize = value;
            }
        }
    }
}