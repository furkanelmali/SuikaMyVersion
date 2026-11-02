using System.Collections;
using DG.Tweening;
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

    // After a rewarded continue the end sequence has released the held object; hand the player a new one.
    public void EnsureHeldObject()
    {
        if (spawnCoroutine != null)
            return;

        GameObject held = playerController != null ? playerController.currentFallObject : null;
        if (held != null && held.activeInHierarchy)
        {
            playerController.AssignCurrentObject(held);
            return;
        }

        SpawnObject(spawnPosition.position, spawnRotation);
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

        // The new piece pops into the dropper, which gives a little bounce.
        Transform piece = spawnedObject.transform;
        Vector3 scale = piece.localScale;
        piece.DOKill(true);
        piece.localScale = scale * 0.2f;
        piece.DOScale(scale, 0.32f).SetEase(Ease.OutBack, 2.5f);
        if (dropBox != null)
        {
            dropBox.transform.DOKill(true);
            dropBox.transform.DOPunchScale(new Vector3(-0.08f, 0.1f, 0f), 0.3f, 6, 0.5f);
        }

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

        // The NEXT bubble bounces and the new preview pops in.
        Transform preview = nextObjectImages[nextObjectSpawning].transform;
        preview.DOKill(true);
        preview.localScale = Vector3.zero;
        preview.DOScale(1f, 0.35f).SetEase(Ease.OutBack, 2.5f).SetUpdate(true);
        Transform bubble = preview.parent;
        if (bubble != null)
        {
            bubble.DOKill(true);
            bubble.DOPunchScale(Vector3.one * 0.16f, 0.35f, 6, 0.6f).SetUpdate(true);
        }
    }
}
