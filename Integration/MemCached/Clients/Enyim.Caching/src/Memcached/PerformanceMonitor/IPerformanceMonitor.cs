// Copyright (c) 2017 Alachisoft
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
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Enyim.Caching.Memcached
{
	public interface IPerformanceMonitor : IDisposable
	{
		void Get(int amount, bool success);
		void Store(StoreMode mode, int amount, bool success);
		void Delete(int amount, bool success);
		void Mutate(MutationMode mode, int amount, bool success);
		void Concatenate(ConcatenationMode mode, int amount, bool success);
	}
}
