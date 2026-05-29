using UnityEngine;

public class ObjectMerge : MonoBehaviour
{
    ObjectController controller;
    GameManager gameManager;
    int rank;
    GameObject nextRankPrefab;
    PlayerController playerController;
    private bool hasMerged = false;

    public AudioClip[] clips;

    static AudioSource collisionSoundSource;
    static AudioSource mergeSoundSource;
    static float lastCollisionSoundTime;
    const float CollisionSoundCooldown = 0.1f;
    const float MinCollisionVelocity = 1.5f;

    void Awake()
    {
        controller = GetComponent<ObjectController>();
    }

    void Start()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        RefreshMergeData();
        CacheAudioSources();
    }

    void OnEnable()
    {
        RefreshMergeData();
    }

    public void ResetForPool()
    {
        hasMerged = false;
        RefreshMergeData();
    }

    void RefreshMergeData()
    {
        if (controller == null)
            controller = GetComponent<ObjectController>();
        if (controller == null)
            return;

        rank = controller.rank;
        nextRankPrefab = controller.nextRankPrefab;
    }

    static void CacheAudioSources()
    {
        if (collisionSoundSource != null && mergeSoundSource != null)
            return;

        GameObject soundsManager = GameObject.FindGameObjectWithTag("SoundManager");
        if (soundsManager != null)
            collisionSoundSource = soundsManager.GetComponent<AudioSource>();

        GameObject mergeSoundsManager = GameObject.FindGameObjectWithTag("MergeSoundManager");
        if (mergeSoundsManager != null)
            mergeSoundSource = mergeSoundsManager.GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision collision)
    {
        ObjectMerge otherObject = collision.collider.GetComponentInParent<ObjectMerge>();
        if (otherObject == null || otherObject == this)
            return;

        RefreshMergeData();
        otherObject.RefreshMergeData();

        if (controller == null || otherObject.controller == null)
            return;

        if (controller.rank != 11 && otherObject.controller.rank == controller.rank)
        {
            if (hasMerged || otherObject.hasMerged)
                return;

            Vector3 mergePosition = (transform.position + otherObject.transform.position) / 2;

            if (nextRankPrefab != null)
            {
                GameObject mergedObject = SpawnMergedObject(mergePosition);
                if (mergedObject != null)
                {
                    Rigidbody mergedRb = mergedObject.GetComponent<Rigidbody>();
                    if (mergedRb != null)
                        mergedRb.useGravity = true;

                    ParticleSystem particles = mergedObject.GetComponentInChildren<ParticleSystem>();
                    if (particles != null)
                        particles.Play();

                    foreach (Collider c in mergedObject.GetComponents<Collider>())
                        c.enabled = true;

                    MergeJuice.Instance?.PlayMergeEffects(mergedObject, controller.rank, controller.scorePoint);
                }
            }

            ReleaseOrDestroy(gameObject);
            ReleaseOrDestroy(collision.gameObject);

            hasMerged = true;
            otherObject.hasMerged = true;

            gameManager.AddScore(controller.scorePoint);
            PlayMergeSound(controller.rank);
        }
        else
        {
            TryPlayCollisionSound(collision);
        }
    }

    GameObject SpawnMergedObject(Vector3 position)
    {
        if (MergeObjectPool.Instance != null)
            return MergeObjectPool.Instance.Get(nextRankPrefab, position, Quaternion.identity);

        GameObject mergedObject = Instantiate(nextRankPrefab, position, Quaternion.identity);
        return mergedObject;
    }

    void PlayMergeSound(int mergeRank)
    {
        if (mergeSoundSource == null)
            return;

        mergeSoundSource.pitch = Random.Range(0.95f, 1.05f);

        if (clips != null && clips.Length > 0)
        {
            int clipIndex = Mathf.Clamp(mergeRank - 1, 0, clips.Length - 1);
            mergeSoundSource.PlayOneShot(clips[clipIndex]);
        }
        else
        {
            mergeSoundSource.Play();
        }

        if (mergeRank >= 7)
            HapticFeedback.VibrateLight();
    }

    void TryPlayCollisionSound(Collision collision)
    {
        if (collisionSoundSource == null)
            return;

        if (collision.relativeVelocity.magnitude < MinCollisionVelocity)
            return;

        if (Time.time - lastCollisionSoundTime < CollisionSoundCooldown)
            return;

        lastCollisionSoundTime = Time.time;
        collisionSoundSource.pitch = Random.Range(0.98f, 1.02f);
        collisionSoundSource.Play();
    }

    static void ReleaseOrDestroy(GameObject obj)
    {
        if (MergeObjectPool.Instance != null)
            MergeObjectPool.Instance.Release(obj);
        else
            Destroy(obj);
    }
}
