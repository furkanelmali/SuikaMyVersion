using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using System;

public class GameManager : MonoBehaviour
{
    int Score, highScore, dailyHighScore;
    public TextMeshProUGUI scoreMesh, highScoreMesh;
    public GameObject[] allInScene;

    
    // Start is called before the first frame update
    void Start()
    {
        Score = 0;
        highScore = PlayerPrefs.GetInt("HighScore");
        highScoreMesh.text = highScore.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        allInScene = GameObject.FindGameObjectsWithTag("MergeObject");
    }

    public void GameOver()
    {
        PlayerController.FindObjectOfType<PlayerController>().controller = false;
        StartCoroutine(EndGameSequent());
        
    }
    public void AddScore(int scorePoint)
    {
        Score = Score + scorePoint;
        scoreMesh.text = Score.ToString();
    }
    public void SettingHighScore()
    {
        if (Score > highScore)
        {
            highScore = Score;
            PlayerPrefs.SetInt("HighScore", highScore);
        }
    }
    public void SettingDailyHighScore() 
    {
        if (Score > dailyHighScore)
        {
            dailyHighScore = Score;
            PlayerPrefs.SetInt("DailyHighScore", highScore);
        }
    }
    IEnumerator EndGameSequent()
    {
        for (int i = 0; i < allInScene.Length; i++)
        {
            GameObject currentObject = allInScene[i];
            ParticleSystem particle = currentObject.GetComponentInChildren<ParticleSystem>();

            if (particle != null)
            {
              
                if (currentObject.GetComponent<ObjectController>().isDead == false)
                {
                    
                    particle.Play();
                    GameObject mergeSoundsManager = GameObject.FindGameObjectWithTag("MergeSoundManager");
                    mergeSoundsManager.GetComponent<AudioSource>().Play();
                    
                    currentObject.GetComponent<ObjectController>().isDead = true;

                    
                    
                }
                yield return new WaitForSeconds(.3f);
                AddScore(currentObject.GetComponent<ObjectController>().scorePoint);
                Destroy(currentObject);
            }

            
            yield return new WaitForSeconds(0.5f);

            
        }

        yield return new WaitForSeconds(0.5f);
        SettingHighScore();
        SettingDailyHighScore();
        SceneManager.LoadScene(0);
    }

    
}
