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
using System.Collections.Generic;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class XrefDictionary
    {
        Dictionary<XReference, long> _sourceToReference;
        Dictionary<long, XReference> _referenceToSource;
        long _referenceIndex;

        public ICollection<XReference> SourceIds
        {
            get
            {
                return _sourceToReference.Keys;
            }
        }

        public XrefDictionary()
        {
            _sourceToReference = new Dictionary<XReference, long>();
            _referenceToSource = new Dictionary<long, XReference>();
            _referenceIndex = -1;
        }

        public bool AddSource(XReference sourceReference)
        {
            if (_sourceToReference.ContainsKey(sourceReference))
                return false;

            _referenceIndex = GetValidReferenceIndex(_referenceIndex + 1);
            _sourceToReference.Add(sourceReference, _referenceIndex);
            _referenceToSource.Add(_referenceIndex, sourceReference);
            return true;
        }

        public long RemoveSource(XReference sourceReference)
        {
            long referenceId = -1;
            bool found = _sourceToReference.TryGetValue(sourceReference, out referenceId);
            if (found)
            {
                _sourceToReference.Remove(sourceReference);
                _referenceToSource.Remove(referenceId);
            }

            return referenceId;
        }

        public long GetFirstReferenceId(long startingIndex, out long requestId)
        {
            long referenceId = -1;
            long tries = 0;

            requestId = -1;

            while (_referenceToSource.Count > 0) //if there are any items in the dictionary, we'll stop at the first entry we find.
            {
                referenceId = GetValidReferenceIndex(startingIndex + tries);
                XReference sourceReference = new XReference(requestId);

                if (_referenceToSource.TryGetValue(referenceId, out sourceReference))
                {
                    requestId = sourceReference.SourceId;
                    break;
                }

                tries++;
            }

            return referenceId;
        }

        long GetValidReferenceIndex(long referenceIdex)
        {
            //index beyond Int64.Max (which is less than 0) should wrap around to zero
            referenceIdex = referenceIdex < 0 ? 0 : referenceIdex;
            return referenceIdex;
        }
    }
}

