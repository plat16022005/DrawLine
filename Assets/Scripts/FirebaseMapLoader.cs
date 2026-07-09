#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase;
using Firebase.Auth;
using Firebase.Database;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class FirebaseMapLoader : MonoBehaviour
{
    public Tilemap tilemap;
    public TileDatabase tileDatabase;

    public TrapDatabase trapDatabase;
    public Transform trapParent;
    public GlobalWind globalWind; // Assign in Inspector

    [Header("Nhân vật trong gameplay (tìm kiếm theo tên hoặc assign)")]
    public Transform knightTransform;
    public Transform demonTransform;
    public Transform princessTransform;
    public GameObject princessCagePrefab; // Prefab lồng cho công chúa

    [Header("Editor spawn marker (chỉ dùng khi load vào map editor)")]
    public SpawnPointEditor spawnPointEditor;
    public MapSettingsPanelUI mapSettingsUI;
    [Tooltip("InputField để tự động điền tên map khi load (tùy chọn)")]
    public TMP_InputField mapNameInput;

#if !UNITY_WEBGL || UNITY_EDITOR
    private DatabaseReference dbRef;
#endif
    private bool firebaseReady = false;
    private GameObject currentCage;

    // Map đang được load (dùng để FirebaseMapSaver save đúng ID)
    [HideInInspector] public string currentMapId = "";

    async void Start()
    {
        // Tự động tìm mapNameInput từ FirebaseMapSaver nếu quên gán trong Inspector
        if (mapNameInput == null)
        {
            FirebaseMapSaver saver = GetComponent<FirebaseMapSaver>();
            if (saver != null) mapNameInput = saver.mapNameInput;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        var result = await FirebaseInitializer.EnsureInitializedAsync();

        if (result == DependencyStatus.Available)
        {
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            firebaseReady = true;
            Debug.Log("Firebase Ready");

            if (DataGame.instance != null && !string.IsNullOrEmpty(DataGame.instance.currentEditMapId) && SceneManager.GetActiveScene().name == "MakeMap")
            {
                LoadMapForEditor(DataGame.instance.currentEditMapId);
                // DataGame.instance.currentEditMapId = ""; // Clear sau khi load
            }
            else if (DataGame.instance != null && !string.IsNullOrEmpty(DataGame.instance.currentCommunityMapId) && SceneManager.GetActiveScene().name == "LvMap")
            {
                LoadMapFromCommunity(DataGame.instance.currentCommunityMapId);
                // DataGame.instance.currentCommunityMapId = ""; // Clear sau khi load
            }
            else if (DataGame.instance != null && !string.IsNullOrEmpty(DataGame.instance.currentTestMapId) && SceneManager.GetActiveScene().name == "LVCustom")
            {
                LoadMap(DataGame.instance.currentTestMapId);
                // DataGame.instance.currentTestMapId = ""; // Clear sau khi load
            }
            else
            {
                // Hardcode load map tạm thời cho gameplay
                LoadMap("-OwZAwjVfUEh8Yefi_mf");
            }
        }
        else
        {
            Debug.LogError("Firebase lỗi dependency: " + result);
        }
#endif
    }

    // ─────────────────────────────────────────────────────────
    // GAMEPLAY – Load map vào scene gameplay (dùng runtimePrefab)
    // ─────────────────────────────────────────────────────────
    public async void LoadMap(string mapId)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (!firebaseReady)
        {
            Debug.LogError("Firebase chưa khởi tạo xong");
            return;
        }

        try
        {
            string userId = "guest_maps";
            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            if (auth != null && auth.CurrentUser != null)
            {
                userId = auth.CurrentUser.UserId;
            }

            DataSnapshot snapshot = await dbRef.Child("maps").Child(userId).Child(mapId).GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError("Không tìm thấy map: " + mapId);
                return;
            }

            string json = snapshot.GetRawJsonValue();
            Debug.Log("Map JSON: " + json);

            MapData mapData = JsonUtility.FromJson<MapData>(json);
            currentMapId = mapId;
            LoadMapFromData(mapData);
            LevelSceneManager.instance.LoadSkin();
            CameraController.Instance.LoadCamera();
            CameraControl.Instance.LoadCamera();
            WindUIDisplay.Instance.RefreshWindUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Load map thất bại: " + e);
        }
#else
        Debug.LogWarning("LoadMap by ID is not supported natively on WebGL. Use FirebaseJSBridge.");
#endif
    }

    // ─────────────────────────────────────────────────────────
    // COMMUNITY – Load map từ mapscommunity (không cần userId)
    // ─────────────────────────────────────────────────────────
    /// <summary>
    /// Load map từ collection mapscommunity/<mapId> để người chơi chơi.
    /// Gọi khi chuyển sang scene LvMap từ màn chọn level community.
    /// </summary>
    public async void LoadMapFromCommunity(string mapId)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (!firebaseReady)
        {
            Debug.LogError("[MapLoader] Firebase chưa khởi tạo xong");
            return;
        }

        try
        {
            DataSnapshot snapshot = await dbRef.Child("mapscommunity").Child(mapId).GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError("[MapLoader] Không tìm thấy map community: " + mapId);
                return;
            }

            string json = snapshot.GetRawJsonValue();
            Debug.Log("[MapLoader] Community map JSON: " + json);

            MapData mapData = JsonUtility.FromJson<MapData>(json);
            currentMapId = mapId;
            LoadMapFromData(mapData);
            LevelSceneManager.instance.LoadSkin();
            CameraController.Instance.LoadCamera();
            CameraControl.Instance.LoadCamera();
            WindUIDisplay.Instance.RefreshWindUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MapLoader] Load community map thất bại: " + e);
        }
#else
        Debug.LogWarning("[MapLoader] LoadMapFromCommunity không hỗ trợ WebGL native.");
#endif
    }

    // ─────────────────────────────────────────────────────────
    // EDITOR – Load map vào Map Editor (dùng editorPrefab)
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Tải map từ Firebase theo mapId và vẽ lại toàn bộ nội dung
    /// (tiles, traps, spawn points, settings) lên scene Map Editor.
    /// Gọi hàm này khi người chơi chọn "Tiếp tục chỉnh sửa" một map.
    /// </summary>
    public async void LoadMapForEditor(string mapId)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (!firebaseReady)
        {
            Debug.LogError("[MapLoader] Firebase chưa khởi tạo xong");
            return;
        }

        try
        {
            string userId = "guest_maps";
            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            if (auth != null && auth.CurrentUser != null)
                userId = auth.CurrentUser.UserId;

            DataSnapshot snapshot = await dbRef.Child("maps").Child(userId).Child(mapId).GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError("[MapLoader] Không tìm thấy map: " + mapId);
                return;
            }

            string json = snapshot.GetRawJsonValue();
            Debug.Log("[MapLoader] Load editor map JSON: " + json);

            MapData mapData = JsonUtility.FromJson<MapData>(json);
            currentMapId = mapId;
            LoadMapFromDataForEditor(mapData);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MapLoader] Load map for editor thất bại: " + e);
        }
#else
        Debug.LogWarning("LoadMapForEditor không hỗ trợ WebGL native.");
#endif
    }

    /// <summary>
    /// Vẽ nội dung MapData lên scene ở chế độ Map Editor:
    /// – Tiles: giống gameplay
    /// – Traps: dùng editorPrefab (có TrapEditorObject) để có thể tiếp tục kéo/xóa
    /// – Spawn points: hiển thị marker qua SpawnPointEditor
    /// – Settings: sync về MapSettingsPanelUI
    /// KHÔNG teleport nhân vật gameplay, KHÔNG tạo lồng công chúa.
    /// </summary>
    public void LoadMapFromDataForEditor(MapData mapData)
    {
        if (mapData == null) return;

        try
        {
            // Điền tên map vào ô input nếu có gán
            if (mapNameInput != null)
                mapNameInput.text = mapData.mapName;

            // --- Tiles ---
            tilemap.ClearAllTiles();
            foreach (TileData tileData in mapData.tiles)
            {
                TileBase tile = GetTileById(tileData.type);
                if (tile != null)
                    tilemap.SetTile(new Vector3Int(tileData.x, tileData.y, 0), tile);
            }

            // --- Traps (dùng editorPrefab) ---
            ClearOldTraps();
            foreach (TrapData trapData in mapData.traps)
            {
                TrapOption option = trapDatabase.GetTrapOptionById(trapData.trapId);

                if (option == null)
                {
                    Debug.LogWarning("[MapLoader] Không tìm thấy TrapOption: " + trapData.trapId);
                    continue;
                }

                // Ưu tiên editorPrefab; fallback runtimePrefab nếu không có
                GameObject prefabToUse = option.editorPrefab != null ? option.editorPrefab : option.runtimePrefab;

                if (prefabToUse == null)
                {
                    Debug.LogWarning("[MapLoader] Trap không có prefab nào để dùng trong editor: " + trapData.trapId);
                    continue;
                }

                GameObject trap = Instantiate(
                    prefabToUse,
                    new Vector3(trapData.x, trapData.y, 0),
                    Quaternion.Euler(0, 0, trapData.angle),
                    trapParent
                );

                trap.transform.localScale = new Vector3(trapData.scaleX, trapData.scaleY, 1);

                // Đảm bảo TrapEditorObject được gán để Saver nhận ra
                TrapEditorObject editorObj = trap.GetComponent<TrapEditorObject>();
                if (editorObj == null)
                    editorObj = trap.AddComponent<TrapEditorObject>();
                editorObj.trapId = trapData.trapId;

                // Restore config
                ITrapConfig config = trap.GetComponent<ITrapConfig>();
                if (config != null && !string.IsNullOrEmpty(trapData.configJson))
                    config.FromJson(trapData.configJson);
            }

            // --- Spawn point markers ---
            if (spawnPointEditor != null)
                spawnPointEditor.LoadSpawnPoints(
                    mapData.knightSpawn,
                    mapData.demonSpawn,
                    mapData.princessSpawn
                );

            // --- Camera / Settings ---
            if (mapData.cameraLens > 0 && Camera.main != null)
            {
                Camera.main.orthographicSize = mapData.cameraLens;
                CameraController camCtrl = Camera.main.GetComponent<CameraController>();
                if (camCtrl != null)
                    camCtrl.UpdateOriginalZoom(mapData.cameraLens);
            }

            if (mapData.inkCostPerUnit > 0 && InkManager.Instance != null)
                InkManager.Instance.inkCostPerUnit = mapData.inkCostPerUnit;

            if (WeatherManager.Instance != null)
                WeatherManager.Instance.SetWeather((WeatherType)mapData.weatherType);

            if (globalWind != null)
            {
                globalWind.gameObject.SetActive(mapData.enableWind);
                if (mapData.enableWind)
                    globalWind.ApplySettings(mapData.windForce, mapData.windAngle);
            }

            // --- Sync UI Settings Panel ---
            if (mapSettingsUI != null)
                mapSettingsUI.LoadFromData(mapData);

            Debug.Log($"[MapLoader] Load map vào editor thành công: {mapData.mapName} (ID: {currentMapId})");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MapLoader] LoadMapFromDataForEditor thất bại: " + e);
        }
    }

    public void LoadMapFromData(MapData mapData)
    {
        if (mapData == null) return;

        try
        {
            tilemap.ClearAllTiles();
            ClearOldTraps();

            foreach (TileData tileData in mapData.tiles)
            {
                TileBase tile = GetTileById(tileData.type);

                if (tile != null)
                {
                    Vector3Int pos = new Vector3Int(tileData.x, tileData.y, 0);
                    tilemap.SetTile(pos, tile);
                }
            }

            foreach (TrapData trapData in mapData.traps)
            {
                TrapOption option = trapDatabase.GetTrapOptionById(trapData.trapId);

                if (option == null || option.runtimePrefab == null)
                {
                    Debug.LogWarning("Không tìm thấy trap runtime prefab: " + trapData.trapId);
                    continue;
                }

                GameObject trap = Instantiate(
                    option.runtimePrefab,
                    new Vector3(trapData.x, trapData.y, 0),
                    Quaternion.Euler(0, 0, trapData.angle),
                    trapParent
                );

                trap.transform.localScale = new Vector3(
                    trapData.scaleX,
                    trapData.scaleY,
                    1
                );

                ITrapConfig config = trap.GetComponent<ITrapConfig>();
                if (config != null && !string.IsNullOrEmpty(trapData.configJson))
                {
                    config.FromJson(trapData.configJson);
                }
                MovingBlock movingBlock = trap.GetComponent<MovingBlock>();
                RotateObject rotateObject = trap.GetComponent<RotateObject>();
                BreakablePlatform breakablePlatform = trap.GetComponent<BreakablePlatform>();

                if(movingBlock != null)
                {
                    movingBlock.Init();
                }
                if (rotateObject != null)
                {
                    rotateObject.Init();
                }
                if (breakablePlatform != null)
                {
                    breakablePlatform.Init();
                }

            }

            Debug.Log("Load map thành công: " + mapData.mapName);

            // Teleport các nhân vật đến spawn position
            if (knightTransform != null && mapData.knightSpawn != Vector2.zero)
                knightTransform.position = new Vector3(mapData.knightSpawn.x, mapData.knightSpawn.y, 0);

            if (demonTransform != null && mapData.demonSpawn != Vector2.zero)
                demonTransform.position = new Vector3(mapData.demonSpawn.x, mapData.demonSpawn.y, 0);

            if (princessTransform != null && mapData.princessSpawn != Vector2.zero)
            {
                princessTransform.position = new Vector3(mapData.princessSpawn.x, mapData.princessSpawn.y, 0);
                
                // Dọn lồng cũ nếu có (vì lồng mới không gán parent, tự quản lý)
                if (currentCage != null) Destroy(currentCage);

                // Tạo lồng bao quanh công chúa (tạo tự do, không gán parent)
                if (princessCagePrefab != null)
                {
                    currentCage = Instantiate(princessCagePrefab, princessTransform.position, Quaternion.identity);
                }
            }

            // Nếu đang trong map editor, load lại marker
            if (spawnPointEditor != null)
                spawnPointEditor.LoadSpawnPoints(
                    mapData.knightSpawn,
                    mapData.demonSpawn,
                    mapData.princessSpawn
                );
                
            // Apply Map Settings
            if (mapData.cameraLens > 0)
            {
                if (Camera.main != null)
                {
                    Camera.main.orthographicSize = mapData.cameraLens;
                    CameraController camCtrl = Camera.main.GetComponent<CameraController>();
                    if (camCtrl != null)
                    {
                        camCtrl.UpdateOriginalZoom(mapData.cameraLens);
                    }
                }
            }

            if (mapData.inkCostPerUnit > 0 && InkManager.Instance != null)
            {
                InkManager.Instance.inkCostPerUnit = mapData.inkCostPerUnit;
            }

            if (WeatherManager.Instance != null)
            {
                WeatherManager.Instance.SetWeather((WeatherType)mapData.weatherType);
            }

            if (globalWind != null)
            {
                globalWind.gameObject.SetActive(mapData.enableWind);
                if (mapData.enableWind)
                {
                    globalWind.ApplySettings(mapData.windForce, mapData.windAngle);
                }
            }

            // Sync settings to UI if in Map Editor
            if (mapSettingsUI != null)
            {
                mapSettingsUI.LoadFromData(mapData);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Quá trình Load Map Data thất bại: " + e);
        }
    }

    void ClearOldTraps()
    {
        if (trapParent == null) return;

        for (int i = trapParent.childCount - 1; i >= 0; i--)
        {
            Destroy(trapParent.GetChild(i).gameObject);
        }
    }

    // ─────────────────────────────────────────────────────────
    // BUTTON WRAPPERS — gán trực tiếp vào Button.OnClick trong Inspector
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Nút "Reload" trong Map Editor — load lại map đang chỉnh sửa
    /// (dùng currentMapId đã được lưu từ lần LoadMapForEditor trước).
    /// </summary>
    public void OnClickReloadCurrentMapForEditor()
    {
        if (string.IsNullOrEmpty(currentMapId))
        {
            Debug.LogError("[MapLoader] Chưa có currentMapId để reload. Hãy load map trước.");
            return;
        }
        LoadMapForEditor(currentMapId);
    }

    TileBase GetTileById(string id)
    {
        foreach (TileOption option in tileDatabase.tileOptions)
        {
            if (option.id == id)
                return option.tile;
        }

        Debug.LogWarning("Không tìm thấy TileBase với id: " + id);
        return null;
    }
}