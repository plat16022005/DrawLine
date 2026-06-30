using System;
using UnityEngine;

public enum WeatherType
{
    None,
    Rain,
    Snow,
    Sandstorm,
    HarshSun
}

/// <summary>
/// Singleton quản lý hệ thống thời tiết.
/// Gán script này lên một GameObject rỗng trong scene (ví dụ: "WeatherManager").
/// </summary>
public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    // ─── Thời tiết hiện tại ────────────────────────────────────────────────────
    public static WeatherType CurrentWeather { get; private set; } = WeatherType.None;

    /// <summary>
    /// Loại đường bị vô hiệu hóa bởi thời tiết hiện tại. null = không cấm gì.
    /// Snow      → Bouncy     (đường nảy xanh lá)
    /// Rain      → SlowDown   (đường làm chậm nâu)
    /// HarshSun  → Rubber     (đường cao su tím – vẫn vẽ được nhưng tự đứt sau 1s)
    /// Sandstorm → SpeedBoost (đường tăng tốc đỏ)
    /// </summary>
    public static LineType? DisabledLineType { get; private set; } = null;

    /// <summary>
    /// Fire khi thời tiết thay đổi. Tham số là WeatherType mới.
    /// Subscribe để cập nhật UI mà không cần polling.
    /// </summary>
    public static event Action<WeatherType> OnWeatherChanged;

    // ─── Inspector References ──────────────────────────────────────────────────
    [Header("Background")]
    [Tooltip("SpriteRenderer của background chính (đổi sprite khi đổi thời tiết)")]
    public SpriteRenderer backgroundRenderer;

    [Tooltip("Sprite background cho từng thời tiết theo thứ tự: None, Rain, Snow, Sandstorm, HarshSun")]
    public Sprite[] backgroundSprites = new Sprite[5]; // index = (int)WeatherType

    [Header("Weather Effect GameObjects (con của Background)")]
    [Tooltip("GameObject chứa hiệu ứng Mưa (Particle System)")]
    public GameObject rainEffectObject;

    [Tooltip("GameObject chứa hiệu ứng Tuyết (Particle System)")]
    public GameObject snowEffectObject;

    [Tooltip("GameObject chứa hiệu ứng Bão Cát (Particle System)")]
    public GameObject sandstormEffectObject;

    [Tooltip("GameObject chứa hiệu ứng Nắng Gắt (Light/Particle)")]
    public GameObject harshSunEffectObject;

    [Header("Thời Tiết Mặc Định Của Map")]
    [Tooltip("Chọn thời tiết mặc định cho map này. Được áp dụng ngay khi scene khởi động.")]
    public WeatherType initialWeather = WeatherType.None;

    // LineCreator được tìm tự động trong Start() — không cần kéo thả vào Inspector
    private LineCreator lineCreator;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Tự động tìm LineCreator trong scene (không cần kéo thả Inspector)
        lineCreator = FindObjectOfType<LineCreator>();
        if (lineCreator == null)
            Debug.LogWarning("[WeatherManager] Không tìm thấy LineCreator trong scene!");

        // Áp dụng thời tiết mặc định đã chọn trong Inspector
        SetWeather(initialWeather);
    }

    // ─── Public API ────────────────────────────────────────────────────────────

    /// <summary>Gọi từ nút UI hoặc bất kỳ script nào để đổi thời tiết.</summary>
    public void SetWeather(WeatherType type)
    {
        CurrentWeather = type;
        ApplyWeather(type);
        OnWeatherChanged?.Invoke(type);
    }

    // Overload nhận int để dễ gán vào UnityEvent trên Button (0=None,1=Rain,...)
    public void SetWeather(int typeIndex)
    {
        SetWeather((WeatherType)typeIndex);
    }

    // ─── Internal ──────────────────────────────────────────────────────────────

    private void ApplyWeather(WeatherType type)
    {
        // 1. Tắt tất cả effect objects
        SetEffectActive(rainEffectObject,       false);
        SetEffectActive(snowEffectObject,       false);
        SetEffectActive(sandstormEffectObject,  false);
        SetEffectActive(harshSunEffectObject,   false);

        // 2. Xác định loại đường bị cấm & bật effect tương ứng
        switch (type)
        {
            case WeatherType.Rain:
                SetEffectActive(rainEffectObject, true);
                DisabledLineType = LineType.SlowDown;
                break;

            case WeatherType.Snow:
                SetEffectActive(snowEffectObject, true);
                DisabledLineType = LineType.Bouncy;
                break;

            case WeatherType.Sandstorm:
                SetEffectActive(sandstormEffectObject, true);
                DisabledLineType = LineType.SpeedBoost;
                break;

            case WeatherType.HarshSun:
                SetEffectActive(harshSunEffectObject, true);
                // Rubber KHÔNG bị chặn hoàn toàn; chỉ tự đứt sau 1s (xử lý trong RubberBehavior)
                DisabledLineType = null;
                break;

            default: // None
                DisabledLineType = null;
                break;
        }

        // 3. Đổi background sprite
        if (backgroundRenderer != null)
        {
            int idx = (int)type;
            if (backgroundSprites != null && idx < backgroundSprites.Length && backgroundSprites[idx] != null)
            {
                backgroundRenderer.sprite = backgroundSprites[idx];
            }
        }

        // 4. Thông báo cho LineCreator cập nhật trạng thái nút UI
        if (lineCreator != null)
        {
            lineCreator.RefreshDisabledState();
        }

        Debug.Log($"[WeatherManager] Thời tiết: {type}  |  Đường bị khóa: {DisabledLineType?.ToString() ?? "Không có"}");
    }

    private static void SetEffectActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
