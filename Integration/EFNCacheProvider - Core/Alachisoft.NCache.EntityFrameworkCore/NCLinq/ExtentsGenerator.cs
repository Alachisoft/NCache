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

using Alachisoft.NCache.EntityFrameworkCore.NCache;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Alachisoft.NCache.EntityFrameworkCore.NCLinq
{
    internal class ExtentsGenerator
    {
        private Dictionary<Expression, string> _extenets;
        private readonly string _extentPrefix = "Extent";
        private int _extentNum;

        public string ExtentPrefix => _extentPrefix;

        internal ExtentsGenerator()
        {
            _extenets = new Dictionary<Expression, string>();
            _extentNum = 0;
        }

        internal ExtentsGenerator(string extentPrefix)
            : this()
        {
            _extentPrefix = extentPrefix;
        }

        internal string GetExtent(Expression node)
        {
            Logger.Log(
                "Getting extent for node '" + node.ToString() + "'.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            lock (_extenets)
            {
                if (_extenets.ContainsKey(node))
                {
                    return _extenets[node];
                }
                else
                {
                    string extent = GetNextExtent();
                    _extenets.Add(node, extent);
                    return extent;
                }
            }
        }

        internal string GetNextExtent()
        {
            return _extentPrefix + ++_extentNum;
        }
    }
}
