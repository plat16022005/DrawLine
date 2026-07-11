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

    [Header("Tooltip Content")]
    [Tooltip("Giải thích cho từng loại thời tiết theo thứ tự: [0] None, [1] Rain, [2] Snow, [3] Sandstorm, [4] HarshSun")]
    [TextArea(2, 4)]
    public string[] weatherDescriptions = new string[5] {
        "Thời tiết bình thường, không có hiệu ứng đặc biệt.",
        "Trời Mưa: Vô hiệu hóa đường Làm Chậm (màu nâu).",
        "Trời Tuyết: Vô hiệu hóa đường Nảy (màu xanh lá).",
        "Bão Cát: Vô hiệu hóa đường Tăng Tốc (màu đỏ).",
        "Nắng Gắt: Đường Cao Su (màu tím) bị đứt sau 1 giây."
    };

    private TooltipTrigger tooltipTrigger;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
            
        tooltipTrigger = GetComponent<TooltipTrigger>();
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
        int index = (int)type;
        
        if (targetImage != null)
        {
            if (weatherSprites != null && index < weatherSprites.Length)
            {
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

        if (tooltipTrigger != null)
        {
            if (weatherDescriptions != null && index < weatherDescriptions.Length)
            {
                tooltipTrigger.content = weatherDescriptions[index];
            }
            else
            {
                tooltipTrigger.content = "";
            }
        }
    }
}
