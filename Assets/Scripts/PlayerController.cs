using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

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

    BombController bombController;
    ObjectSpawner spawner;
    UIManager UI;

    

    void Start()
    {
        spawner = FindObjectOfType<ObjectSpawner>();
        UI = FindObjectOfType<UIManager>();
        controller = true;
        

        if (boundary != null)
        {
            boundaryCollider = boundary.GetComponent<Collider>();
        }
    }

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.nearClipPlane;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePos);
        Bounds bounds = boundaryCollider.bounds;

        if(mouseWorldPos.x >= bounds.min.x && mouseWorldPos.x <= bounds.max.x) 
           CharacterController(Time.timeScale); 
    }

    public Vector3 ClampPositionToBoundary(Vector3 position, float objectDimension)
    {
        if (boundaryCollider == null) return position;

        Bounds bounds = boundaryCollider.bounds;

        float objectDimensiondivided = objectDimension / 2;
       
        position.x = Mathf.Clamp(position.x, bounds.min.x + objectDimensiondivided, bounds.max.x - objectDimensiondivided);

        return position;
    }

    public void CharacterController(float timePoint) 
    {
        
        if (timePoint>0 && controller)
        {
            if (Input.GetMouseButtonDown(0))
            {
                
                    isDragging = true;

                    screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);

                        Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, screenPoint.y, screenPoint.z);
                        Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint);
                    Vector3 newPosition = new Vector3(curPosition.x, transform.position.y, transform.position.z);

                    transform.position = ClampPositionToBoundary(newPosition,spawner.currentObjectDimension);

                    offsetX = transform.position.x - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, screenPoint.y, screenPoint.z)).x;
                
            }

            if (Input.GetMouseButtonUp(0))
            {
                    currentFallObject.GetComponent<ObjectController>().FallController();
                    StartCoroutine(spawner.DelayedSpawn());
                    currentFallObject = null;
                    isDragging = false;
            }

            if (isDragging)
            {
                Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, screenPoint.y, screenPoint.z);
                Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint);
                Vector3 newPosition = new Vector3(curPosition.x + offsetX, transform.position.y, transform.position.z);

                transform.position = ClampPositionToBoundary(newPosition, spawner.currentObjectDimension);
            }
        }
    }
}