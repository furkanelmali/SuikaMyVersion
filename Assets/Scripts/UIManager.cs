using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject wChef, mChef;
    public GameObject MainMenu, ResetMenu, OptionsMenu, GameMenu, LeaderBoard, RestartMenu;
    PlayerController playerController;
    PostProcessVolume postProcessVolume;
    private DepthOfField depthOfField;
    BannerAd bannerAd;
    bool isThereABanner = false;
    InterstitialAdd interstitialAdd;
    bool waitingForInterstitial;

    public int ResetNum;

    void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        postProcessVolume = FindObjectOfType<PostProcessVolume>();
        bannerAd = FindObjectOfType<BannerAd>();
        interstitialAdd = FindObjectOfType<InterstitialAdd>();

        if (postProcessVolume != null)
            postProcessVolume.profile.TryGetSettings(out depthOfField);

        if (AdsManager.Instance == null)
        {
            GameObject adsObject = new GameObject("AdsManager");
            adsObject.AddComponent<AdsManager>();
        }

        AdsManager.Instance.OnInterstitialClosed += OnInterstitialClosed;
        AdsManager.Instance.OnInterstitialFailed += OnInterstitialClosed;

        if (PlayerPrefs.GetInt("ResetNum") == 0)
            MainMenu.SetActive(true);

        if (PlayerPrefs.GetInt("ResetNum") == 1)
        {
            ResetNum = 0;
            PlayerPrefs.SetInt("ResetNum", ResetNum);
            GameMenu.SetActive(true);
        }
    }

    void OnDestroy()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnInterstitialClosed -= OnInterstitialClosed;
            AdsManager.Instance.OnInterstitialFailed -= OnInterstitialClosed;
        }
    }

    void Update()
    {
        SettingTimeScale();
    }

    public void ChefChanger()
    {
        if (mChef.activeSelf)
        {
            mChef.SetActive(false);
            wChef.SetActive(true);
        }
        else
        {
            mChef.SetActive(true);
            wChef.SetActive(false);
        }
    }

    public void GameOverScene()
    {
        GameMenu.SetActive(false);
        RestartMenu.SetActive(false);
        waitingForInterstitial = true;

        bool shown = AdsManager.Instance != null && AdsManager.Instance.TryShowInterstitial();
        if (!shown)
            OnInterstitialClosed();
    }

    void OnInterstitialClosed()
    {
        if (!waitingForInterstitial)
            return;

        waitingForInterstitial = false;
        RestartMenu.SetActive(true);
    }

    public void SettingTimeScale()
    {
        if (GameMenu.activeSelf)
        {
            if (depthOfField != null)
                depthOfField.active = false;

            if (playerController != null)
                playerController.enabled = true;

            if (isThereABanner)
            {
                if (bannerAd != null)
                    bannerAd.DestroyAd();
                else if (AdsManager.Instance != null)
                    AdsManager.Instance.HideBanner();

                isThereABanner = false;
            }
        }
        else
        {
            if (depthOfField != null)
                depthOfField.active = true;

            if (playerController != null)
                playerController.enabled = false;

            if (!isThereABanner)
            {
                if (bannerAd != null)
                    bannerAd.LoadAd();
                else if (AdsManager.Instance != null)
                    AdsManager.Instance.ShowBanner();

                isThereABanner = true;
            }
        }
    }

    public void MainMenuBtn()
    {
        ResetNum = 0;
        PlayerPrefs.SetInt("ResetNum", ResetNum);
        SceneManager.LoadScene(0);
    }

    public void Restart()
    {
        ResetNum = 1;
        PlayerPrefs.SetInt("ResetNum", ResetNum);
        SceneManager.LoadScene(0);
    }
}
