using UnityEngine;

/// <summary>
/// Tự động scale Background theo kích thước màn hình khi bắt đầu game
/// Dùng cho SpriteRenderer (background ảnh)
/// Gắn script này vào GameObject chứa background
/// </summary>
public class AutoScaleBackground : MonoBehaviour
{
    private void Start()
    {
        ScaleBackground();
    }

    void ScaleBackground()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            Debug.LogError("Không tìm thấy SpriteRenderer!");
            return;
        }

        // Lấy chiều cao camera
        float screenHeight = Camera.main.orthographicSize * 2f;

        // Lấy chiều rộng camera theo tỉ lệ màn hình
        float screenWidth = screenHeight * Screen.width / Screen.height;

        // Kích thước gốc của sprite
        Vector2 spriteSize = sr.sprite.bounds.size;

        // Tính scale cần thiết
        float scaleX = screenWidth / spriteSize.x;
        float scaleY = screenHeight / spriteSize.y;

        // Chọn scale lớn hơn để tránh hở viền
        float scale = Mathf.Max(scaleX, scaleY);

        transform.localScale = new Vector3(scale, scale, 1f);
    }
}