using System.Collections;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject[] objectsToSpawn;
    public int spawnNumber, spawnOrder, nextObjectSpawning;
    public Transform spawnPosition;
    public Quaternion spawnRotation = Quaternion.identity;
    public GameObject dropBox;
    public float currentObjectDimension;

    public GameObject[] nextObjectImages;

    PlayerController playerController;
    MergeObjectPool mergeObjectPool;
    Coroutine spawnCoroutine;
    static readonly WaitForSeconds SpawnDelay = new WaitForSeconds(1f);

    void Start()
    {
        dropBox = GameObject.FindGameObjectWithTag("DropBox");
        playerController = FindObjectOfType<PlayerController>();
        mergeObjectPool = FindObjectOfType<MergeObjectPool>();

        if (mergeObjectPool == null)
        {
            GameObject poolObject = new GameObject("MergeObjectPool");
            mergeObjectPool = poolObject.AddComponent<MergeObjectPool>();
        }

        mergeObjectPool.RegisterPrefabChain(objectsToSpawn);

        spawnNumber = 0;
        SpawnObject(spawnPosition.position, spawnRotation);
        spawnOrder = 1;
    }

    public IEnumerator DelayedSpawn()
    {
        yield return SpawnDelay;
        SpawnObject(spawnPosition.position, spawnRotation);
        spawnCoroutine = null;
    }

    public void BeginDelayedSpawn()
    {
        if (spawnCoroutine != null)
            return;

        if (playerController != null)
            playerController.NotifyWaitingForSpawn();

        spawnCoroutine = StartCoroutine(DelayedSpawn());
    }

    public void SpawnObject(Vector3 position, Quaternion rotation)
    {
        spawnNumber = nextObjectSpawning;
        GameObject prefab = objectsToSpawn[spawnNumber];
        if (prefab == null)
        {
            Debug.LogError("objectToSpawn is not assigned.");
            return;
        }

        GameObject spawnedObject = mergeObjectPool != null
            ? mergeObjectPool.Get(prefab, position, rotation)
            : Instantiate(prefab, position, rotation);

        spawnedObject.transform.SetParent(dropBox.transform, false);
        spawnedObject.transform.position = position;
        spawnedObject.transform.rotation = rotation;
        ObjectController objectController = spawnedObject.GetComponent<ObjectController>();
        currentObjectDimension = objectController != null ? objectController.objectDimension : 0f;

        if (playerController != null)
            playerController.AssignCurrentObject(spawnedObject);

        spawnOrder++;
        ChoosingSpawnObject();
    }

    void ChoosingSpawnObject()
    {
        if (spawnOrder <= 3)
            nextObjectSpawning = 0;
        else if (spawnOrder == 4)
            nextObjectSpawning = 1;
        else if (spawnOrder == 5)
            nextObjectSpawning = 2;
        else if (spawnOrder == 7)
            nextObjectSpawning = 3;
        else if (spawnOrder > 7)
            nextObjectSpawning = Random.Range(0, 5);

        activatingNextObjectsImage();
    }

    void activatingNextObjectsImage()
    {
        for (int i = 0; i < 5; i++)
            nextObjectImages[i].SetActive(i == nextObjectSpawning);
    }
}
