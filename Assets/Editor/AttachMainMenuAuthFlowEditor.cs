using UnityEditor;
using UnityEngine;

public static class AttachMainMenuAuthFlowEditor
{
    [MenuItem("Tools/MainMenu/Attach Auth Flow")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas not found.");
            return;
        }

        var mainMenuUI = canvas.transform.Find("MainMenuUI");
        if (mainMenuUI == null)
        {
            Debug.LogError("Canvas/MainMenuUI not found.");
            return;
        }

        var btnGroup = mainMenuUI.Find("BTN");
        if (btnGroup == null)
        {
            Debug.LogError("Canvas/MainMenuUI/BTN not found.");
            return;
        }

        var panelDangNhap = canvas.transform.Find("PanelDangNhap");
        var panelDangKy = canvas.transform.Find("PanelDangKy");
        if (panelDangNhap == null || panelDangKy == null)
        {
            Debug.LogError("PanelDangNhap or PanelDangKy not found under Canvas.");
            return;
        }

        var dangNhapBtn = btnGroup.Find("DangNhap")?.GetComponent<UnityEngine.UI.Button>();
        var dangKyBtn = btnGroup.Find("DangKy")?.GetComponent<UnityEngine.UI.Button>();

        var quayLaiDangNhapBtn = panelDangNhap.Find("LayoutButton/QuayLai")?.GetComponent<UnityEngine.UI.Button>();
        var quayLaiDangKyBtn = panelDangKy.Find("LayoutButton/QuayLai")?.GetComponent<UnityEngine.UI.Button>();

        var flow = mainMenuUI.GetComponent<MainMenuAuthFlow>();
        if (flow == null)
            flow = Undo.AddComponent<MainMenuAuthFlow>(mainMenuUI.gameObject);

        var so = new SerializedObject(flow);
        so.FindProperty("btnGroup").objectReferenceValue = btnGroup.GetComponent<RectTransform>();
        so.FindProperty("panelDangNhap").objectReferenceValue = panelDangNhap.GetComponent<RectTransform>();
        so.FindProperty("panelDangKy").objectReferenceValue = panelDangKy.GetComponent<RectTransform>();

        so.FindProperty("dangNhapButton").objectReferenceValue = dangNhapBtn;
        so.FindProperty("dangKyButton").objectReferenceValue = dangKyBtn;
        so.FindProperty("quayLaiDangNhapButton").objectReferenceValue = quayLaiDangNhapBtn;
        so.FindProperty("quayLaiDangKyButton").objectReferenceValue = quayLaiDangKyBtn;

        so.FindProperty("duration").floatValue = 0.25f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(flow);

        // Ensure panels are inactive by default in edit mode.
        panelDangNhap.gameObject.SetActive(false);
        panelDangKy.gameObject.SetActive(false);

        Debug.Log("MainMenuAuthFlow attached and wired.");
    }
}
