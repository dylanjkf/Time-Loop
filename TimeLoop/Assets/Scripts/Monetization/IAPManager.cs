using System;

namespace TimeLoop.Monetization
{
    /// <summary>
    /// App-facing in-app purchase API, backed by an in-memory mock "store" — there is no real
    /// billing here. The mock always succeeds instantly so the rest of the game (UI, unlock
    /// gating) can be built and tested without a live store connection.
    ///
    /// State ownership: this class does NOT read or write SaveData itself. The caller reads
    /// SaveData.IsPremiumUnlocked at startup and passes it into the constructor, and the caller
    /// must persist SaveData.IsPremiumUnlocked = true (and save to disk) when
    /// <see cref="OnPurchaseCompleted"/> fires. Keeping disk I/O out of this class keeps it easy
    /// to unit test and keeps the mock-vs-real-store swap localized to this one file.
    /// </summary>
    public sealed class IAPManager
    {
        /// <summary>
        /// Product identifier for the single non-consumable "remove ads + unlock all worlds"
        /// entitlement. This is the exact string that would be registered as a product ID in
        /// Unity IAP / the platform store console when wiring up the real SDK.
        /// </summary>
        public const string PremiumProductId = "com.timeloop.premium_unlock";

        public bool IsPremiumUnlocked { get; private set; }

        /// <summary>Fired after a successful purchase or a successful restore.</summary>
        public event Action OnPurchaseCompleted;

        /// <summary>Fired when a purchase or restore attempt fails; the string is a human-readable reason.</summary>
        public event Action<string> OnPurchaseFailed;

        /// <param name="startingIsPremiumUnlocked">
        /// The value of SaveData.IsPremiumUnlocked at load time, so entitlement state survives
        /// across app sessions even though this class itself never touches disk.
        /// </param>
        public IAPManager(bool startingIsPremiumUnlocked)
        {
            IsPremiumUnlocked = startingIsPremiumUnlocked;
        }

        /// <summary>
        /// Kicks off a purchase of <see cref="PremiumProductId"/>.
        ///
        /// MOCK IMPLEMENTATION: this succeeds immediately and unconditionally — there is no
        /// real store round-trip, no payment sheet, and no receipt validation.
        ///
        /// -------------------------------------------------------------------------------
        /// REAL-SDK INTEGRATION POINT: when wiring up Unity IAP, this method's body is where
        /// you would call `StoreController.InitiatePurchase(PremiumProductId)`, and the
        /// success path below (setting IsPremiumUnlocked + firing OnPurchaseCompleted) is what
        /// belongs inside your `IStoreListener.ProcessPurchase(PurchaseEventArgs args)`
        /// callback once the store confirms the purchase (and, ideally, after receipt
        /// validation). The failure path (OnPurchaseFailed) is what belongs inside
        /// `IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason reason)`.
        /// -------------------------------------------------------------------------------
        /// </summary>
        public void PurchasePremium()
        {
            IsPremiumUnlocked = true;
            OnPurchaseCompleted?.Invoke();
        }

        /// <summary>
        /// Restores a previous purchase of <see cref="PremiumProductId"/>.
        ///
        /// MOCK IMPLEMENTATION: since there is no real store, "restoring" just re-confirms
        /// whatever entitlement state this instance was constructed with (or already granted
        /// this session) — it does not contact any backend.
        ///
        /// REAL-SDK INTEGRATION POINT: this is where Unity IAP's restore-transactions flow
        /// (e.g. `IAppleExtensions.RestoreTransactions` on iOS, or re-querying product receipts
        /// on Android) would go, resolving to OnPurchaseCompleted / OnPurchaseFailed based on
        /// what the store actually reports.
        /// </summary>
        public void RestorePurchases()
        {
            if (IsPremiumUnlocked)
            {
                OnPurchaseCompleted?.Invoke();
            }
            else
            {
                OnPurchaseFailed?.Invoke("No previous purchase found.");
            }
        }
    }
}
