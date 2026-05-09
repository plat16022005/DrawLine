using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lớp tĩnh lưu danh sách các UI target đã đăng ký trong scene.
/// TutorialManager tra cứu RectTransform qua ID string.
/// </summary>
public static class TutorialTargetRegistry
{
    private static readonly Dictionary<string, Transform> _targets = new();

    /// <summary>Đăng ký một Transform với ID. Gọi bởi TutorialTarget.OnEnable.</summary>
    public static void Register(string id, Transform t)
    {
        if (string.IsNullOrEmpty(id)) return;
        _targets[id] = t;
    }

    /// <summary>Hủy đăng ký. Gọi bởi TutorialTarget.OnDisable.</summary>
    public static void Unregister(string id, Transform t)
    {
        if (!string.IsNullOrEmpty(id))
        {
            if (_targets.TryGetValue(id, out Transform existing) && existing == t)
            {
                _targets.Remove(id);
            }
        }
    }

    /// <summary>Lấy Transform theo ID. Trả về null nếu không tìm thấy.</summary>
    public static Transform Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _targets.TryGetValue(id, out Transform t);
        return t;
    }
}
