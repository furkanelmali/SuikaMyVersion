using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The UI is laid out for a 390x844 portrait screen. Narrower screens match width, wider ones (tablets,
/// desktop browsers) match height, so the layout always fits.
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class CanvasAspectMatch : MonoBehaviour
{
    CanvasScaler scaler;
    Vector2Int lastSize;

    void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        Apply();
    }

    void Update()
    {
        if (lastSize.x != Screen.width || lastSize.y != Screen.height)
            Apply();
    }

    void Apply()
    {
        lastSize = new Vector2Int(Screen.width, Screen.height);
        Vector2 reference = scaler.referenceResolution;
        float screenAspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 1f;
        scaler.matchWidthOrHeight = screenAspect > reference.x / reference.y ? 1f : 0f;
    }
}
