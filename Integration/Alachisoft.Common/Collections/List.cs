// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace Alachisoft.Common.Collections
{
    public class List
    {
        public static bool RemoveAll<T>(IList<T> list, Predicate<T> condition)
        {
            bool removed = false;
            for (int i = list.Count - 1; i >= 0; i--)
                if (condition(list[i]))
                {
                    list.RemoveAt(i);
                    removed = true;
                }
            return removed;
        }
    }
}
