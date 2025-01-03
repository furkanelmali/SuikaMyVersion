using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombMechanic : MonoBehaviour
{
    float detectionRadius = 2;
    Collider[] objectsInRange;
   
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 3);
    }

    void Update()
    {
        
    }

    public void ObjectDestroyer()
    {
        objectsInRange = Physics.OverlapSphere(transform.position, detectionRadius);

        foreach (Collider obj in objectsInRange)
        {
            if (obj.gameObject.tag == "MergeObject")
            { Destroy(obj.gameObject); } 
        }

        Destroy(this.gameObject);  
    }
}
