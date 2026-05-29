using UnityEngine;
using DG.Tweening;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [SerializeField] float mergeShakeDuration = 0.12f;
    [SerializeField] float gameOverShakeDuration = 0.08f;

    Vector3 originalLocalPosition;
    Tween activeTween;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        originalLocalPosition = transform.localPosition;
    }

    public void ShakeOnMerge(int rank)
    {
        float strength = 0.05f + rank * 0.01f;
        Shake(strength, mergeShakeDuration);
    }

    public void ShakeGameOverPop()
    {
        Shake(0.08f, gameOverShakeDuration);
    }

    public void ShakeDanger(float intensity)
    {
        Shake(intensity * 0.04f, 0.06f);
    }

    void Shake(float strength, float duration)
    {
        activeTween?.Kill();
        transform.localPosition = originalLocalPosition;
        activeTween = transform.DOShakePosition(duration, strength, 12, 90f, false, true)
            .OnComplete(() => transform.localPosition = originalLocalPosition);
    }
}
