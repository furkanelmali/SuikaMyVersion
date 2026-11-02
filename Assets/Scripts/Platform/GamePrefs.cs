using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Replaces PlayerPrefs in game code. PlayerPrefs on mobile/editor; on WebGL one Bridge Storage key
/// holding every value, written only on Save() (Playgama allows no other browser storage).
/// </summary>
public static class GamePrefs
{
    [Serializable] sealed class Entry { public string key; public int value; }
    [Serializable] sealed class Snapshot { public List<Entry> values = new List<Entry>(); }

    static readonly Dictionary<string, int> values = new Dictionary<string, int>();
    static bool loaded, dirty;

    static Dictionary<string, int> Values
    {
        get
        {
            if (loaded || !Ready)
                return values;

            // Stored values arrive once Bridge answered; anything set earlier this session wins.
            loaded = true;
            if (PlaygamaService.TryGet(PlaygamaService.SaveKey, out string json))
            {
                try
                {
                    Snapshot snapshot = JsonUtility.FromJson<Snapshot>(json);
                    if (snapshot?.values != null)
                    {
                        foreach (Entry entry in snapshot.values)
                        {
                            if (!string.IsNullOrEmpty(entry.key) && !values.ContainsKey(entry.key))
                                values[entry.key] = entry.value;
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // Corrupt data falls back to defaults.
                }
            }
            return values;
        }
    }

    // On WebGL, values read before Bridge Storage answered would be defaults; callers wait for this.
    public static bool Ready => PlaygamaService.Ready;

    public static int GetInt(string key, int fallback = 0)
    {
        if (!PlaygamaService.IsBridge)
            return PlayerPrefs.GetInt(key, fallback);
        return Values.TryGetValue(key, out int value) ? value : fallback;
    }

    public static void SetInt(string key, int value)
    {
        if (!PlaygamaService.IsBridge)
        {
            PlayerPrefs.SetInt(key, value);
            return;
        }

        if (Values.TryGetValue(key, out int old) && old == value)
            return;
        Values[key] = value;
        dirty = true;
    }

    public static void Save()
    {
        if (!PlaygamaService.IsBridge)
        {
            PlayerPrefs.Save();
            return;
        }

        if (!dirty || !Ready)
            return;
        dirty = false;

        var snapshot = new Snapshot();
        foreach (var pair in Values)
            snapshot.values.Add(new Entry { key = pair.Key, value = pair.Value });
        PlaygamaService.Save(PlaygamaService.SaveKey, JsonUtility.ToJson(snapshot));
    }
}
