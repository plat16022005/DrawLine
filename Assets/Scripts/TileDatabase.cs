using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileDatabase", menuName = "InkKnight/Tile Database")]
public class TileDatabase : ScriptableObject
{
    public List<TileOption> tileOptions;

    public TileBase GetTileById(string id)
    {
        foreach (TileOption option in tileOptions)
        {
            if (option.id == id)
                return option.tile;
        }

        return null;
    }
} 