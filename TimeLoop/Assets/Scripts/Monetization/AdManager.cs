using System;

namespace TimeLoop.Monetization
{
    /// <summary>
    /// App-facing interstitial ad API, backed by a mock that never actually shows an ad — there
    /// is no ad mediation SDK wired in here. It exists so the rest of the game can call a stable
    /// API for "show an interstitial here" without depending on a real ad network yet.
    ///
    /// IMPORTANT PLACEMENT RULE (enforced by callers, not by this class): interstitials must
    /// ONLY ever be shown between campaign level completions (e.g. on the post-level results
    /// screen, before returning to the world map) — never mid-puzzle, and never during Daily
    /// Challenge or Infinite Mode. This class has no way to know what screen the caller is on,
    /// so it trusts the caller to only invoke <see cref="ShowInterstitial"/> at those moments.
    /// </summary>
    public sealed class AdManager
    {
        /// <summary>
        /// When true (premium unlocked, or an explicit "remove ads" purchase), callers should
        /// avoid invoking <see cref="ShowInterstitial"/> at all. This class does not enforce
        /// that itself — it only tracks the flag for callers to check.
        /// </summary>
        public bool AdsRemoved { get; set; }

        /// <summary>
        /// Requests an interstitial ad be shown, then invokes <paramref name="onComplete"/>.
        ///
        /// MOCK IMPLEMENTATION: there is no real ad SDK here, so this calls
        /// <paramref name="onComplete"/> immediately and synchronously in every case (no ad is
        /// actually displayed, no fill/no-fill distinction, no reward). Callers should treat the
        /// callback exactly as they would a real "ad finished or was skipped/failed" signal, and
        /// should keep gameplay flow working correctly even though nothing visibly happens here.
        ///
        /// -------------------------------------------------------------------------------
        /// REAL-SDK INTEGRATION POINT: this exact call site is where a mediation SDK's
        /// "load and show interstitial, then invoke completion callback" would go — e.g.
        /// something like `MediationSdk.LoadInterstitial(...)` followed by showing it and
        /// calling `onComplete()` from the ad-closed/ad-failed-to-show delegate, instead of
        /// calling it synchronously as done below.
        /// -------------------------------------------------------------------------------
        ///
        /// CALLER CONTRACT: only call this between campaign level completions (e.g. the
        /// post-level results screen). Never call it mid-puzzle, and never during Daily
        /// Challenge or Infinite Mode — this class does not and cannot enforce that; it is the
        /// caller's responsibility to only invoke it at the right moments.
        /// </summary>
        public void ShowInterstitial(Action onComplete)
        {
            // MOCK: no ad SDK to load/show, so we just complete immediately in all cases.
            onComplete?.Invoke();
        }
    }
}
