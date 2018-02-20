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
// $Id: RspCollector.java,v 1.2 2004/03/30 06:47:12 belaban Exp $
using System;
using Message = Alachisoft.NGroups.Message;
using View = Alachisoft.NGroups.View;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups.Blocks
{
	

    public interface RspCollector
    {
        void receiveResponse(Message msg);
         void suspect(Address mbr);
        void viewChange(View new_view);
    }

}