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

namespace Alachisoft.NGroups
{
	/// <summary>
	/// Allows messages to be received from the Channel, used to pass messages
	/// to a 'Building Block'
	/// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	public interface UpHandler 
	{
		/// <summary>
		/// Receives an Event from the Channel
		/// </summary>
		/// <param name="evt">Event received</param>
		void up(Event evt);
	}
}
