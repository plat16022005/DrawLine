using System;
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

    [Header("Cursor Settings")]
    public Texture2D pencilCursor;
    public Texture2D eraserCursor;
    public Vector2 pencilHotSpot = new Vector2(0, 32); 
    public Vector2 eraserHotSpot = new Vector2(8, 8);  

    [Header("Eraser Settings")]
    [Tooltip("Bán kính vùng tẩy trong World Space")]
    public float eraserRadius = 0.3f;

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
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Không cần check UI ở đây nữa, vì nếu chạm UI từ đầu, activeLine đã là null
            if (activeLine != null)
            {
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
}
