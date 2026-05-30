using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class MergeJuice : MonoBehaviour
{
    static MergeJuice _instance;
    public static MergeJuice Instance => _instance != null ? _instance : null;

    [SerializeField] GameObject floatingScorePrefab;
    [SerializeField] int floatingScorePoolSize = 5;
    [SerializeField] Transform floatingScoreParent;

    readonly Queue<TextMeshProUGUI> floatingScorePool = new Queue<TextMeshProUGUI>();
    Camera mainCamera;

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

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        floatingScoreParent = canvas.transform;

        floatingScorePrefab = new GameObject("FloatingScoreTemplate", typeof(RectTransform));
        floatingScorePrefab.transform.SetParent(floatingScoreParent, false);

        RectTransform rect = floatingScorePrefab.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120f, 40f);

        TextMeshProUGUI label = floatingScorePrefab.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 28;
        label.color = Color.yellow;
        label.raycastTarget = false;
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

        Color labelColor = label.color;
        labelColor.a = 1f;
        label.color = labelColor;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
        rect.position = screenPos;

        rect.DOKill();
        rect.DOAnchorPosY(rect.anchoredPosition.y + 60f, 0.6f).SetEase(Ease.OutQuad);
        label.DOFade(0f, 0.6f).OnComplete(() =>
        {
            label.gameObject.SetActive(false);
            floatingScorePool.Enqueue(label);
        });
    }
}
