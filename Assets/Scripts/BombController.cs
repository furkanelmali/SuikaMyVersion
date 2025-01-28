using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombController : MonoBehaviour
{
    public float radius = 3f;
    public int numSegments = 50;
    private LineRenderer lineRenderer;

    public bool isDragging = false; 
    private Vector3 offset; 
    private Camera mainCamera;

    BombMechanic BombMechanic;
    PlayerController playerController;

    Animator animator;
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        BombMechanic = GetComponent<BombMechanic>();
        playerController = FindObjectOfType<PlayerController>();
        
        lineRenderer.positionCount = numSegments + 1;
        lineRenderer.useWorldSpace = false;
        mainCamera = Camera.main;
        
    }

    void DrawCircle()
    {
        float deltaTheta = (2f * Mathf.PI) / numSegments;
        float theta = 0f;

        for (int i = 0; i < numSegments + 1; i++)
        {
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            theta += deltaTheta;
        }
    }

    void Update()
    {
        
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            

            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform)
                {   
                    this.gameObject.GetComponent<Animator>().enabled = false;
                    isDragging = true;
                    playerController.enabled = false;
                    radius = 3f;
                    DrawCircle();
                    offset = transform.position - GetMouseWorldPosition();
                    transform.position = new Vector3(transform.position.x, transform.position.y, -8f);
                    playerController.controller = false;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            playerController.enabled = true;        
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;


            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform)
                {
                    
                    isDragging = false;
                    transform.position = new Vector3(transform.position.x, transform.position.y, -6.43f);
                    DrawCircle();
                    
                    BombMechanic.ObjectDestroyer();
                    playerController.controller = true;
                }
            }
        }

       
        if (isDragging)
        {
            transform.position = GetMouseWorldPosition() + offset; 
        }
    }

   
    public Vector3 GetMouseWorldPosition()
    {
        
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = mainCamera.WorldToScreenPoint(transform.position).z; 
        return mainCamera.ScreenToWorldPoint(mouseScreenPosition); 
    }
}

