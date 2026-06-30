using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiển thị thông tin gió lên UI game.
/// - Tìm GlobalWind trong scene tự động (không cần kéo thả).
/// - Nếu tìm thấy: quay arrowObject theo windAngle và hiển thị windForce lên text.
/// - Nếu không tìm thấy: đổi sprite của indicatorRenderer thành noWindSprite và text = "0".
/// </summary>
public class WindUIDisplay : MonoBehaviour
{
    [Header("Arrow Indicator")]
    [Tooltip("GameObject mũi tên (hoặc icon gió) sẽ được xoay theo hướng gió")]
    public Transform arrowObject;

    [Header("Wind Force Text")]
    [Tooltip("TextMeshPro hiển thị cường độ gió")]
    public TextMeshProUGUI windForceText;

    [Header("Image Indicator")]
    [Tooltip("Image UI của icon chỉ hướng / trạng thái gió")]
    public Image indicatorImage;

    [Tooltip("Sprite hiển thị khi có gió (mũi tên bình thường)")]
    public Sprite windSprite;

    [Tooltip("Sprite hiển thị khi KHÔNG có gió trong scene")]
    public Sprite noWindSprite;

    [Header("Cập nhật")]
    [Tooltip("Tần suất kiểm tra GlobalWind (giây). 0 = chỉ kiểm tra lúc Start.")]
    [Min(0f)]
    public float refreshInterval = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    private GlobalWind wind;
    private float timer;

    private void Start()
    {
        FindWind();
        Refresh();
        timer = refreshInterval;
    }

    private void Update()
    {
        if (refreshInterval <= 0f) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = refreshInterval;
            FindWind();   // tìm lại phòng trường hợp GlobalWind bị thêm/xóa runtime
        }

        Refresh();
    }

    // ─── Internal ─────────────────────────────────────────────────────────────

    private void FindWind()
    {
        wind = FindObjectOfType<GlobalWind>();
    }

    private void Refresh()
    {
        if (wind != null)
        {
            ShowWind(wind.windAngle, wind.windForce);
        }
        else
        {
            ShowNoWind();
        }
    }

    /// <summary>Có gió: quay mũi tên + hiển thị lực gió.</summary>
    private void ShowWind(float angle, float force)
    {
        // Quay arrowObject theo windAngle (trục Z, khớp với cách GlobalWind quay GameObject của nó)
        if (arrowObject != null)
        {
            arrowObject.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // Hiển thị lực gió (làm tròn đến 1 chữ số thập phân)
        if (windForceText != null)
        {
            windForceText.text = Mathf.Round(force).ToString();
        }

        // Đổi sprite về trạng thái có gió
        if (indicatorImage != null && windSprite != null)
        {
            indicatorImage.sprite = windSprite;
        }
    }

    /// <summary>Không có gió: sprite trống + text "0".</summary>
    private void ShowNoWind()
    {
        // Reset góc về mặc định (mũi tên chỉ phải)
        if (arrowObject != null)
        {
            arrowObject.rotation = Quaternion.identity;
        }

        if (windForceText != null)
        {
            windForceText.text = "0";
        }

        // Đổi sang sprite "không có gió"
        if (indicatorImage != null && noWindSprite != null)
        {
            indicatorImage.sprite = noWindSprite;
        }
    }
}
