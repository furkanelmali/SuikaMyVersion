using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_WEBGL
using Playgama;
using Playgama.Modules.Leaderboards;
#endif

/// <summary>
/// Best-score leaderboard through Playgama Bridge. The board id is declared in
/// Assets/WebGLTemplates/Bridge/playgama-bridge-config.json. Mobile builds report no leaderboard.
/// </summary>
public static class LeaderboardService
{
    public const string BoardId = "best_score";

    // InGame: the game draws the entries. NativePopup: the platform opens its own window.
    // Native: the platform shows scores elsewhere, the game only submits.
    public enum Kind { None, InGame, Native, NativePopup }

    public struct Entry
    {
        public int rank;
        public string id;
        public string name;
        public int score;
    }

    public static Kind Type
    {
        get
        {
#if UNITY_WEBGL
            switch (Bridge.leaderboards.type)
            {
                case LeaderboardType.InGame: return Kind.InGame;
                case LeaderboardType.Native: return Kind.Native;
                case LeaderboardType.NativePopup: return Kind.NativePopup;
            }
#endif
            return Kind.None;
        }
    }

    // Whether the game has a leaderboard screen or popup to open.
    public static bool CanOpen => Type == Kind.InGame || Type == Kind.NativePopup;

    public static bool CanAuthorize
    {
        get
        {
#if UNITY_WEBGL
            return Bridge.player.isAuthorizationSupported && !Bridge.player.isAuthorized;
#else
            return false;
#endif
        }
    }

    public static string PlayerId
    {
        get
        {
#if UNITY_WEBGL
            return Bridge.player.id;
#else
            return null;
#endif
        }
    }

    public static void Submit(int score)
    {
        if (score <= 0 || Type == Kind.None)
            return;
#if UNITY_WEBGL
        Bridge.leaderboards.SetScore(BoardId, score);
#endif
    }

    public static void Fetch(Action<bool, List<Entry>> done)
    {
#if UNITY_WEBGL
        if (Type != Kind.InGame)
        {
            done?.Invoke(false, null);
            return;
        }

        Bridge.leaderboards.GetEntries(BoardId, (success, rows) =>
        {
            var entries = new List<Entry>();
            if (success && rows != null)
            {
                foreach (Dictionary<string, string> row in rows)
                {
                    entries.Add(new Entry
                    {
                        rank = ReadInt(row, "rank"),
                        id = Read(row, "id"),
                        name = Read(row, "name"),
                        score = ReadInt(row, "score")
                    });
                }
                entries.Sort((a, b) => a.rank != b.rank ? a.rank.CompareTo(b.rank) : b.score.CompareTo(a.score));
            }
            done?.Invoke(success, entries);
        });
#else
        done?.Invoke(false, null);
#endif
    }

    public static void ShowNativePopup()
    {
#if UNITY_WEBGL
        Bridge.leaderboards.ShowNativePopup(BoardId);
#endif
    }

    // Optional sign-in from the leaderboard screen; never required to play.
    public static void Authorize(Action<bool> done)
    {
#if UNITY_WEBGL
        Bridge.player.Authorize(null, done);
#else
        done?.Invoke(false);
#endif
    }

    static string Read(Dictionary<string, string> row, string key) => row.TryGetValue(key, out string value) ? value : null;

    static int ReadInt(Dictionary<string, string> row, string key)
    {
        string value = Read(row, key);
        if (string.IsNullOrEmpty(value))
            return 0;
        return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float number)
            ? Mathf.RoundToInt(number)
            : 0;
    }
}
