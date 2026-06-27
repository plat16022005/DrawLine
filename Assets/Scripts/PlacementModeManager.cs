using UnityEngine;

public class PlacementModeManager : MonoBehaviour
{
    public static PlacementMode CurrentMode = PlacementMode.None;
    public string CurrentStringMode;
    void Update()
    {
        CurrentStringMode = CurrentMode.ToString();
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
}

public enum PlacementMode
{
    None,
    Tile,
    Trap,
    MoveTrap,
    TrapSetting,
    SpawnPoint
}