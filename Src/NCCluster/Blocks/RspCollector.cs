// $Id: RspCollector.java,v 1.2 2004/03/30 06:47:12 belaban Exp $
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