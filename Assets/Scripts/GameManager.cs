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

    const float PopSeconds = 0.3f;
    const float EndPopWindow = 2.5f;
    const float EndPopMaxStep = 0.8f;

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

    // Includes the held piece; close enough to tell whether the board is filling up.
    public int PieceCount => activeMergeObjects.Count;

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

    // Rewarded cleanup: pops the smallest pieces already on the board. No score, so ads can't buy a leaderboard place.
    public void ClearSmallestPieces(int count)
    {
        var dropped = new List<ObjectController>();
        foreach (GameObject obj in activeMergeObjects)
        {
            if (obj == null || !obj.activeInHierarchy)
                continue;
            ObjectController controller = obj.GetComponent<ObjectController>();
            Rigidbody body = obj.GetComponent<Rigidbody>();
            if (controller != null && !controller.isDead && body != null && body.useGravity)
                dropped.Add(controller);
        }

        dropped.Sort((a, b) => a.rank.CompareTo(b.rank));
        if (dropped.Count > count)
            dropped.RemoveRange(count, dropped.Count - count);
        StartCoroutine(PopSequence(dropped));
    }

    IEnumerator PopSequence(List<ObjectController> pieces)
    {
        foreach (ObjectController piece in pieces)
        {
            if (piece != null && piece.gameObject.activeInHierarchy && !piece.isDead)
                StartCoroutine(PopPiece(piece, false));
            yield return new WaitForSeconds(0.06f);
        }
    }

    IEnumerator PopPiece(ObjectController piece, bool award)
    {
        ParticleSystem particle = piece.GetComponentInChildren<ParticleSystem>();
        if (particle != null)
            particle.Play();
        if (mergeSoundSource != null)
        {
            mergeSoundSource.pitch = Mathf.Lerp(1.1f, 0.85f, (float)piece.rank / 11f);
            mergeSoundSource.Play();
        }
        piece.isDead = true;
        if (award)
            CameraShake.Instance?.ShakeGameOverPop();

        yield return new WaitForSeconds(PopSeconds);

        // The pool clears isDead, so a piece that has already gone back is left alone.
        if (piece == null || !piece.isDead)
            yield break;

        if (award)
        {
            AddScore(piece.scorePoint);
            MergeJuice.Instance?.ShowFloatingScore(piece.scorePoint, piece.transform.position);
        }
        ReleaseOrDestroy(piece.gameObject);
    }

    IEnumerator EndGameSequent()
    {
        var snapshot = new List<GameObject>(activeMergeObjects);

        // A full board still finishes in about 3 s, so the player reaches the ad and the game over card.
        float step = Mathf.Min(EndPopMaxStep, EndPopWindow / Mathf.Max(1, snapshot.Count));

        foreach (GameObject currentObject in snapshot)
        {
            if (currentObject == null || !currentObject.activeInHierarchy)
                continue;

            ObjectController objectController = currentObject.GetComponent<ObjectController>();
            if (objectController == null || objectController.isDead)
                continue;

            StartCoroutine(PopPiece(objectController, true));
            yield return new WaitForSeconds(step);
        }

        // Longer than PopSeconds: every pop above has banked its score before the totals are saved.
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
