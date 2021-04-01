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
using System.Text;

namespace Alachisoft.ContentOptimization
{
    [Serializable]
    public class ResourceMetadata: IComparable<ResourceMetadata>
    {
        public string Url { get; set; }
        public Dictionary<string, string> Options { get; private set; }

        public ResourceMetadata()
        {
            Options = new Dictionary<string, string>();
        }

        public static ResourceMetadata Parse(string data)
        {
            var resource = new ResourceMetadata();
            var tokens = data.Split('|');
            resource.Url = tokens[0];
            for (int i = 1; i < tokens.Length; i++)
            {
                string[] pair = tokens[i].Split('=');
                if (pair.Length > 1)
                    resource.Options.Add(pair[0], pair[1]);
            }
            return resource;
        }

        public override string ToString()
        {
            StringBuilder url = new StringBuilder();
            url.Append(this.Url);
            foreach (string key in Options.Keys)
                url.AppendFormat("|{0}={1}", key, Options[key]);
            return url.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            bool equals;
            if (obj is ResourceMetadata)
            {
                var res = (ResourceMetadata)obj;
                equals = this.Url == res.Url &&
                         this.Options.Count == res.Options.Count;
                if (equals)
                {
                    foreach (string key in this.Options.Keys)
                    {
                        equals &= this.Options[key] == res.Options[key];
                        if (!equals)
                            break;
                    }
                }
            }
            else
                equals = base.Equals(obj);

            return equals;
        }

        public override int GetHashCode()
        {
            return Url.GetHashCode();
        }

        #region IComparable<ResourceMetadata> Members

        public int CompareTo(ResourceMetadata other)
        {
            int result = this.ToString().CompareTo(other.ToString());
            return result;
        }

        #endregion
    }
}
