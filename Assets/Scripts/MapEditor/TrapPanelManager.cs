using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý panel nhập thông số bẫy trong Map Editor.
/// 
/// == SETUP TRONG UNITY INSPECTOR ==
/// Gắn script này vào GameObject "TrapPanel" (con của Canvas).
/// Mỗi sub-panel tương ứng một loại bẫy – ẩn tất cả mặc định.
/// 
/// Cấu trúc UI cần tạo:
///   TrapPanel (Panel – ẩn mặc định)
///   ├── FireSpawnerPanel
///   │   ├── InputFireDuration   (TMP_InputField)
///   │   ├── InputRespawnDelay   (TMP_InputField)
///   │   ├── InputFireAngle      (TMP_InputField)
///   │   └── InputYOffset        (TMP_InputField)
///   ├── MovingBlockPanel
///   │   ├── InputMoveX          (TMP_InputField)
///   │   ├── InputMoveY          (TMP_InputField)
///   │   └── InputSpeed          (TMP_InputField)
///   ├── BreakablePlatformPanel
///   │   └── InputBreakDelay     (TMP_InputField)
///   ├── SlowDownPanel
///   │   └── InputSlowRate       (TMP_InputField)
///   ├── SpeedBoostPanel         (không có input thêm)
///   ├── BtnConfirm              (Button)
///   └── BtnCancel               (Button)
/// </summary>
public class TrapPanelManager : MonoBehaviour
{
    // ── Inspector References ─────────────────────────────────────────
    [Header("Sub-Panels")]
    public GameObject fireSpawnerPanel;
    public GameObject movingBlockPanel;
    public GameObject breakablePlatformPanel;
    public GameObject slowDownPanel;
    public GameObject speedBoostPanel;

    [Header("FireSpawner Fields")]
    public TMP_InputField inputFireDuration;
    public TMP_InputField inputRespawnDelay;
    public TMP_InputField inputFireAngle;
    public TMP_InputField inputYOffset;

    [Header("MovingBlock Fields")]
    public TMP_InputField inputMoveX;
    public TMP_InputField inputMoveY;
    public TMP_InputField inputSpeed;

    [Header("BreakablePlatform Fields")]
    public TMP_InputField inputBreakDelay;

    [Header("SlowDown Fields")]
    public TMP_InputField inputSlowRate;

    [Header("Buttons")]
    public Button btnConfirm;
    public Button btnCancel;

    // ── Runtime State ────────────────────────────────────────────────
    public bool IsPanelOpen { get; private set; } = false;

    private TrapType _currentType;
    private MapEditorManager _manager;

    // ── Lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        gameObject.SetActive(false);

        if (btnConfirm != null) btnConfirm.onClick.AddListener(OnConfirm);
        if (btnCancel != null)  btnCancel.onClick.AddListener(OnCancel);
    }

    // ── Public API ───────────────────────────────────────────────────
    /// <summary>
    /// Mở panel tương ứng với loại bẫy đang chọn.
    /// </summary>
    public void Open(TrapType trapType, MapEditorManager manager)
    {
        _currentType = trapType;
        _manager = manager;

        gameObject.SetActive(true);
        IsPanelOpen = true;

        // Ẩn tất cả sub-panel trước
        SetAllPanelsActive(false);

        // Hiện sub-panel tương ứng và điền giá trị mặc định
        switch (trapType)
        {
            case TrapType.FireSpawner:
                ShowPanel(fireSpawnerPanel);
                SetDefaults_FireSpawner();
                break;
            case TrapType.MovingBlock:
                ShowPanel(movingBlockPanel);
                SetDefaults_MovingBlock();
                break;
            case TrapType.BreakablePlatform:
                ShowPanel(breakablePlatformPanel);
                SetDefaults_BreakablePlatform();
                break;
            case TrapType.SlowDown:
                ShowPanel(slowDownPanel);
                SetDefaults_SlowDown();
                break;
            case TrapType.SpeedBoost:
                ShowPanel(speedBoostPanel);
                break;
        }
    }

    // ── Private Helpers ──────────────────────────────────────────────
    private void ShowPanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(true);
    }

    private void SetAllPanelsActive(bool active)
    {
        if (fireSpawnerPanel != null)        fireSpawnerPanel.SetActive(active);
        if (movingBlockPanel != null)        movingBlockPanel.SetActive(active);
        if (breakablePlatformPanel != null)  breakablePlatformPanel.SetActive(active);
        if (slowDownPanel != null)           slowDownPanel.SetActive(active);
        if (speedBoostPanel != null)         speedBoostPanel.SetActive(active);
    }

    private void SetDefaults_FireSpawner()
    {
        if (inputFireDuration != null)  inputFireDuration.text  = "1";
        if (inputRespawnDelay != null)  inputRespawnDelay.text  = "3";
        if (inputFireAngle != null)     inputFireAngle.text     = "0";
        if (inputYOffset != null)       inputYOffset.text       = "1.62";
    }

    private void SetDefaults_MovingBlock()
    {
        if (inputMoveX != null) inputMoveX.text = "0";
        if (inputMoveY != null) inputMoveY.text = "0";
        if (inputSpeed != null) inputSpeed.text = "2";
    }

    private void SetDefaults_BreakablePlatform()
    {
        if (inputBreakDelay != null) inputBreakDelay.text = "1";
    }

    private void SetDefaults_SlowDown()
    {
        if (inputSlowRate != null) inputSlowRate.text = "10";
    }

    // ── Button Handlers ──────────────────────────────────────────────
    private void OnConfirm()
    {
        if (_manager == null) { Close(); return; }

        TrapData data = new TrapData { type = _currentType };

        switch (_currentType)
        {
            case TrapType.FireSpawner:
                data.fireDuration  = ParseFloat(inputFireDuration,  1f);
                data.respawnDelay  = ParseFloat(inputRespawnDelay,  3f);
                data.fireAngle     = ParseFloat(inputFireAngle,     0f);
                data.yOffset       = ParseFloat(inputYOffset,       1.62f);
                break;
            case TrapType.MovingBlock:
                data.moveX = ParseFloat(inputMoveX, 0f);
                data.moveY = ParseFloat(inputMoveY, 0f);
                data.speed = ParseFloat(inputSpeed, 2f);
                break;
            case TrapType.BreakablePlatform:
                data.breakDelay = ParseFloat(inputBreakDelay, 1f);
                break;
            case TrapType.SlowDown:
                data.slowRate = ParseFloat(inputSlowRate, 10f);
                break;
            case TrapType.SpeedBoost:
                // Không có param bổ sung
                break;
        }

        _manager.ConfirmTrapPlacement(data);
        Close();
    }

    private void OnCancel()
    {
        // Hủy prefab preview đã spawn tại vị trí click
        if (_manager != null) _manager.CancelTrapPlacement();
        Close();
    }

    private void Close()
    {
        IsPanelOpen = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Đóng panel ngay lập tức mà không trigger CancelTrapPlacement.
    /// Dùng khi Manager đã tự hủy preview rồi (VD: người dùng đổi tool).
    /// </summary>
    public void ForceClose()
    {
        IsPanelOpen = false;
        gameObject.SetActive(false);
    }

    private float ParseFloat(TMP_InputField field, float defaultVal)
    {
        if (field == null) return defaultVal;
        return float.TryParse(field.text, out float v) ? v : defaultVal;
    }
}
