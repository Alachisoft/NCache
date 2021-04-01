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
    public class RuntimeContext
    {
        private static RtContextValue? s_current;
              
        public static RtContextValue CurrentContext
        {
            get
            {
                if (s_current.HasValue) return s_current.Value;
                else return DefaultContext;
            }
            set
            {
                if (value == RtContextValue.NCACHE || value == RtContextValue.JVCACHE)
                    s_current = value;
            }

        }

        public static RtContextValue DefaultContext { get { return RtContextValue.NCACHE; } }

        static RuntimeContext()
        {
            SetToDefault();
        }

        public static void SetToDefault()
        {
            CurrentContext = DefaultContext;
        }

        public static string CurrentContextName
        {
            get
            {
                 return "NCache";
            }
        }    
    }
}
