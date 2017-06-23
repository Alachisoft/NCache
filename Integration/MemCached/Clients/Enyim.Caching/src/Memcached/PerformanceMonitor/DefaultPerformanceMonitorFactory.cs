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
	public class DefaultPerformanceMonitorFactory : IProviderFactory<IPerformanceMonitor>
	{
		private string name;

		internal DefaultPerformanceMonitorFactory() { }

		public DefaultPerformanceMonitorFactory(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Name must be specified.", "name");

			this.name = name;
		}

		void IProvider.Initialize(Dictionary<string, string> parameters)
		{
			if ((parameters != null
					&& (!parameters.TryGetValue("name", out this.name)
						|| String.IsNullOrEmpty(this.name)))
				|| (parameters == null && String.IsNullOrEmpty(this.name)))
				throw new ArgumentException("The DefaultPerformanceMonitor must have a name assigned. Use the name attribute in the configuration file.");
		}

		IPerformanceMonitor IProviderFactory<IPerformanceMonitor>.Create()
		{
			return new DefaultPerformanceMonitor(this.name);
		}
	}
}
