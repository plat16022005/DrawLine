using UnityEngine;

/// <summary>
/// Các loại điều kiện để hoàn thành một bước tutorial.
/// </summary>
public enum TutorialCompletionCondition
{
    Manual,             // Gọi TutorialManager.Instance.NextStep() thủ công từ code/UI
    AutoTimer,          // Tự động chuyển sau autoAdvanceDelay giây
    ClickRegion,        // Người chơi nhấn vào vùng viewport (0-1) được chỉ định
    ClickObject,        // Người chơi nhấn vào đúng một GameObject 2D cụ thể
    ClickUITarget,      // Người chơi nhấn vào một UI Target cụ thể (theo ID)
    AnyClick,           // Nhấn vào bất cứ đâu trên màn hình
    DrawInRegion,       // Người chơi vẽ đường đi vào vùng world-space (radius)
    PlayerReachPosition, // Player di chuyển đến vị trí world-space (radius)
    EraseAll,           // Xóa toàn bộ các đường đã vẽ
    ErasePartial        // Xóa đường cho đến khi tổng độ dài còn lại dưới mức minDrawLength
}

/// <summary>
/// Vị trí preset của TextPanel trên màn hình.
/// Custom = dùng textPanelCustomPos để đặt anchoredPosition thủ công.
/// </summary>
public enum TextPanelPreset
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
    Custom          // Dùng textPanelCustomPos (anchoredPosition pixel)
}

/// <summary>
/// Vị trí của bàn tay so với mục tiêu UI.
/// </summary>
public enum HandPointerAlignment
{
    Center,
    Top,
    Bottom,
    Left,
    Right
}

/// <summary>
/// Dữ liệu một bước tutorial. Thêm vào danh sách trong TutorialScenario.
/// </summary>
[System.Serializable]
public class TutorialStepData
{
    [TextArea(2, 5)]
    [Tooltip("Nội dung hướng dẫn hiển thị cho người chơi")]
    public string description;

    [Tooltip("Loại điều kiện để hoàn thành bước này")]
    public TutorialCompletionCondition completionCondition;

    // ── Text Panel Position ──────────────────────────────────────────────

    [Header("Text Panel Position")]
    [Tooltip("Vị trí của TextPanel cho bước này.\nChọn preset hoặc Custom để nhập tọa độ thủ công.")]
    public TextPanelPreset textPanelPreset = TextPanelPreset.BottomCenter;

    [Tooltip("anchoredPosition (pixel) khi chọn Custom.\nGốc tọa độ = tâm canvas.")]
    public Vector2 textPanelCustomPos = Vector2.zero;

    [Tooltip("Offset bổ sung cộng thêm vào vị trí preset (pixel). Dùng để tinh chỉnh.")]
    public Vector2 textPanelOffset = Vector2.zero;

    // ── Highlight & UI ──────────────────────────────────────────────────

    [Header("Highlight")]
    [Tooltip("Nhập 'targetId' của UI (đã gắn script TutorialTarget) cần làm sáng. Để trống = không highlight.")]
    public string highlightTargetId;

    [Tooltip("Khoảng padding (pixel) quanh vùng highlight")]
    public float highlightPadding = 12f;

    [Tooltip("Nếu true, sẽ phủ màn tối chặn tương tác toàn màn hình (hoặc chỉ chừa lỗ sáng nếu có Highlight Target).")]
    public bool blockInteraction = true;

    [Tooltip("Nếu true, sẽ highlight vùng world mục tiêu (dùng cho PlayerReachPosition hoặc DrawInRegion).")]
    public bool highlightWorldTarget = false;

    [Tooltip("Nếu true, vòng sáng và bàn tay sẽ liên tục di chuyển bám theo mục tiêu khi mục tiêu di chuyển.")]
    public bool followTarget = false;

    // ── Hand Pointer ────────────────────────────────────────────────────

    [Header("Hand Pointer")]
    [Tooltip("Có hiển thị mũi tay chỉ vào mục tiêu không?")]
    public bool showHandPointer = true;

    [Tooltip("Nhập 'targetId' của UI (đã gắn script TutorialTarget) để neo mũi tay vào.")]
    public string handPointerAnchorId;

    [Tooltip("Vị trí của mũi tay so với UI mục tiêu (Tự động tính theo kích thước UI).")]
    public HandPointerAlignment handPointerAlignment = HandPointerAlignment.Bottom;

    [Tooltip("Vị trí World để đặt mũi tay (dùng khi handPointerAnchorId trống)")]
    public Vector2 handPointerWorldPos;

    [Tooltip("Offset bổ sung cho mũi tay (pixel) để tránh che mất UI.")]
    public Vector2 handPointerOffset = Vector2.zero;

    // ── ClickRegion ─────────────────────────────────────────────────────

    [Header("ClickRegion Settings")]
    [Tooltip("Vùng viewport (0-1) người chơi phải nhấn vào.\nVí dụ: (0.4, 0.3, 0.2, 0.2) = vùng giữa màn hình")]
    public Rect clickViewportRect = new Rect(0f, 0f, 1f, 1f);

    // ── ClickObject ─────────────────────────────────────────────────────

    [Header("ClickObject Settings")]
    [Tooltip("GameObject (phải có Collider2D) mà người chơi cần nhấn vào")]
    public GameObject targetObject;

    // ── DrawInRegion / PlayerReachPosition ──────────────────────────────

    [Header("DrawInRegion / PlayerReachPosition Settings")]
    [Tooltip("ID của vật thể đích (đã gán script TutorialTarget). Nếu nhập ID, hệ thống sẽ tự tìm vị trí của vật thể đó.")]
    public string worldTargetId;

    [Tooltip("GameObject làm tâm (Chỉ dùng nếu bạn gán trực tiếp trong Scene, không khuyến khích dùng trong Scenario asset)")]
    public GameObject worldTargetObject;

    [Tooltip("Tâm vùng mục tiêu trong World Space (Dùng nếu cả 2 ô trên đều trống)")]
    public Vector2 targetWorldPosition;

    [Tooltip("Bán kính vùng mục tiêu (Nếu dùng hình tròn)")]
    public float targetWorldRadius = 1.5f;

    [Tooltip("Dùng hình chữ nhật thay vì hình tròn?")]
    public bool isRectangleTarget = false;

    [Tooltip("Tự động lấy kích thước và hình dáng (Tròn/Chữ nhật) từ Collider2D của mục tiêu nếu có?")]
    public bool autoFitCollider = true;

    [Tooltip("Kích thước vùng hình chữ nhật (Rộng, Cao)")]
    public Vector2 targetWorldSize = new Vector2(2f, 2f);

    [Tooltip("Độ dài tối thiểu (khi vẽ) hoặc Ngưỡng độ dài còn lại tối đa (khi xóa một phần) để hoàn thành bước này.")]
    public float minDrawLength = 0f;

    // ── Custom UI Position ──────────────────────────────────────────────

    [Header("Custom UI Target (Optional)")]
    [Tooltip("Sử dụng tọa độ UI (Anchored Position) để tạo lỗ sáng và làm vùng đích (thay vì World Space hoặc Target ID)")]
    public bool useCustomUIPosition = false;
    
    [Tooltip("Tâm vùng mục tiêu trên UI (Anchored Position tính từ tâm màn hình)")]
    public Vector2 customUIPosition = Vector2.zero;
    
    [Tooltip("Bán kính vùng mục tiêu trên UI (pixel)")]
    public float customUIRadius = 50f;

    // ── AutoTimer ───────────────────────────────────────────────────────

    [Header("AutoTimer Settings")]
    [Tooltip("Số giây trước khi tự động sang bước tiếp theo (chỉ dùng với AutoTimer)")]
    public float autoAdvanceDelay = 2f;

    [Header("Time Settings")]
    [Tooltip("Dừng thời gian khi ở bước này? (Time.timeScale = 0)\nNếu không tick, tốc độ thời gian sẽ phụ thuộc vào GameController.isPlaying.")]
    public bool freezeTime = false;

    [Header("Cleanup Settings")]
    [Tooltip("Xóa toàn bộ các đường đã vẽ trên màn hình trước khi chuyển sang bước tiếp theo?")]
    public bool clearLinesOnComplete = false;

    [Header("Editor Debug")]
    [Tooltip("Màu vòng tròn Gizmo hiển thị vùng mục tiêu trong Scene View")]
    public Color gizmoColor = new Color(0f, 1f, 0.5f, 0.5f);
}