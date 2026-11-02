using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Backdrop sparkle: fades and spins in and out forever, each one on its own rhythm.</summary>
public class Twinkle : MonoBehaviour
{
    Sequence loop;

    void OnEnable()
    {
        Graphic graphic = GetComponent<Graphic>();
        float period = Random.Range(1.6f, 2.8f);
        transform.localScale = Vector3.one * 0.6f;
        graphic.canvasRenderer.SetAlpha(0.35f);

        loop = DOTween.Sequence()
            .Append(transform.DOScale(1.1f, period * 0.5f).SetEase(Ease.InOutSine))
            .Join(transform.DOLocalRotate(new Vector3(0, 0, 45), period, RotateMode.LocalAxisAdd).SetEase(Ease.Linear))
            .Join(DOTween.To(() => graphic.canvasRenderer.GetAlpha(), a => graphic.canvasRenderer.SetAlpha(a), 1f, period * 0.5f))
            .Append(transform.DOScale(0.6f, period * 0.5f).SetEase(Ease.InOutSine))
            .Join(DOTween.To(() => graphic.canvasRenderer.GetAlpha(), a => graphic.canvasRenderer.SetAlpha(a), 0.35f, period * 0.5f))
            .SetDelay(Random.Range(0f, 1.5f))
            .SetLoops(-1)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    void OnDisable() => loop?.Kill();
}
