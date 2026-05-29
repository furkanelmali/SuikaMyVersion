using UnityEngine;

public class GameFeelBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        if (FindObjectOfType<GameFeelBootstrap>() != null)
            return;

        GameObject bootstrap = new GameObject("GameFeelBootstrap");
        bootstrap.AddComponent<GameFeelBootstrap>();
    }

    void Awake()
    {
        if (CameraShake.Instance == null && Camera.main != null)
            Camera.main.gameObject.AddComponent<CameraShake>();

        if (MergeJuice.Instance == null)
        {
            GameObject juice = new GameObject("MergeJuice");
            juice.AddComponent<MergeJuice>();
        }

        if (DangerZoneFeedback.Instance == null)
        {
            GameObject danger = new GameObject("DangerZoneFeedback");
            danger.AddComponent<DangerZoneFeedback>();
        }
    }
}
