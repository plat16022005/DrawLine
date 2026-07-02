using System.Collections.Generic;
#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase;
using Firebase.Auth;
using Firebase.Database;
#endif
using UnityEngine;
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

    async void Start()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        var result = await FirebaseApp.CheckAndFixDependenciesAsync();

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

public async void SaveMap()
{
    MapData mapData = new MapData();
    mapData.mapName = "Map 1";
    mapData.width = width;
    mapData.height = height;

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
        // Fallback
        if (Camera.main != null) mapData.cameraLens = Camera.main.orthographicSize;
        if (InkManager.Instance != null) mapData.inkCostPerUnit = InkManager.Instance.inkCostPerUnit;
        if (WeatherManager.Instance != null) mapData.weatherType = (int)WeatherManager.CurrentWeather;
        GlobalWind wind = FindObjectOfType<GlobalWind>(true);
        if (wind != null) {
            mapData.enableWind = wind.gameObject.activeSelf;
            mapData.windForce = wind.windForce;
            mapData.windAngle = wind.windAngle;
        }
    }

    BoundsInt bounds = tilemap.cellBounds;

    foreach (Vector3Int pos in bounds.allPositionsWithin)
    {
        TileBase tile = tilemap.GetTile(pos);

        if (tile == null) continue;

        string tileId = GetTileId(tile);

        if (!string.IsNullOrEmpty(tileId))
        {
            mapData.tiles.Add(new TileData(pos.x, pos.y, tileId));
        }
    }

    foreach (Transform child in trapParent)
    {
        TrapEditorObject trapEditor = child.GetComponent<TrapEditorObject>();

        if (trapEditor == null)
            continue;

        ITrapConfig config = child.GetComponent<ITrapConfig>();

        string configJson = config != null ? config.ToJson() : "";

        Vector3 pos = child.position;
        Vector3 scale = child.localScale;
        float angle = child.eulerAngles.z;

        mapData.traps.Add(new TrapData(
            trapEditor.trapId,
            pos.x,
            pos.y,
            angle,
            scale.x,
            scale.y,
            configJson
        ));
    }

    // Ghi vị trí spawn của các nhân vật
    if (spawnPointEditor != null)
    {
        mapData.knightSpawn = spawnPointEditor.GetKnightSpawn();
        mapData.demonSpawn = spawnPointEditor.GetDemonSpawn();
        mapData.princessSpawn = spawnPointEditor.GetPrincessSpawn();
    }

    string json = JsonUtility.ToJson(mapData);

#if !UNITY_WEBGL || UNITY_EDITOR
    if (!firebaseReady)
    {
        Debug.LogError("Firebase chưa khởi tạo xong");
        return;
    }

    string mapId = dbRef.Child("maps").Push().Key;

    try
    {
        await dbRef.Child("maps").Child(mapId).SetRawJsonValueAsync(json);

        Debug.Log("Lưu map THÀNH CÔNG: " + mapId);
        Debug.Log(json);
    }
    catch (System.Exception e)
    {
        Debug.LogError("Lưu map THẤT BẠI: " + e);
    }
#else
    Debug.Log("Map JSON (Firebase Native Upload Disabled on WebGL): \n" + json);
#endif
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
}