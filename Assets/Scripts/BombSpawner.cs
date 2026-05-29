using UnityEngine;

public class BombSpawner : MonoBehaviour
{
    public GameObject Bomb;

    public void SpawnBomb()
    {
        GameObject newBomb = Instantiate(Bomb, transform.position, transform.rotation);
        ParticleSystem particle = newBomb.GetComponentInChildren<ParticleSystem>();
        if (particle != null)
            particle.Play();
    }
}
