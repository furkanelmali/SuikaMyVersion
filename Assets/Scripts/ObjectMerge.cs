using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectMerge : MonoBehaviour
{
    ObjectController controller;
    GameManager gameManager;
    int rank;
    GameObject nextRankPrefab;
    Collider[] collidersInMergedObject;
    PlayerController playerController;
    ParticleSystem particles;
    private bool hasMerged = false;
    public AudioClip[] clips;
    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<ObjectController>();
        gameManager = FindObjectOfType<GameManager>();
        playerController = FindObjectOfType<PlayerController>();
        
        rank = controller.rank;
        nextRankPrefab = controller.nextRankPrefab;
    }

    void OnCollisionEnter(Collision collision)
    {
        ObjectMerge otherObject = collision.gameObject.GetComponent<ObjectMerge>();
        GameObject soundsManager = GameObject.FindGameObjectWithTag("SoundManager");
        GameObject mergeSoundsManager = GameObject.FindGameObjectWithTag("MergeSoundManager");

        if (otherObject != null && otherObject.rank != 11 && otherObject.rank == rank)
        {
            
            if (hasMerged || otherObject.hasMerged)
                return;

            
            Vector3 mergePosition = (transform.position + otherObject.transform.position) / 2;

           
            if (nextRankPrefab != null)
            {
              
              GameObject mergedObject  = Instantiate(nextRankPrefab, mergePosition, Quaternion.identity);
              mergedObject.GetComponent<Rigidbody>().useGravity = true;
              particles = mergedObject.GetComponentInChildren<ParticleSystem>();
              
              particles.Play();
                
                collidersInMergedObject = mergedObject.GetComponents<Collider>();
              foreach (Collider c in collidersInMergedObject) 
              {
                c.enabled = true;
              }
            }

            Destroy(gameObject);
            Destroy(collision.gameObject);

            
            hasMerged = true;
            otherObject.hasMerged = true;

            gameManager.AddScore(controller.scorePoint);
            if (mergeSoundsManager.GetComponent<AudioSource>() != null)
            {
                mergeSoundsManager.GetComponent<AudioSource>().Play();
            }
        }
        else if(otherObject == null || otherObject.rank != rank || soundsManager.GetComponent<AudioSource>() != null)
        { 
            soundsManager.GetComponent<AudioSource>().Play();
        }
    }
}
