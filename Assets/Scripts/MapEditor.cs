using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class MapEditor : MonoBehaviour
{
    public Tilemap tilemap;
    public TileDropdownUI tileDropdownUI;

    void Update()
    {
        // Không vẽ khi đang bấm UI dropdown/button
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = tilemap.WorldToCell(mouseWorld);
        cellPos.z = 0;

        // Chuột trái: vẽ tile
        if (Input.GetMouseButton(0))
        {
            TileBase selectedTile = tileDropdownUI.selectedTile;

            if (selectedTile != null)
            {
                tilemap.SetTile(cellPos, selectedTile);
            }
        }

        // Chuột phải: xóa tile
        if (Input.GetMouseButton(1))
        {
            tilemap.SetTile(cellPos, null);
        }
    }
}