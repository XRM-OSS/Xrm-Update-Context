using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Xrm.Oss.UnitOfWork
{
    public partial class UpdateContext<T> : IDisposable where T : Entity
    {
        public bool AddToTransaction(ExecuteTransactionRequest transaction)
        {
            if (transaction.Requests == null)
            {
                transaction.Requests = new OrganizationRequestCollection();
            }

            var updateRequest = GetUpdateRequest();

            if (updateRequest == null)
            {
                return false;
            }

            transaction.Requests.Add(updateRequest);
            return true;
        }
    }
}
