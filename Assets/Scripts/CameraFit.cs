using UnityEngine;

/// <summary>
/// Keeps the whole container in view on narrow (portrait) screens by widening the vertical field of view
/// until the container's width fits; wide screens keep the authored field of view.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    public float baseFieldOfView = 50f;
    public float containerLeft, containerRight;
    public float planeZ;
    public float margin = 0.8f;

    Camera cam;
    float lastAspect = -1f;

    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        if (Mathf.Approximately(cam.aspect, lastAspect))
            return;
        lastAspect = cam.aspect;

        float distance = Mathf.Abs(planeZ - transform.position.z);
        float halfWidth = Mathf.Max(Mathf.Abs(containerLeft - transform.position.x), Mathf.Abs(containerRight - transform.position.x)) + margin;
        float needed = 2f * Mathf.Atan(halfWidth / (distance * cam.aspect)) * Mathf.Rad2Deg;
        cam.fieldOfView = Mathf.Clamp(Mathf.Max(baseFieldOfView, needed), baseFieldOfView, 80f);
    }
}
