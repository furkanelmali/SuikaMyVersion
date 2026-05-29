using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    int Score, highScore, dailyHighScore;
    public TextMeshProUGUI scoreMesh, highScoreMesh, restartSceneScoreMesh;

    readonly List<GameObject> activeMergeObjects = new List<GameObject>();

    PlayerController playerController;
    UIManager uiManager;
    AudioSource mergeSoundSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        Score = 0;
        highScore = PlayerPrefs.GetInt("HighScore");
        dailyHighScore = PlayerPrefs.GetInt("DailyHighScore");
        highScoreMesh.text = highScore.ToString();

        playerController = FindObjectOfType<PlayerController>();
        uiManager = FindObjectOfType<UIManager>();

        GameObject mergeSoundsManager = GameObject.FindGameObjectWithTag("MergeSoundManager");
        if (mergeSoundsManager != null)
            mergeSoundSource = mergeSoundsManager.GetComponent<AudioSource>();
    }

    public void RegisterMergeObject(GameObject obj)
    {
        if (obj != null && !activeMergeObjects.Contains(obj))
            activeMergeObjects.Add(obj);
    }

    public void UnregisterMergeObject(GameObject obj)
    {
        activeMergeObjects.Remove(obj);
    }

    bool isGameOver;

    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        if (playerController != null)
            playerController.controller = false;

        DangerZoneFeedback.Instance?.Reset();
        CameraShake.Instance?.StopDangerShake();

        StartCoroutine(EndGameSequent());
    }

    public void AddScore(int scorePoint)
    {
        Score += scorePoint;
        scoreMesh.text = Score.ToString();
    }

    public int CurrentScore => Score;

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
            PlayerPrefs.SetInt("DailyHighScore", dailyHighScore);
        }
    }

    IEnumerator EndGameSequent()
    {
        var snapshot = new List<GameObject>(activeMergeObjects);

        foreach (GameObject currentObject in snapshot)
        {
            if (currentObject == null)
                continue;

            ObjectController objectController = currentObject.GetComponent<ObjectController>();
            if (objectController == null)
                continue;

            ParticleSystem particle = currentObject.GetComponentInChildren<ParticleSystem>();

            if (particle != null && !objectController.isDead)
            {
                particle.Play();
                if (mergeSoundSource != null)
                {
                    mergeSoundSource.pitch = Mathf.Lerp(1.1f, 0.85f, (float)objectController.rank / 11f);
                    mergeSoundSource.Play();
                }
                objectController.isDead = true;
                CameraShake.Instance?.ShakeGameOverPop();
            }

            yield return new WaitForSeconds(0.3f);

            if (currentObject != null)
            {
                AddScore(objectController.scorePoint);
                MergeJuice.Instance?.ShowFloatingScore(objectController.scorePoint, currentObject.transform.position);
                ReleaseOrDestroy(currentObject);
            }

            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.5f);
        SettingHighScore();
        SettingDailyHighScore();
        restartSceneScoreMesh.text = Score.ToString();

        if (HapticFeedback.IsEnabled)
            Handheld.Vibrate();

        if (uiManager != null)
            uiManager.GameOverScene();
        else
            FindObjectOfType<UIManager>()?.GameOverScene();
    }

    static void ReleaseOrDestroy(GameObject obj)
    {
        if (MergeObjectPool.Instance != null)
            MergeObjectPool.Instance.Release(obj);
        else
            Destroy(obj);
    }
}
