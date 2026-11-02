using DG.Tweening;
using UnityEngine;

/// <summary>HUD pieces slide in from off screen when a run starts.</summary>
public class UISlideIn : MonoBehaviour
{
    public Vector2 from = new Vector2(-220, 0);
    public float delay;

    Vector2 home;
    bool homeKnown;

    void OnEnable()
    {
        var rect = (RectTransform)transform;
        if (!homeKnown)
        {
            home = rect.anchoredPosition;
            homeKnown = true;
        }
        rect.DOKill();
        rect.anchoredPosition = home + from;
        rect.DOAnchorPos(home, 0.55f).SetEase(Ease.OutBack).SetDelay(delay).SetUpdate(true);
    }
}
