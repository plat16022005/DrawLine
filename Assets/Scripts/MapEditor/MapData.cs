using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một ô tile được lưu trong map (toạ độ cell + index tile đã chọn).
/// </summary>
[Serializable]
public class TileEntry
{
    public int x;
    public int y;
    /// <summary>Index trong mảng TileBase[] của MapEditorManager.</summary>
    public int tileIndex;

    public TileEntry() { }
    public TileEntry(int x, int y, int tileIndex)
    {
        this.x = x;
        this.y = y;
        this.tileIndex = tileIndex;
    }
}

/// <summary>
/// Toàn bộ dữ liệu một map do người chơi thiết kế.
/// Được serialize thành JSON và lưu ra file.
/// </summary>
[Serializable]
public class MapData
{
    public string mapName = "MyMap";
    public string authorUid = "";

    /// <summary>Danh sách tile được đặt trên Tilemap.</summary>
    public List<TileEntry> tiles = new List<TileEntry>();

    /// <summary>Vị trí spawn của người chơi.</summary>
    public Vector3 playerSpawn = Vector3.zero;
    public bool hasPlayerSpawn = false;

    /// <summary>Vị trí spawn của quỷ.</summary>
    public Vector3 demonSpawn = Vector3.zero;
    public bool hasDemonSpawn = false;

    /// <summary>Vị trí spawn của công chúa + lồng.</summary>
    public Vector3 princessSpawn = Vector3.zero;
    public bool hasPrincessSpawn = false;

    /// <summary>Danh sách các bẫy được đặt trong map.</summary>
    public List<TrapData> traps = new List<TrapData>();
}
