using System;
using UnityEngine.Purchasing;

namespace Iap
{
    public interface IPurchaseVerificator
    {
        public void ValidateAndLogPurchase(Product product, Action<bool> onValidated, Action<string> onError);
    }
}