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
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups
{
	/// <summary>
	/// Used to listen for connection changes in the Channel
	/// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	public interface ChannelListener 
	{
		/// <summary>
		/// Channel Connected Event
		/// </summary>
		/// <param name="channel">Channel that was connected</param>
		void channelConnected(Channel channel);

		/// <summary>
		/// Channel Disconnected Event
		/// </summary>
		/// <param name="channel">Channel that was disconnected</param>
		void channelDisconnected(Channel channel);

		/// <summary>
		/// Channel Closed Event
		/// </summary>
		/// <param name="channel">Channel that was closed</param>
		void channelClosed(Channel channel);

		/// <summary>
		/// Channel Shunned Event
		/// </summary>
		void channelShunned();

		/// <summary>
		/// Channel Reconnected Event 
		/// </summary>
		/// <param name="addr">Channel that was reconnected</param>
		void channelReconnected(Address addr);
	}
}
