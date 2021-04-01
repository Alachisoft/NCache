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
namespace Alachisoft.NCache.Common
{
    public class RecepientInfo
    {
        private readonly string _id;
        private  bool _isAssigned;

        public RecepientInfo(string recepientId)
        {
            _id = recepientId;
        }

        public bool IsAssigned
        {
            get { return _isAssigned; }
            set { _isAssigned = value; }
        }

        public string Id
        {
            get { return _id; }
        }

        public override bool Equals(object obj)
        {
            return obj as string != null && ((string)obj).Equals(_id);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }
    }
}
