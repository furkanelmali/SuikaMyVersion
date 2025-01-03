using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class ObjectController : MonoBehaviour
{
    public MergeObject objectData;
    public int scorePoint, rank;
    public GameObject nextRankPrefab;
    public Collider[] collidersInObject;
    public float objectDimension;
    public bool isDead;
    
    // Start is called before the first frame update
    void Awake()
    {
        SetData();
        collidersInObject = GetComponents<Collider>();
        foreach (Collider collider in collidersInObject)
        {
            Bounds bounds = collider.bounds;
            objectDimension += bounds.size.x;
            collider.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void FallController()
    {
        this.gameObject.GetComponent<Rigidbody>().useGravity = true;
        

        foreach (Collider collider in collidersInObject)
        {
          collider.enabled = true; 
        }
        this.gameObject.transform.parent = null;
        
    }
    void SetData()
    {
        scorePoint = objectData.ScorePoint;
        rank = objectData.Rank;
        nextRankPrefab = objectData.NextObject;
        isDead = false;
    }
   
}
