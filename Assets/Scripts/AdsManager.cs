using System;
using UnityEngine;
using GoogleMobileAds.Api;
#if UNITY_ANDROID
using GoogleMobileAds.Ump.Api;
#endif

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    public event Action OnInterstitialClosed;
    public event Action OnInterstitialFailed;

    const float InterstitialFallbackSeconds = 3f;

#if UNITY_ANDROID
    const string BannerUnitId = "ca-app-pub-8143324127173924/9651262375";
    const string InterstitialUnitId = "ca-app-pub-8143324127173924/6279256900";
#elif UNITY_IPHONE
    const string BannerUnitId = "ca-app-pub-3940256099942544/2934735716";
    const string InterstitialUnitId = "ca-app-pub-3940256099942544/4411468910";
#else
    const string BannerUnitId = "unused";
    const string InterstitialUnitId = "unused";
#endif

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    static readonly bool UseTestAds = true;
#else
    static readonly bool UseTestAds = false;
#endif

    const string AndroidTestBanner = "ca-app-pub-3940256099942544/6300978111";
    const string AndroidTestInterstitial = "ca-app-pub-3940256099942544/1033173712";

    bool sdkInitialized;
    bool bannerVisible;
    BannerView bannerView;
    InterstitialAd interstitialAd;
    float interstitialFallbackTimer;
    bool interstitialFinishNotified;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        MobileAds.RaiseAdEventsOnUnityMainThread = true;
    }

    void Start()
    {
        RequestConsentAndInitialize();
    }

    void Update()
    {
        if (interstitialFallbackTimer > 0f)
        {
            interstitialFallbackTimer -= Time.unscaledDeltaTime;
            if (interstitialFallbackTimer <= 0f)
                NotifyInterstitialFinished();
        }
    }

    void RequestConsentAndInitialize()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var request = new ConsentRequestParameters();
        ConsentInformation.Update(request, (FormError updateError) =>
        {
            if (updateError != null)
                Debug.LogWarning("[AdsManager] Consent update error: " + updateError.Message);

            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                if (formError != null)
                    Debug.LogWarning("[AdsManager] Consent form error: " + formError.Message);

                InitializeMobileAds();
            });
        });
#else
        InitializeMobileAds();
#endif
    }

    void InitializeMobileAds()
    {
        if (sdkInitialized)
            return;

        MobileAds.Initialize(_ =>
        {
            sdkInitialized = true;
            LoadInterstitial();
        });
    }

    string GetBannerUnitId() => UseTestAds ? AndroidTestBanner : BannerUnitId;
    string GetInterstitialUnitId() => UseTestAds ? AndroidTestInterstitial : InterstitialUnitId;

    public void ShowBanner()
    {
        if (!sdkInitialized)
            return;

        if (bannerView == null)
            CreateBannerView();

        if (bannerVisible)
            return;

        bannerView.LoadAd(new AdRequest());
        bannerVisible = true;
    }

    public void HideBanner()
    {
        if (bannerView == null)
            return;

        bannerView.Destroy();
        bannerView = null;
        bannerVisible = false;
    }

    void CreateBannerView()
    {
        bannerView = new BannerView(GetBannerUnitId(), AdSize.Banner, AdPosition.Bottom);
    }

    public void LoadInterstitial()
    {
        if (!sdkInitialized)
            return;

        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        InterstitialAd.Load(GetInterstitialUnitId(), new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("[AdsManager] Interstitial load failed: " + error);
                return;
            }

            interstitialAd = ad;
            RegisterInterstitialEvents(interstitialAd);
        });
    }

    void RegisterInterstitialEvents(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            LoadInterstitial();
            NotifyInterstitialFinished();
        };

        ad.OnAdFullScreenContentFailed += _ =>
        {
            LoadInterstitial();
            OnInterstitialFailed?.Invoke();
            NotifyInterstitialFinished();
        };
    }

    public bool TryShowInterstitial()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialFinishNotified = false;
            interstitialFallbackTimer = InterstitialFallbackSeconds;
            interstitialAd.Show();
            return true;
        }

        Debug.LogWarning("[AdsManager] Interstitial not ready.");
        OnInterstitialFailed?.Invoke();
        return false;
    }

    void NotifyInterstitialFinished()
    {
        if (interstitialFinishNotified)
            return;

        interstitialFinishNotified = true;
        interstitialFallbackTimer = 0f;
        OnInterstitialClosed?.Invoke();
    }
}
