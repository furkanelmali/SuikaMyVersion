using UnityEngine;

/// <summary>
/// Legacy wrapper — delegates to AdsManager.
/// </summary>
public class InterstitialAdd : MonoBehaviour
{
    public void LoadInterstitialAd()
    {
        if (AdsManager.Instance != null)
            AdsManager.Instance.LoadInterstitial();
    }

    public void ShowInterstitialAd()
    {
        if (AdsManager.Instance != null)
            AdsManager.Instance.TryShowInterstitial();
    }
}
