using UnityEngine;
using UnityEngine.Tilemaps;

public class TilePreview : MonoBehaviour
{
    public Tilemap tilemap;
    public TileDropdownUI tileDropdownUI;
    public SpriteRenderer spriteRenderer;

    void Update()
    {
        TileBase selectedTile = tileDropdownUI.selectedTile;

        if (selectedTile == null)
        {
            spriteRenderer.enabled = false;
            return; 
        }

        spriteRenderer.enabled = true;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = tilemap.WorldToCell(mouseWorld);

        Vector3 cellCenter = tilemap.GetCellCenterWorld(cellPos);
        transform.position = cellCenter;

        Tile tile = selectedTile as Tile;

        if (tile != null)
        {
            spriteRenderer.sprite = tile.sprite;
            spriteRenderer.color = new Color(1, 1, 1, 0.5f);
        }
    }
}