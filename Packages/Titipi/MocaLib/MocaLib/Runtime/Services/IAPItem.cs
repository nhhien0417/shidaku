using UnityEngine;
using UnityEngine.Purchasing;
using Titipi.MocaLib.Runtime.Common;

namespace Titipi.MocaLib.Runtime.Services
{
    public class IAPItem : ScriptableObject
    {
        private const string TAG = "IAPItem";

        public string ProductId;
        public ProductType ProductType;
        public string Name;
        public string Description;
        public float Price;
        public Sprite Image;

        public virtual void OnPurchaseCompleted()
        {
            Utils.MocaLibLog(TAG, $"OnPurchaseCompleted -> Purchase/restore completed for product: {ProductId}");
        }

        public virtual void OnPurchaseFailed(string error)
        {
            Utils.MocaLibLogError(TAG, $"OnPurchaseFailed -> Purchase failed for product: {ProductId} - Reason: {error}");
        }
    }
}
