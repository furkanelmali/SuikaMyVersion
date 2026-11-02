using System;
using System.Collections;
using UnityEngine;
#if UNITY_WEBGL
using Playgama;
using Playgama.Modules.Advertisement;
#else
using GoogleMobileAds.Api;
#endif
#if UNITY_ANDROID
using GoogleMobileAds.Ump.Api;
#endif

/// <summary>
/// Single ad entry for the game. Android/iOS use AdMob (banner + interstitial);
/// WebGL uses Playgama Bridge (interstitial + rewarded, no banners).
/// </summary>
public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    // Placement ids; the web ones are declared in Assets/WebGLTemplates/Bridge/playgama-bridge-config.json.
    public const string GameOverPlacement = "game_over";
    public const string ContinuePlacement = "continue";

    public event Action OnInterstitialClosed;
    public event Action OnInterstitialFailed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
#if !UNITY_WEBGL
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
#endif
    }

#if UNITY_WEBGL
    // Bridge owns frequency (initial delay, minimum interval); the game never counts or times ads itself.
    const float StartGrace = 0.6f;
    const float LoadLimit = 8f;

    bool busy;

    public bool RewardedSupported => Bridge.advertisement.isRewardedSupported;
    public bool Busy => busy;

    public void ShowBanner() { }
    public void HideBanner() { }
    public void LoadInterstitial() { }

    // Returns false when no request was made; otherwise OnInterstitialClosed fires exactly once, shown or not.
    public bool TryShowInterstitial(string placement = GameOverPlacement)
    {
        if (busy || !Bridge.advertisement.isInterstitialSupported)
        {
            OnInterstitialFailed?.Invoke();
            return false;
        }

        StartCoroutine(ShowInterstitialRoutine(placement));
        return true;
    }

    IEnumerator ShowInterstitialRoutine(string placement)
    {
        busy = true;
        bool started = false, opened = false, finished = false;

        void Changed(InterstitialState state)
        {
            started = true;
            if (state == InterstitialState.Opened)
            {
                opened = true;
                PlaygamaService.SetAdOpen(true);
            }
            else if (state == InterstitialState.Closed || state == InterstitialState.Failed)
            {
                finished = true;
            }
        }

        Bridge.advertisement.interstitialStateChanged += Changed;
        Bridge.advertisement.ShowInterstitial(placement);
        float begun = Time.realtimeSinceStartup;

        // No state at all means Bridge skipped this request (frequency rules); a stuck load is abandoned.
        while (!finished)
        {
            float waited = Time.realtimeSinceStartup - begun;
            if (!started && waited > StartGrace)
                break;
            if (!opened && waited > LoadLimit)
                break;
            yield return null;
        }

        Bridge.advertisement.interstitialStateChanged -= Changed;
        PlaygamaService.SetAdOpen(false);
        busy = false;
        OnInterstitialClosed?.Invoke();
    }

    // onRewarded fires at most once, only after Bridge reports a completed view; onDone always fires once.
    public void ShowRewarded(string placement, Action onRewarded, Action onDone)
    {
        if (busy || !RewardedSupported)
        {
            onDone?.Invoke();
            return;
        }

        StartCoroutine(ShowRewardedRoutine(placement, onRewarded, onDone));
    }

    IEnumerator ShowRewardedRoutine(string placement, Action onRewarded, Action onDone)
    {
        busy = true;
        bool opened = false, finished = false, granted = false;

        void Changed(RewardedState state)
        {
            if (state == RewardedState.Opened)
            {
                opened = true;
                PlaygamaService.SetAdOpen(true);
            }
            else if (state == RewardedState.Rewarded)
            {
                granted = true;
            }
            else if (state == RewardedState.Closed || state == RewardedState.Failed)
            {
                finished = true;
            }
        }

        Bridge.advertisement.rewardedStateChanged += Changed;
        Bridge.advertisement.ShowRewarded(placement);
        float begun = Time.realtimeSinceStartup;

        while (!finished && (opened || Time.realtimeSinceStartup - begun < LoadLimit))
            yield return null;

        Bridge.advertisement.rewardedStateChanged -= Changed;
        PlaygamaService.SetAdOpen(false);
        busy = false;

        // The reward is applied after the ad closed, so gameplay never resumes behind the ad.
        if (granted)
            onRewarded?.Invoke();
        onDone?.Invoke();
    }
#else
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

    // Mobile builds have no rewarded unit; the continue offer stays hidden there.
    public bool RewardedSupported => false;
    public bool Busy => false;

    public void ShowRewarded(string placement, Action onRewarded, Action onDone)
    {
        onDone?.Invoke();
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

    public bool TryShowInterstitial(string placement = GameOverPlacement)
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
#endif
}
