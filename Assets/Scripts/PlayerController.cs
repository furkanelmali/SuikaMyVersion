using System.Collections;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(MeshCollider))]
public class PlayerController : MonoBehaviour
{
    public GameObject boundary;
    private Vector3 screenPoint;
    private float offsetX;
    private bool isDragging = false;
    public bool controller = true;
    private Collider boundaryCollider;
    public GameObject currentFallObject;

    ObjectSpawner spawner;

    void Start()
    {
        spawner = FindObjectOfType<ObjectSpawner>();
        controller = true;

        if (boundary != null)
            boundaryCollider = boundary.GetComponent<Collider>();
    }

    void Update()
    {
        if (!controller || spawner == null)
            return;

        CharacterController(Time.timeScale);
    }

    public Vector3 ClampPositionToBoundary(Vector3 position, float objectDimension)
    {
        if (boundaryCollider == null)
            return position;

        Bounds bounds = boundaryCollider.bounds;
        float halfWidth = objectDimension * 0.5f;
        position.x = Mathf.Clamp(position.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth);
        return position;
    }

    public void CharacterController(float timePoint)
    {
        if (timePoint <= 0)
            return;

        if (Input.GetMouseButtonDown(0) && currentFallObject != null)
        {
            isDragging = true;
            screenPoint = Camera.main.WorldToScreenPoint(currentFallObject.transform.position);

            Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, screenPoint.y, screenPoint.z);
            Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint);
            Vector3 newPosition = new Vector3(curPosition.x, currentFallObject.transform.position.y, currentFallObject.transform.position.z);

            newPosition = ClampPositionToBoundary(newPosition, spawner.currentObjectDimension);
            ApplyDragPosition(newPosition);

            offsetX = currentFallObject.transform.position.x - Camera.main.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, screenPoint.y, screenPoint.z)).x;
        }

        if (Input.GetMouseButtonUp(0) && currentFallObject != null)
        {
            Transform fallTransform = currentFallObject.transform;
            fallTransform.DOKill();
            fallTransform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 6, 0.4f);

            currentFallObject.GetComponent<ObjectController>().FallController();
            StartCoroutine(spawner.DelayedSpawn());
            currentFallObject = null;
            isDragging = false;
        }

        if (isDragging && currentFallObject != null)
        {
            Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, screenPoint.y, screenPoint.z);
            Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint);
            Vector3 newPosition = new Vector3(curPosition.x + offsetX, currentFallObject.transform.position.y, currentFallObject.transform.position.z);

            newPosition = ClampPositionToBoundary(newPosition, spawner.currentObjectDimension);
            ApplyDragPosition(newPosition);
        }
    }

    void ApplyDragPosition(Vector3 worldPosition)
    {
        if (currentFallObject == null)
            return;

        currentFallObject.transform.position = worldPosition;

        Vector3 dropBoxPos = transform.position;
        dropBoxPos.x = worldPosition.x;
        transform.position = dropBoxPos;
    }
}
