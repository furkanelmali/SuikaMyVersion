using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_WEBGL
using Playgama;
using Playgama.Modules.Platform;
#endif

/// <summary>
/// The only gameplay-facing entry to Playgama Bridge. Mobile builds compile the pass-through
/// branch and keep PlayerPrefs; WebGL routes storage, pause/audio state and lifecycle through Bridge.
/// </summary>
public static class PlaygamaService
{
#if UNITY_WEBGL
    public static readonly bool IsBridge = true;
#else
    public static readonly bool IsBridge = false;
#endif

    public const string SaveKey = "mm_save";
    static readonly string[] BootKeys = { SaveKey };

    static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
    static readonly Dictionary<string, string> pending = new Dictionary<string, string>();
    static bool writing, gameReadySent, platformPaused, adOpen, focusLost;
    static float retryAt = -1f;

    public static bool Ready { get; private set; } = !IsBridge;
    public static bool PlatformAudioEnabled { get; private set; } = true;

    // Platform pause (tab hidden, platform overlay) or a full-screen ad.
    public static bool ExternalPause => platformPaused || adOpen;
    public static event Action<bool> ExternalPauseChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateRunner()
    {
#if UNITY_WEBGL
        // The page forwards Bridge events as soon as the player starts; the receiver must exist before the first scene.
        _ = Bridge.platform;
#endif
        GameObject runner = new GameObject("PlaygamaService");
        runner.hideFlags = HideFlags.HideInHierarchy;
        UnityEngine.Object.DontDestroyOnLoad(runner);
        runner.AddComponent<Runner>().StartCoroutine(Boot());
    }

    sealed class Runner : MonoBehaviour
    {
        void Update() => Tick();
        void OnApplicationFocus(bool focused) => SetFocus(focused);
    }

    // Loads every persistent key in one request. Gameplay starts at once; only saved values wait for this.
    static IEnumerator Boot()
    {
        if (Ready)
            yield break;
#if UNITY_WEBGL
        PlatformAudioEnabled = Bridge.platform.isAudioEnabled;
        Bridge.platform.audioStateChanged += enabled => { PlatformAudioEnabled = enabled; ApplyAudio(); };
        Bridge.platform.pauseStateChanged += SetPlatformPause;

        bool done = false;
        Bridge.storage.Get(new List<string>(BootKeys), (success, values) =>
        {
            if (success && values != null)
            {
                for (int i = 0; i < BootKeys.Length && i < values.Count; i++)
                {
                    if (!pending.ContainsKey(BootKeys[i]) && !string.IsNullOrEmpty(values[i]))
                        cache[BootKeys[i]] = values[i];
                }
            }
            done = true;
        });

        // Bridge resolves or rejects the request; the timeout only guards a broken host page.
        float giveUp = Time.realtimeSinceStartup + 10f;
        while (!done && Time.realtimeSinceStartup < giveUp)
            yield return null;

        ApplyAudio();
#endif
        Ready = true;
    }

    public static bool TryGet(string key, out string value) => cache.TryGetValue(key, out value) && !string.IsNullOrEmpty(value);

    // Memory is the source of truth for the session; Bridge receives every changed key in one call.
    public static void Save(string key, string value)
    {
        cache[key] = value;
        pending[key] = value;
        Flush();
    }

    static void Flush()
    {
#if UNITY_WEBGL
        if (writing || pending.Count == 0)
            return;

        var batch = new Dictionary<string, object>();
        foreach (var pair in pending)
            batch[pair.Key] = pair.Value;
        pending.Clear();
        writing = true;

        Bridge.storage.Set(batch, success =>
        {
            writing = false;
            if (!success)
            {
                foreach (var pair in batch)
                {
                    if (!pending.ContainsKey(pair.Key))
                        pending[pair.Key] = (string)pair.Value;
                }
                retryAt = Time.realtimeSinceStartup + 3f;
            }
            else
            {
                Flush();
            }
        });
#else
        pending.Clear();
#endif
    }

    // Only retries a failed write, never writes on its own.
    static void Tick()
    {
        if (retryAt >= 0f && Time.realtimeSinceStartup >= retryAt)
        {
            retryAt = -1f;
            Flush();
        }
    }

    public static void SendGameReady()
    {
        if (gameReadySent)
            return;
        gameReadySent = true;
#if UNITY_WEBGL
        Bridge.platform.SendMessage(PlatformMessage.GameReady);
#endif
    }

    public enum Level { Started, Paused, Resumed, Failed }

    public static void SendLevel(Level level)
    {
#if UNITY_WEBGL
        PlatformMessage message = level == Level.Started ? PlatformMessage.LevelStarted
            : level == Level.Paused ? PlatformMessage.LevelPaused
            : level == Level.Resumed ? PlatformMessage.LevelResumed
            : PlatformMessage.LevelFailed;
        Bridge.platform.SendMessage(message);
#endif
    }

    internal static void SetAdOpen(bool open)
    {
        if (adOpen == open)
            return;
        bool before = ExternalPause;
        adOpen = open;
        Changed(before);
    }

    static void SetPlatformPause(bool paused)
    {
        if (platformPaused == paused)
            return;
        bool before = ExternalPause;
        platformPaused = paused;
        Changed(before);
    }

    static void SetFocus(bool focused)
    {
        focusLost = !focused;
        ApplyAudio();
    }

    static void Changed(bool before)
    {
        ApplyAudio();
        if (before != ExternalPause)
            ExternalPauseChanged?.Invoke(ExternalPause);
    }

    // Platform audio state, ads and hidden tabs silence the listener; the player's own sound toggles stay in the scene.
    static void ApplyAudio()
    {
        if (!IsBridge)
            return;
        AudioListener.pause = ExternalPause || focusLost;
        AudioListener.volume = PlatformAudioEnabled ? 1f : 0f;
    }
}
