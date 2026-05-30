using System.Collections.Generic;
using UnityEngine;

public class TriggerLoss : MonoBehaviour
{
    const float LossDelay = 4f;

    GameManager gameManager;
    float stayTime;
    bool gameOverTriggered;

    readonly HashSet<GameObject> objectsInZone = new HashSet<GameObject>();

    void Start()
    {
        if (CompareTag("MergeObject"))
        {
            enabled = false;
            return;
        }

        gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsValidMergeObject(other))
            objectsInZone.Add(other.gameObject);
    }

    void OnTriggerExit(Collider other)
    {
        objectsInZone.Remove(other.gameObject);

        if (objectsInZone.Count == 0)
        {
            stayTime = 0f;
            DangerZoneFeedback.Instance?.Reset();
        }
    }

    void Update()
    {
        if (gameOverTriggered || gameManager == null)
            return;

        objectsInZone.RemoveWhere(IsInvalid);

        if (objectsInZone.Count == 0)
        {
            if (stayTime > 0f)
            {
                stayTime = 0f;
                DangerZoneFeedback.Instance?.Reset();
            }
            return;
        }

        stayTime += Time.deltaTime;
        DangerZoneFeedback.Instance?.SetDangerProgress(stayTime, LossDelay);

        if (stayTime >= LossDelay)
        {
            gameOverTriggered = true;
            gameManager.GameOver();
        }
    }

    static bool IsValidMergeObject(Collider other)
    {
        if (other == null || !other.CompareTag("MergeObject"))
            return false;

        ObjectController controller = other.GetComponent<ObjectController>();
        if (controller == null || controller.isDead)
            return false;

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && !rb.useGravity)
            return false;

        return true;
    }

    static bool IsInvalid(GameObject obj)
    {
        if (obj == null || !obj.activeInHierarchy)
            return true;

        ObjectController controller = obj.GetComponent<ObjectController>();
        if (controller == null || controller.isDead)
            return true;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null && !rb.useGravity)
            return true;

        return false;
    }
}
