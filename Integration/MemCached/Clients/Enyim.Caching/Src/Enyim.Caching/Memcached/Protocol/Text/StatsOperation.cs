// Copyright (c) 2015 Alachisoft
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
using System.Net;
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached.Results.Extensions;

namespace Enyim.Caching.Memcached.Protocol.Text
{
	public class StatsOperation : Operation, IStatsOperation
	{
		private static Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(StatsOperation));

		private string type;
		private Dictionary<string, string> result;

		public StatsOperation(string type)
		{
			this.type = type;
		}

		protected internal override IList<ArraySegment<byte>> GetBuffer()
		{
			var command = String.IsNullOrEmpty(this.type)
							? "stats" + TextSocketHelper.CommandTerminator
							: "stats " + this.type + TextSocketHelper.CommandTerminator;

			return TextSocketHelper.GetCommandBuffer(command);
		}

		protected internal override IOperationResult ReadResponse(PooledSocket socket)
		{
			var serverData = new Dictionary<string, string>();

			while (true)
			{
				string line = TextSocketHelper.ReadResponse(socket);

				// stat values are terminated by END
				if (String.Compare(line, "END", StringComparison.Ordinal) == 0)
					break;

				// expected response is STAT item_name item_value
				if (line.Length < 6 || String.Compare(line, 0, "STAT ", 0, 5, StringComparison.Ordinal) != 0)
				{
					if (log.IsWarnEnabled)
						log.Warn("Unknow response: " + line);

					continue;
				}

				// get the key&value
				string[] parts = line.Remove(0, 5).Split(' ');
				if (parts.Length != 2)
				{
					if (log.IsWarnEnabled)
						log.Warn("Unknow response: " + line);

					continue;
				}

				// store the stat item
				serverData[parts[0]] = parts[1];
			}

			this.result = serverData;

			return new TextOperationResult().Pass();
		}

		Dictionary<string, string> IStatsOperation.Result
		{
			get { return result; }
		}

		protected internal override bool ReadResponseAsync(PooledSocket socket, System.Action<bool> next)
		{
			throw new System.NotSupportedException();
		}
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk�, enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
