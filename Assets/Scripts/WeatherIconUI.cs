using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cập nhật một Image UI theo thời tiết hiện tại.
/// Gắn lên bất kỳ Image nào trong scene, điền sprites vào mảng là xong.
/// </summary>
public class WeatherIconUI : MonoBehaviour
{
    [Header("Target Image")]
    [Tooltip("Image UI cần đổi icon. Nếu bỏ trống sẽ tự lấy Image trên cùng GameObject.")]
    public Image targetImage;

    [Header("Sprites theo thời tiết")]
    [Tooltip("Theo thứ tự: [0] None, [1] Rain, [2] Snow, [3] Sandstorm, [4] HarshSun")]
    public Sprite[] weatherSprites = new Sprite[5];

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        WeatherManager.OnWeatherChanged += OnWeatherChanged;

        // Áp dụng ngay thời tiết hiện tại khi object được bật
        OnWeatherChanged(WeatherManager.CurrentWeather);
    }

    private void OnDisable()
    {
        WeatherManager.OnWeatherChanged -= OnWeatherChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void OnWeatherChanged(WeatherType type)
    {
        if (targetImage == null) return;

        int index = (int)type;
        if (weatherSprites == null || index >= weatherSprites.Length) return;

        Sprite sprite = weatherSprites[index];

        if (sprite != null)
        {
            targetImage.sprite  = sprite;
            targetImage.enabled = true;
        }
        else
        {
            // Không có sprite cho thời tiết này → ẩn image đi
            targetImage.enabled = false;
        }
    }
}
