using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Gắn lên bất kỳ UI element nào để hiện tooltip khi rê chuột vào (PC) hoặc Nhấn giữ (Mobile).
/// Yêu cầu: TooltipUI phải tồn tại trong scene.
/// </summary>
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [TextArea(2, 5)]
    [Tooltip("Nội dung tooltip hiển thị khi rê chuột vào element này")]
    public string content;

    [Tooltip("Thời gian trễ (giây) trước khi hiện Tooltip (để tránh hiện khi chỉ lướt ngang hoặc bấm click nhanh)")]
    public float delayTime = 0.4f;

    private Coroutine showRoutine;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Trên PC, rê chuột vào bắt đầu đếm giờ
        StartHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopHover();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Trên Điện thoại, chạm ngón tay xuống (nhấn giữ) bắt đầu đếm giờ
        StartHover();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Thả ngón tay ra (hoặc click xong) thì hủy bỏ
        StopHover();
    }

    private void StartHover()
    {
        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(ShowTooltipAfterDelay());
    }

    private void StopHover()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();
    }

    private IEnumerator ShowTooltipAfterDelay()
    {
        // Dùng WaitForSecondsRealtime để Tooltip vẫn hoạt động ngay cả khi Game đang Pause (Time.timeScale = 0)
        yield return new WaitForSecondsRealtime(delayTime);
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Show(content);
    }

    private void OnDisable()
    {
        StopHover();
    }
}
