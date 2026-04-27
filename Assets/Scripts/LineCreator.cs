using UnityEngine;
using UnityEngine.UI;

public class LineCreator : MonoBehaviour
{
    private Line activeLine;
    private Vector2 lastDrawPoint; // Điểm cuối cùng đã vẽ (để tính khoảng cách tiêu mực)

    // Lưu trạng thái hiện tại đang chọn bút màu gì
    public LineType currentLineType = LineType.Normal;
    public Image CurrentColor;

    [Header("Cursor Settings")]
    public Texture2D pencilCursor;
    public Texture2D eraserCursor;
    public Vector2 pencilHotSpot = new Vector2(0, 32); // Điểm tác động của chuột (đầu bút chì)
    public Vector2 eraserHotSpot = new Vector2(8, 8);  // Điểm tác động của cục tẩy (giữa)

    [Header("Eraser Settings")]
    [Tooltip("Bán kính vùng tẩy trong World Space")]
    public float eraserRadius = 0.3f;

    void Start()
    {
        SetPencilCursor();
        CurrentColor.color = Color.black;
    }

    void OnDisable()
    {
        // Reset lại con trỏ chuột về mặc định khi sang Scene khác hoặc khi ẩn công cụ vẽ
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void SetPencilCursor()
    {
        if (pencilCursor != null)
            Cursor.SetCursor(pencilCursor, pencilHotSpot, CursorMode.Auto);
        else
            Debug.LogWarning("Chưa gán ảnh pencilCursor trong Inspector cho LineCreator!");
    }

    public void SetEraserCursor()
    {
        if (eraserCursor != null)
            Cursor.SetCursor(eraserCursor, eraserHotSpot, CursorMode.Auto);
        else
            Debug.LogWarning("Chưa gán ảnh eraserCursor trong Inspector cho LineCreator!");
    }

    // --- CÁC HÀM CHỌN BÚT ---

    public void SelectNormalPen()
    {
        currentLineType = LineType.Normal;
        SetPencilCursor();
        Debug.Log("Lựa chọn Bút: ĐEN (Đứng yên)");
        CurrentColor.color = Color.black;
    }

    public void SelectBouncyPen()
    {
        currentLineType = LineType.Bouncy;
        SetPencilCursor();
        Debug.Log("Lựa chọn Bút: XANH LÁ CÂY (Phản lực nhún nhảy)");
        CurrentColor.color = Color.green;
    }

    public void SelectRubberPen()
    {
        currentLineType = LineType.Rubber;
        SetPencilCursor();
        Debug.Log("Lựa chọn Bút: MÀU TÍM (Dây cao su đàn hồi)");
        CurrentColor.color = new Color(0.6f, 0.1f, 0.9f);
    }

    public void SelectSpeedBoostPen()
    {
        currentLineType = LineType.SpeedBoost;
        SetPencilCursor();
        Debug.Log("Lựa chọn Bút: MÀU ĐỎ (Tăng tốc x2 và giữ vận tốc)");
        CurrentColor.color = Color.red;
    }

    public void SelectConstantSpeedPen()
    {
        currentLineType = LineType.ConstantSpeed;
        SetPencilCursor();
        Debug.Log("Lựa chọn Bút: MÀU XANH DƯƠNG (Giữ nguyên vận tốc)");
        CurrentColor.color = Color.blue;
    }

    public void SelectSlowDownPen()
    {
        currentLineType = LineType.SlowDown;
        SetPencilCursor();
        Debug.Log("Lựa chọn Bút: MÀU NÂU (Giảm tốc dần dần)");
        CurrentColor.color = new Color(0.5f, 0.25f, 0.0f);
    }

    // Hàm gọi từ nút UI để chuyển sang chế độ Tẩy
    public void SelectEraserTool()
    {
        currentLineType = LineType.Eraser;
        SetEraserCursor();
        Debug.Log("Lựa chọn: CỤC TẨY (Xóa đường vẽ)");
    }

    void Update()
    {
        // Nếu trò chơi đang diễn ra, HOẶC đã kết thúc (thắng/thua), không cho vẽ hay tẩy thêm nữa.
        if (GameController.isPlaying || GameController.isGameOver) return;

        // --- CHẾ ĐỘ TẨY ---
        if (currentLineType == LineType.Eraser)
        {
            if (Input.GetMouseButton(0))
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                // Tìm tất cả các Collider trong vùng bán kính của cục tẩy
                Collider2D[] hits = Physics2D.OverlapCircleAll(mousePos, eraserRadius);

                foreach (Collider2D hit in hits)
                {
                    Line line = hit.GetComponent<Line>();
                    if (line == null) continue;

                    // Gọi hàm xóa đoạn, nhận về độ dài bị xóa để hoàn mực
                    bool shouldDestroy = line.EraseAt(mousePos, eraserRadius, out float refundedLength);

                    // Hoàn lại mực tương ứng với phần đường đã bị tẩy
                    if (InkManager.Instance != null && refundedLength > 0f)
                        InkManager.Instance.RefundInk(refundedLength);

                    if (shouldDestroy)
                    {
                        Destroy(line.gameObject);
                    }
                }
            }
            return; // Chế độ tẩy => không vẽ thêm
        }

        // --- CHẾ ĐỘ VẼ ---

        // Khi nhấn chuột trái xuống
        if (Input.GetMouseButtonDown(0))
        {
            // Không bắt đầu vẽ nếu không còn mực
            if (!InkManager.HasInk()) return;

            Vector2 startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lastDrawPoint = startPos;

            GameObject lineGO = new GameObject("Drawn Line");
            activeLine = lineGO.AddComponent<Line>();
            activeLine.Initialize(currentLineType);
        }

        // Khi nhả chuột trái ra
        if (Input.GetMouseButtonUp(0))
        {
            activeLine = null;
        }

        // Nếu đang trong quá trình vẽ
        if (activeLine != null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Tính khoảng cách từ điểm cuối đến vị trí chuột hiện tại
            float dist = Vector2.Distance(lastDrawPoint, mousePos);

            if (dist >= activeLine.pointsMinDistance)
            {
                // Thử tiêu mực theo khoảng cách; nếu hết mực thì kết thúc nét vẽ
                if (InkManager.Instance != null && !InkManager.Instance.ConsumeInk(dist))
                {
                    // Hết mực — kết thúc nét hiện tại
                    activeLine = null;
                    return;
                }

                lastDrawPoint = mousePos;
            }

            activeLine.UpdateLine(mousePos);
        }
    }
}
