using System;
using System.Collections.Generic;

[Serializable]
public class TileData
{
    public int x;
    public int y;
    public string type;

    public TileData(int x, int y, string type)
    {
        this.x = x;
        this.y = y;
        this.type = type;
    }
}