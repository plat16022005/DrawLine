using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tự động sinh UI InputField cho các public field của ITrapConfig
/// bằng C# Reflection — không cần code riêng cho từng loại bẫy.
/// </summary>
public class TrapConfigPanelUI : MonoBehaviour
{
    [Header("Container để chứa các row được sinh ra")]
    public Transform fieldsContainer;

    [Header("Prefab row (Label + InputField)")]
    public GameObject fieldRowPrefab;

    [Header("Panel wrapper (ẩn/hiện)")]
    public GameObject panel;

    // --- Runtime state ---
    private ITrapConfig currentConfig;
    private readonly List<FieldEntry> activeEntries = new();

    private struct FieldEntry
    {
        public FieldInfo field;
        public TMP_InputField inputField;
    }

    // -------------------------------------------------------
    // Public API
    // -------------------------------------------------------

    /// <summary>
    /// Gán config mới và build lại UI.
    /// Truyền null để ẩn panel.
    /// </summary>
    public void SetConfig(ITrapConfig config)
    {
        ClearRows();

        currentConfig = config;

        if (config == null)
        {
            Debug.Log("[TrapConfigPanelUI] config = null → ẩn panel");
            if (panel != null) panel.SetActive(false);
            return;
        }

        Debug.Log($"[TrapConfigPanelUI] SetConfig: {config.GetType().Name}");
        BuildRows(config);
        Debug.Log($"[TrapConfigPanelUI] Số field tìm được: {activeEntries.Count}  |  fieldRowPrefab={(fieldRowPrefab != null ? fieldRowPrefab.name : "NULL")}  |  fieldsContainer={(fieldsContainer != null ? fieldsContainer.name : "NULL")}");

        if (panel != null) panel.SetActive(activeEntries.Count > 0);
        else Debug.LogWarning("[TrapConfigPanelUI] panel chưa được assign!");
    }

    /// <summary>
    /// Ghi giá trị từ các InputField ngược lại vào currentConfig.
    /// Gọi trước khi lưu / đặt bẫy.
    /// </summary>
    public void Apply()
    {
        if (currentConfig == null) return;

        foreach (FieldEntry entry in activeEntries)
        {
            SetFieldFromString(entry.field, currentConfig, entry.inputField.text);
        }
    }

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    void BuildRows(ITrapConfig config)
    {
        Type type = config.GetType();

        // Lấy tất cả public instance field khai báo trực tiếp trên class đó
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (FieldInfo field in fields)
        {
            // Chỉ hỗ trợ float, int, bool, string
            if (!IsSupportedType(field.FieldType)) continue;

            if (fieldRowPrefab == null || fieldsContainer == null) continue;

            GameObject row = Instantiate(fieldRowPrefab, fieldsContainer);

            // --- Label ---
            // Dùng helper để tránh bắt nhầm TMP_Text bên trong TMP_InputField
            TMP_Text label = FindLabelText(row);
            if (label != null)
                label.text = FormatFieldName(field.Name);

            // --- InputField ---
            TMP_InputField input = row.GetComponentInChildren<TMP_InputField>();
            if (input == null) continue;

            // Thiết lập keyboard type phù hợp
            if (field.FieldType == typeof(int))
                input.contentType = TMP_InputField.ContentType.IntegerNumber;
            else if (field.FieldType == typeof(float))
                input.contentType = TMP_InputField.ContentType.DecimalNumber;
            else
                input.contentType = TMP_InputField.ContentType.Standard;

            // Điền giá trị hiện tại
            object value = field.GetValue(config);
            input.SetTextWithoutNotify(value != null ? value.ToString() : "");

            activeEntries.Add(new FieldEntry { field = field, inputField = input });
        }
    }

    void ClearRows()
    {
        activeEntries.Clear();

        if (fieldsContainer == null) return;

        for (int i = fieldsContainer.childCount - 1; i >= 0; i--)
            Destroy(fieldsContainer.GetChild(i).gameObject);
    }

    static bool IsSupportedType(Type t)
        => t == typeof(float) || t == typeof(int) || t == typeof(bool) || t == typeof(string);

    /// <summary>
    /// Tìm TMP_Text dùng làm label — bỏ qua đúng textComponent và placeholder
    /// nội bộ của TMP_InputField, bất kể chúng nằm ở đâu trong hierarchy.
    /// </summary>
    static TMP_Text FindLabelText(GameObject row)
    {
        TMP_InputField input = row.GetComponentInChildren<TMP_InputField>(true);

        TMP_Text[] allTexts = row.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in allTexts)
        {
            // Bỏ qua text và placeholder nội bộ của TMP_InputField
            if (input != null && (t == input.textComponent || t == (TMP_Text)input.placeholder))
                continue;
            return t;
        }
        return null;
    }

    static void SetFieldFromString(FieldInfo field, object target, string text)
    {
        try
        {
            if (field.FieldType == typeof(float))
            {
                if (float.TryParse(text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float f))
                    field.SetValue(target, f);
            }
            else if (field.FieldType == typeof(int))
            {
                if (int.TryParse(text, out int i))
                    field.SetValue(target, i);
            }
            else if (field.FieldType == typeof(bool))
            {
                if (bool.TryParse(text, out bool b))
                    field.SetValue(target, b);
                // Hỗ trợ "1"/"0" thay cho "true"/"false"
                else if (text == "1") field.SetValue(target, true);
                else if (text == "0") field.SetValue(target, false);
            }
            else if (field.FieldType == typeof(string))
            {
                field.SetValue(target, text);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TrapConfigPanelUI] Không thể gán field '{field.Name}': {e.Message}");
        }
    }

    /// <summary>camelCase / PascalCase → "Camel Case"</summary>
    static string FormatFieldName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        System.Text.StringBuilder sb = new();
        sb.Append(char.ToUpper(name[0]));
        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString();
    }
}
