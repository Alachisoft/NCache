using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Client
{
    internal enum WriteResult
    {
        QueueEmptyAfterWrite,
        NothingToDo,
        MoreWork,
        CompetingWriter,
        NoConnection,
    }
}
