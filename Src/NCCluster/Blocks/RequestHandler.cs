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
// $Id: RequestHandler.java,v 1.1.1.1 2003/09/09 01:24:08 belaban Exp $
using System;
using Message = Alachisoft.NGroups.Message;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups.Blocks
{
	
	
	public interface RequestHandler
	{
		object handle(Message msg);

        object handleNHopRequest(Message msg, out Address destination, out Message replicationMsg);
	}
}
