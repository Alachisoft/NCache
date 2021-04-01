using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Transactions
{
    public interface ITransactableStore
    {
        bool BeginTransaction();

        void CommitTransaction();

        void RollbackTransaction();
    }
}
