using DG.Tweening;
using UnityEngine;

/// <summary>Primary buttons and titles breathe gently so the eye finds them.</summary>
public class UIBreathe : MonoBehaviour
{
    public float scale = 1.045f;
    public float bob;          // canvas units of vertical drift, 0 for none
    public float period = 1.6f;

    Tween scaleTween, bobTween;
    Vector2 home;

    void OnEnable()
    {
        transform.localScale = Vector3.one;
        scaleTween = transform.DOScale(scale, period * 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetDelay(0.4f);
        if (bob != 0 && transform is RectTransform rect)
        {
            home = rect.anchoredPosition;
            bobTween = rect.DOAnchorPosY(home.y + bob, period * 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }
    }

    void OnDisable()
    {
        scaleTween?.Kill();
        bobTween?.Kill();
        transform.localScale = Vector3.one;
        if (bob != 0 && transform is RectTransform rect)
            rect.anchoredPosition = home;
    }
}
