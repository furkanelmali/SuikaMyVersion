using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DangerZoneFeedback : MonoBehaviour
{
    static DangerZoneFeedback _instance;
    public static DangerZoneFeedback Instance => _instance != null ? _instance : null;

    [SerializeField] Image dangerLineImage;
    [SerializeField] Image vignetteImage;
    [SerializeField] float shakeStartRatio = 0.35f;
    [SerializeField] float pulseStartRatio = 0.7f;
    [SerializeField] float maxVignetteAlpha = 0.75f;

    static Sprite cachedVignetteSprite;

    Color lineBaseColor = new Color(1f, 0.2f, 0.2f);
    bool active;

    void Awake()
    {
        _instance = this;

        if (dangerLineImage == null)
            CreateDangerLine();

        if (vignetteImage == null)
            CreateVignette();

        SetLineAlpha(0f);
        SetVignetteAlpha(0f);
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
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
        dangerLineImage.color = new Color(lineBaseColor.r, lineBaseColor.g, lineBaseColor.b, 0f);
        dangerLineImage.raycastTarget = false;
    }

    void CreateVignette()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        GameObject go = new GameObject("DangerVignette", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        vignetteImage = go.GetComponent<Image>();
        vignetteImage.sprite = GetVignetteSprite();
        vignetteImage.type = Image.Type.Simple;
        vignetteImage.color = new Color(1f, 0.05f, 0.05f, 0f);
        vignetteImage.raycastTarget = false;
    }

    static Sprite GetVignetteSprite()
    {
        if (cachedVignetteSprite != null)
            return cachedVignetteSprite;

        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                // Transparent center, opaque red toward edges.
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, dist));
                byte a = (byte)(Mathf.Clamp01(alpha) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        cachedVignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return cachedVignetteSprite;
    }

    public void SetDangerProgress(float stayTime, float maxTime)
    {
        active = true;
        float ratio = Mathf.Clamp01(stayTime / maxTime);

        // Danger line
        if (dangerLineImage != null)
        {
            Color c = lineBaseColor;
            c.a = ratio * 0.85f;
            dangerLineImage.color = c;
        }

        // Red vignette with pulse near critical
        if (vignetteImage != null)
        {
            float baseAlpha = ratio * maxVignetteAlpha;

            if (ratio >= pulseStartRatio)
            {
                float pulse = (Mathf.Sin(Time.unscaledTime * 10f) + 1f) * 0.5f;
                baseAlpha = Mathf.Lerp(baseAlpha, maxVignetteAlpha, pulse * 0.4f);
            }

            SetVignetteAlpha(baseAlpha);
        }

        // Camera shake escalates after a threshold
        if (CameraShake.Instance != null)
        {
            if (ratio >= shakeStartRatio)
            {
                float shakeRatio = Mathf.InverseLerp(shakeStartRatio, 1f, ratio);
                CameraShake.Instance.SetDangerShake(shakeRatio);
            }
            else
            {
                CameraShake.Instance.StopDangerShake();
            }
        }
    }

    public void Reset()
    {
        if (!active)
        {
            SetLineAlpha(0f);
            SetVignetteAlpha(0f);
            return;
        }

        active = false;
        SetLineAlpha(0f);

        if (vignetteImage != null)
        {
            vignetteImage.DOKill();
            vignetteImage.DOFade(0f, 0.25f);
        }

        CameraShake.Instance?.StopDangerShake();
    }

    void SetLineAlpha(float alpha)
    {
        if (dangerLineImage == null)
            return;

        Color c = dangerLineImage.color;
        c.a = alpha;
        dangerLineImage.color = c;
    }

    void SetVignetteAlpha(float alpha)
    {
        if (vignetteImage == null)
            return;

        vignetteImage.DOKill();
        Color c = vignetteImage.color;
        c.a = Mathf.Clamp01(alpha);
        vignetteImage.color = c;
    }
}
