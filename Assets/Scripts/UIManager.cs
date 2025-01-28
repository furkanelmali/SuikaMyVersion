using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject wChef,mChef;
    public GameObject MainMenu, ResetMenu, OptionsMenu, GameMenu, LeaderBoard,RestartMenu;
    PlayerController playerController;
    PostProcessVolume postProcessVolume;
    private DepthOfField depthOfField;
    BannerAd bannerAd;
    bool isThereABanner = false;
    InterstitialAdd InterstitialAdd;

    public int ResetNum;
    

    // Start is called before the first frame update
    void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        postProcessVolume = FindObjectOfType<PostProcessVolume>();
        bannerAd = FindObjectOfType<BannerAd>();
        InterstitialAdd = FindObjectOfType<InterstitialAdd>();
        postProcessVolume.profile.TryGetSettings(out depthOfField);
        if(PlayerPrefs.GetInt("ResetNum") == 0) 
        {
            MainMenu.SetActive(true);
        }
        if (PlayerPrefs.GetInt("ResetNum") == 1) 
        {
            ResetNum = 0;
            PlayerPrefs.SetInt("ResetNum", ResetNum);
            GameMenu.SetActive(true);        
        }
    }

    // Update is called once per frame
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
        InterstitialAdd.ShowInterstitialAd();
        GameMenu.SetActive(false);
        RestartMenu.SetActive(true);
    }
    public void SettingTimeScale() 
    {
        if (GameMenu.activeSelf) 
        {
            depthOfField.active = false;
            playerController.enabled = true;
            if (isThereABanner)
            {
                bannerAd.DestroyAd();
                isThereABanner = false;
            }
        }
        else 
        {
            depthOfField.active = true;
            playerController.enabled = false;
            if (!isThereABanner)
            {
                bannerAd.LoadAd();
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
