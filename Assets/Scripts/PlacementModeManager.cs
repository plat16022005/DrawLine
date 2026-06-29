using UnityEngine;

public class PlacementModeManager : MonoBehaviour
{
    private static PlacementMode _currentMode = PlacementMode.None;
    public static PlacementMode CurrentMode
    {
        get => _currentMode;
        set
        {
            _currentMode = value;
            if (Instance != null) Instance.UpdateCursor();
        }
    }
    public string CurrentStringMode;

    [Header("Cursor Settings")]
    public Texture2D moveTrapCursor;
    public Vector2 moveTrapHotSpot = Vector2.zero;
    public Texture2D trapSettingCursor;
    public Vector2 trapSettingHotSpot = Vector2.zero;
    public Texture2D eraseCursor;
    public Vector2 eraseHotSpot = Vector2.zero;

    public static PlacementModeManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SetNoneMode();
    }

    void Update()
    {
        CurrentStringMode = CurrentMode.ToString();
    }

    public void UpdateCursor()
    {
        if (CurrentMode == PlacementMode.MoveTrap)
        {
            if (moveTrapCursor != null) Cursor.SetCursor(moveTrapCursor, moveTrapHotSpot, CursorMode.Auto);
            else Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else if (CurrentMode == PlacementMode.TrapSetting)
        {
            if (trapSettingCursor != null) Cursor.SetCursor(trapSettingCursor, trapSettingHotSpot, CursorMode.Auto);
            else Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else if (CurrentMode == PlacementMode.Erase)
        {
            if (eraseCursor != null) Cursor.SetCursor(eraseCursor, eraseHotSpot, CursorMode.Auto);
            else Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    public void SetTileMode()
    {
        CurrentMode = PlacementMode.Tile;
    }

    public void SetTrapMode()
    {
        CurrentMode = PlacementMode.Trap;
    }

    public void SetNoneMode()
    {
        CurrentMode = PlacementMode.None;
    }
    public void SetMoveTrapMode()
    {
        CurrentMode = PlacementMode.MoveTrap;
    }
    public void SetTrapSettingMode()
    {
        CurrentMode = PlacementMode.TrapSetting;
    }
    public void SetSpawnPointMode()
    {
        CurrentMode = PlacementMode.SpawnPoint;
    }
    public void SetEraseMode()
    {
        CurrentMode = PlacementMode.Erase;
    }
}

public enum PlacementMode
{
    None,
    Tile,
    Trap,
    MoveTrap,
    TrapSetting,
    SpawnPoint,
    Erase
}