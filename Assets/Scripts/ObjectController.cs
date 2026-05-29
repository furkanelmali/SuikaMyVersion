using DG.Tweening;
using UnityEngine;

public class ObjectController : MonoBehaviour
{
    public MergeObject objectData;
    public int scorePoint, rank;
    public GameObject nextRankPrefab;
    public Collider[] collidersInObject;
    public float objectDimension;
    public bool isDead;

    [HideInInspector] public GameObject poolPrefabKey;

    Rigidbody rb;
    Vector3 defaultLocalScale;
    float cachedObjectDimension;
    static PhysicsMaterial sharedPhysicsMaterial;

    void Awake()
    {
        defaultLocalScale = transform.localScale;
        SetData();
        rb = GetComponent<Rigidbody>();
        collidersInObject = GetComponents<Collider>();

        foreach (Collider collider in collidersInObject)
        {
            collider.enabled = true;
            ApplyPhysicsMaterial(collider);
        }

        cachedObjectDimension = CalculateObjectDimension();
        objectDimension = cachedObjectDimension;

        foreach (Collider collider in collidersInObject)
            collider.enabled = false;

        if (rb != null)
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    float CalculateObjectDimension()
    {
        float dimension = 0f;
        foreach (Collider collider in collidersInObject)
        {
            if (collider != null)
                dimension += collider.bounds.size.x;
        }
        return dimension;
    }

    void OnEnable()
    {
        if (CompareTag("MergeObject") && GameManager.Instance != null)
            GameManager.Instance.RegisterMergeObject(gameObject);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterMergeObject(gameObject);
    }

    public void FallController()
    {
        if (rb != null)
            rb.useGravity = true;

        foreach (Collider collider in collidersInObject)
            collider.enabled = true;

        transform.parent = null;
    }

    public void ResetForPool()
    {
        isDead = false;
        SetData();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        objectDimension = cachedObjectDimension;

        foreach (Collider collider in collidersInObject)
            collider.enabled = false;

        transform.DOKill();
        transform.localScale = defaultLocalScale;
    }

    void SetData()
    {
        if (objectData == null)
            return;

        scorePoint = objectData.ScorePoint;
        rank = objectData.Rank;
        nextRankPrefab = objectData.NextObject;
        isDead = false;
    }

    static void ApplyPhysicsMaterial(Collider collider)
    {
        if (sharedPhysicsMaterial == null)
        {
            sharedPhysicsMaterial = new PhysicsMaterial("MergeObjectMaterial")
            {
                dynamicFriction = 0.4f,
                staticFriction = 0.4f,
                bounciness = 0.05f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        collider.material = sharedPhysicsMaterial;
    }
}
