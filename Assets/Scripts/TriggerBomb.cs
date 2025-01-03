using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerBomb : MonoBehaviour
{
    float stayTime;
    BombSpawner spawner;
    int inGameBombCount;
    public float timeSpawnDuration = 120f;
    bool isTimerActive;
    private void Start()
    {
        spawner = FindObjectOfType<BombSpawner>();
        isTimerActive = false;
    }
    private void Update() 
    {
        Timer();
    }
    private void OnTriggerStay(Collider other)
    {
        stayTime += Time.deltaTime;
        if (stayTime > 4f && isTimerActive==false && inGameBombCount < 2)
        {
            spawner.SpawnBomb();
            inGameBombCount++;
            isTimerActive = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        stayTime = 0;
    }

    void Timer() 
    {
        if (isTimerActive)
        {
            timeSpawnDuration -= Time.deltaTime;
            if (timeSpawnDuration <= 0)
            {
                isTimerActive = false;
                timeSpawnDuration = 120;
            }
        }
    }
}
