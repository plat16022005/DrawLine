using UnityEditor;
using UnityEngine;

public static class InstallUISfxEditor
{
    [MenuItem("Tools/MainMenu/Install UI SFX (Hover/Click)")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas not found.");
            return;
        }

        // Ensure a dedicated sfx AudioSource exists.
        var uiAudio = GameObject.Find("UIAudio");
        if (uiAudio == null)
        {
            uiAudio = new GameObject("UIAudio");
            Undo.RegisterCreatedObjectUndo(uiAudio, "Create UIAudio");
            uiAudio.transform.SetParent(canvas.transform, false);
            var src = Undo.AddComponent<AudioSource>(uiAudio);
            src.playOnAwake = false;
            src.loop = false;
        }

        var sfxSource = uiAudio.GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = Undo.AddComponent<AudioSource>(uiAudio);

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        var hover = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/hover.wav");
        var click = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/click.wav");
        if (hover == null || click == null)
        {
            Debug.LogError("Missing hover/click clips in Assets/Sound (expected hover.wav and click.wav).");
            return;
        }

        int count = 0;

        void InstallOnPath(string path)
        {
            var go = GameObject.Find(path);
            if (go == null) return;

            var sfx = go.GetComponent<UIButtonSfx>();
            if (sfx == null) sfx = Undo.AddComponent<UIButtonSfx>(go);

            // Configure and mark dirty.
            sfx.Configure(sfxSource, hover, click, 1f);
            EditorUtility.SetDirty(sfx);
            count++;
        }

        // Main menu buttons.
        InstallOnPath("Canvas/MainMenuUI/BTN/DangNhap");
        InstallOnPath("Canvas/MainMenuUI/BTN/DangKy");
        InstallOnPath("Canvas/MainMenuUI/BTN/QuenMatKhau");

        // Login panel.
        InstallOnPath("Canvas/PanelDangNhap/LayoutButton/VaoGame");
        InstallOnPath("Canvas/PanelDangNhap/LayoutButton/QuayLai");

        // Register panel.
        InstallOnPath("Canvas/PanelDangKy/LayoutButton/DangKy");
        InstallOnPath("Canvas/PanelDangKy/LayoutButton/QuayLai");

        Debug.Log($"Installed UIButtonSfx on {count} button(s). AudioSource: UIAudio, Clips: hover/click.");
    }
}
