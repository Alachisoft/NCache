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
    public class BitSetConstants
    {
        public const int Flattened = 1;
        public const int Compressed = 2;
        public const int WriteThru = 4;
        public const int WriteBehind = 8;
        // readthru is only there for backward compatibility. one can use this bit.
        public const int ReadThru = 16;
        public const int JsonData = 32;
        public const int LockedItem = 64;
        public const int BinaryData = 128;
        
    }
}