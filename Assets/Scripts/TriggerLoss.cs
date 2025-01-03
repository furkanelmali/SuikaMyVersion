using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerLoss : MonoBehaviour
{
    GameManager gameManager;
    float stayTime;
    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "GameOver")
        { 
            stayTime += Time.deltaTime;
            if (stayTime > 4f)
            {
                gameManager.GameOver();
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        stayTime = 0;
    }
}
