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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Runtime.Caching
{
  

	/// <summary>
	/// Represents an string based identifier that can be associated with the cache items so that they are logically grouped 
	/// together and can be retrieved efficiently.
	/// </summary>
	/// <example>
	/// To create an instance of Tag class you can use code as follows:
	/// <code>
	/// Tag tag = new Tag("Alpha");
	/// </code>
	/// </example>
    public class Tag
	{
		private string _tag;

		/// <summary>
		/// Initializes a new instance of Tag class.
		/// </summary>
		/// <param name="tag"></param>
		public Tag(string tag)
		{
			_tag = tag;
		}

		/// <summary>
		/// Gets the string based tag name.
		/// </summary>
		public string TagName
		{
			get { return _tag; }
		}

		/// <summary>
		/// String representation of the tag class.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return _tag;
		}

        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType())
            {
                throw new ArgumentException("Type mismatch");
            }
            return this._tag.Equals(((Tag)obj)._tag); 
        }

        public override int GetHashCode()
        {
            return _tag.GetHashCode();
        }
	}

}