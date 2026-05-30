using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(MeshCollider))]
public class PlayerController : MonoBehaviour
{
    enum ControlState
    {
        Ready,
        Dragging,
        WaitingForSpawn,
        Locked
    }

    public GameObject boundary;
    public bool controller = true;
    public GameObject currentFallObject;

    [SerializeField] float dragFollowSpeed = 24f;
    [SerializeField] float edgeFeedbackCooldown = 0.15f;
    [SerializeField] float edgePunchScale = 0.035f;

    Vector3 screenPoint;
    Vector3 targetDragPosition;
    float offsetX;
    bool isDragging;
    bool inputLocked;
    int activePointerId = int.MinValue;
    float lastEdgeFeedbackTime = -999f;
    Collider boundaryCollider;
    ControlState state = ControlState.WaitingForSpawn;

    ObjectSpawner spawner;
    Camera mainCamera;
    ObjectController currentFallController;
    static readonly List<RaycastResult> UiRaycastResults = new List<RaycastResult>();

    void Start()
    {
        spawner = FindObjectOfType<ObjectSpawner>();
        mainCamera = Camera.main;
        controller = true;

        if (boundary != null)
            boundaryCollider = boundary.GetComponent<Collider>();

        if (currentFallObject != null)
            AssignCurrentObject(currentFallObject);
    }

    void Update()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (!controller)
        {
            CancelDrag();
            return;
        }

        if (inputLocked || spawner == null || mainCamera == null)
            return;

        CharacterController(Time.timeScale);
    }

    public Vector3 ClampPositionToBoundary(Vector3 position, float objectDimension)
    {
        return ClampPositionToBoundary(position, objectDimension, out _);
    }

    Vector3 ClampPositionToBoundary(Vector3 position, float objectDimension, out bool clamped)
    {
        if (boundaryCollider == null)
        {
            clamped = false;
            return position;
        }

        Bounds bounds = boundaryCollider.bounds;
        float halfWidth = objectDimension * 0.5f;
        float originalX = position.x;
        position.x = Mathf.Clamp(originalX, bounds.min.x + halfWidth, bounds.max.x - halfWidth);
        clamped = !Mathf.Approximately(originalX, position.x);
        return position;
    }

    public void CharacterController(float timePoint)
    {
        if (timePoint <= 0)
            return;

        if (state == ControlState.Ready)
        {
            if (TryGetPointerDown(out Vector2 pointerPosition, out int pointerId))
                BeginDrag(pointerPosition, pointerId);
        }

        if (state != ControlState.Dragging)
            return;

        if (TryGetActivePointerUp(out bool canceled))
        {
            if (canceled)
                CancelDrag();
            else
                DropCurrentObject();

            return;
        }

        if (TryGetActivePointerPosition(out Vector2 activePointerPosition))
        {
            targetDragPosition = GetDragWorldPosition(activePointerPosition);
            ApplyDragPosition(targetDragPosition, immediate: false);
        }
    }

    public void AssignCurrentObject(GameObject obj)
    {
        currentFallObject = obj;
        currentFallController = obj != null ? obj.GetComponent<ObjectController>() : null;
        isDragging = false;
        activePointerId = int.MinValue;

        if (obj != null)
            targetDragPosition = obj.transform.position;

        state = inputLocked ? ControlState.Locked : (obj != null ? ControlState.Ready : ControlState.WaitingForSpawn);
    }

    public void NotifyWaitingForSpawn()
    {
        state = inputLocked ? ControlState.Locked : ControlState.WaitingForSpawn;
        isDragging = false;
        activePointerId = int.MinValue;
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        if (locked)
        {
            CancelDrag();
            state = ControlState.Locked;
            return;
        }

        state = currentFallObject != null ? ControlState.Ready : ControlState.WaitingForSpawn;
    }

    void BeginDrag(Vector2 pointerPosition, int pointerId)
    {
        if (currentFallObject == null)
            return;

        currentFallController = currentFallObject.GetComponent<ObjectController>();
        if (currentFallController == null)
            return;

        isDragging = true;
        activePointerId = pointerId;
        state = ControlState.Dragging;

        screenPoint = mainCamera.WorldToScreenPoint(currentFallObject.transform.position);
        Vector3 pointerWorld = ScreenToHeldObjectPlane(pointerPosition);
        offsetX = currentFallObject.transform.position.x - pointerWorld.x;

        targetDragPosition = GetDragWorldPosition(pointerPosition);
        ApplyDragPosition(targetDragPosition, immediate: true);
    }

    void DropCurrentObject()
    {
        if (currentFallObject == null || currentFallController == null)
        {
            CancelDrag();
            return;
        }

        Transform fallTransform = currentFallObject.transform;
        fallTransform.DOKill();
        fallTransform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 6, 0.4f);

        currentFallController.FallController();
        spawner.BeginDelayedSpawn();

        currentFallObject = null;
        currentFallController = null;
        isDragging = false;
        activePointerId = int.MinValue;
        state = ControlState.WaitingForSpawn;
    }

    void CancelDrag()
    {
        isDragging = false;
        activePointerId = int.MinValue;

        if (currentFallObject != null && !inputLocked)
            state = ControlState.Ready;
    }

    Vector3 GetDragWorldPosition(Vector2 pointerPosition)
    {
        Vector3 pointerWorld = ScreenToHeldObjectPlane(pointerPosition);
        Vector3 newPosition = new Vector3(pointerWorld.x + offsetX, currentFallObject.transform.position.y, currentFallObject.transform.position.z);
        newPosition = ClampPositionToBoundary(newPosition, spawner.currentObjectDimension, out bool clamped);

        if (clamped)
            PlayBoundaryFeedback();

        return newPosition;
    }

    Vector3 ScreenToHeldObjectPlane(Vector2 pointerPosition)
    {
        Vector3 curScreenPoint = new Vector3(pointerPosition.x, screenPoint.y, screenPoint.z);
        return mainCamera.ScreenToWorldPoint(curScreenPoint);
    }

    void ApplyDragPosition(Vector3 worldPosition, bool immediate)
    {
        if (currentFallObject == null)
            return;

        Vector3 nextPosition = immediate
            ? worldPosition
            : Vector3.Lerp(currentFallObject.transform.position, worldPosition, 1f - Mathf.Exp(-dragFollowSpeed * Time.deltaTime));

        currentFallObject.transform.position = nextPosition;

        Vector3 dropBoxPos = transform.position;
        dropBoxPos.x = nextPosition.x;
        transform.position = dropBoxPos;
    }

    void PlayBoundaryFeedback()
    {
        if (currentFallObject == null || Time.time - lastEdgeFeedbackTime < edgeFeedbackCooldown)
            return;

        lastEdgeFeedbackTime = Time.time;
        currentFallObject.transform.DOPunchScale(Vector3.one * edgePunchScale, 0.12f, 4, 0.35f);
    }

    bool TryGetPointerDown(out Vector2 pointerPosition, out int pointerId)
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began || IsPointerOverBlockingUI(touch.position))
                    continue;

                pointerPosition = touch.position;
                pointerId = touch.fingerId;
                return true;
            }
        }
        else if (Input.GetMouseButtonDown(0) && !IsPointerOverBlockingUI(Input.mousePosition))
        {
            pointerPosition = Input.mousePosition;
            pointerId = -1;
            return true;
        }

        pointerPosition = default;
        pointerId = int.MinValue;
        return false;
    }

    bool TryGetActivePointerPosition(out Vector2 pointerPosition)
    {
        if (activePointerId >= 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId == activePointerId)
                {
                    pointerPosition = touch.position;
                    return true;
                }
            }

            pointerPosition = default;
            return false;
        }

        pointerPosition = Input.mousePosition;
        return Input.GetMouseButton(0);
    }

    bool TryGetActivePointerUp(out bool canceled)
    {
        canceled = false;

        if (activePointerId >= 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId != activePointerId)
                    continue;

                canceled = touch.phase == TouchPhase.Canceled;
                return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            }

            canceled = true;
            return true;
        }

        return Input.GetMouseButtonUp(0);
    }

    static bool IsPointerOverBlockingUI(Vector2 pointerPosition)
    {
        if (EventSystem.current == null)
            return false;

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = pointerPosition
        };

        UiRaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, UiRaycastResults);

        foreach (RaycastResult result in UiRaycastResults)
        {
            if (result.gameObject == null)
                continue;

            if (result.gameObject.GetComponentInParent<Selectable>() != null)
                return true;
        }

        return false;
    }
}
