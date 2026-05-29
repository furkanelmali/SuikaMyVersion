using UnityEngine;
using DG.Tweening;

public class CameraShake : MonoBehaviour
{
    static CameraShake _instance;
    // Unity-safe getter: returns true C# null if the underlying object was destroyed,
    // so callers using ?. won't hit a MissingReferenceException after a scene reload.
    public static CameraShake Instance => _instance != null ? _instance : null;

    [SerializeField] float mergeShakeDuration = 0.12f;
    [SerializeField] float gameOverShakeDuration = 0.08f;
    [SerializeField] float maxDangerShake = 0.35f;
    [SerializeField] float dangerShakeFrequency = 18f;

    Vector3 originalLocalPosition;
    Tween activeTween;

    bool dangerActive;
    float dangerIntensity;
    float noiseSeedX;
    float noiseSeedY;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;
        originalLocalPosition = transform.localPosition;
        noiseSeedX = Random.value * 100f;
        noiseSeedY = Random.value * 100f;
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    void LateUpdate()
    {
        if (!dangerActive)
            return;

        float strength = dangerIntensity * maxDangerShake;
        float t = Time.unscaledTime * dangerShakeFrequency;

        float offsetX = (Mathf.PerlinNoise(noiseSeedX, t) - 0.5f) * 2f * strength;
        float offsetY = (Mathf.PerlinNoise(noiseSeedY, t) - 0.5f) * 2f * strength;

        transform.localPosition = originalLocalPosition + new Vector3(offsetX, offsetY, 0f);
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

    /// <summary>
    /// Sustained escalating shake driven by danger ratio (0..1).
    /// </summary>
    public void SetDangerShake(float ratio)
    {
        dangerActive = true;
        dangerIntensity = Mathf.Clamp01(ratio);
    }

    public void StopDangerShake()
    {
        dangerActive = false;
        dangerIntensity = 0f;
        transform.localPosition = originalLocalPosition;
    }

    void Shake(float strength, float duration)
    {
        if (dangerActive)
            return;

        activeTween?.Kill();
        transform.localPosition = originalLocalPosition;
        activeTween = transform.DOShakePosition(duration, strength, 12, 90f, false, true)
            .OnComplete(() => transform.localPosition = originalLocalPosition);
    }
}
