using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapData
{
    public string mapName;
    // public string ownerId;
    public int width;
    public int height;
    public List<TileData> tiles = new List<TileData>();
    public List<TrapData> traps = new List<TrapData>();

    // Vị trí spawn của các nhân vật
    public Vector2 knightSpawn;
    public Vector2 demonSpawn;
    public Vector2 princessSpawn;
}
