using System.Collections.Generic;
using System.Linq;
using Titipi.MocaLib.Runtime.Services;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Iap
{
    public static class IapCatalogSync
    {
        private static bool _iapInitialized;
        private static readonly HashSet<string> _registeredProductIds = new();
        private static readonly HashSet<string> _pendingProductIds = new();

        public static void MarkIapInitialized(List<(string productId, ProductType productType)> initialProducts)
        {
            _iapInitialized = true;

            if (initialProducts == null)
                return;

            foreach (var (productId, _) in initialProducts)
                _registeredProductIds.Add(productId);
        }

        public static void SyncNewProducts(List<(string productId, ProductType productType)> allProducts)
        {
            if (!_iapInitialized || allProducts == null)
                return;

            var newProducts = allProducts.FindAll(p => !_registeredProductIds.Contains(p.productId) && !_pendingProductIds.Contains(p.productId));
            if (newProducts.Count == 0)
                return;

            foreach (var group in newProducts.GroupBy(p => p.productType))
            {
                var productIds = group.Select(p => p.productId).ToArray();
                foreach (var id in productIds)
                    _pendingProductIds.Add(id);

                MocaLib.Instance.IAPManager.FetchAdditionalProducts(
                    () =>
                    {
                        foreach (var id in productIds)
                        {
                            _pendingProductIds.Remove(id);
                            _registeredProductIds.Add(id);
                        }
                        Debug.Log($"[IapCatalogSync] Fetched additional IAP products: {string.Join(", ", productIds)}");
                    },
                    (reason, message) =>
                    {
                        foreach (var id in productIds)
                            _pendingProductIds.Remove(id);
                        Debug.LogError($"[IapCatalogSync] Failed to fetch additional IAP products ({group.Key}): {reason} - {message}");
                    },
                    group.Key,
                    productIds);
            }
        }
    }
}
