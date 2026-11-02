using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Opening a menu panel: the dim fades in, then the listed items pop in one after another.
/// Unscaled, so it works while paused.
/// </summary>
public class PanelIntro : MonoBehaviour
{
    public Transform[] stagger;
    public float firstDelay = 0.12f;
    public float step = 0.06f;

    Image dim;
    float dimAlpha = -1f;

    void OnEnable()
    {
        if (dim == null)
            dim = GetComponent<Image>();
        if (dim != null)
        {
            if (dimAlpha < 0f)
                dimAlpha = dim.color.a;
            dim.DOKill();
            Color c = dim.color;
            c.a = 0f;
            dim.color = c;
            dim.DOFade(dimAlpha, 0.25f).SetUpdate(true);
        }

        if (stagger == null)
            return;
        for (int i = 0; i < stagger.Length; i++)
        {
            Transform item = stagger[i];
            if (item == null || !item.gameObject.activeSelf)
                continue;
            item.DOKill();
            item.localScale = Vector3.zero;
            item.DOScale(1f, 0.35f).SetEase(Ease.OutBack, 2f).SetDelay(firstDelay + i * step).SetUpdate(true);
        }
    }
}
