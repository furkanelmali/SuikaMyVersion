using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Repeats a RawImage's texture at a fixed size in canvas units, centred on the screen,
/// so backdrop bands fill any width without stretching. 0 on an axis stretches that axis instead.
/// </summary>
[RequireComponent(typeof(RawImage))]
[ExecuteAlways]
public class TileFill : MonoBehaviour
{
    public Vector2 tileSize = new Vector2(64, 64);

    RawImage image;

    void OnEnable() => Apply();
    void OnRectTransformDimensionsChange() => Apply();

    void Apply()
    {
        if (image == null)
            image = GetComponent<RawImage>();
        Rect rect = ((RectTransform)transform).rect;
        float w = tileSize.x > 0 ? rect.width / tileSize.x : 1f;
        float h = tileSize.y > 0 ? rect.height / tileSize.y : 1f;
        // Centre the pattern horizontally; vertical tiling starts at the band's top edge.
        image.uvRect = new Rect(tileSize.x > 0 ? -w * 0.5f : 0f, tileSize.y > 0 ? 1f - h : 0f, w, h);
    }
}
