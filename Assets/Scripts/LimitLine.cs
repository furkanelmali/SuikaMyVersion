using UnityEngine;

/// <summary>
/// The dashed line at the loss height between the container's posts. Soft white while safe,
/// turning red and pulsing as a piece stays above it.
/// </summary>
public class LimitLine : MonoBehaviour
{
    public static LimitLine Instance { get; private set; }

    public LineRenderer line;
    public Color calm = new Color(1f, 1f, 1f, 0.7f);
    public Color danger = new Color(0.93f, 0.25f, 0.2f, 1f);

    float ratio;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetDanger(float value) => ratio = Mathf.Clamp01(value);

    void Update()
    {
        if (line == null)
            return;
        float pulse = ratio > 0.5f ? (Mathf.Sin(Time.unscaledTime * 12f) + 1f) * 0.5f * ratio : 0f;
        Color c = Color.Lerp(calm, danger, Mathf.Max(ratio, pulse));
        line.startColor = line.endColor = c;
        float width = Mathf.Lerp(0.14f, 0.24f, ratio);
        line.startWidth = line.endWidth = width;
        // Tile mode repeats once per width; one dash per half unit reads as a dashed rule.
        line.material.mainTextureScale = new Vector2(width / 0.5f, 1f);
        line.material.mainTextureOffset = new Vector2(-Time.unscaledTime * (0.4f + ratio * 2f), 0f);
    }
}
