using DG.Tweening;
using UnityEngine;

/// <summary>Cards drop in with a springy overshoot whenever their panel opens. Unscaled, so it works while paused.</summary>
public class UIPop : MonoBehaviour
{
    void OnEnable()
    {
        var rect = (RectTransform)transform;
        rect.DOKill();
        rect.localScale = Vector3.one * 0.7f;
        rect.localRotation = Quaternion.Euler(0, 0, -4f);
        DOTween.Sequence()
            .Append(rect.DOScale(1f, 0.45f).SetEase(Ease.OutBack, 2.2f))
            .Join(rect.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutElastic, 1f, 0.4f))
            .SetUpdate(true)
            .SetTarget(rect);
    }
}
