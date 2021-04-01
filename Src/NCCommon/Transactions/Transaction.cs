using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Transactions
{
    public class Transaction
    {
        IList<IRollbackOperation> _rollbackOperations = new List<IRollbackOperation>();
        bool _rollbackIsUnderProgress = false;
        public Transaction AddRollbackOperation(IRollbackOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if(!_rollbackIsUnderProgress)
            {
                _rollbackOperations.Add(operation);
            }
            return this;
        }

        public void Commit()
        {
            _rollbackOperations.Clear();
        }

        public void Rollback()
        {
            try
            {
                _rollbackIsUnderProgress = true;
                for (int i = _rollbackOperations.Count - 1; i >= 0; i--)
                {
                    _rollbackOperations[i].Execute();
                }
            }
            finally
            {
                _rollbackOperations.Clear();
                _rollbackIsUnderProgress = false;
            }
        }
    }
}
