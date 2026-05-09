using UnityEngine;

/// <summary>
/// Đánh dấu một vật thể (UI hoặc World Object) để TutorialManager có thể tìm thấy nó thông qua ID.
///
/// CÁCH DÙNG:
/// 1. Gắn script này vào UI Button/Image hoặc vật thể trong Scene mà bạn muốn tutorial chỉ vào.
/// 2. Đặt một 'targetId' duy nhất (ví dụ: "PlayButton").
/// 3. Trong TutorialScenario asset, nhập ID này vào ô 'highlightTargetId' hoặc 'handPointerAnchorId'.
/// </summary>
public class TutorialTarget : MonoBehaviour
{
    [Tooltip("ID duy nhất để nhận diện vật thể này trong Tutorial. Phải khớp với ID nhập trong Scenario asset.")]
    public string targetId;

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(targetId))
        {
            TutorialTargetRegistry.Register(targetId, transform);
        }
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(targetId))
        {
            TutorialTargetRegistry.Unregister(targetId, transform);
        }
    }
}
