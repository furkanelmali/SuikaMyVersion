using UnityEngine;

public class BombMechanic : MonoBehaviour
{
    float detectionRadius = 2;
    Collider[] objectsInRange;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    public void ObjectDestroyer()
    {
        objectsInRange = Physics.OverlapSphere(transform.position, detectionRadius);

        foreach (Collider obj in objectsInRange)
        {
            if (!obj.CompareTag("MergeObject"))
                continue;

            if (MergeObjectPool.Instance != null)
                MergeObjectPool.Instance.Release(obj.gameObject);
            else
                Destroy(obj.gameObject);
        }

        Destroy(gameObject);
    }
}
