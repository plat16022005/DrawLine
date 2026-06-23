using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class TileOption
{
    public string id;
    public string tileName;
    public Sprite icon;
    public TileBase tile;
    [TextArea] public string description;
}