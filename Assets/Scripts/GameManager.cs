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
        StartCoroutine(LoadHighScores());

        playerController = FindObjectOfType<PlayerController>();
        uiManager = FindObjectOfType<UIManager>();

        GameObject mergeSoundsManager = GameObject.FindGameObjectWithTag("MergeSoundManager");
        if (mergeSoundsManager != null)
            mergeSoundSource = mergeSoundsManager.GetComponent<AudioSource>();
    }

    // On web the saved scores arrive from Bridge Storage a moment after play has already started.
    IEnumerator LoadHighScores()
    {
        while (!GamePrefs.Ready)
            yield return null;

        highScore = Mathf.Max(highScore, GamePrefs.GetInt("HighScore"));
        dailyHighScore = Mathf.Max(dailyHighScore, GamePrefs.GetInt("DailyHighScore"));
        highScoreMesh.text = UIManager.Format(Mathf.Max(highScore, Score));
        highScoreLoaded = true;
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

    public bool IsGameOver => isGameOver;
    bool bestCelebrated, highScoreLoaded;
    public int HighScore => highScore;
    // The run that just ended beat the stored best.
    public bool NewBest { get; private set; }

    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        if (playerController != null)
            playerController.controller = false;

        DangerZoneFeedback.Instance?.Reset();
        CameraShake.Instance?.StopDangerShake();

        PlaygamaService.SendLevel(PlaygamaService.Level.Failed);
        StartCoroutine(EndGameSequent());
    }

    // Rewarded continue: the board was already cleared by the end sequence, so play resumes
    // with the banked score and a fresh object in the drop box.
    public void ContinueRun()
    {
        if (!isGameOver)
            return;

        isGameOver = false;
        FindObjectOfType<TriggerLoss>()?.ResetLoss();
        FindObjectOfType<ObjectSpawner>()?.EnsureHeldObject();

        if (playerController != null)
            playerController.controller = true;

        PlaygamaService.SendLevel(PlaygamaService.Level.Started);
    }

    public void AddScore(int scorePoint)
    {
        Score += scorePoint;
        if (uiManager != null)
            uiManager.AnimateScore(Score);
        else
            scoreMesh.text = UIManager.Format(Score);

        // Passing the stored best shows live on the HUD; the first time in a run gets a celebration.
        if (Score > highScore)
        {
            highScoreMesh.text = UIManager.Format(Score);
            if (!bestCelebrated && highScoreLoaded && highScore > 0 && !isGameOver)
            {
                bestCelebrated = true;
                uiManager?.CelebrateBest();
            }
        }
    }

    public int CurrentScore => Score;

    public void SettingHighScore()
    {
        if (Score > highScore)
        {
            highScore = Score;
            NewBest = true;
            highScoreMesh.text = UIManager.Format(highScore);
            GamePrefs.SetInt("HighScore", highScore);
        }
    }

    public void SettingDailyHighScore()
    {
        if (Score > dailyHighScore)
        {
            dailyHighScore = Score;
            GamePrefs.SetInt("DailyHighScore", dailyHighScore);
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
        GamePrefs.Save();
        restartSceneScoreMesh.text = UIManager.Format(Score);

#if UNITY_ANDROID || UNITY_IOS
        if (HapticFeedback.IsEnabled)
            Handheld.Vibrate();
#endif

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
