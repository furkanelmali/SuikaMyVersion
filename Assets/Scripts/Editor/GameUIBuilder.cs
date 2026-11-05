using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the game UI from the "Merge &amp; Munch UI" design canvas into SampleScene: HUD, pause + settings,
/// game over, leaderboard and main menu. Deletes the previous overlay canvas. Units are the design's px on a
/// 390x844 reference screen. Safe to run again: the "UI" canvas is rebuilt from scratch.
/// </summary>
public static class GameUIBuilder
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string FontPath = "Assets/GUIPackCartoon/Demo/Fonts/LilitaOne - Regular SDF.asset";
    static readonly Vector2 Reference = new Vector2(390, 844);

    // Palette (design canvas).
    static readonly Color Cream = Hex("FFF7EC"), CreamLip = Hex("E6C9A6");
    static readonly Color Cocoa = Hex("5A3A2E"), CocoaSoft = Hex("8A6A5C"), CocoaBody = Hex("6E5044");
    static readonly Color Coral = Hex("EE6A4C"), CoralLip = Hex("C24E35");
    static readonly Color Mint = Hex("2FA387"), MintLip = Hex("1F7C66");
    static readonly Color Butter = Hex("FFC94D"), ButterLip = Hex("D9A22E");
    static readonly Color OffTile = Hex("E9DDD0"), OffTileLip = Hex("CDBBA8");
    static readonly Color RowA = Hex("FBEBDA"), RowB = Hex("F7E4CF"), YouRow = Hex("FFE7A3");
    static readonly Color Dim = new Color(90 / 255f, 58 / 255f, 46 / 255f, 0.45f);
    static readonly Color MenuDim = new Color(90 / 255f, 58 / 255f, 46 / 255f, 0.30f);

    static TMP_FontAsset font;

    [MenuItem("Merge&Munch/UI/Build Scene UI")]
    public static void Build()
    {
        UISpriteFactory.GenerateAll();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            throw new System.InvalidOperationException("Missing font " + FontPath);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        UIManager ui = Object.FindObjectOfType<UIManager>(true);
        GameManager game = Object.FindObjectOfType<GameManager>(true);
        ObjectSpawner spawner = Object.FindObjectOfType<ObjectSpawner>(true);
        if (ui == null || game == null || spawner == null)
            throw new System.InvalidOperationException("UIManager, GameManager or ObjectSpawner missing in " + ScenePath);

        // Keep the food previews (in spawner order) before the old canvas goes.
        var nextSprites = new List<Sprite>();
        foreach (GameObject preview in spawner.nextObjectImages)
            nextSprites.Add(preview != null && preview.GetComponent<Image>() != null ? preview.GetComponent<Image>().sprite : null);

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas != null && (canvas.renderMode == RenderMode.ScreenSpaceOverlay || root.name == "UI"))
                Object.DestroyImmediate(root);
        }

        // ---------- canvas ----------
        var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasAspectMatch));
        var uiCanvas = canvasGo.GetComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 10;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = Reference;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        Transform root2 = canvasGo.transform;

        // ---------- HUD ----------
        RectTransform hud = Stretch("HUD", root2);

        RectTransform scoreCard = Lipped("ScoreCard", hud, Corner.TopLeft, new Vector2(16, 18), new Vector2(150, 68), Cream, CreamLip, 5, 22);
        Label(scoreCard, "Label", "SCORE", 13, CocoaSoft, new Vector2(20, 6), new Vector2(120, 18), TextAlignmentOptions.TopLeft, 1.5f);
        TMP_Text score = Label(scoreCard, "Value", "0", 40, Cocoa, new Vector2(18, 20), new Vector2(124, 44), TextAlignmentOptions.TopLeft);
        AutoSize(score, 22, 40);

        RectTransform bestChip = Lipped("BestChip", hud, Corner.TopLeft, new Vector2(16, 99), new Vector2(150, 30), Butter, ButterLip, 3, 15);
        Icon(bestChip, "icon_crown", Cocoa, new Vector2(10, 6), 18);
        Label(bestChip, "Label", "BEST", 16, Cocoa, new Vector2(33, 4), new Vector2(44, 22), TextAlignmentOptions.TopLeft);
        TMP_Text hudBest = Label(bestChip, "Value", "0", 16, Cocoa, new Vector2(76, 4), new Vector2(68, 22), TextAlignmentOptions.TopLeft);
        AutoSize(hudBest, 11, 16);

        CenterPivot(scoreCard);
        CenterPivot(bestChip);
        SlideIn(scoreCard, new Vector2(-240, 0), 0.05f);
        SlideIn(bestChip, new Vector2(-240, 0), 0.14f);

        RectTransform pause = Lipped("PauseButton", hud, Corner.TopRight, new Vector2(16, 18), new Vector2(56, 56), Coral, CoralLip, 5, 0, circle: true);
        Icon(pause, "icon_pause", Color.white, new Vector2(15, 15), 26);
        Button pauseButton = MakeButton(pause);

        RectTransform next = Lipped("NextBubble", hud, Corner.TopRight, new Vector2(82, 18), new Vector2(76, 76), Cream, CreamLip, 5, 0, circle: true);
        Label(next, "Label", "NEXT", 11, CocoaSoft, new Vector2(0, 7), new Vector2(76, 14), TextAlignmentOptions.Top, 1.5f);
        var previews = new GameObject[nextSprites.Count];
        for (int i = 0; i < nextSprites.Count; i++)
        {
            RectTransform p = Rect("Next" + i, next, Corner.TopLeft, new Vector2(16, 22), new Vector2(44, 46));
            Image image = p.gameObject.AddComponent<Image>();
            image.sprite = nextSprites[i];
            image.preserveAspect = true;
            image.raycastTarget = false;
            p.gameObject.SetActive(i == 0);
            previews[i] = p.gameObject;
        }

        CenterPivot(next);
        SlideIn(next, new Vector2(240, 0), 0.1f);

        // Rewarded cleanup offer: the text says an ad plays and what it gives. UIManager shows it once the board fills up.
        RectTransform cleanupRect = Rect("CleanupOffer", hud, Corner.TopLeft, new Vector2(16, 138), new Vector2(150, 54));
        CenterPivot(cleanupRect);
        RectTransform cleanupBreath = Rect("Breath", cleanupRect, Corner.TopLeft, Vector2.zero, new Vector2(150, 54));
        cleanupBreath.gameObject.AddComponent<UIBreathe>();
        RectTransform cleanup = Lipped("Button", cleanupBreath, Corner.TopLeft, Vector2.zero, new Vector2(150, 54), Mint, MintLip, 5, 18);
        Icon(cleanup, "icon_video", Color.white, new Vector2(11, 14), 26);
        Label(cleanup, "Title", "CLEAN UP", 19, Color.white, new Vector2(44, 8), new Vector2(102, 22), TextAlignmentOptions.TopLeft);
        Label(cleanup, "Hint", "WATCH AN AD", 11, new Color(1, 1, 1, 0.9f), new Vector2(45, 31), new Vector2(102, 14), TextAlignmentOptions.TopLeft);
        MakeButton(cleanup);

        TMP_Text toast = Label(hud, "Toast", "COMBO x2", 38, Color.white, Vector2.zero, new Vector2(390, 60), TextAlignmentOptions.Center);
        RectTransform toastRect = toast.rectTransform;
        toastRect.anchorMin = toastRect.anchorMax = new Vector2(0.5f, 1f);
        toastRect.pivot = new Vector2(0.5f, 0.5f);
        toastRect.anchoredPosition = new Vector2(0, -170);

        RectTransform fx = Stretch("FX", root2);

        // ---------- pause ----------
        RectTransform pausePanel = Overlay("PausePanel", root2, Dim);
        RectTransform pauseCard = Card("Card", pausePanel, 320, 380, 30);
        Label(pauseCard, "Title", "PAUSED", 44, Cocoa, new Vector2(0, 26), new Vector2(320, 50), TextAlignmentOptions.Top);
        RectTransform resume = Lipped("ResumeButton", pauseCard, Corner.TopLeft, new Vector2(24, 92), new Vector2(272, 64), Coral, CoralLip, 6, 22);
        IconText(resume, "icon_play", "RESUME", 28, Color.white, 24);
        Button resumeButton = MakeButton(resume);

        UIManager.ToggleView pauseSound = Tile("SoundToggle", pauseCard, new Vector2(24, 174), "icon_sound_on", "icon_sound_off", "SOUND ON", "SOUND OFF");
        UIManager.ToggleView pauseMusic = Tile("MusicToggle", pauseCard, new Vector2(166, 174), "icon_music_on", "icon_music_off", "MUSIC ON", "MUSIC OFF");

        Button pauseRestart = RoundWithLabel(pauseCard, "Restart", "icon_restart", "RESTART", new Vector2(160 - 48 - 30, 268), 60);
        Button pauseHome = RoundWithLabel(pauseCard, "Home", "icon_home", "HOME", new Vector2(160 + 48 - 30, 268), 60);
        Intro(pausePanel, pauseCard.Find("Title"), resume, pauseCard.Find("SoundToggle"), pauseCard.Find("MusicToggle"), pauseCard.Find("Restart"), pauseCard.Find("Home"));

        // ---------- chef's break ----------
        RectTransform breakPanel = Overlay("BreakPanel", root2, Dim);
        RectTransform breakCard = Card("Card", breakPanel, 320, 276, 10);
        RectTransform breakRibbon = Lipped("Ribbon", breakCard, Corner.TopCenter, new Vector2(0, -26), new Vector2(240, 54), Butter, ButterLip, 5, 18);
        Label(breakRibbon, "Title", "CHEF'S BREAK", 28, Cocoa, Vector2.zero, new Vector2(240, 54), TextAlignmentOptions.Center);
        TMP_Text breakHeadline = Label(breakCard, "Headline", "NEW DISH UNLOCKED!", 24, Coral, new Vector2(0, 42), new Vector2(320, 30), TextAlignmentOptions.Top);
        TMP_Text breakScoreLabel = Label(breakCard, "ScoreLabel", "SCORE", 14, CocoaSoft, new Vector2(0, 80), new Vector2(320, 18), TextAlignmentOptions.Top, 1.5f);
        TMP_Text breakScore = Label(breakCard, "Score", "0", 48, Cocoa, new Vector2(24, 98), new Vector2(272, 54), TextAlignmentOptions.Top);
        AutoSize(breakScore, 26, 48);
        RectTransform breakContinue = Lipped("ContinueButton", breakCard, Corner.TopLeft, new Vector2(24, 162), new Vector2(272, 64), Coral, CoralLip, 6, 22);
        IconText(breakContinue, "icon_play", "CONTINUE", 28, Color.white, 24);
        Button breakContinueButton = MakeButton(breakContinue);
        TMP_Text breakHint = Label(breakCard, "Hint", "A SHORT AD MAY PLAY", 13, CocoaSoft, new Vector2(0, 240), new Vector2(320, 18), TextAlignmentOptions.Top, 1f);
        Intro(breakPanel, breakHeadline.transform, breakScoreLabel.transform, breakScore.transform, breakContinue, breakHint.transform);

        // ---------- game over ----------
        RectTransform overPanel = Overlay("GameOverPanel", root2, Dim);
        RectTransform overCard = Card("Card", overPanel, 320, 0, 10);
        var column = overCard.gameObject.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(24, 24, 44, 28);
        column.spacing = 12;
        column.childAlignment = TextAnchor.UpperCenter;
        column.childControlWidth = false;
        column.childControlHeight = true;
        column.childForceExpandWidth = false;
        column.childForceExpandHeight = false;
        var fit = overCard.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform ribbon = Lipped("Ribbon", overCard, Corner.TopCenter, new Vector2(0, -26), new Vector2(220, 54), Coral, CoralLip, 5, 18);
        Ignore(ribbon);
        Label(ribbon, "Title", "GAME OVER", 30, Color.white, Vector2.zero, new Vector2(220, 54), TextAlignmentOptions.Center);

        Label(Flow("ScoreLabel", overCard, 272, 18), "Text", "SCORE", 14, CocoaSoft, Vector2.zero, new Vector2(272, 18), TextAlignmentOptions.Center, 1.5f);
        TMP_Text finalScore = Label(Flow("Score", overCard, 272, 58), "Text", "0", 64, Cocoa, Vector2.zero, new Vector2(272, 58), TextAlignmentOptions.Center);
        AutoSize(finalScore, 30, 64);

        RectTransform bestRow = Flow("BestRow", overCard, 272, 28);
        var bestLayout = bestRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        bestLayout.spacing = 10;
        bestLayout.childAlignment = TextAnchor.MiddleCenter;
        bestLayout.childControlWidth = false;
        bestLayout.childControlHeight = false;
        bestLayout.childForceExpandWidth = false;
        bestLayout.childForceExpandHeight = false;
        RectTransform newBest = Lipped("NewBest", bestRow, Corner.TopLeft, Vector2.zero, new Vector2(126, 26), Butter, ButterLip, 3, 13);
        Icon(newBest, "icon_crown", Cocoa, new Vector2(10, 5), 16);
        Label(newBest, "Text", "NEW BEST!", 15, Cocoa, new Vector2(31, 3), new Vector2(92, 20), TextAlignmentOptions.TopLeft);
        RectTransform bestText = Rect("Best", bestRow, Corner.TopLeft, Vector2.zero, new Vector2(120, 26));
        TMP_Text overBest = Label(bestText, "Text", "BEST 0", 15, CocoaSoft, Vector2.zero, new Vector2(120, 26), TextAlignmentOptions.Center);

        RectTransform continueRect = Flow("ContinueButton", overCard, 272, 80);
        RectTransform contBreath = Rect("Breath", continueRect, Corner.Center, new Vector2(0, 4), new Vector2(272, 72));
        contBreath.gameObject.AddComponent<UIBreathe>();
        RectTransform cont = Lipped("Button", contBreath, Corner.TopLeft, Vector2.zero, new Vector2(272, 72), Mint, MintLip, 6, 22);
        Icon(cont, "icon_video", Color.white, new Vector2(48, 21), 30);
        Label(cont, "Title", "CONTINUE", 26, Color.white, new Vector2(92, 13), new Vector2(170, 28), TextAlignmentOptions.TopLeft);
        Label(cont, "Hint", "WATCH AN AD", 13, new Color(1, 1, 1, 0.9f), new Vector2(93, 42), new Vector2(170, 16), TextAlignmentOptions.TopLeft);
        MakeButton(cont);

        RectTransform againRect = Flow("PlayAgain", overCard, 272, 72);
        RectTransform again = Lipped("Button", againRect, Corner.TopLeft, Vector2.zero, new Vector2(272, 64), Coral, CoralLip, 6, 22);
        IconText(again, "icon_restart", "PLAY AGAIN", 26, Color.white, 24);
        Button playAgain = MakeButton(again);

        RectTransform overRow = Flow("Actions", overCard, 272, 90);
        var rowLayout = overRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 26;
        rowLayout.childAlignment = TextAnchor.UpperCenter;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        Button overHome = RoundWithLabel(overRow, "Home", "icon_home", "HOME", Vector2.zero, 56);
        Button overRanking = RoundWithLabel(overRow, "Ranking", "icon_trophy", "RANKING", Vector2.zero, 56);
        Intro(overPanel, overCard.Find("ScoreLabel"), overCard.Find("Score"), bestRow, continueRect, againRect, overRow);

        // ---------- leaderboard ----------
        RectTransform boardPanel = Overlay("LeaderboardPanel", root2, Dim);
        RectTransform boardCard = Card("Card", boardPanel, 350, 630, 0);
        RectTransform boardRibbon = Lipped("Ribbon", boardCard, Corner.TopCenter, new Vector2(0, -26), new Vector2(220, 54), Butter, ButterLip, 5, 18);
        Icon(boardRibbon, "icon_trophy", Cocoa, new Vector2(34, 15), 24);
        Label(boardRibbon, "Title", "RANKING", 26, Cocoa, new Vector2(64, 0), new Vector2(140, 54), TextAlignmentOptions.MidlineLeft);
        RectTransform close = Lipped("Close", boardCard, Corner.TopRight, new Vector2(-10, -18), new Vector2(48, 48), Coral, CoralLip, 4, 0, circle: true);
        Icon(close, "icon_close", Color.white, new Vector2(13, 13), 22);
        Button closeButton = MakeButton(close);

        var rows = new UIManager.RowView[7];
        for (int i = 0; i < rows.Length; i++)
            rows[i] = Row(boardCard, "Row" + (i + 1), new Vector2(18, 46 + i * 62), i % 2 == 0 ? RowA : RowB, Hex("F0E1D0"));
        TMP_Text status = Label(boardCard, "Status", "LOADING...", 18, CocoaSoft, new Vector2(18, 220), new Vector2(314, 60), TextAlignmentOptions.Center);
        status.textWrappingMode = TextWrappingModes.Normal;

        UIManager.RowView you = Row(boardCard, "You", new Vector2(18, 486), YouRow, Cocoa);
        you.rank.color = Cream;
        var youRing = Rect("Ring", you.root.transform, Corner.TopLeft, Vector2.zero, new Vector2(314, 56));
        youRing.SetAsFirstSibling();
        Image ring = youRing.gameObject.AddComponent<Image>();
        Sliced(ring, "rounded", Butter, 24f / 16f);
        RectTransform youFace = (RectTransform)you.root.transform.Find("Face");
        youFace.offsetMin = new Vector2(3, 3);
        youFace.offsetMax = new Vector2(-3, -3);

        RectTransform signIn = Rect("SignIn", boardCard, Corner.TopLeft, new Vector2(18, 558), new Vector2(314, 50));
        TMP_Text signInText = Label(signIn, "Text", "SIGN IN TO PUT YOUR BEST SCORE ON THE BOARD", 14, CocoaBody, new Vector2(4, 0), new Vector2(190, 50), TextAlignmentOptions.MidlineLeft);
        signInText.textWrappingMode = TextWrappingModes.Normal;
        RectTransform signInButtonRect = Lipped("Button", signIn, Corner.TopRight, new Vector2(0, 3), new Vector2(110, 44), Mint, MintLip, 4, 14);
        Label(signInButtonRect, "Text", "SIGN IN", 17, Color.white, Vector2.zero, new Vector2(110, 44), TextAlignmentOptions.Center);
        Button signInButton = MakeButton(signInButtonRect);
        CenterPivot((RectTransform)you.root.transform);
        Intro(boardPanel, you.root.transform);

        // ---------- main menu ----------
        RectTransform menuPanel = Overlay("MainMenuPanel", root2, MenuDim);
        // Title above the dropper, best score just over PLAY: the drop box sits between them on tall phones.
        RectTransform titles = Rect("Titles", menuPanel, Corner.TopCenter, new Vector2(0, 64), new Vector2(390, 120));
        CenterPivot(titles);
        RectTransform merge = ShadowedTitle(titles, "Merge", "MERGE", 72, Cream, CoralLip, 6, 0, 76);
        RectTransform munch = ShadowedTitle(titles, "Munch", "& MUNCH", 34, Butter, Cocoa, 4, 72, 40);
        Breathe(merge, 1.03f, 5f, 2.2f);
        Breathe(munch, 1.05f, 3f, 1.7f);

        RectTransform menuChip = Lipped("BestChip", menuPanel, Corner.Center, new Vector2(0, -72), new Vector2(170, 32), Butter, ButterLip, 3, 16);
        Icon(menuChip, "icon_crown", Cocoa, new Vector2(14, 7), 18);
        Label(menuChip, "Label", "BEST", 17, Cocoa, new Vector2(38, 4), new Vector2(46, 24), TextAlignmentOptions.TopLeft);
        TMP_Text menuBest = Label(menuChip, "Value", "0", 17, Cocoa, new Vector2(84, 4), new Vector2(76, 24), TextAlignmentOptions.TopLeft);
        AutoSize(menuBest, 11, 17);

        RectTransform playBreath = Rect("PlayBreath", menuPanel, Corner.Center, new Vector2(0, -150), new Vector2(300, 84));
        playBreath.gameObject.AddComponent<UIBreathe>();
        RectTransform play = Lipped("PlayButton", playBreath, Corner.TopLeft, Vector2.zero, new Vector2(300, 84), Coral, CoralLip, 8, 28);
        IconText(play, "icon_play", "PLAY", 40, Color.white, 34);
        Button playButton = MakeButton(play);

        RectTransform menuRow = Rect("Row", menuPanel, Corner.Center, new Vector2(0, -262), new Vector2(230, 62));
        var menuLayout = menuRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        menuLayout.spacing = 22;
        menuLayout.childAlignment = TextAnchor.UpperCenter;
        menuLayout.childControlWidth = false;
        menuLayout.childControlHeight = false;
        menuLayout.childForceExpandWidth = false;
        menuLayout.childForceExpandHeight = false;
        Button menuRanking = Round(menuRow, "Ranking", "icon_trophy", new Vector2(0, 0), 62, Cream, CreamLip, Cocoa);
        UIManager.ToggleView menuSound = RoundToggle(menuRow, "Sound", new Vector2(84, 0), "icon_sound_on", "icon_sound_off");
        UIManager.ToggleView menuMusic = RoundToggle(menuRow, "Music", new Vector2(168, 0), "icon_music_on", "icon_music_off");
        Intro(menuPanel, titles, menuChip, playBreath, menuRow);

        // ---------- wire ----------
        ui.hud = hud.gameObject;
        ui.pausePanel = pausePanel.gameObject;
        ui.gameOverPanel = overPanel.gameObject;
        ui.leaderboardPanel = boardPanel.gameObject;
        ui.mainMenuPanel = menuPanel.gameObject;
        ui.fxLayer = fx;
        ui.pauseButton = pauseButton;
        ui.hudScore = score;
        ui.hudScoreCard = scoreCard;
        ui.hudBestChip = bestChip;
        ui.toast = toast;
        ui.gameOverScore = finalScore;
        ui.uiFont = font;
        ui.confettiSprite = UISpriteFactory.Load("white");
        ui.confettiColors = new[] { Coral, Butter, Mint, Cream, Hex("F4A9C0") };
        AudioSource click = ui.GetComponent<AudioSource>();
        if (click == null)
            click = ui.gameObject.AddComponent<AudioSource>();
        click.playOnAwake = false;
        click.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Assets/Sounds/104940__glaneur-de-sons__bubble-1.wav");
        click.volume = 0.45f;
        ui.clickSource = click;
        ui.resumeButton = resumeButton;
        ui.pauseRestartButton = pauseRestart;
        ui.pauseHomeButton = pauseHome;
        ui.pauseSound = pauseSound;
        ui.pauseMusic = pauseMusic;
        ui.gameOverBest = overBest;
        ui.newBestChip = newBest.gameObject;
        ui.continueButton = continueRect.gameObject;
        ui.breakPanel = breakPanel.gameObject;
        ui.breakHeadline = breakHeadline;
        ui.breakScore = breakScore;
        ui.breakContinueButton = breakContinueButton;
        ui.cleanupButton = cleanupRect.gameObject;
        ui.playAgainButton = playAgain;
        ui.gameOverHomeButton = overHome;
        ui.gameOverRankingButton = overRanking;
        ui.leaderboardCloseButton = closeButton;
        ui.leaderboardRows = rows;
        ui.youRow = you;
        ui.leaderboardStatus = status;
        ui.signInRow = signIn.gameObject;
        ui.signInButton = signInButton;
        ui.medalColors = new[] { Butter, Hex("D9DEE3"), Hex("E8B48A") };
        ui.plainBadgeColor = Hex("F0E1D0");
        ui.playButton = playButton;
        ui.menuRankingButton = menuRanking;
        ui.menuBest = menuBest;
        ui.menuSound = menuSound;
        ui.menuMusic = menuMusic;
        ui.sfxSources = new[] { FindSource("SoundMANAGER"), FindSource("MergeSoundMANAGER") };
        ui.musicSource = FindSource("MusicMANAGER");
        EditorUtility.SetDirty(ui);

        game.scoreMesh = (TextMeshProUGUI)score;
        game.highScoreMesh = (TextMeshProUGUI)hudBest;
        game.restartSceneScoreMesh = (TextMeshProUGUI)finalScore;
        EditorUtility.SetDirty(game);

        spawner.nextObjectImages = previews;
        EditorUtility.SetDirty(spawner);

        foreach (RectTransform panel in new[] { pausePanel, breakPanel, overPanel, boardPanel, menuPanel })
            panel.gameObject.SetActive(false);

        KitchenSceneBuilder.BuildInto(scene, spawner);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("MM_UI_BUILD_OK");
    }

    // ---------- building blocks ----------

    enum Corner { TopLeft, TopRight, TopCenter, Center }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }

    // Position in design px measured from the given corner (x right/inward, y down).
    static RectTransform Rect(string name, Transform parent, Corner corner, Vector2 offset, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        Vector2 anchor = corner == Corner.TopLeft ? new Vector2(0, 1) : corner == Corner.TopRight ? new Vector2(1, 1) : corner == Corner.TopCenter ? new Vector2(0.5f, 1) : new Vector2(0.5f, 0.5f);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = corner == Corner.Center ? new Vector2(0.5f, 0.5f) : corner == Corner.TopCenter ? new Vector2(0.5f, 1) : anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = corner == Corner.TopLeft ? new Vector2(offset.x, -offset.y)
            : corner == Corner.TopRight ? new Vector2(-offset.x, -offset.y)
            : corner == Corner.TopCenter ? new Vector2(offset.x, -offset.y)
            : offset;
        return rect;
    }

    static RectTransform Stretch(string name, Transform parent)
    {
        var rect = Rect(name, parent, Corner.Center, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        return rect;
    }

    static void Sliced(Image image, string sprite, Color color, float cornerScale)
    {
        image.sprite = UISpriteFactory.Load(sprite);
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = cornerScale;
        image.color = color;
    }

    // A face with a darker "lip" drawn below it, the toy-button look of the design.
    static RectTransform Lipped(string name, Transform parent, Corner corner, Vector2 offset, Vector2 size, Color face, Color lip, float lipHeight, float radius, bool circle = false)
    {
        RectTransform rect = Rect(name, parent, corner, offset, size);
        foreach (var (part, color, drop) in new[] { ("Lip", lip, lipHeight), ("Face", face, 0f) })
        {
            RectTransform layer = Stretch(part, rect);
            layer.offsetMin = new Vector2(0, -drop);
            layer.offsetMax = new Vector2(0, -drop);
            Image image = layer.gameObject.AddComponent<Image>();
            if (circle)
            {
                image.sprite = UISpriteFactory.Load("circle");
                image.color = color;
            }
            else
            {
                Sliced(image, "rounded", color, UISpriteFactory.RoundedRadius / Mathf.Max(1f, radius));
            }
            image.raycastTarget = part == "Face";
            Ignore(layer);
        }
        return rect;
    }

    static void Ignore(RectTransform rect)
    {
        var element = rect.GetComponent<LayoutElement>();
        if (element == null)
            element = rect.gameObject.AddComponent<LayoutElement>();
        element.ignoreLayout = true;
    }

    static RectTransform Flow(string name, Transform parent, float width, float height)
    {
        RectTransform rect = Rect(name, parent, Corner.TopLeft, Vector2.zero, new Vector2(width, height));
        rect.pivot = new Vector2(0.5f, 0.5f); // the layout group places it; staggered pops grow from the centre
        var element = rect.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = height;
        return rect;
    }

    static RectTransform Overlay(string name, Transform parent, Color dim)
    {
        RectTransform panel = Stretch(name, parent);
        Image shade = panel.gameObject.AddComponent<Image>();
        shade.sprite = UISpriteFactory.Load("white");
        shade.color = dim;
        shade.raycastTarget = true; // blocks drops behind open menus
        return panel;
    }

    static RectTransform Card(string name, Transform parent, float width, float height, float lift)
    {
        RectTransform card = Lipped(name, parent, Corner.Center, new Vector2(0, lift), new Vector2(width, height), Cream, CreamLip, 8, 32);
        card.gameObject.AddComponent<UIPop>();
        return card;
    }

    static TMP_Text Label(Transform parent, string name, string text, float size, Color color, Vector2 offset, Vector2 box, TextAlignmentOptions align, float spacing = 0f)
    {
        RectTransform rect = Rect(name, parent, Corner.TopLeft, offset, box);
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.characterSpacing = spacing * 10f;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.margin = Vector4.zero;
        return tmp;
    }

    static void AutoSize(TMP_Text text, float min, float max)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = min;
        text.fontSizeMax = max;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    static Image Icon(Transform parent, string icon, Color color, Vector2 offset, float size)
    {
        RectTransform rect = Rect(icon, parent, Corner.TopLeft, offset, new Vector2(size, size));
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = UISpriteFactory.Load(icon);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // Icon + label centred together on a wide button.
    static void IconText(RectTransform button, string icon, string text, float size, Color color, float iconSize)
    {
        Vector2 box = button.sizeDelta;
        float textWidth = size * 0.62f * text.Length;
        float total = iconSize + 10 + textWidth;
        float left = (box.x - total) * 0.5f;
        Icon(button, icon, color, new Vector2(left, (box.y - iconSize) * 0.5f), iconSize);
        Label(button, "Text", text, size, color, new Vector2(left + iconSize + 10, 0), new Vector2(textWidth + 20, box.y), TextAlignmentOptions.MidlineLeft);
    }

    static Button MakeButton(RectTransform rect)
    {
        if (rect.anchorMin == rect.anchorMax)
            CenterPivot(rect);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.Find("Face").GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.pressedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.highlightedColor = new Color(1f, 1f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.7f);
        button.colors = colors;
        var nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
        rect.gameObject.AddComponent<UIPress>();
        return button;
    }

    static Button Round(Transform parent, string name, string icon, Vector2 offset, float size, Color face, Color lip, Color iconColor)
    {
        RectTransform rect = Lipped(name, parent, Corner.TopLeft, offset, new Vector2(size, size), face, lip, 5, 0, circle: true);
        float iconSize = size * 0.46f;
        Icon(rect, icon, iconColor, new Vector2((size - iconSize) * 0.5f, (size - iconSize) * 0.5f), iconSize);
        return MakeButton(rect);
    }

    // Round butter button with a caption under it; the column is what a layout or the UI hides.
    static Button RoundWithLabel(Transform parent, string name, string icon, string caption, Vector2 offset, float size)
    {
        RectTransform columnRect = Rect(name, parent, Corner.TopLeft, offset, new Vector2(size, size + 30));
        Button button = Round(columnRect, "Button", icon, Vector2.zero, size, Butter, ButterLip, Cocoa);
        Label(columnRect, "Caption", caption, 14, CocoaSoft, new Vector2(-20, size + 11), new Vector2(size + 40, 18), TextAlignmentOptions.Top);
        CenterPivot(columnRect);
        return button;
    }

    static UIManager.ToggleView Tile(string name, Transform parent, Vector2 offset, string onIcon, string offIcon, string onText, string offText)
    {
        RectTransform rect = Lipped(name, parent, Corner.TopLeft, offset, new Vector2(130, 74), Mint, MintLip, 5, 20);
        Image icon = Icon(rect, onIcon, Color.white, new Vector2(52, 12), 26);
        TMP_Text label = Label(rect, "Label", onText, 16, Color.white, new Vector2(0, 44), new Vector2(130, 20), TextAlignmentOptions.Top);
        return new UIManager.ToggleView
        {
            button = MakeButton(rect),
            background = rect.Find("Face").GetComponent<Image>(),
            lip = rect.Find("Lip").GetComponent<Image>(),
            icon = icon,
            label = label,
            onIcon = UISpriteFactory.Load(onIcon),
            offIcon = UISpriteFactory.Load(offIcon),
            onText = onText,
            offText = offText,
            onBackground = Mint,
            offBackground = OffTile,
            onLip = MintLip,
            offLip = OffTileLip,
            onContent = Color.white,
            offContent = CocoaSoft
        };
    }

    static UIManager.ToggleView RoundToggle(Transform parent, string name, Vector2 offset, string onIcon, string offIcon)
    {
        Button button = Round(parent, name, onIcon, offset, 62, Cream, CreamLip, Cocoa);
        return new UIManager.ToggleView
        {
            button = button,
            icon = button.transform.Find(onIcon).GetComponent<Image>(),
            onIcon = UISpriteFactory.Load(onIcon),
            offIcon = UISpriteFactory.Load(offIcon),
            onContent = Cocoa,
            offContent = CocoaSoft
        };
    }

    static UIManager.RowView Row(Transform parent, string name, Vector2 offset, Color fill, Color badgeColor)
    {
        RectTransform rect = Rect(name, parent, Corner.TopLeft, offset, new Vector2(314, name == "You" ? 56 : 52));
        RectTransform face = Stretch("Face", rect);
        Image bg = face.gameObject.AddComponent<Image>();
        Sliced(bg, "rounded", fill, UISpriteFactory.RoundedRadius / 16f);
        bg.raycastTarget = false;

        float mid = rect.sizeDelta.y * 0.5f;
        RectTransform badgeRect = Rect("Badge", rect, Corner.TopLeft, new Vector2(14, mid - 17), new Vector2(34, 34));
        Image badge = badgeRect.gameObject.AddComponent<Image>();
        badge.sprite = UISpriteFactory.Load("circle");
        badge.color = badgeColor;
        badge.raycastTarget = false;
        TMP_Text rank = Label(badgeRect, "Rank", "1", 17, Cocoa, Vector2.zero, new Vector2(34, 34), TextAlignmentOptions.Center);
        AutoSize(rank, 10, 17);
        TMP_Text title = Label(rect, "Name", "PLAYER", 19, Cocoa, new Vector2(60, 0), new Vector2(150, rect.sizeDelta.y), TextAlignmentOptions.MidlineLeft);
        title.overflowMode = TextOverflowModes.Ellipsis;
        TMP_Text points = Label(rect, "Score", "0", 19, Cocoa, new Vector2(214, 0), new Vector2(86, rect.sizeDelta.y), TextAlignmentOptions.MidlineRight);
        AutoSize(points, 12, 19);
        return new UIManager.RowView { root = rect.gameObject, badge = badge, rank = rank, name = title, score = points };
    }

    // Title text with a solid offset copy under it, like the buttons' lips.
    static RectTransform ShadowedTitle(Transform parent, string name, string text, float size, Color color, Color shadow, float drop, float top, float height)
    {
        RectTransform rect = Rect(name, parent, Corner.TopCenter, new Vector2(0, top), new Vector2(390, height));
        Label(rect, "Shadow", text, size, shadow, new Vector2(0, drop), new Vector2(390, height), TextAlignmentOptions.Top);
        Label(rect, "Text", text, size, color, Vector2.zero, new Vector2(390, height), TextAlignmentOptions.Top);
        CenterPivot(rect);
        return rect;
    }

    // Scale and punch animations grow from the pivot: move it to the centre without moving the element.
    static void CenterPivot(RectTransform rect)
    {
        Vector2 delta = (new Vector2(0.5f, 0.5f) - rect.pivot) * rect.sizeDelta;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition += delta;
    }

    static void SlideIn(RectTransform rect, Vector2 from, float delay)
    {
        var slide = rect.gameObject.AddComponent<UISlideIn>();
        slide.from = from;
        slide.delay = delay;
    }

    static void Breathe(RectTransform rect, float scale, float bob, float period)
    {
        var breathe = rect.gameObject.AddComponent<UIBreathe>();
        breathe.scale = scale;
        breathe.bob = bob;
        breathe.period = period;
    }

    static void Intro(RectTransform panel, params Transform[] items)
    {
        var intro = panel.gameObject.AddComponent<PanelIntro>();
        intro.stagger = items;
        foreach (Transform item in items)
        {
            if (item is RectTransform rect && rect.anchorMin == rect.anchorMax && rect.pivot != new Vector2(0.5f, 0.5f))
                CenterPivot(rect);
        }
    }

    static AudioSource FindSource(string name)
    {
        foreach (AudioSource source in Object.FindObjectsOfType<AudioSource>(true))
        {
            if (source.gameObject.name == name)
                return source;
        }
        return null;
    }
}
