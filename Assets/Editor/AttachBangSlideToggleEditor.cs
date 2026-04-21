using UnityEditor;
using UnityEngine;

public static class AttachBangSlideToggleEditor
{
    [MenuItem("Tools/Attach BangSlideToggle")]
    public static void Execute()
    {
        var bangVe = GameObject.Find("BangVe");
        if (bangVe == null)
        {
            Debug.LogError("BangVe not found in scene.");
            return;
        }

        // Robust lookup (in case the hierarchy/names differ slightly).
        Transform offT = null;
        Transform bangT = null;

        foreach (var t in bangVe.GetComponentsInChildren<Transform>(true))
        {
            if (bangT == null && t.name == "Bang")
                bangT = t;

            if (offT == null && t.GetComponent<UnityEngine.UI.Button>() != null)
            {
                // Prefer exact match, otherwise a contains check.
                if (t.name == "Off" || t.name.Contains("Off"))
                    offT = t;
            }

            if (bangT != null && offT != null)
                break;
        }

        if (offT == null)
        {
            Debug.LogError("Could not find the On/Off button (a Button named 'Off') under BangVe.");
            return;
        }

        if (bangT == null)
        {
            Debug.LogError("BangVe/Bang not found.");
            return;
        }

        var toggle = offT.GetComponent<BangSlideToggle>();
        if (toggle == null)
            toggle = Undo.AddComponent<BangSlideToggle>(offT.gameObject);

        // Assign serialized private fields.
        var so = new SerializedObject(toggle);
        so.FindProperty("bang").objectReferenceValue = bangT.GetComponent<RectTransform>();
        so.FindProperty("toggleButton").objectReferenceValue = offT.GetComponent<RectTransform>();
        so.FindProperty("duration").floatValue = 0.25f;
        so.FindProperty("leftMargin").floatValue = 16f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(toggle);
        Debug.Log("BangSlideToggle attached and wired on BangVe/On/Off.");
    }
}
