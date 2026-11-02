using UnityEngine;

public static class HapticFeedback
{
    const string PrefKey = "HapticEnabled";

    public static bool IsEnabled
    {
        get => GamePrefs.GetInt(PrefKey, 1) == 1;
        set => GamePrefs.SetInt(PrefKey, value ? 1 : 0);
    }

    public static void VibrateLight()
    {
        if (!IsEnabled)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}
