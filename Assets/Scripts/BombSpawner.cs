using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombSpawner : MonoBehaviour
{
    public GameObject Bomb;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnBomb() 
    {
       GameObject newBomb = Instantiate(Bomb, transform.position, transform.rotation);
       newBomb.GetComponentInChildren<ParticleSystem>().Play();
    }
}
