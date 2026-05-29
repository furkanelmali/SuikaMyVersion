using System.Collections.Generic;
using UnityEngine;

public class MergeObjectPool : MonoBehaviour
{
    public static MergeObjectPool Instance { get; private set; }

    [SerializeField] int prewarmPerPrefab = 2;
    Transform poolRoot;

    readonly Dictionary<GameObject, Queue<GameObject>> poolsByPrefab = new Dictionary<GameObject, Queue<GameObject>>();
    // Guards against the same instance being enqueued more than once (double-release),
    // which would let Get() hand out the same live object to multiple callers.
    readonly HashSet<GameObject> pooledObjects = new HashSet<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        poolRoot = new GameObject("MergeObjectPool_Root").transform;
        poolRoot.SetParent(transform);
    }

    public void RegisterPrefabs(GameObject[] prefabs)
    {
        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null || poolsByPrefab.ContainsKey(prefab))
                continue;

            poolsByPrefab[prefab] = new Queue<GameObject>();

            for (int i = 0; i < prewarmPerPrefab; i++)
            {
                GameObject instance = CreateInstance(prefab);
                // Tag the prewarmed instance so Release() pools it instead of destroying it.
                ObjectController controller = instance.GetComponent<ObjectController>();
                if (controller != null)
                    controller.poolPrefabKey = prefab;
                Release(instance);
            }
        }
    }

    public void RegisterPrefabChain(GameObject[] spawnPrefabs)
    {
        var visited = new HashSet<GameObject>();
        var queue = new Queue<GameObject>();

        foreach (GameObject prefab in spawnPrefabs)
        {
            if (prefab != null)
                queue.Enqueue(prefab);
        }

        while (queue.Count > 0)
        {
            GameObject prefab = queue.Dequeue();
            if (prefab == null || !visited.Add(prefab))
                continue;

            RegisterPrefabs(new[] { prefab });

            ObjectController controller = prefab.GetComponent<ObjectController>();
            if (controller != null && controller.objectData != null &&
                controller.objectData.NextObject != null)
            {
                queue.Enqueue(controller.objectData.NextObject);
            }
        }
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        if (!poolsByPrefab.ContainsKey(prefab))
            poolsByPrefab[prefab] = new Queue<GameObject>();

        Queue<GameObject> queue = poolsByPrefab[prefab];
        GameObject obj = null;

        // Pull a valid, currently-pooled instance from the queue.
        while (queue.Count > 0)
        {
            GameObject candidate = queue.Dequeue();
            if (candidate == null)
                continue;

            pooledObjects.Remove(candidate);
            obj = candidate;
            break;
        }

        if (obj == null)
            obj = CreateInstance(prefab);

        obj.transform.SetParent(null);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        ObjectController controller = obj.GetComponent<ObjectController>();
        if (controller != null)
        {
            controller.poolPrefabKey = prefab;
            controller.ResetForPool();
        }

        ObjectMerge merge = obj.GetComponent<ObjectMerge>();
        merge?.ResetForPool();

        return obj;
    }

    public void Release(GameObject obj)
    {
        if (obj == null)
            return;

        // Already pooled -> ignore. This is the key guard against the
        // "thousands of objects" / duplicate-spawn explosion.
        if (!pooledObjects.Add(obj))
            return;

        ObjectController controller = obj.GetComponent<ObjectController>();
        GameObject prefabKey = controller != null ? controller.poolPrefabKey : null;

        if (prefabKey == null)
        {
            pooledObjects.Remove(obj);
            Destroy(obj);
            return;
        }

        if (!poolsByPrefab.ContainsKey(prefabKey))
            poolsByPrefab[prefabKey] = new Queue<GameObject>();

        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterMergeObject(obj);

        controller.ResetForPool();

        ObjectMerge merge = obj.GetComponent<ObjectMerge>();
        merge?.ResetForPool();

        obj.SetActive(false);
        obj.transform.SetParent(poolRoot);
        poolsByPrefab[prefabKey].Enqueue(obj);
    }

    GameObject CreateInstance(GameObject prefab)
    {
        GameObject instance = Instantiate(prefab, poolRoot);
        instance.name = prefab.name;
        return instance;
    }
}
