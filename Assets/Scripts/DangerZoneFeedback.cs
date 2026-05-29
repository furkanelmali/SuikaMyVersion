using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DangerZoneFeedback : MonoBehaviour
{
    public static DangerZoneFeedback Instance { get; private set; }

    [SerializeField] Image dangerLineImage;
    [SerializeField] float warningThreshold = 3f;
    [SerializeField] float criticalThreshold = 1f;

    Tween pulseTween;
    Color baseColor = Color.red;

    void Awake()
    {
        Instance = this;

        if (dangerLineImage == null)
            CreateDangerLine();

        if (dangerLineImage != null)
            SetAlpha(0f);
    }

    void CreateDangerLine()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        GameObject line = new GameObject("DangerLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(canvas.transform, false);

        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 8f);
        rect.anchoredPosition = new Vector2(0f, -120f);

        dangerLineImage = line.GetComponent<Image>();
        dangerLineImage.color = new Color(1f, 0.2f, 0.2f, 0f);
        dangerLineImage.raycastTarget = false;
    }

    public void SetDangerProgress(float stayTime, float maxTime)
    {
        if (dangerLineImage == null)
            return;

        float ratio = Mathf.Clamp01(stayTime / maxTime);
        float alpha = ratio * 0.85f;
        Color c = Color.Lerp(baseColor, Color.red, ratio);
        c.a = alpha;
        dangerLineImage.color = c;

        if (stayTime >= warningThreshold && pulseTween == null)
        {
            pulseTween = dangerLineImage.DOFade(Mathf.Min(alpha + 0.2f, 1f), 0.35f)
                .SetLoops(-1, LoopType.Yoyo);

            if (stayTime >= criticalThreshold)
                CameraShake.Instance?.ShakeDanger(ratio);
        }

        if (stayTime <= 0f)
            Reset();
    }

    public void Reset()
    {
        pulseTween?.Kill();
        pulseTween = null;
        SetAlpha(0f);
    }

    void SetAlpha(float alpha)
    {
        if (dangerLineImage == null)
            return;

        Color c = dangerLineImage.color;
        c.a = alpha;
        dangerLineImage.color = c;
    }
}
