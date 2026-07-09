using System;
using System.Collections.Generic;
using UnityEngine;

public enum MapStatus
{
    Private,
    Publish
}

[System.Serializable]
public class MapData
{
    public string mapName;
    public string status = "private"; // private | publish | maintenance
    // public string ownerId;
    public int width;
    public int height;
    public List<TileData> tiles = new List<TileData>();
    public List<TrapData> traps = new List<TrapData>();

    // Vị trí spawn của các nhân vật
    public Vector2 knightSpawn;
    public Vector2 demonSpawn;
    public Vector2 princessSpawn;

    // Cấu hình map
    public float cameraLens = 5f;
    public float inkCostPerUnit = 30f;
    public int weatherType = 0;
    public bool enableWind = false;
    public float windForce = 15f;
    public float windAngle = 180f;
}
