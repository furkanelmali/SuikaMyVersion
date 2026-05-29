using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFeelBootstrap : MonoBehaviour
{
    static GameFeelBootstrap instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        if (instance != null)
            return;

        GameObject bootstrap = new GameObject("GameFeelBootstrap");
        instance = bootstrap.AddComponent<GameFeelBootstrap>();
        DontDestroyOnLoad(bootstrap);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureManagers();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureManagers();
    }

    // Recreates per-scene feel managers. Camera and Canvas are destroyed on scene
    // reload, so their attached managers must be re-instantiated each load.
    void EnsureManagers()
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
