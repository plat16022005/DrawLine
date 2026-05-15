using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Điều khiển toàn bộ UI toolbar của Map Editor:
/// chọn công cụ, chọn tile, chọn loại bẫy, lưu map, quay về.
///
/// == SETUP TRONG UNITY INSPECTOR ==
/// Gắn script này vào Canvas (hoặc một Panel con).
/// 
/// Cấu trúc UI cần tạo trong Canvas:
///
///   Toolbar (Panel – góc trái/trên)
///   ├── BtnTile         (Button)
///   ├── BtnErase        (Button)
///   ├── BtnPlayer       (Button)
///   ├── BtnDemon        (Button)
///   ├── BtnPrincess     (Button)
///   ├── BtnTrap         (Button)
///   ├── TileDropdown    (TMP_Dropdown) – hiện khi tool = TilePaint
///   └── TrapDropdown    (TMP_Dropdown) – hiện khi tool = PlaceTrap
///
///   BottomBar (Panel – dưới)
///   ├── MapNameInput    (TMP_InputField) – nhập tên map
///   ├── BtnSave         (Button)
///   ├── BtnLoad         (Button)
///   ├── BtnClear        (Button)
///   └── BtnBack         (Button)
///
///   SaveMessage (TextMeshProUGUI – ẩn mặc định)
/// </summary>
public class MapEditorUI : MonoBehaviour
{
    // ── Inspector References ─────────────────────────────────────────
    [Header("Manager")]
    public MapEditorManager manager;

    [Header("Tool Buttons")]
    public Button btnTile;
    public Button btnErase;
    public Button btnPlayer;
    public Button btnDemon;
    public Button btnPrincess;
    public Button btnTrap;

    [Header("Dropdowns")]
    public TMP_Dropdown tileDropdown;
    public TMP_Dropdown trapDropdown;
    public TMP_Dropdown tileToolDropdown; // Dropdown chọn tile khi đang ở tool TilePaint

    [Header("Bottom Bar")]
    public TMP_InputField mapNameInput;
    public Button btnSave;
    public Button btnLoad;
    public Button btnClear;
    public Button btnBack;

    [Header("Feedback")]
    public TextMeshProUGUI saveMessageText;
    public float saveMessageDuration = 2.5f;

    [Header("Tile Names (hiển thị trong dropdown)")]
    public string[] tileNames; // Điền tên tile tương ứng với tiles[] của MapEditorManager

    // ── Private ──────────────────────────────────────────────────────
    private float _saveMessageTimer = 0f;
    private Button _activeToolBtn;

    // ── Lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        // Gắn listener công cụ
        if (btnTile != null)     btnTile.onClick.AddListener(()     => SelectTool(MapEditorTool.TilePaint,    btnTile));
        if (btnErase != null)    btnErase.onClick.AddListener(()    => SelectTool(MapEditorTool.TileErase,   btnErase));
        if (btnPlayer != null)   btnPlayer.onClick.AddListener(()   => SelectTool(MapEditorTool.PlacePlayer, btnPlayer));
        if (btnDemon != null)    btnDemon.onClick.AddListener(()    => SelectTool(MapEditorTool.PlaceDemon,  btnDemon));
        if (btnPrincess != null) btnPrincess.onClick.AddListener(() => SelectTool(MapEditorTool.PlacePrincess, btnPrincess));
        if (btnTrap != null)     btnTrap.onClick.AddListener(()     => SelectTool(MapEditorTool.PlaceTrap,   btnTrap));

        // Gắn listener bottom bar
        if (btnSave != null)  btnSave.onClick.AddListener(OnSave);
        if (btnLoad != null)  btnLoad.onClick.AddListener(OnLoad);
        if (btnClear != null) btnClear.onClick.AddListener(OnClear);
        if (btnBack != null)  btnBack.onClick.AddListener(OnBack);

        // Dropdown tile
        if (tileDropdown != null)
        {
            tileDropdown.ClearOptions();
            if (tileNames != null && tileNames.Length > 0)
            {
                var opts = new System.Collections.Generic.List<string>(tileNames);
                tileDropdown.AddOptions(opts);
            }
            tileDropdown.onValueChanged.AddListener(idx =>
            {
                if (manager != null) manager.SelectedTileIndex = idx;
            });
        }

        // Dropdown bẫy – populate từ enum TrapType
        if (trapDropdown != null)
        {
            trapDropdown.ClearOptions();
            var trapNames = System.Enum.GetNames(typeof(TrapType));
            trapDropdown.AddOptions(new System.Collections.Generic.List<string>(trapNames));
            trapDropdown.onValueChanged.AddListener(idx =>
            {
                if (manager != null) manager.SelectedTrapType = (TrapType)idx;
            });
        }

        // Ẩn save message
        if (saveMessageText != null) saveMessageText.gameObject.SetActive(false);

        // Ẩn các dropdown phụ khi khởi động
        SetDropdownsForTool(MapEditorTool.None);
    }

    private void Update()
    {
        // Đếm ngược ẩn thông báo lưu
        if (saveMessageText != null && saveMessageText.gameObject.activeSelf)
        {
            _saveMessageTimer -= Time.unscaledDeltaTime;
            if (_saveMessageTimer <= 0f)
                saveMessageText.gameObject.SetActive(false);
        }
    }

    // ── Tool Selection ───────────────────────────────────────────────
    private void SelectTool(MapEditorTool tool, Button btn)
    {
        if (manager == null) return;
        manager.SetTool(tool);

        // Highlight nút đang chọn (đổi màu)
        if (_activeToolBtn != null)
            SetButtonHighlight(_activeToolBtn, false);
        _activeToolBtn = btn;
        SetButtonHighlight(_activeToolBtn, true);

        // Hiện/ẩn dropdown phù hợp
        SetDropdownsForTool(tool);
    }

    private void SetDropdownsForTool(MapEditorTool tool)
    {
        bool showTile = (tool == MapEditorTool.TilePaint);
        bool showTrap = (tool == MapEditorTool.PlaceTrap);

        if (tileDropdown != null) tileDropdown.gameObject.SetActive(showTile);
        if (trapDropdown != null) trapDropdown.gameObject.SetActive(showTrap);
    }

    private void SetButtonHighlight(Button btn, bool highlight)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = highlight ? new Color(0.3f, 0.8f, 0.3f) : Color.white;
        btn.colors = colors;
    }

    // ── Bottom Bar ───────────────────────────────────────────────────
    private void OnSave()
    {
        if (manager == null) return;
        if (mapNameInput != null && !string.IsNullOrWhiteSpace(mapNameInput.text))
            manager.SetMapName(mapNameInput.text);
        manager.SaveMap();
    }

    private void OnLoad()
    {
        if (manager == null) return;
        string name = (mapNameInput != null && !string.IsNullOrWhiteSpace(mapNameInput.text))
                      ? mapNameInput.text
                      : manager.defaultMapName;
        manager.LoadMap(name);
    }

    private void OnClear()
    {
        if (manager != null) manager.ClearEditor();
    }

    private void OnBack()
    {
        if (manager != null) manager.BackToMainMenu();
    }

    // ── Feedback ─────────────────────────────────────────────────────
    /// <summary>
    /// Hiển thị thông báo ngắn (VD: "Lưu thành công!") rồi tự ẩn.
    /// </summary>
    public void ShowSaveMessage(string message)
    {
        if (saveMessageText == null) return;
        saveMessageText.text = message;
        saveMessageText.gameObject.SetActive(true);
        _saveMessageTimer = saveMessageDuration;
    }
}
