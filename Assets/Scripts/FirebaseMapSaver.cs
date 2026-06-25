using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FirebaseMapSaver : MonoBehaviour
{
    public Tilemap tilemap;
    public TileDatabase tileDatabase;

    public int width = 20;
    public int height = 12;

    private DatabaseReference dbRef;
    private FirebaseAuth auth;
    private bool firebaseReady = false;
    public Transform trapParent;

    async void Start()
    {
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
    }

public async void SaveMap()
{
    if (!firebaseReady)
    {
        Debug.LogError("Firebase chưa khởi tạo xong");
        return;
    }

    MapData mapData = new MapData();
    mapData.mapName = "Map 1";
    mapData.width = width;
    mapData.height = height;

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

    string json = JsonUtility.ToJson(mapData);

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