using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

[System.Serializable]
public class LineData
{
    public List<Vector2> points;
    public LineType type;

    public LineData(Line line)
    {
        points = new List<Vector2>(line.Points);
        type = line.myType;
    }
}

[System.Serializable]
public class GameState
{
    public List<LineData> lines = new List<LineData>();
    public float ink;

    public GameState(float currentInk)
    {
        ink = currentInk;
        Line[] sceneLines = Object.FindObjectsOfType<Line>();
        foreach (var line in sceneLines)
        {
            lines.Add(new LineData(line));
        }
    }
}

public class UndoRedoManager : MonoBehaviour
{
    public static UndoRedoManager Instance { get; private set; }

    private List<GameState> undoStack = new List<GameState>();
    private List<GameState> redoStack = new List<GameState>();

    [Header("Settings")]
    [Tooltip("Số lượng bước hoàn tác tối đa")]
    public int maxUndoSteps = 50;

    [Header("UI Buttons (Optional)")]
    public Button undoButton;
    public Button redoButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Chụp trạng thái ban đầu (thường là trống)
        SaveState();
    }

    private void Update()
    {
        // Tự động bật/tắt nút dựa trên trạng thái game
        bool canInteract = !GameController.isPlaying && !GameController.isGameOver;

        if (undoButton != null)
            undoButton.interactable = canInteract && undoStack.Count > 1;

        if (redoButton != null)
            redoButton.interactable = canInteract && redoStack.Count > 0;
    }

    public void SaveState()
    {
        GameState currentState = new GameState(InkManager.CurrentInk);

        // Kiểm tra xem trạng thái có thực sự thay đổi không
        if (undoStack.Count > 0 && AreStatesEqual(undoStack[undoStack.Count - 1], currentState))
            return;

        undoStack.Add(currentState);
        
        // Giới hạn số bước hoàn tác
        if (undoStack.Count > maxUndoSteps)
        {
            undoStack.RemoveAt(0);
        }

        redoStack.Clear(); 
        Debug.Log($"Đã lưu trạng thái. Undo steps: {undoStack.Count}");
    }

    public void Undo()
    {
        // Không cho phép hoàn tác khi đang chơi hoặc đã kết thúc
        if (GameController.isPlaying || GameController.isGameOver) return;

        if (undoStack.Count <= 1) return;

        // Chuyển trạng thái hiện tại sang redo
        GameState currentState = undoStack[undoStack.Count - 1];
        undoStack.RemoveAt(undoStack.Count - 1);
        redoStack.Add(currentState);

        // Khôi phục trạng thái mới nhất còn lại
        RestoreState(undoStack[undoStack.Count - 1]);
    }

    public void Redo()
    {
        // Không cho phép Redo khi đang chơi hoặc đã kết thúc
        if (GameController.isPlaying || GameController.isGameOver) return;

        if (redoStack.Count == 0) return;

        // Chuyển từ redo sang undo
        GameState nextState = redoStack[redoStack.Count - 1];
        redoStack.RemoveAt(redoStack.Count - 1);
        undoStack.Add(nextState);

        RestoreState(nextState);
    }

    private void RestoreState(GameState state)
    {
        // 1. Xóa tất cả các đường hiện tại
        Line[] currentLines = Object.FindObjectsOfType<Line>();
        foreach (var l in currentLines) if (l != null) Destroy(l.gameObject);

        // 2. Tái tạo các đường
        foreach (var lineData in state.lines)
        {
            GameObject go = new GameObject("Drawn Line (Restored)");
            Line line = go.AddComponent<Line>();
            line.InitializeFromPoints(lineData.points, lineData.type);
        }

        // 3. Khôi phục lượng mực
        if (InkManager.Instance != null)
            InkManager.Instance.SetCurrentInk(state.ink);
    }

    private bool AreStatesEqual(GameState s1, GameState s2)
    {
        if (Mathf.Abs(s1.ink - s2.ink) > 0.001f) return false;
        if (s1.lines.Count != s2.lines.Count) return false;
        
        // So sánh tổng số điểm để chắc chắn hơn
        int p1 = 0; foreach(var l in s1.lines) p1 += l.points.Count;
        int p2 = 0; foreach(var l in s2.lines) p2 += l.points.Count;
        
        return p1 == p2;
    }
}
