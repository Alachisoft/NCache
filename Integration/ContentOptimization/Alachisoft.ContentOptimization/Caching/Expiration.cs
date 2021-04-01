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

namespace Alachisoft.ContentOptimization.Caching
{
    public enum ExpirationType
    {
        None,
        Absolute,
        Sliding
    }  

    public class Expiration
    {
        public static Expiration None = new Expiration() { ExpirationType = ExpirationType.None, Duration = 0 };

        public ExpirationType ExpirationType { get; set; }
        public int Duration { get; set; }

        public Expiration() { }

        public Expiration(ExpirationType type, int duration)
        {
            ExpirationType = type;
            Duration = duration;
        }
    }
}
