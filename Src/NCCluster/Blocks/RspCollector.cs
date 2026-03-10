
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