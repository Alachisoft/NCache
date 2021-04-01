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
namespace Alachisoft.NCache.Caching
{
    public class TagComparisonTypeConverter
    {
        public static string ToString(TagComparisonType comparisonType)
        {
            switch (comparisonType)
            {
                case TagComparisonType.BY_TAG:
                    return "1";

                case TagComparisonType.ALL_MATCHING_TAGS:
                    return "2";

                case TagComparisonType.ANY_MATCHING_TAG:
                    return "3";
            }
            return string.Empty;
        }

        public static TagComparisonType FromString(string str)
        {
            switch (str)
            {
                case "1":
                    return TagComparisonType.BY_TAG;

                case "2":
                    return TagComparisonType.ALL_MATCHING_TAGS;

                case "3":
                    return TagComparisonType.ANY_MATCHING_TAG;
            }
            return TagComparisonType.DEFAULT;
        }
    }
}