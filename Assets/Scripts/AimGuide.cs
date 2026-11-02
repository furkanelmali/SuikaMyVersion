using UnityEngine;

/// <summary>
/// Dashed drop guide from the held piece down to whatever it would land on. Shown only while the player can drop.
/// </summary>
public class AimGuide : MonoBehaviour
{
    public LineRenderer line;
    public float dashLength = 0.6f;

    PlayerController player;
    readonly RaycastHit[] hits = new RaycastHit[16];

    void Awake()
    {
        player = GetComponent<PlayerController>();
        if (line != null)
            line.enabled = false;
    }

    void LateUpdate()
    {
        if (line == null)
            return;

        GameObject held = player != null ? player.currentFallObject : null;
        bool show = held != null && held.activeInHierarchy && player.enabled && player.controller && Time.timeScale > 0f
                    && (GameManager.Instance == null || !GameManager.Instance.IsGameOver);
        line.enabled = show;
        if (!show)
            return;

        // The held piece's colliders are off until it is dropped. The piece hangs inside the dropper, whose own
        // mesh collider would stop the ray at once, so the dropper and anything under it are skipped.
        Vector3 origin = held.transform.position;
        Vector3 end = origin + Vector3.down * 20f;
        float nearest = float.MaxValue;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, hits, 60f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider.transform.IsChildOf(transform) || hits[i].distance >= nearest)
                continue;
            nearest = hits[i].distance;
            end = hits[i].point;
        }
        line.SetPosition(0, origin);
        line.SetPosition(1, end);

        // Tile mode repeats the texture once per line width; scale that up to one dash per dashLength.
        line.material.mainTextureScale = new Vector2(line.startWidth / dashLength, 1f);
        line.material.mainTextureOffset = new Vector2(-Time.unscaledTime * 1.5f, 0f);
    }
}
