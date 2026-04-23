using UnityEngine;

public static class UISfxInstaller
{
    public static void InstallOn(GameObject go, AudioSource src, AudioClip hover, AudioClip click, float volume = 1f)
    {
        if (go == null) return;
        var sfx = go.GetComponent<UIButtonSfx>();
        if (sfx == null) sfx = go.AddComponent<UIButtonSfx>();

        // Assign private serialized fields via reflection to keep UIButtonSfx simple.
        var t = typeof(UIButtonSfx);
        t.GetField("sfxSource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(sfx, src);
        t.GetField("hoverClip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(sfx, hover);
        t.GetField("clickClip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(sfx, click);
        t.GetField("volume", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(sfx, volume);
    }
}
