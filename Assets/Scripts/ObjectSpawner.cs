using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject[] objectsToSpawn;
    public int spawnNumber, spawnOrder, nextObjectSpawning;
    public Transform spawnPosition;
    public Quaternion spawnRotation = Quaternion.identity;
    public GameObject dropBox;
    public float currentObjectDimension;

    Collider[] collidersInSpawnedObjects;
    public GameObject[] nextObjectImages;
    void Start()
    {

        dropBox = GameObject.FindGameObjectWithTag("DropBox");
        spawnNumber = 0;
        SpawnObject(spawnPosition.position, spawnRotation);
        spawnOrder = 1;
    }

    private void Update()
    {

    }

    public IEnumerator DelayedSpawn()
    {
        Debug.Log("Object Delayed Spawned.");
        yield return new WaitForSeconds(1f);
        SpawnObject(spawnPosition.position, spawnRotation);
    }

    public void SpawnObject(Vector3 position, Quaternion rotation)
    {
        spawnNumber = nextObjectSpawning;
        if (objectsToSpawn[spawnNumber] != null)
        {
            GameObject spawnedObject = Instantiate(objectsToSpawn[spawnNumber], position, rotation);
            FindObjectOfType<PlayerController>().currentFallObject = spawnedObject;

            spawnedObject.transform.SetParent(dropBox.transform, false);
            spawnedObject.transform.position = position;
            spawnedObject.transform.rotation = rotation;
            currentObjectDimension = spawnedObject.GetComponent<ObjectController>().objectDimension;

            Debug.Log("Object Spawned.");
            spawnOrder++;
            ChoosingSpawnObject();


        }
        else
        {
            Debug.LogError("objectToSpawn is not assigned.");
        }



    }

    void ChoosingSpawnObject()
    {
        if (spawnOrder <= 3)
        {
            nextObjectSpawning = 0;
        }
        else if (spawnOrder == 4)
        {
            nextObjectSpawning = 1;
        }
        else if (spawnOrder == 5)
        {
            nextObjectSpawning = 2;
        }
        else if (spawnOrder == 7)
        {
            nextObjectSpawning = 3;
        }
        else if (spawnOrder > 7)
        {
            nextObjectSpawning = Random.Range(0, 5);
        }

        activatingNextObjectsImage();
    }

    void activatingNextObjectsImage()
    {
        for (int i = 0; i < 5; i++)
        {
            if (i == nextObjectSpawning)
            {
                nextObjectImages[i].active = true;
            }
            else
            {
                nextObjectImages[i].active = false;
            }
        }
    }
}
