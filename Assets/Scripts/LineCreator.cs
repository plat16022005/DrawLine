using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LineCreator : MonoBehaviour
{
    public static event Action<Vector2> OnDrawPoint;
    private Line activeLine;
    private Vector2 lastDrawPoint; 
    private bool hasErasedSinceLastDown = false;

    public LineType currentLineType = LineType.Normal;
    public Image CurrentColor;
    public Image CurrentTool;
    public Sprite[] Tools;

    [Header("Weather – Pen Buttons")]
    [Tooltip("Button của bút Nảy (Bouncy) – bị khóa khi Tuyết")]
    public Button bouncyPenButton;
    [Tooltip("Button của bút Cao Su (Rubber) – (tự đứt khi Nắng Gắt, không bị khóa)")]
    public Button rubberPenButton;
    [Tooltip("Button của bút Tăng Tốc (SpeedBoost) – bị khóa khi Bão Cát")]
    public Button speedBoostPenButton;
    [Tooltip("Button của bút Làm Chậm (SlowDown) – bị khóa khi Mưa")]
    public Button slowDownPenButton;

    // Alpha khi nút bị vô hiệu hóa do thời tiết
    private const float DisabledAlpha = 0.35f;

    [Header("Cursor Settings")]
    public Texture2D pencilCursor;
    public Texture2D eraserCursor;
    public Vector2 pencilHotSpot = new Vector2(0, 32); 
    public Vector2 eraserHotSpot = new Vector2(8, 8);  

    [Header("Eraser Settings")]
    [Tooltip("Bán kính vùng tẩy trong World Space")]
    public float eraserRadius = 0.3f;

    [Header("Smart Draw Settings")]
    [Tooltip("Toggle UI điều khiển bật/tắt chế độ vẽ thông minh. Nếu không gán thì mặc định là BẬT.")]
    public Toggle smartDrawToggle;
    [Tooltip("Số điểm tối thiểu mới xử lý smart draw")]
    public int smartMinPoints = 5;
    [Tooltip("Ngưỡng sai số tuyến tính (0=thẳng tuyệt đối): nhỏ hơn ngưỡng này ⇒ cước là đường thẳng")]
    [Range(0.02f, 0.25f)]
    public float smartStraightThreshold = 0.08f;
    [Tooltip("Tỷ lệ width/height tối thiểu để snap vào trục ngang hoặc dọc")]
    [Range(1.5f, 10f)]
    public float smartAxisSnapRatio = 3.0f;
    [Tooltip("Số điểm output khi resample đường cong")]
    [Range(10, 80)]
    public int smartCurveResolution = 40;
    [Tooltip("Số điểm control khi fit spline (càng ít → càng mịn, càng nhữu → càng sát nét gốc)")]
    [Range(4, 20)]
    public int smartCurveControlPoints = 8;

    void Start()
    {
        SelectNormalPen();
    }

    void OnDisable()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void SetPencilCursor()
    {
        if (pencilCursor != null) Cursor.SetCursor(pencilCursor, pencilHotSpot, CursorMode.Auto);
    }

    public void SetEraserCursor()
    {
        if (eraserCursor != null) Cursor.SetCursor(eraserCursor, eraserHotSpot, CursorMode.Auto);
    }

    private void ActivatePen()
    {
        if (CameraControl.Instance != null) CameraControl.Instance.DisableCameraMode();
        SetPencilCursor();
        CurrentTool.sprite = Tools[0];
    }

    public void SelectNormalPen()
    {
        currentLineType = LineType.Normal;
        ActivatePen();
        CurrentColor.color = Color.black;
    }

    public void SelectBouncyPen()
    {
        currentLineType = LineType.Bouncy;
        ActivatePen();
        CurrentColor.color = Color.green;
    }

    public void SelectRubberPen()
    {
        currentLineType = LineType.Rubber;
        ActivatePen();
        CurrentColor.color = new Color(0.6f, 0.1f, 0.9f);
    }

    public void SelectSpeedBoostPen()
    {
        currentLineType = LineType.SpeedBoost;
        ActivatePen();
        CurrentColor.color = Color.red;
    }

    public void SelectConstantSpeedPen()
    {
        currentLineType = LineType.ConstantSpeed;
        ActivatePen();
        CurrentColor.color = Color.blue;
    }

    public void SelectSlowDownPen()
    {
        currentLineType = LineType.SlowDown;
        ActivatePen();
        CurrentColor.color = new Color(0.5f, 0.25f, 0.0f);
    }

    public void SelectEraserTool()
    {
        currentLineType = LineType.Eraser;
        if (CameraControl.Instance != null) CameraControl.Instance.DisableCameraMode();
        SetEraserCursor();
        CurrentTool.sprite = Tools[1];
    }

    public void SelectCameraMode()
    {
        activeLine = null;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if (CameraControl.Instance != null) CameraControl.Instance.EnableCameraMode();
        CurrentTool.sprite = Tools[2];
    }

    public void Undo()
    {
        if (UndoRedoManager.Instance != null) UndoRedoManager.Instance.Undo();
    }

    public void Redo()
    {
        if (UndoRedoManager.Instance != null) UndoRedoManager.Instance.Redo();
    }

    void Update()
    {
        if (GameController.isPlaying || GameController.isGameOver) return;
        if (CameraControl.Instance != null && CameraControl.Instance.IsActive) return;

        // --- CHẾ ĐỘ TẨY ---
        if (currentLineType == LineType.Eraser)
        {
            if (Input.GetMouseButtonDown(0))
            {
                hasErasedSinceLastDown = false;
            }

            if (Input.GetMouseButton(0))
            {
                if (IsPointerOverUI()) return;

                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Collider2D[] hits = Physics2D.OverlapCircleAll(mousePos, eraserRadius);

                foreach (Collider2D hit in hits)
                {
                    Line line = hit.GetComponent<Line>();
                    if (line == null) continue;

                    bool shouldDestroy = line.EraseAt(mousePos, eraserRadius, out float refundedLength);
                    if (InkManager.Instance != null && refundedLength > 0f)
                        InkManager.Instance.RefundInk(refundedLength);

                    if (shouldDestroy)
                    {
                        hasErasedSinceLastDown = true;
                        Destroy(line.gameObject);
                    }
                }
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                // Chỉ lưu nếu thực sự có đường bị xóa
                if (hasErasedSinceLastDown && UndoRedoManager.Instance != null)
                {
                    UndoRedoManager.Instance.SaveState();
                    hasErasedSinceLastDown = false;
                }
            }
            return; 
        }

        // --- CHẾ ĐỘ VẼ ---
        if (Input.GetMouseButtonDown(0))
        {
            // NGĂN CHẶN TẬN GỐC: Nếu chạm vào UI, không thèm tạo activeLine luôn
            if (IsPointerOverUI()) return;
            if (!InkManager.HasInk()) return;

            Vector2 startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (TutorialManager.Instance != null && !TutorialManager.Instance.IsPositionAllowed(startPos)) return;

            lastDrawPoint = startPos;
            GameObject lineGO = new GameObject("Drawn Line");
            activeLine = lineGO.AddComponent<Line>();
            activeLine.Initialize(currentLineType);

            // Nếu loại đường bị thời tiết vô hiệu hóa → giữ màu nhưng hành xử như đường thường
            if (WeatherManager.DisabledLineType.HasValue &&
                currentLineType == WeatherManager.DisabledLineType.Value)
            {
                activeLine.DowngradeToNormal();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Không cần check UI ở đây nữa, vì nếu chạm UI từ đầu, activeLine đã là null
            if (activeLine != null)
            {
                // ─── SMART DRAW ─────────────────────────────────────────────
                bool smartDrawEnabled = smartDrawToggle == null || smartDrawToggle.isOn;
                if (smartDrawEnabled && activeLine.Points.Count >= smartMinPoints)
                {
                    // Truyền tham số từ Inspector vào utility class
                    SmartLineSmoother.StraightLineThreshold = smartStraightThreshold;
                    SmartLineSmoother.AxisSnapRatio         = smartAxisSnapRatio;
                    SmartLineSmoother.CurveResolution       = smartCurveResolution;
                    SmartLineSmoother.CurveControlPoints    = smartCurveControlPoints;
                    SmartLineSmoother.MinPointsToSmooth     = smartMinPoints;

                    // Nhận dạng hình dạng
                    SmartLineSmoother.ShapeType shape = SmartLineSmoother.Recognize(activeLine.Points);

                    // Lưu độ dài cũ trước khi smooth
                    float oldLength = SmartLineSmoother.ComputeLength(activeLine.Points);

                    // Sinh tập điểm mới đã smooth
                    System.Collections.Generic.List<Vector2> smoothedPoints =
                        SmartLineSmoother.Smooth(activeLine.Points, shape);

                    // Rebuild đường với điểm mới
                    activeLine.RebuildWithPoints(smoothedPoints);

                    // Hoàn lại mực nếu đường sau snap ngắn hơn đường thô
                    float newLength = SmartLineSmoother.ComputeLength(smoothedPoints);
                    float refund    = oldLength - newLength;
                    if (refund > 0f && InkManager.Instance != null)
                        InkManager.Instance.RefundInk(refund);

                    // Visual feedback: flash trắng báo hiệu snap
                    activeLine.PlaySnapFlash();
                }
                // ─────────────────────────────────────────────────────

                if (UndoRedoManager.Instance != null) UndoRedoManager.Instance.SaveState();
            }
            activeLine = null;
        }

        if (activeLine != null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (TutorialManager.Instance != null && !TutorialManager.Instance.IsPositionAllowed(mousePos))
            {
                activeLine = null;
                return;
            }

            float dist = Vector2.Distance(lastDrawPoint, mousePos);
            if (dist >= activeLine.pointsMinDistance)
            {
                if (InkManager.Instance != null && !InkManager.Instance.ConsumeInk(dist))
                {
                    activeLine = null;
                    return;
                }
                lastDrawPoint = mousePos;
                OnDrawPoint?.Invoke(mousePos); 
            }
            activeLine.UpdateLine(mousePos);
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // Mobile: Kiểm tra tất cả các ngón tay đang chạm
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    return true;
            }
        }

        // PC/Editor
        return EventSystem.current.IsPointerOverGameObject();
    }

    // ─── Weather Integration ──────────────────────────────────────────────────

    /// <summary>
    /// Được WeatherManager gọi khi thời tiết thay đổi.
    /// Cập nhật trạng thái interactable và alpha của các nút bút bị cấm.
    /// </summary>
    public void RefreshDisabledState()
    {
        LineType? disabled = WeatherManager.DisabledLineType;

        SetButtonState(bouncyPenButton,     disabled != LineType.Bouncy);
        SetButtonState(slowDownPenButton,   disabled != LineType.SlowDown);
        SetButtonState(speedBoostPenButton, disabled != LineType.SpeedBoost);
        // Rubber không bị khóa hoàn toàn (chỉ tự đứt khi Nắng Gắt), nút vẫn bật
        SetButtonState(rubberPenButton, true);
    }

    private void SetButtonState(Button btn, bool isNormal)
    {
        if (btn == null) return;
        // Nút vẫn luôn nhấn được — chỉ làm mờ để báo hiệu bút bị ảnh hưởng bởi thời tiết
        btn.interactable = true;

        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = isNormal ? 1f : DisabledAlpha;
    }

    /// <summary>Rung nút bị khóa một chút để phản hồi người dùng.</summary>
    private void ShakeDisabledButton(LineType type)
    {
        Button target = type switch
        {
            LineType.Bouncy     => bouncyPenButton,
            LineType.SlowDown   => slowDownPenButton,
            LineType.SpeedBoost => speedBoostPenButton,
            LineType.Rubber     => rubberPenButton,
            _                   => null
        };
        if (target != null)
            StartCoroutine(ShakeRoutine(target.transform as RectTransform));
    }

    private IEnumerator ShakeRoutine(RectTransform rt)
    {
        if (rt == null) yield break;
        Vector2 originalPos = rt.anchoredPosition;
        float duration = 0.3f;
        float elapsed  = 0f;
        float magnitude = 6f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float x = Mathf.Sin(elapsed * 60f) * magnitude * (1f - elapsed / duration);
            rt.anchoredPosition = originalPos + new Vector2(x, 0f);
            yield return null;
        }
        rt.anchoredPosition = originalPos;
    }
}
