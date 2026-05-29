using UnityEngine;

/// <summary>
/// Legacy wrapper — delegates to AdsManager.
/// </summary>
public class BannerAd : MonoBehaviour
{
    public void LoadAd()
    {
        if (AdsManager.Instance != null)
            AdsManager.Instance.ShowBanner();
    }

    public void DestroyAd()
    {
        if (AdsManager.Instance != null)
            AdsManager.Instance.HideBanner();
    }
}
