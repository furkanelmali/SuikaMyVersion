using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject wChef,mChef;
    public GameObject MainMenu, ResetMenu, OptionsMenu, GameMenu, LeaderBoard;
    PlayerController playerController;
    PostProcessVolume postProcessVolume;
    private DepthOfField depthOfField;

    // Start is called before the first frame update
    void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        postProcessVolume = FindObjectOfType<PostProcessVolume>();
        postProcessVolume.profile.TryGetSettings(out depthOfField);
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
    public void RestartScene() 
    {
        SceneManager.LoadScene(0);
    }
    public void SettingTimeScale() 
    {
        if (GameMenu.activeSelf) 
        {
            depthOfField.active = false;
            playerController.enabled = true;
        }
        else 
        {
            depthOfField.active = true;
            playerController.enabled = false;
        }
    }
}
