using System;
using System.Collections.Generic;

[System.Serializable]
public class MapData
{
    public string mapName;
    // public string ownerId;
    public int width;
    public int height;
    public List<TileData> tiles = new List<TileData>();
}
