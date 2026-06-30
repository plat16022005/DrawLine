using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gắn lên bất kỳ UI element nào để hiện tooltip khi rê chuột vào.
/// Yêu cầu: TooltipUI phải tồn tại trong scene.
/// </summary>
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea(2, 5)]
    [Tooltip("Nội dung tooltip hiển thị khi rê chuột vào element này")]
    public string content;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Show(content);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();
    }

    private void OnDisable()
    {
        // Ẩn tooltip nếu element bị tắt khi đang hover
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();
    }
}
