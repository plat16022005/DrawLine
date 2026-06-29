using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class EraserController : MonoBehaviour
{
    public Tilemap tilemap;
    public Camera mainCamera;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (tilemap == null)
        {
            MapEditor editor = FindFirstObjectByType<MapEditor>();
            if (editor != null)
            {
                tilemap = editor.tilemap;
            }
            else
            {
                tilemap = FindFirstObjectByType<Tilemap>();
            }
        }
    }

    void Update()
    {
        if (PlacementModeManager.CurrentMode != PlacementMode.Erase)
            return;

        // Bỏ qua nếu chuột đang nằm trên UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Khi giữ chuột trái
        if (Input.GetMouseButton(0))
        {
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            
            // Xóa tile
            if (tilemap != null)
            {
                Vector3Int cellPos = tilemap.WorldToCell(mouseWorld);
                cellPos.z = 0;
                tilemap.SetTile(cellPos, null);
            }

            // Xóa Trap hoặc SpawnPoint (những object có collider2D)
            Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);
            Collider2D[] hits = Physics2D.OverlapPointAll(mousePos2D);
            
            foreach (Collider2D hit in hits)
            {
                // Bỏ qua nếu hit vào chính Tilemap (tránh xóa nhầm toàn bộ map)
                if (tilemap != null && hit.gameObject == tilemap.gameObject)
                    continue;

                // Nếu click vào một trap, spawn point hoặc Line vẽ (nếu có collider)
                if (hit.gameObject.CompareTag("Untagged") || hit.gameObject.CompareTag("Trap") || hit.gameObject.CompareTag("SpawnPoint"))
                {
                    // Nếu là trap do TrapPlacementController sinh ra (nằm trong trapParent)
                    // Hoặc spawn point, line
                    Destroy(hit.gameObject);
                }
            }
        }
    }
}
