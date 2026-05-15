using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

/// <summary>
/// Script điều phối chính của Map Editor.
/// Xử lý input chuột, gọi đúng hành động theo công cụ đang chọn,
/// và quản lý lưu/tải MapData.
/// 
/// == SETUP TRONG UNITY INSPECTOR ==
/// Gắn script này vào GameObject "MapEditorManager" trong scene MakeMap.
/// Kéo thả các reference sau vào Inspector:
///   - tilemap          : GameObject Grid > Tilemap
///   - locationObjects  : GameObject LocationObjects
///   - locationTraps    : GameObject LocationTraps
///   - playerMarker     : prefab Player (hoặc icon placeholder)
///   - demonMarker      : prefab Demon
///   - princessMarker   : prefab Princess
///   - trapMarkerPrefab : một sprite/prefab đơn giản làm icon bẫy
///   - tiles[]          : kéo tất cả TileBase assets muốn dùng vào đây
///   - trapPanelManager : TrapPanelManager component
///   - editorUI         : MapEditorUI component
/// </summary>
public class MapEditorManager : MonoBehaviour
{
    // ── Inspector References ─────────────────────────────────────────
    [Header("Grid & Tilemap")]
    public Tilemap tilemap;
    public TileBase[] tiles; // tile index 0,1,2... tương ứng dropdown

    [Header("Location Containers")]
    public Transform locationObjects;  // cha chứa marker Player/Demon/Princess
    public Transform locationTraps;    // cha chứa các trap marker

    [Header("Markers / Prefabs")]
    public GameObject playerMarkerPrefab;
    public GameObject demonMarkerPrefab;
    public GameObject princessMarkerPrefab;
    public GameObject trapMarkerPrefab;   // icon generic fallback nếu không có prefab cụ thể

    [Header("Trap Prefabs – prefab thật cho từng loại bẫy")]
    public GameObject fireSpawnerPrefab;
    public GameObject movingBlockPrefab;
    public GameObject breakablePlatformPrefab;
    public GameObject slowDownPrefab;
    public GameObject speedBoostPrefab;

    [Header("References")]
    public TrapPanelManager trapPanelManager;
    public MapEditorUI editorUI;
    public Camera editorCamera;

    [Header("Map Info")]
    public string defaultMapName = "MyMap";

    // ── Runtime State ────────────────────────────────────────────────
    public MapEditorTool CurrentTool { get; private set; } = MapEditorTool.None;
    public int SelectedTileIndex { get; set; } = 0;
    public TrapType SelectedTrapType { get; set; } = TrapType.FireSpawner;

    private MapData currentMapData = new MapData();

    // Singleton markers (chỉ có 1 Player/Demon/Princess trên map)
    private GameObject _playerMarkerInst;
    private GameObject _demonMarkerInst;
    private GameObject _princessMarkerInst;

    // Danh sách trap marker đã đặt (cùng index với currentMapData.traps)
    private List<GameObject> _trapMarkerInsts = new List<GameObject>();

    // Vị trí cell đang chờ xác nhận bẫy (chưa đặt hẳn vào map)
    private Vector3Int _pendingTrapCell;

    // Preview GameObject được spawn ngay khi click – sẽ giữ lại khi Confirm, hủy khi Cancel
    private GameObject _pendingTrapPreviewGO;

    // ── Lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        if (editorCamera == null)
            editorCamera = Camera.main;

        currentMapData.mapName = defaultMapName;
    }

    private void Update()
    {
        // Không xử lý click khi con chuột đang ở trên UI element
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Chỉ xử lý input khi không có UI panel đang mở
        if (trapPanelManager != null && trapPanelManager.IsPanelOpen)
            return;

        if (Input.GetMouseButton(0))
            HandleMouseInput();
    }

    // ── Tool Selection ───────────────────────────────────────────────
    public void SetTool(MapEditorTool tool)
    {
        // Nếu đang có preview bẫy chưa confirm mà đổi tool → hủy preview và đóng panel
        if (_pendingTrapPreviewGO != null)
            CancelTrapPlacement();
        if (trapPanelManager != null && trapPanelManager.IsPanelOpen)
            trapPanelManager.ForceClose();

        CurrentTool = tool;
        Debug.Log($"[MapEditor] Tool đã chọn: {tool}");
    }

    // ── Input Handling ───────────────────────────────────────────────
    private void HandleMouseInput()
    {
        Vector3 worldPos = editorCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0f;

        switch (CurrentTool)
        {
            case MapEditorTool.TilePaint:
                PaintTile(worldPos);
                break;
            case MapEditorTool.TileErase:
                EraseTile(worldPos);
                break;
            case MapEditorTool.PlacePlayer:
                if (Input.GetMouseButtonDown(0)) PlaceMarker(ref _playerMarkerInst, playerMarkerPrefab, worldPos, ref currentMapData.playerSpawn, ref currentMapData.hasPlayerSpawn);
                break;
            case MapEditorTool.PlaceDemon:
                if (Input.GetMouseButtonDown(0)) PlaceMarker(ref _demonMarkerInst, demonMarkerPrefab, worldPos, ref currentMapData.demonSpawn, ref currentMapData.hasDemonSpawn);
                break;
            case MapEditorTool.PlacePrincess:
                if (Input.GetMouseButtonDown(0)) PlaceMarker(ref _princessMarkerInst, princessMarkerPrefab, worldPos, ref currentMapData.princessSpawn, ref currentMapData.hasPrincessSpawn);
                break;
            case MapEditorTool.PlaceTrap:
                if (Input.GetMouseButtonDown(0)) OpenTrapPanel(worldPos);
                break;
        }
    }

    // ── Tile Paint / Erase ───────────────────────────────────────────
    private void PaintTile(Vector3 worldPos)
    {
        if (tiles == null || tiles.Length == 0) return;
        int idx = Mathf.Clamp(SelectedTileIndex, 0, tiles.Length - 1);
        if (tiles[idx] == null) return;

        Vector3Int cell = tilemap.WorldToCell(worldPos);
        tilemap.SetTile(cell, tiles[idx]);

        // Cập nhật MapData
        UpdateTileEntry(cell.x, cell.y, idx);
    }

    private void EraseTile(Vector3 worldPos)
    {
        Vector3Int cell = tilemap.WorldToCell(worldPos);
        tilemap.SetTile(cell, null);
        RemoveTileEntry(cell.x, cell.y);
    }

    private void UpdateTileEntry(int x, int y, int tileIndex)
    {
        var entry = currentMapData.tiles.Find(t => t.x == x && t.y == y);
        if (entry != null)
            entry.tileIndex = tileIndex;
        else
            currentMapData.tiles.Add(new TileEntry(x, y, tileIndex));
    }

    private void RemoveTileEntry(int x, int y)
    {
        currentMapData.tiles.RemoveAll(t => t.x == x && t.y == y);
    }

    // ── Place Marker (Player / Demon / Princess) ─────────────────────
    private void PlaceMarker(ref GameObject markerInst, GameObject prefab,
                             Vector3 worldPos, ref Vector3 dataPos, ref bool hasSpawn)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[MapEditor] Prefab marker chưa được gán trong Inspector.");
            return;
        }

        // Snap về giữa cell
        Vector3Int cell = tilemap.WorldToCell(worldPos);
        Vector3 snapped = tilemap.GetCellCenterWorld(cell);

        if (markerInst == null)
            markerInst = Instantiate(prefab, snapped, Quaternion.identity, locationObjects);
        else
            markerInst.transform.position = snapped;

        // Đặt Time.timeScale = 0 đảm bảo vật lý không chạy trong editor
        Time.timeScale = 0f;

        dataPos = snapped;
        hasSpawn = true;
    }

    // ── Trap Placement ───────────────────────────────────────────────
    private void OpenTrapPanel(Vector3 worldPos)
    {
        Vector3Int cell = tilemap.WorldToCell(worldPos);
        _pendingTrapCell = cell;

        // Không tạo prefab preview ngay lập tức nữa (theo yêu cầu user)
        // Chỉ lưu vị trí click và mở panel nhập thông số
        if (trapPanelManager != null)
            trapPanelManager.Open(SelectedTrapType, this);
    }

    /// <summary>
    /// Trả về prefab tương ứng với loại bẫy.
    /// </summary>
    private GameObject GetTrapPrefab(TrapType type)
    {
        switch (type)
        {
            case TrapType.FireSpawner:       return fireSpawnerPrefab       != null ? fireSpawnerPrefab       : trapMarkerPrefab;
            case TrapType.MovingBlock:       return movingBlockPrefab       != null ? movingBlockPrefab       : trapMarkerPrefab;
            case TrapType.BreakablePlatform: return breakablePlatformPrefab != null ? breakablePlatformPrefab : trapMarkerPrefab;
            case TrapType.SlowDown:          return slowDownPrefab          != null ? slowDownPrefab          : trapMarkerPrefab;
            case TrapType.SpeedBoost:        return speedBoostPrefab        != null ? speedBoostPrefab        : trapMarkerPrefab;
            default:                         return trapMarkerPrefab;
        }
    }

    /// <summary>
    /// Tắt các component runtime (Rigidbody, MonoBehaviour game script) để
    /// preview không chạy physics / logic trong Map Editor.
    /// </summary>
    private void DisableRuntimeComponents(GameObject go)
    {
        // Tắt simulate vật lý
        foreach (var rb in go.GetComponentsInChildren<Rigidbody2D>())
            rb.simulated = false;

        // Tắt tất cả MonoBehaviour trên prefab (script game logic)
        // Giữ lại Renderer để vẫn nhìn thấy hình
        foreach (var mono in go.GetComponentsInChildren<MonoBehaviour>())
            mono.enabled = false;
    }

    /// <summary>
    /// Gọi bởi TrapPanelManager khi người dùng nhấn Cancel – hủy preview.
    /// </summary>
    public void CancelTrapPlacement()
    {
        // Hiện tại không có preview GO nên chỉ log
        Debug.Log("[MapEditor] Đã hủy đặt bẫy.");
    }

    /// <summary>
    /// Gọi bởi TrapPanelManager khi người dùng nhấn Confirm.
    /// Giữ lại preview GO đã spawn, lưu TrapData vào MapData.
    /// </summary>
    public void ConfirmTrapPlacement(TrapData data)
    {
        Vector3 snapped = tilemap.GetCellCenterWorld(_pendingTrapCell);
        data.position = snapped;

        // Tạo prefab tương ứng loại bẫy sau khi đã nhấn Confirm
        GameObject prefabToUse = GetTrapPrefab(data.type);
        if (prefabToUse != null)
        {
            GameObject marker = Instantiate(prefabToUse, snapped, Quaternion.identity, locationTraps);
            DisableRuntimeComponents(marker);
            _trapMarkerInsts.Add(marker);
        }
        else if (trapMarkerPrefab != null)
        {
            // Fallback: dùng icon generic nếu prefab cụ thể chưa được gán
            GameObject marker = Instantiate(trapMarkerPrefab, snapped, Quaternion.identity, locationTraps);
            var label = marker.GetComponentInChildren<TMPro.TextMeshPro>();
            if (label != null) label.text = data.type.ToString();
            _trapMarkerInsts.Add(marker);
        }

        currentMapData.traps.Add(data);
        Debug.Log($"[MapEditor] Đã đặt bẫy {data.type} tại {snapped}");
    }

    // ── Save / Load ──────────────────────────────────────────────────
    public void SaveMap()
    {
        string path = Path.Combine(Application.persistentDataPath, currentMapData.mapName + ".json");
        string json = JsonUtility.ToJson(currentMapData, true);
        File.WriteAllText(path, json);
        Debug.Log($"[MapEditor] Đã lưu map tại: {path}");
        if (editorUI != null) editorUI.ShowSaveMessage("Lưu thành công! (" + currentMapData.mapName + ".json)");
    }

    public void LoadMap(string mapName)
    {
        string path = Path.Combine(Application.persistentDataPath, mapName + ".json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[MapEditor] Không tìm thấy file: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        currentMapData = JsonUtility.FromJson<MapData>(json);
        RebuildFromMapData();
        Debug.Log($"[MapEditor] Đã tải map: {mapName}");
    }

    private void RebuildFromMapData()
    {
        // Xóa scene cũ
        ClearEditor();

        // Vẽ lại tile
        foreach (var entry in currentMapData.tiles)
        {
            if (entry.tileIndex >= 0 && entry.tileIndex < tiles.Length)
                tilemap.SetTile(new Vector3Int(entry.x, entry.y, 0), tiles[entry.tileIndex]);
        }

        // Khôi phục marker
        if (currentMapData.hasPlayerSpawn)
            PlaceMarker(ref _playerMarkerInst, playerMarkerPrefab,
                        currentMapData.playerSpawn, ref currentMapData.playerSpawn, ref currentMapData.hasPlayerSpawn);
        if (currentMapData.hasDemonSpawn)
            PlaceMarker(ref _demonMarkerInst, demonMarkerPrefab,
                        currentMapData.demonSpawn, ref currentMapData.demonSpawn, ref currentMapData.hasDemonSpawn);
        if (currentMapData.hasPrincessSpawn)
            PlaceMarker(ref _princessMarkerInst, princessMarkerPrefab,
                        currentMapData.princessSpawn, ref currentMapData.princessSpawn, ref currentMapData.hasPrincessSpawn);

        // Khôi phục bẫy – dùng đúng prefab theo loại
        foreach (var trap in currentMapData.traps)
        {
            GameObject prefabToUse = GetTrapPrefab(trap.type);
            if (prefabToUse != null)
            {
                GameObject marker = Instantiate(prefabToUse, trap.position, Quaternion.identity, locationTraps);
                DisableRuntimeComponents(marker);
                _trapMarkerInsts.Add(marker);
            }
        }
    }

    public void ClearEditor()
    {
        tilemap.ClearAllTiles();
        currentMapData.tiles.Clear();

        if (_playerMarkerInst != null) Destroy(_playerMarkerInst);
        if (_demonMarkerInst != null) Destroy(_demonMarkerInst);
        if (_princessMarkerInst != null) Destroy(_princessMarkerInst);

        foreach (var m in _trapMarkerInsts) if (m != null) Destroy(m);
        _trapMarkerInsts.Clear();

        currentMapData.traps.Clear();
        currentMapData.hasPlayerSpawn = false;
        currentMapData.hasDemonSpawn = false;
        currentMapData.hasPrincessSpawn = false;
    }

    public void SetMapName(string name)
    {
        currentMapData.mapName = string.IsNullOrWhiteSpace(name) ? defaultMapName : name;
    }

    public MapData GetCurrentMapData() => currentMapData;

    // ── Scene Navigation ─────────────────────────────────────────────
    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
