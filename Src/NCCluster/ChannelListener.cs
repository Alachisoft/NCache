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
