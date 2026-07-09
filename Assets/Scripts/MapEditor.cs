using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class MapEditor : MonoBehaviour
{
    public Tilemap tilemap;
    public TileDropdownUI tileDropdownUI;

    void Update()
    {
        if (PlacementModeManager.CurrentMode != PlacementMode.Tile)
            return;

        if (IsPointerOverUI())
            return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(GetPointerPosition());
        Vector3Int cellPos = tilemap.WorldToCell(mouseWorld);
        cellPos.z = 0;

        // Chuột trái / chạm màn hình: vẽ tile
        if (Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase != TouchPhase.Ended && Input.GetTouch(0).phase != TouchPhase.Canceled))
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

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = GetPointerPosition();
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    private Vector2 GetPointerPosition()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Canceled)
                return touch.position;
        }

        return Input.mousePosition;
    }
}