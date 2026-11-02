using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class MergeJuice : MonoBehaviour
{
    static MergeJuice _instance;
    public static MergeJuice Instance => _instance != null ? _instance : null;

    [SerializeField] GameObject floatingScorePrefab;
    [SerializeField] int floatingScorePoolSize = 10;
    [SerializeField] Transform floatingScoreParent;

    readonly Queue<TextMeshProUGUI> floatingScorePool = new Queue<TextMeshProUGUI>();
    Camera mainCamera;

    // Merges landing within this window of each other build a combo.
    const float ComboWindow = 1.1f;
    int combo;
    float lastMergeTime = -10f;

    public int Combo => Time.time - lastMergeTime <= ComboWindow ? combo : 0;
    // Each chained merge raises the merge sound a little, capped after six.
    public float ComboPitch => 1f + Mathf.Min(Mathf.Max(Combo - 1, 0), 6) * 0.05f;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        mainCamera = Camera.main;

        EnsureFloatingScoreSetup();
        BuildFloatingScorePool();
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    void EnsureFloatingScoreSetup()
    {
        if (floatingScorePrefab != null)
            return;

        // The UI's FX layer sits above the HUD and below the menus.
        Transform layer = UIManager.FxLayer != null ? UIManager.FxLayer : FindObjectOfType<Canvas>()?.transform;
        if (layer == null)
            return;

        floatingScoreParent = layer;

        floatingScorePrefab = new GameObject("FloatingScoreTemplate", typeof(RectTransform));
        floatingScorePrefab.transform.SetParent(floatingScoreParent, false);

        RectTransform rect = floatingScorePrefab.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120f, 40f);

        TextMeshProUGUI label = floatingScorePrefab.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 30;
        label.color = Color.white;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        if (UIManager.UiFont != null)
            label.font = UIManager.UiFont;
        floatingScorePrefab.SetActive(false);
    }

    void BuildFloatingScorePool()
    {
        if (floatingScorePrefab == null)
            return;

        if (floatingScoreParent == null)
            floatingScoreParent = floatingScorePrefab.transform.parent;

        for (int i = 0; i < floatingScorePoolSize; i++)
        {
            GameObject instance = Instantiate(floatingScorePrefab, floatingScoreParent);
            instance.SetActive(false);
            TextMeshProUGUI label = instance.GetComponent<TextMeshProUGUI>();
            if (label != null)
                floatingScorePool.Enqueue(label);
        }
    }

    public void PlayMergeEffects(GameObject mergedObject, int rank, int scoreGained)
    {
        if (mergedObject == null)
            return;

        Transform target = mergedObject.transform;
        Vector3 baseScale = target.localScale;

        target.DOKill();
        target.localScale = baseScale;
        target.DOScale(baseScale * 1.15f, 0.08f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutBack);
        target.DOPunchPosition(Vector3.up * 0.05f, 0.15f, 8, 0.5f);

        CameraShake.Instance?.ShakeOnMerge(rank);

        combo = Time.time - lastMergeTime <= ComboWindow ? combo + 1 : 1;
        lastMergeTime = Time.time;
        if (combo >= 2)
            UIManager.Instance?.ShowToast("COMBO x" + combo);

        ShowFloatingScore(scoreGained, target.position);
    }

    public void ShowFloatingScore(int score, Vector3 worldPosition)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (floatingScorePool.Count == 0 || mainCamera == null)
            return;

        TextMeshProUGUI label = floatingScorePool.Dequeue();
        RectTransform rect = label.rectTransform;
        label.gameObject.SetActive(true);
        label.text = "+" + score;
        // Bigger merges read bigger and warmer.
        float weight = Mathf.InverseLerp(1f, 60f, score);
        label.fontSize = Mathf.Lerp(30f, 52f, weight);
        label.color = Color.Lerp(Color.white, new Color(1f, 0.79f, 0.3f), weight);
        label.outlineWidth = 0.28f;
        label.outlineColor = new Color32(90, 58, 46, 255);

        rect.position = mainCamera.WorldToScreenPoint(worldPosition);
        rect.localScale = Vector3.one * 0.4f;
        rect.localRotation = Quaternion.Euler(0, 0, Random.Range(-8f, 8f));

        rect.DOKill();
        label.DOKill();
        DOTween.Sequence()
            .Append(rect.DOScale(1.15f, 0.16f).SetEase(Ease.OutBack))
            .Append(rect.DOScale(1f, 0.1f))
            .Insert(0f, rect.DOAnchorPosY(rect.anchoredPosition.y + 90f, 0.8f).SetEase(Ease.OutCubic))
            .Insert(0.45f, label.DOFade(0f, 0.35f))
            .SetTarget(rect)
            .OnComplete(() =>
            {
                label.gameObject.SetActive(false);
                floatingScorePool.Enqueue(label);
            });
    }
}
