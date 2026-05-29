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
        gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();
        Debug.Log($"[TriggerLoss] Start. gameManager bulundu mu: {gameManager != null}. Collider isTrigger: {GetComponent<Collider>()?.isTrigger}");
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[TriggerLoss] OnTriggerEnter -> {other.name} (tag: {other.tag})");

        if (IsValidMergeObject(other))
        {
            objectsInZone.Add(other.gameObject);
            Debug.Log($"[TriggerLoss] '{other.name}' bolgeye eklendi. Bolgedeki obje sayisi: {objectsInZone.Count}");
        }
        else
        {
            Debug.Log($"[TriggerLoss] '{other.name}' gecerli MergeObject degil, sayilmadi.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (objectsInZone.Remove(other.gameObject))
            Debug.Log($"[TriggerLoss] '{other.name}' bolgeden cikti. Kalan obje sayisi: {objectsInZone.Count}");

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

        Debug.Log($"[TriggerLoss] Bolgede {objectsInZone.Count} obje var. stayTime: {stayTime:F2} / {LossDelay}");

        if (stayTime >= LossDelay)
        {
            gameOverTriggered = true;
            Debug.Log("[TriggerLoss] SURE DOLDU -> GameOver() cagriliyor!");
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
        {
            Debug.Log($"[TriggerLoss] '{other.name}' henuz birakilmamis (gravity kapali), sayilmiyor.");
            return false;
        }

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
