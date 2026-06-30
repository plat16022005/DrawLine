using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton quản lý panel tooltip toàn cục.
/// Gắn script này lên Panel tooltip trong Canvas.
/// Panel nên có: Image (nền) + TextMeshProUGUI (nội dung).
/// </summary>
public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [Header("References")]
    [Tooltip("Text hiển thị nội dung tooltip")]
    public TextMeshProUGUI tooltipText;

    [Tooltip("RectTransform của panel tooltip (để căn vị trí theo chuột)")]
    public RectTransform tooltipPanel;

    [Header("Offset so với con trỏ chuột (pixels)")]
    public Vector2 cursorOffset = new Vector2(12f, -12f);

    [Header("Padding tự động")]
    [Tooltip("Khoảng cách tối thiểu từ mép tooltip đến mép màn hình")]
    public float screenPadding = 8f;

    private Canvas canvas;
    private RectTransform canvasRect;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        canvas     = GetComponentInParent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();

        Hide();
    }

    // ─── Public API ────────────────────────────────────────────────────────────

    public void Show(string content)
    {
        tooltipText.text = content;
        tooltipPanel.gameObject.SetActive(true);

        // Force layout rebuild ngay để kích thước panel cập nhật trước khi căn vị trí
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);

        PositionTooltip();
    }

    public void Hide()
    {
        tooltipPanel.gameObject.SetActive(false);
    }

    // ─── Internal ──────────────────────────────────────────────────────────────

    private void Update()
    {
        if (tooltipPanel.gameObject.activeSelf)
            PositionTooltip();
    }

    private void PositionTooltip()
    {
        // Chuyển vị trí chuột màn hình sang tọa độ Canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint
        );

        Vector2 targetPos = localPoint + cursorOffset;

        // Kích thước panel và canvas
        Vector2 panelSize  = tooltipPanel.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 half       = canvasSize * 0.5f;   // canvas gốc ở center

        // Clamp để tooltip không tràn ra ngoài màn hình
        float minX = -half.x + screenPadding;
        float maxX =  half.x - panelSize.x - screenPadding;
        float minY = -half.y + panelSize.y + screenPadding;
        float maxY =  half.y - screenPadding;

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        tooltipPanel.anchoredPosition = targetPos;
    }
}
