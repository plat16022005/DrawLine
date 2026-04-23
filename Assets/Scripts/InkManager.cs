using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton quản lý lượng mực vẽ toàn cục.
/// Cắm vào bất kỳ GameObject nào trong scene (ví dụ: GameController).
/// Gán InkBarFill (Image - Filled) và InkText (TextMeshProUGUI) qua Inspector.
/// </summary>
public class InkManager : MonoBehaviour
{
    public static InkManager Instance { get; private set; }

    [Header("Ink Settings")]
    [Tooltip("Tổng lượng mực ban đầu")]
    public float maxInk = 1000f;

    [Tooltip("Lượng mực tiêu tốn mỗi 1 unit vẽ (world space)")]
    public float inkCostPerUnit = 1f;

    [Header("UI References")]
    [Tooltip("Image (FillMethod: Horizontal) đại diện thanh mực")]
    public Image inkBarFill;

    [Tooltip("Text hiển thị số mực còn lại (TextMeshProUGUI)")]
    public TextMeshProUGUI inkText;

    // Lượng mực hiện tại
    public static float CurrentInk { get; private set; }

    // Snapshot khi StopSimulation để restore về
    private float inkAtStart;

    // ----------------------------------------------------------------

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        CurrentInk = maxInk;
        inkAtStart = maxInk;
        RefreshUI();
    }

    // ----------------------------------------------------------------
    // API công khai
    // ----------------------------------------------------------------

    /// <summary>Kiểm tra còn mực để vẽ không.</summary>
    public static bool HasInk() => CurrentInk > 0f;

    /// <summary>
    /// Tiêu mực tương ứng với khoảng cách vừa vẽ.
    /// Trả về true nếu vẽ được, false nếu hết mực.
    /// </summary>
    public bool ConsumeInk(float distance)
    {
        if (CurrentInk <= 0f) return false;

        float cost = distance * inkCostPerUnit;
        CurrentInk = Mathf.Max(0f, CurrentInk - cost);
        RefreshUI();
        return true;
    }

    /// <summary>
    /// Hoàn lại mực tương ứng với độ dài đường vừa bị tẩy.
    /// Không vượt quá maxInk.
    /// </summary>
    public void RefundInk(float erasedLength)
    {
        if (erasedLength <= 0f) return;

        float refund = erasedLength * inkCostPerUnit;
        CurrentInk = Mathf.Min(maxInk, CurrentInk + refund);
        RefreshUI();
    }

    /// <summary>Chụp snapshot mực lúc StartSimulation (để StopSimulation restore về).</summary>
    public void SnapshotInk()
    {
        inkAtStart = CurrentInk;
    }

    /// <summary>Khôi phục mực về lúc StartSimulation được bấm.</summary>
    public void RestoreInk()
    {
        CurrentInk = inkAtStart;
        RefreshUI();
    }

    /// <summary>Reset mực hoàn toàn về tối đa (ví dụ load level mới).</summary>
    public void ResetInk()
    {
        CurrentInk = maxInk;
        inkAtStart = maxInk;
        RefreshUI();
    }

    // ----------------------------------------------------------------
    // UI
    // ----------------------------------------------------------------

    private void RefreshUI()
    {
        float ratio = maxInk > 0f ? CurrentInk / maxInk : 0f;

        if (inkBarFill != null)
            inkBarFill.fillAmount = ratio;

        if (inkText != null)
            inkText.text = Mathf.CeilToInt(CurrentInk).ToString();
    }
}
