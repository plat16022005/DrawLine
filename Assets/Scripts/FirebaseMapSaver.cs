using System.Collections.Generic;
#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase;
using Firebase.Auth;
using Firebase.Database;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class FirebaseMapSaver : MonoBehaviour
{
    public Tilemap tilemap;
    public TileDatabase tileDatabase;

    public int width = 20;
    public int height = 12;

#if !UNITY_WEBGL || UNITY_EDITOR
    private DatabaseReference dbRef;
    private FirebaseAuth auth;
#endif
    private bool firebaseReady = false;
    public Transform trapParent;
    public SpawnPointEditor spawnPointEditor;
    public MapSettingsPanelUI mapSettingsUI;

    [Tooltip("(Tùy chọn) Gán FirebaseMapLoader để tự động biết currentMapId khi đang edit map cũ")]
    public FirebaseMapLoader mapLoader;

    [Header("Button Wrappers")]
    [Tooltip("InputField chứa tên map — dùng bởi OnClickSaveCurrentMap và OnClickSaveNewMap")]
    public TMP_InputField mapNameInput;
    [Tooltip("Text hiển thị thông báo kết quả save")]
    public TextMeshProUGUI saveNotificationText;
    public GameObject Toolbar;
    public GameObject Optionsbar;

    public GameObject BangThongBao;
    public TextMeshProUGUI TextThongBao;
    async void Start()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        var result = await FirebaseInitializer.EnsureInitializedAsync();

        if (result == DependencyStatus.Available)
        {
            auth = FirebaseAuth.DefaultInstance;
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;

            firebaseReady = true;
            Debug.Log("Firebase Ready");
        }
        else
        {
            Debug.LogError("Firebase lỗi dependency: " + result);
        }
#endif
    }

/// <summary>
/// Tạo map mới với chỉ tên map. Dùng cho nút "Tạo Map".
/// Trả về mapId vừa tạo (rỗng nếu thất bại).
/// </summary>
public async System.Threading.Tasks.Task<string> CreateMap(string mapName)
{
#if !UNITY_WEBGL || UNITY_EDITOR
    if (!firebaseReady)
    {
        Debug.LogError("Firebase chưa khởi tạo xong");
        return "";
    }

    if (string.IsNullOrWhiteSpace(mapName))
    {
        Debug.LogError("Tên map không được để trống");
        return "";
    }

    string userId = "guest_maps";
    if (auth != null && auth.CurrentUser != null)
        userId = auth.CurrentUser.UserId;

    string mapId = dbRef.Child("maps").Child(userId).Push().Key;

    try
    {
        // maps/<userId>/<mapId> — dữ liệu riêng của người chơi, không có status
        var mapEntry = new System.Collections.Generic.Dictionary<string, object>
        {
            { "mapName", mapName }
        };

        // mapscommunity/<mapId> — entry community, bắt đầu là private
        var communityEntry = new System.Collections.Generic.Dictionary<string, object>
        {
            { "mapName", mapName },
            { "ownerId", userId },
            { "status", "private" },
            { "hasPublishedOnce", false }
        };

        await dbRef.Child("maps").Child(userId).Child(mapId).SetValueAsync(mapEntry);
        await dbRef.Child("mapscommunity").Child(mapId).SetValueAsync(communityEntry);

        Debug.Log($"Tạo map THÀNH CÔNG: mapId={mapId}, tên={mapName}");
        return mapId;
    }
    catch (System.Exception e)
    {
        Debug.LogError("Tạo map THẤT BẠI: " + e);
        return "";
    }
#else
    if (FirebaseJSBridge.instance == null)
    {
        Debug.LogError("FirebaseJSBridge chưa khởi tạo xong");
        return "";
    }

    if (string.IsNullOrWhiteSpace(mapName))
    {
        Debug.LogError("Tên map không được để trống");
        return "";
    }

    string userId = FirebaseJSBridge.instance.GetCurrentUserId();
    if (string.IsNullOrEmpty(userId)) userId = "guest_maps";

    string mapId = await FirebaseJSBridge.instance.PushKeyAsync($"maps/{userId}");

    try
    {
        var mapEntry = new System.Collections.Generic.Dictionary<string, object>
        {
            { "mapName", mapName }
        };

        var communityEntry = new System.Collections.Generic.Dictionary<string, object>
        {
            { "mapName", mapName },
            { "ownerId", userId },
            { "status", "private" },
            { "hasPublishedOnce", false }
        };

        await FirebaseJSBridge.instance.WriteDatabaseAsync($"maps/{userId}/{mapId}", Newtonsoft.Json.JsonConvert.SerializeObject(mapEntry));
        await FirebaseJSBridge.instance.WriteDatabaseAsync($"mapscommunity/{mapId}", Newtonsoft.Json.JsonConvert.SerializeObject(communityEntry));

        Debug.Log($"Tạo map THÀNH CÔNG: mapId={mapId}, tên={mapName}");
        return mapId;
    }
    catch (System.Exception e)
    {
        Debug.LogError("Tạo map THẤT BẠI: " + e);
        return "";
    }
#endif
}

// ─────────────────────────────────────────────────────────
// Helper: Build MapData từ scene hiện tại
// ─────────────────────────────────────────────────────────
MapData BuildMapData(string mapName)
{
    MapData mapData = new MapData();
    mapData.mapName = mapName;
    mapData.width = width;
    mapData.height = height;

    // Settings
    if (mapSettingsUI != null)
    {
        mapData.cameraLens = mapSettingsUI.currentCameraLens;
        mapData.inkCostPerUnit = mapSettingsUI.currentInkCostPerUnit;
        mapData.weatherType = mapSettingsUI.currentWeatherType;
        mapData.enableWind = mapSettingsUI.currentEnableWind;
        mapData.windForce = mapSettingsUI.currentWindForce;
        mapData.windAngle = mapSettingsUI.currentWindAngle;
    }
    else
    {
        if (Camera.main != null) mapData.cameraLens = Camera.main.orthographicSize;
        if (InkManager.Instance != null) mapData.inkCostPerUnit = InkManager.Instance.inkCostPerUnit;
        if (WeatherManager.Instance != null) mapData.weatherType = (int)WeatherManager.CurrentWeather;
        GlobalWind wind = FindObjectOfType<GlobalWind>(true);
        if (wind != null)
        {
            mapData.enableWind = wind.gameObject.activeSelf;
            mapData.windForce = wind.windForce;
            mapData.windAngle = wind.windAngle;
        }
    }

    // Tiles
    BoundsInt bounds = tilemap.cellBounds;
    foreach (Vector3Int pos in bounds.allPositionsWithin)
    {
        TileBase tile = tilemap.GetTile(pos);
        if (tile == null) continue;
        string tileId = GetTileId(tile);
        if (!string.IsNullOrEmpty(tileId))
            mapData.tiles.Add(new TileData(pos.x, pos.y, tileId));
    }

    // Traps
    foreach (Transform child in trapParent)
    {
        TrapEditorObject trapEditor = child.GetComponent<TrapEditorObject>();
        if (trapEditor == null) continue;

        ITrapConfig config = child.GetComponent<ITrapConfig>();
        string configJson = config != null ? config.ToJson() : "";

        Vector3 pos = child.position;
        Vector3 scale = child.localScale;
        float angle = child.eulerAngles.z;

        mapData.traps.Add(new TrapData(
            trapEditor.trapId, pos.x, pos.y, angle, scale.x, scale.y, configJson
        ));
    }

    // Spawn points
    if (spawnPointEditor != null)
    {
        mapData.knightSpawn = spawnPointEditor.GetKnightSpawn();
        mapData.demonSpawn = spawnPointEditor.GetDemonSpawn();
        mapData.princessSpawn = spawnPointEditor.GetPrincessSpawn();
    }

    return mapData;
}

// ─────────────────────────────────────────────────────────
// SaveMap: tạo map MÓI (push key mới trên Firebase)
// ─────────────────────────────────────────────────────────
public async void SaveMap(string mapName)
{
    MapData mapData = BuildMapData(mapName);
    string json = JsonUtility.ToJson(mapData);

#if !UNITY_WEBGL || UNITY_EDITOR
    if (!firebaseReady)
    {
        Debug.LogError("Firebase chưa khởi tạo xong");
        return;
    }

    string userId = "guest_maps";
    if (auth != null && auth.CurrentUser != null)
        userId = auth.CurrentUser.UserId;

    string mapId = dbRef.Child("maps").Child(userId).Push().Key;

    try
    {
        await dbRef.Child("maps").Child(userId).Child(mapId).SetRawJsonValueAsync(json);
        Debug.Log("Lưu map MỚI THÀNH CÔNG: " + mapId);
    }
    catch (System.Exception e)
    {
        Debug.LogError("Lưu map THẤT BẠI: " + e);
    }
#else
    if (FirebaseJSBridge.instance == null)
    {
        Debug.LogError("Firebase chưa khởi tạo xong");
        return;
    }

    string userId = FirebaseJSBridge.instance.GetCurrentUserId();
    if (string.IsNullOrEmpty(userId)) userId = "guest_maps";

    string mapId = await FirebaseJSBridge.instance.PushKeyAsync($"maps/{userId}");

    try
    {
        await FirebaseJSBridge.instance.WriteDatabaseAsync($"maps/{userId}/{mapId}", json);
        Debug.Log("Lưu map MỚI THÀNH CÔNG: " + mapId);
    }
    catch (System.Exception e)
    {
        Debug.LogError("Lưu map THẤT BẠI: " + e);
    }
#endif
}

// ─────────────────────────────────────────────────────────
// SaveMapToId: GHI ĐÈ vào map đã tồn tại (dùng khi chỉnh sửa map cũ)
// ─────────────────────────────────────────────────────────

/// <summary>
/// Lưu map hiện tại vào đúng mapId trên Firebase (overwrite),
/// dùng khi người chơi chỉnh sửa và save lại map cũ.
/// Nếu mapId rỗng sẽ cố tự lấy từ mapLoader.currentMapId.
/// </summary>
public async void SaveMapToId(string mapId, string mapName)
{
    if (string.IsNullOrEmpty(mapId))
    {
        // Thử lấy từ mapLoader nếu có
        if (mapLoader != null && !string.IsNullOrEmpty(mapLoader.currentMapId))
            mapId = mapLoader.currentMapId;
        else
        {
            Debug.LogError("[MapSaver] Không có mapId để overwrite. Hãy truyền mapId hoặc gán mapLoader.");
            return;
        }
    }

    MapData mapData = BuildMapData(mapName);
    string json = JsonUtility.ToJson(mapData);

#if !UNITY_WEBGL || UNITY_EDITOR
    if (!firebaseReady)
    {
        Debug.LogError("[MapSaver] Firebase chưa khởi tạo xong");
        return;
    }

    string userId = "guest_maps";
    if (auth != null && auth.CurrentUser != null)
        userId = auth.CurrentUser.UserId;

    try
    {
        await dbRef.Child("maps").Child(userId).Child(mapId).SetRawJsonValueAsync(json);
        Debug.Log($"[MapSaver] Lưu đè map THÀNH CÔNG: mapId={mapId}, tên={mapName}");
    }
    catch (System.Exception e)
    {
        Debug.LogError("[MapSaver] Lưu đè map THẤT BẠI: " + e);
    }
#else
    if (FirebaseJSBridge.instance == null)
    {
        Debug.LogError("[MapSaver] Firebase chưa khởi tạo xong");
        return;
    }

    string userId = FirebaseJSBridge.instance.GetCurrentUserId();
    if (string.IsNullOrEmpty(userId)) userId = "guest_maps";

    try
    {
        await FirebaseJSBridge.instance.WriteDatabaseAsync($"maps/{userId}/{mapId}", json);
        Debug.Log($"[MapSaver] Lưu đè map THÀNH CÔNG: mapId={mapId}, tên={mapName}");
    }
    catch (System.Exception e)
    {
        Debug.LogError("[MapSaver] Lưu đè map THẤT BẠI: " + e);
    }
#endif
}

// ─────────────────────────────────────────────────────────
// BUTTON WRAPPERS — gán trực tiếp vào Button.OnClick trong Inspector
// ─────────────────────────────────────────────────────────

/// <summary>
/// Nút "Lưu" trong Map Editor khi đang chỉnh sửa map đã có sẵn.
/// Tự lấy tên từ mapNameInput và mapId từ mapLoader.currentMapId.
/// </summary>
public void OnClickSaveCurrentMap()
{
    string mapName = mapNameInput != null ? mapNameInput.text.Trim() : "";
    if (string.IsNullOrWhiteSpace(mapName))
    {
        ShowNotification("Vui lòng nhập tên map!", false);
        return;
    }

    string mapId = mapLoader != null ? mapLoader.currentMapId : "";
    if (string.IsNullOrEmpty(mapId))
    {
        ShowNotification("Không có mapId — hãy gán FirebaseMapLoader!", false);
        return;
    }

    SaveMapToId(mapId, mapName);
    ShowNotification($"Đã lưu map \"{mapName}\"!", true);
}

    /// <summary>
    /// Nút "Lưu Mới" — push map mới lên Firebase với key được tạo tự động.
    /// </summary>
    public void OnClickSaveNewMap()
    {
        string mapName = mapNameInput != null ? mapNameInput.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(mapName))
        {
            ShowNotification("Vui lòng nhập tên map!", false);
            return;
        }

        SaveMap(mapName);
        ShowNotification($"Đã lưu map mới \"{mapName}\"!", true);
    }

    /// <summary>
    /// Nút "Test Map" — Lưu lại map hiện tại, gán id vào DataGame và chuyển sang scene LVCustom.
    /// </summary>
    public void OnClickTestCurrentMap()
    {
        if (spawnPointEditor == null || 
            spawnPointEditor.GetKnightInstance() == null || !spawnPointEditor.GetKnightInstance().activeSelf ||
            spawnPointEditor.GetDemonInstance() == null || !spawnPointEditor.GetDemonInstance().activeSelf ||
            spawnPointEditor.GetPrincessInstance() == null || !spawnPointEditor.GetPrincessInstance().activeSelf)
        {
            ShowNotification("Lỗi: Bạn chưa đặt đủ 3 vị trí Player, Demon và Princess!", false);
            return;
        }

        string mapName = mapNameInput != null ? mapNameInput.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(mapName))
        {
            ShowNotification("Vui lòng nhập tên map trước khi test!", false);
            return;
        }

        string mapId = mapLoader != null ? mapLoader.currentMapId : "";
        if (string.IsNullOrEmpty(mapId))
        {
            ShowNotification("Lỗi: Không tìm thấy Map ID. Hãy bấm 'Lưu Mới' map này trước!", false);
            return;
        }

        // Lưu đè những thay đổi mới nhất lên Firebase
        SaveMapToId(mapId, mapName);

        // Đẩy id sang cho DataGame để LVCustom biết map nào cần load
        if (DataGame.instance != null)
        {
            DataGame.instance.currentTestMapId = mapId;
        }
        else
        {
            // Dự phòng nếu không có DataGame (ví dụ chạy rời rạc)
            PlayerPrefs.SetString("TestMapId", mapId);
        }

        // Chuyển scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("LVCustom");
    }

void ShowNotification(string message, bool success)
{
    if (BangThongBao == null) return;
    BangThongBao.SetActive(true);
    TextThongBao.text = message;
    TextThongBao.color = success ? UnityEngine.Color.green : UnityEngine.Color.red;
}

    string GetTileId(TileBase tile)
    {
        foreach (TileOption option in tileDatabase.tileOptions)
        {
            if (option.tile == tile)
                return option.id;
        }

        Debug.LogWarning("Không tìm thấy tileId cho tile: " + tile.name);
        return "";
    }
    public void OpenorHideToolBar()
    {
        if (Toolbar.activeSelf)
        {
            Toolbar.SetActive(false);
        }
        else
        {
            Toolbar.SetActive(true);
        }
    }
    public void OpenorHideOptions()
    {
        if (Optionsbar.activeSelf)
        {
            Optionsbar.SetActive(false);
        }
        else
        {
            Optionsbar.SetActive(true);
        }        
    }
    public void CloseNotification()
    {
        BangThongBao.SetActive(false);
    }
    public void ExitEditMap()
    {
        SceneManager.LoadScene("SampleScene");
    }

    // ─────────────────────────────────────────────────────────
    // Quản lý trạng thái map: private | publish
    // ─────────────────────────────────────────────────────────

    /// <summary>Đổi trạng thái map lên Firebase theo mapId đang edit.</summary>
    public async void SetMapStatus(string newStatus)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (!firebaseReady)
        {
            ShowNotification("Firebase chưa sẵn sàng!", false);
            return;
        }

        string mapId = mapLoader != null ? mapLoader.currentMapId : "";
        if (string.IsNullOrEmpty(mapId) && DataGame.instance != null)
            mapId = DataGame.instance.currentEditMapId;

        if (string.IsNullOrEmpty(mapId))
        {
            ShowNotification("Không tìm thấy Map ID!", false);
            return;
        }

        string userId = "guest_maps";
        if (auth != null && auth.CurrentUser != null)
            userId = auth.CurrentUser.UserId;

        try
        {
            await dbRef.Child("maps").Child(userId).Child(mapId).Child("status").SetValueAsync(newStatus);
            string displayName = newStatus == "publish" ? "Công khai" : "Riêng tư";
            ShowNotification($"Trạng thái map: {displayName}", true);
            Debug.Log($"[MapSaver] Đổi status map {mapId} thành: {newStatus}");
        }
        catch (System.Exception e)
        {
            ShowNotification("Đổi trạng thái thất bại!", false);
            Debug.LogError("[MapSaver] SetMapStatus lỗi: " + e);
        }
#else
        if (FirebaseJSBridge.instance == null)
        {
            ShowNotification("Firebase chưa sẵn sàng!", false);
            return;
        }

        string mapId = mapLoader != null ? mapLoader.currentMapId : "";
        if (string.IsNullOrEmpty(mapId) && DataGame.instance != null)
            mapId = DataGame.instance.currentEditMapId;

        if (string.IsNullOrEmpty(mapId))
        {
            ShowNotification("Không tìm thấy Map ID!", false);
            return;
        }

        string userId = FirebaseJSBridge.instance.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) userId = "guest_maps";

        try
        {
            await FirebaseJSBridge.instance.WriteDatabaseAsync($"maps/{userId}/{mapId}/status", $"\"{newStatus}\"");
            string displayName = newStatus == "publish" ? "Công khai" : "Riêng tư";
            ShowNotification($"Trạng thái map: {displayName}", true);
            Debug.Log($"[MapSaver] Đổi status map {mapId} thành: {newStatus}");
        }
        catch (System.Exception e)
        {
            ShowNotification("Đổi trạng thái thất bại!", false);
            Debug.LogError("[MapSaver] SetMapStatus lỗi: " + e);
        }
#endif
    }

    /// <summary>Nút "Publish" — công khai map.</summary>
    public void OnClickPublishMap() => SetMapStatus("publish");
    /// <summary>Nút "Private" — đặt map về riêng tư.</summary>
    public void OnClickSetPrivate() => SetMapStatus("private");
}