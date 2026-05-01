using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class RemoveTouchedTile : MonoBehaviour
{
    public Tilemap tilemap;
    public float delay = 2f;

    private bool triggered = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!triggered && collision.gameObject.CompareTag("Player"))
        {
            triggered = true;

            Vector2 hitPoint = collision.GetContact(0).point;
            Vector3Int cellPosition = tilemap.WorldToCell(hitPoint);

            Debug.Log("Ô sẽ bị phá: " + cellPosition);

            StartCoroutine(RemoveTileAfterDelay(cellPosition));
        }
    }

    IEnumerator RemoveTileAfterDelay(Vector3Int cellPosition)
    {
        yield return new WaitForSeconds(delay);

        // Bỏ lock nếu có
        tilemap.SetTileFlags(cellPosition, TileFlags.None);

        // Xóa tile
        tilemap.SetTile(cellPosition, null);

        // Refresh lại tile ngay lập tức
        tilemap.RefreshTile(cellPosition);

        // Refresh toàn bộ tilemap cho chắc
        tilemap.CompressBounds();

        Debug.Log("Tile đã bị phá");
    }
}