using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the generated UI (Merge&amp;Munch/UI/Build Scene UI): HUD, pause + settings, game over,
/// leaderboard and main menu. A fresh launch and Restart drop straight into a run; only Home shows the menu.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Serializable]
    public class ToggleView
    {
        public Button button;
        public Image background, lip;
        public Image icon;
        public TMP_Text label;
        public Sprite onIcon, offIcon;
        public string onText, offText;
        public Color onBackground, offBackground, onLip, offLip, onContent, offContent;

        public void Show(bool on)
        {
            if (background != null) background.color = on ? onBackground : offBackground;
            if (lip != null) lip.color = on ? onLip : offLip;
            if (icon != null) { icon.sprite = on ? onIcon : offIcon; icon.color = on ? onContent : offContent; }
            if (label != null) { label.text = on ? onText : offText; label.color = on ? onContent : offContent; }
        }
    }

    [Serializable]
    public class RowView
    {
        public GameObject root;
        public Image badge;
        public TMP_Text rank, name, score;
    }

    [Header("Panels")]
    public GameObject hud;
    public GameObject pausePanel, gameOverPanel, leaderboardPanel, mainMenuPanel;
    public Transform fxLayer;

    [Header("HUD")]
    public Button pauseButton;
    public TMP_Text hudScore;
    public Transform hudScoreCard, hudBestChip;
    public TMP_Text toast;

    [Header("Pause")]
    public Button resumeButton;
    public Button pauseRestartButton, pauseHomeButton;
    public ToggleView pauseSound, pauseMusic;

    [Header("Game over")]
    public TMP_Text gameOverScore;
    public TMP_Text gameOverBest;
    public GameObject newBestChip;
    public GameObject continueButton;
    public Button playAgainButton, gameOverHomeButton, gameOverRankingButton;

    [Header("Leaderboard")]
    public Button leaderboardCloseButton;
    public RowView[] leaderboardRows;
    public RowView youRow;
    public TMP_Text leaderboardStatus;
    public GameObject signInRow;
    public Button signInButton;
    public Color[] medalColors;
    public Color plainBadgeColor;

    [Header("Main menu")]
    public Button playButton;
    public Button menuRankingButton;
    public TMP_Text menuBest;
    public ToggleView menuSound, menuMusic;

    [Header("Audio")]
    public AudioSource[] sfxSources;
    public AudioSource musicSource;
    public AudioSource clickSource;

    [Header("Juice")]
    public TMP_FontAsset uiFont;
    public Sprite confettiSprite;
    public Color[] confettiColors;

    public static UIManager Instance { get; private set; }
    // Floating scores attach here, above the HUD and under the menus.
    public static Transform FxLayer { get; private set; }
    public static TMP_FontAsset UiFont { get; private set; }

    enum UiScreen { Menu, Playing, Paused, GameOverWait, GameOver, Leaderboard }

    static bool openMenuOnLoad;
    UiScreen screen;
    UiScreen leaderboardReturn;
    bool runStarted, levelPaused, continueUsed, continueFlow, waitingForInterstitial, bannerShown;
    bool soundOn = true, musicOn = true;
    int leaderboardRequest;
    int shownMenuBest = -1;
    int shownScore;
    Tween scoreCount;

    PlayerController playerController;
    DepthOfField depthOfField;

    void Awake()
    {
        Instance = this;
        FxLayer = fxLayer;
        UiFont = uiFont;
        if (toast != null)
        {
            toast.outlineWidth = 0.3f;
            toast.outlineColor = new Color32(90, 58, 46, 255);
            toast.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        PostProcessVolume volume = FindObjectOfType<PostProcessVolume>();
        if (volume != null)
            volume.profile.TryGetSettings(out depthOfField);

        if (AdsManager.Instance == null)
            new GameObject("AdsManager").AddComponent<AdsManager>();
        AdsManager.Instance.OnInterstitialClosed += OnInterstitialClosed;
        AdsManager.Instance.OnInterstitialFailed += OnInterstitialClosed;
        PlaygamaService.ExternalPauseChanged += OnExternalPause;

        Wire();
        StartCoroutine(LoadAudioPrefs());

        SetScreen(openMenuOnLoad ? UiScreen.Menu : UiScreen.Playing);
        openMenuOnLoad = false;
        StartCoroutine(SignalGameReady());
    }

    void OnDestroy()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnInterstitialClosed -= OnInterstitialClosed;
            AdsManager.Instance.OnInterstitialFailed -= OnInterstitialClosed;
        }
        PlaygamaService.ExternalPauseChanged -= OnExternalPause;
        if (FxLayer == fxLayer)
            FxLayer = null;
        if (Instance == this)
            Instance = null;
    }

    void Wire()
    {
        pauseButton.onClick.AddListener(Pause);
        resumeButton.onClick.AddListener(Resume);
        pauseRestartButton.onClick.AddListener(Restart);
        pauseHomeButton.onClick.AddListener(MainMenuBtn);
        pauseSound.button.onClick.AddListener(ToggleSound);
        pauseMusic.button.onClick.AddListener(ToggleMusic);

        continueButton.GetComponentInChildren<Button>(true).onClick.AddListener(WatchAndContinue);
        playAgainButton.onClick.AddListener(Restart);
        gameOverHomeButton.onClick.AddListener(MainMenuBtn);
        gameOverRankingButton.onClick.AddListener(OpenLeaderboard);

        leaderboardCloseButton.onClick.AddListener(CloseLeaderboard);
        signInButton.onClick.AddListener(SignIn);

        playButton.onClick.AddListener(Play);
        menuRankingButton.onClick.AddListener(OpenLeaderboard);
        menuSound.button.onClick.AddListener(ToggleSound);
        menuMusic.button.onClick.AddListener(ToggleMusic);

        // Every button on the UI canvas clicks.
        foreach (Button button in hud.transform.parent.GetComponentsInChildren<Button>(true))
            button.onClick.AddListener(PlayClick);
    }

    void PlayClick()
    {
        if (clickSource == null || !soundOn)
            return;
        clickSource.pitch = UnityEngine.Random.Range(1.15f, 1.3f);
        clickSource.Play();
    }

    // ---------- juice ----------

    // HUD score counts up to the new total and the card bumps.
    public void AnimateScore(int score)
    {
        scoreCount?.Kill();
        scoreCount = DOTween.To(() => shownScore, v => { shownScore = v; hudScore.text = Format(v); }, score, 0.35f)
            .SetEase(Ease.OutCubic).SetUpdate(true);
        hudScoreCard.DOComplete();
        hudScoreCard.DOPunchScale(Vector3.one * 0.08f, 0.25f, 5, 0.6f).SetUpdate(true);
    }

    // First time in a run the live score passes the stored best.
    public void CelebrateBest()
    {
        ShowToast("NEW BEST!");
        hudBestChip.DOComplete();
        hudBestChip.DOPunchScale(Vector3.one * 0.25f, 0.5f, 6, 0.6f).SetUpdate(true);
        hudBestChip.DOPunchRotation(new Vector3(0, 0, 8f), 0.5f, 8, 0.6f).SetUpdate(true);
    }

    public void ShowToast(string text)
    {
        if (toast == null || screen != UiScreen.Playing)
            return;
        toast.text = text;
        toast.gameObject.SetActive(true);
        RectTransform rect = toast.rectTransform;
        rect.DOKill();
        toast.DOKill();
        toast.alpha = 1f;
        rect.localScale = Vector3.one * 0.3f;
        rect.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-6f, 6f));
        DOTween.Sequence()
            .Append(rect.DOScale(1.15f, 0.18f).SetEase(Ease.OutBack))
            .Append(rect.DOScale(1f, 0.12f))
            .AppendInterval(0.5f)
            .Append(toast.DOFade(0f, 0.3f))
            .Join(rect.DOScale(0.85f, 0.3f))
            .SetTarget(rect)
            .SetUpdate(true)
            .OnComplete(() => toast.gameObject.SetActive(false));
    }

    // Confetti burst in front of every panel, in the UI palette.
    void Celebrate()
    {
        if (confettiSprite == null || confettiColors == null || confettiColors.Length == 0)
            return;
        Transform canvas = hud.transform.parent;
        RectTransform canvasRect = (RectTransform)canvas;
        for (int i = 0; i < 46; i++)
        {
            var piece = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)piece.transform;
            rect.SetParent(canvas, false);
            rect.SetAsLastSibling();
            rect.sizeDelta = new Vector2(UnityEngine.Random.Range(9f, 14f), UnityEngine.Random.Range(14f, 24f));
            rect.anchoredPosition = new Vector2(0f, canvasRect.rect.height * 0.18f);
            rect.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));
            Image image = piece.GetComponent<Image>();
            image.sprite = confettiSprite;
            image.color = confettiColors[i % confettiColors.Length];
            image.raycastTarget = false;

            Vector2 burst = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(140f, 320f) + new Vector2(0f, 120f);
            float fall = UnityEngine.Random.Range(380f, 620f);
            float time = UnityEngine.Random.Range(1.3f, 1.9f);
            DOTween.Sequence()
                .Append(rect.DOAnchorPos(rect.anchoredPosition + burst, 0.45f).SetEase(Ease.OutCubic))
                .Append(rect.DOAnchorPosY(rect.anchoredPosition.y + burst.y - fall, time).SetEase(Ease.InQuad))
                .Join(rect.DOAnchorPosX(rect.anchoredPosition.x + burst.x + UnityEngine.Random.Range(-60f, 60f), time).SetEase(Ease.InOutSine))
                .Insert(0f, rect.DOLocalRotate(new Vector3(0, 0, UnityEngine.Random.Range(-720f, 720f)), 0.45f + time, RotateMode.LocalAxisAdd))
                .Insert(0.45f + time * 0.75f, image.DOFade(0f, time * 0.25f))
                .SetUpdate(true)
                .SetLink(piece)
                .OnComplete(() => Destroy(piece));
        }
    }

    IEnumerator SignalGameReady()
    {
        // The first playable frame must be on screen and accepting input, not just built.
        yield return null;
        yield return new WaitForEndOfFrame();
        PlaygamaService.SendGameReady();
    }

    void Update()
    {
        // The run ends inside GameManager; the HUD stays up while its end sequence plays.
        if (screen == UiScreen.Playing && GameManager.Instance != null && GameManager.Instance.IsGameOver)
            SetScreen(UiScreen.GameOverWait);

        // On web the stored best arrives from Bridge Storage a moment after the menu opened.
        if (screen == UiScreen.Menu && GameManager.Instance != null && GameManager.Instance.HighScore != shownMenuBest)
        {
            shownMenuBest = GameManager.Instance.HighScore;
            menuBest.text = Format(shownMenuBest);
        }
    }

    // ---------- screens ----------

    void SetScreen(UiScreen next)
    {
        screen = next;
        hud.SetActive(next != UiScreen.Menu && !(next == UiScreen.Leaderboard && leaderboardReturn == UiScreen.Menu));
        pauseButton.gameObject.SetActive(next == UiScreen.Playing);
        pausePanel.SetActive(next == UiScreen.Paused);
        gameOverPanel.SetActive(next == UiScreen.GameOver);
        leaderboardPanel.SetActive(next == UiScreen.Leaderboard);
        mainMenuPanel.SetActive(next == UiScreen.Menu);

        bool playing = next == UiScreen.Playing;
        if (playing && !runStarted)
        {
            runStarted = true;
            PlaygamaService.SendLevel(PlaygamaService.Level.Started);
        }

        if (playerController != null)
            playerController.enabled = playing;
        if (depthOfField != null)
            depthOfField.active = !playing && next != UiScreen.GameOverWait;

        // Mobile only: AdMob banner on menus, never over play.
        bool wantBanner = next == UiScreen.Menu || next == UiScreen.Paused || next == UiScreen.GameOver;
        if (wantBanner != bannerShown && AdsManager.Instance != null)
        {
            bannerShown = wantBanner;
            if (wantBanner) AdsManager.Instance.ShowBanner();
            else AdsManager.Instance.HideBanner();
        }

        if (next == UiScreen.GameOver)
            RefreshGameOver();

        bool canRank = LeaderboardService.CanOpen;
        gameOverRankingButton.transform.parent.gameObject.SetActive(canRank);
        menuRankingButton.gameObject.SetActive(canRank);

        ApplyTimeScale();
    }

    public void Pause()
    {
        if (screen == UiScreen.Playing)
            SetScreen(UiScreen.Paused);
    }

    public void Resume()
    {
        if (screen == UiScreen.Paused)
            SetScreen(UiScreen.Playing);
    }

    public void Play()
    {
        if (screen == UiScreen.Menu)
            SetScreen(UiScreen.Playing);
    }

    void OnExternalPause(bool external) => ApplyTimeScale();

    // The pause panel, a hidden tab or a full-screen ad freezes physics and the loss timer.
    void ApplyTimeScale()
    {
        bool paused = screen == UiScreen.Paused;
        Time.timeScale = paused || PlaygamaService.ExternalPause ? 0f : 1f;

        if (paused != levelPaused)
        {
            levelPaused = paused;
            PlaygamaService.SendLevel(paused ? PlaygamaService.Level.Paused : PlaygamaService.Level.Resumed);
        }
    }

    // ---------- game over ----------

    // Called by GameManager once the end sequence has finished.
    public void GameOverScene()
    {
        waitingForInterstitial = true;
        LeaderboardService.Submit(GameManager.Instance != null ? GameManager.Instance.HighScore : 0);

        bool shown = AdsManager.Instance != null && AdsManager.Instance.TryShowInterstitial(AdsManager.GameOverPlacement);
        if (!shown)
            OnInterstitialClosed();
    }

    void OnInterstitialClosed()
    {
        if (!waitingForInterstitial)
            return;
        waitingForInterstitial = false;
        SetScreen(UiScreen.GameOver);
    }

    void RefreshGameOver()
    {
        GameManager game = GameManager.Instance;
        int best = game != null ? game.HighScore : 0;
        gameOverBest.text = "BEST " + Format(best);
        bool newBest = game != null && game.NewBest;
        newBestChip.SetActive(newBest);

        // The final score counts up after the card lands; a new best gets a wobble and confetti.
        int final = game != null ? game.CurrentScore : 0;
        gameOverScore.DOKill();
        gameOverScore.text = "0";
        int counted = 0;
        DOTween.To(() => counted, v => { counted = v; gameOverScore.text = Format(v); }, final, 0.9f)
            .SetEase(Ease.OutCubic).SetDelay(0.25f).SetUpdate(true).SetTarget(gameOverScore)
            .OnComplete(() =>
            {
                gameOverScore.transform.DOPunchScale(Vector3.one * 0.2f, 0.35f, 6, 0.6f).SetUpdate(true);
                if (newBest)
                {
                    newBestChip.transform.DOPunchRotation(new Vector3(0, 0, 12f), 0.6f, 8, 0.5f).SetUpdate(true);
                    Celebrate();
                }
            });

        bool offered = !continueUsed && !continueFlow && AdsManager.Instance != null && AdsManager.Instance.RewardedSupported;
        continueButton.SetActive(offered);
    }

    // Game over screen: watch a rewarded ad, keep the score and play on with a cleared board.
    public void WatchAndContinue()
    {
        if (continueUsed || continueFlow || AdsManager.Instance == null || GameManager.Instance == null || !GameManager.Instance.IsGameOver)
            return;

        continueFlow = true;
        RefreshGameOver();
        AdsManager.Instance.ShowRewarded(AdsManager.ContinuePlacement, () =>
        {
            if (continueUsed)
                return;
            continueUsed = true;
            GameManager.Instance.ContinueRun();
            SetScreen(UiScreen.Playing);
        }, () =>
        {
            continueFlow = false;
            if (screen == UiScreen.GameOver)
                RefreshGameOver();
        });
    }

    // ---------- leaderboard ----------

    public void OpenLeaderboard()
    {
        if (LeaderboardService.Type == LeaderboardService.Kind.NativePopup)
        {
            LeaderboardService.ShowNativePopup();
            return;
        }
        if (LeaderboardService.Type != LeaderboardService.Kind.InGame || screen == UiScreen.Leaderboard)
            return;

        leaderboardReturn = screen;
        SetScreen(UiScreen.Leaderboard);
        LoadLeaderboard();
    }

    public void CloseLeaderboard()
    {
        if (screen == UiScreen.Leaderboard)
            SetScreen(leaderboardReturn);
    }

    void LoadLeaderboard()
    {
        foreach (RowView row in leaderboardRows)
            row.root.SetActive(false);
        int best = GameManager.Instance != null ? GameManager.Instance.HighScore : 0;
        ShowRow(youRow, 0, "YOU", best);
        leaderboardStatus.gameObject.SetActive(true);
        leaderboardStatus.text = "LOADING...";
        signInRow.SetActive(LeaderboardService.CanAuthorize);

        int request = ++leaderboardRequest;
        LeaderboardService.Fetch((success, entries) =>
        {
            if (request != leaderboardRequest || screen != UiScreen.Leaderboard)
                return;

            if (!success)
            {
                leaderboardStatus.text = "COULDN'T LOAD SCORES";
                return;
            }

            leaderboardStatus.gameObject.SetActive(entries.Count == 0);
            leaderboardStatus.text = "NO SCORES YET. BE THE FIRST!";

            string me = LeaderboardService.PlayerId;
            for (int i = 0; i < leaderboardRows.Length; i++)
            {
                if (i >= entries.Count)
                    break;
                LeaderboardService.Entry entry = entries[i];
                bool mine = !string.IsNullOrEmpty(me) && entry.id == me;
                string name = mine ? "YOU" : string.IsNullOrEmpty(entry.name) ? "PLAYER" : entry.name;
                ShowRow(leaderboardRows[i], entry.rank > 0 ? entry.rank : i + 1, name, entry.score);
                Transform row = leaderboardRows[i].root.transform;
                row.DOKill();
                row.localScale = new Vector3(0.6f, 0.6f, 1f);
                row.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(i * 0.05f).SetUpdate(true);
            }

            foreach (LeaderboardService.Entry entry in entries)
            {
                if (!string.IsNullOrEmpty(me) && entry.id == me)
                    ShowRow(youRow, entry.rank, "YOU", Mathf.Max(entry.score, best));
            }
            signInRow.SetActive(LeaderboardService.CanAuthorize);
        });
    }

    void ShowRow(RowView row, int rank, string name, int score)
    {
        row.root.SetActive(true);
        row.rank.text = rank > 0 ? rank.ToString(CultureInfo.InvariantCulture) : "-";
        row.name.text = name;
        row.score.text = Format(score);
        if (row != youRow)
            row.badge.color = rank >= 1 && rank <= medalColors.Length ? medalColors[rank - 1] : plainBadgeColor;
    }

    void SignIn()
    {
        signInButton.interactable = false;
        LeaderboardService.Authorize(success =>
        {
            signInButton.interactable = true;
            if (!success)
                return;
            LeaderboardService.Submit(GameManager.Instance != null ? GameManager.Instance.HighScore : 0);
            if (screen == UiScreen.Leaderboard)
                LoadLeaderboard();
        });
    }

    // ---------- audio ----------

    IEnumerator LoadAudioPrefs()
    {
        while (!GamePrefs.Ready)
            yield return null;
        soundOn = GamePrefs.GetInt("SoundOn", 1) == 1;
        musicOn = GamePrefs.GetInt("MusicOn", 1) == 1;
        ApplyAudio();
    }

    public void ToggleSound()
    {
        soundOn = !soundOn;
        GamePrefs.SetInt("SoundOn", soundOn ? 1 : 0);
        GamePrefs.Save();
        ApplyAudio();
    }

    public void ToggleMusic()
    {
        musicOn = !musicOn;
        GamePrefs.SetInt("MusicOn", musicOn ? 1 : 0);
        GamePrefs.Save();
        ApplyAudio();
    }

    void ApplyAudio()
    {
        foreach (AudioSource source in sfxSources)
        {
            if (source != null)
                source.mute = !soundOn;
        }
        if (clickSource != null)
            clickSource.mute = !soundOn;
        if (musicSource != null)
            musicSource.mute = !musicOn;

        pauseSound.Show(soundOn);
        menuSound.Show(soundOn);
        pauseMusic.Show(musicOn);
        menuMusic.Show(musicOn);
    }

    // ---------- scene ----------

    public void MainMenuBtn()
    {
        openMenuOnLoad = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void Restart()
    {
        openMenuOnLoad = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public static string Format(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
}
