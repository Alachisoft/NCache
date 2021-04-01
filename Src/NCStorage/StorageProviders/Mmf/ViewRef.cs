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
namespace Alachisoft.NCache.Storage.Mmf
{
    internal class ViewRef : View
    {
        private int _refCount;

        public ViewRef(MmfFile mmf, uint id, uint size)
            : base(mmf, id, size)
        {
        }

        public new int Open()
        {
            lock (this)
            {
                if (!IsOpen)
                {
                    base.Open();
                }
                return ++_refCount;
            }
        }

        public new int Close()
        {
            lock (this)
            {
                if (IsOpen)
                {
                    _refCount--;
                    if (_refCount <= 0)
                    {
                        base.Close();
                    }
                }
                return _refCount;
            }
        }

        public void ForceClose()
        {
            lock (this)
            {
                if (IsOpen)
                {
                    base.Close();
                    _refCount = 0;
                }
            }
        }
    }
}