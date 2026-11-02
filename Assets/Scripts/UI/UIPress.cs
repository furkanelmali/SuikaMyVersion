using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Buttons squash while held and spring back with a little overshoot. Unscaled, so it works while paused.</summary>
public class UIPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    const float PressedScale = 0.9f;

    Selectable selectable;
    bool held;

    void Awake() => selectable = GetComponent<Selectable>();

    void OnDisable()
    {
        held = false;
        transform.DOKill();
        transform.localScale = Vector3.one;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable)
            return;
        held = true;
        transform.DOKill();
        transform.DOScale(PressedScale, 0.08f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData) => Release();
    public void OnPointerExit(PointerEventData eventData) => Release();

    void Release()
    {
        if (!held)
            return;
        held = false;
        transform.DOKill();
        transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack, 3f).SetUpdate(true);
    }
}
