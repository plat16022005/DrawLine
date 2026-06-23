using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FirebaseMapLoader : MonoBehaviour
{
    public Tilemap tilemap;
    public TileDatabase tileDatabase;

    private DatabaseReference dbRef;
    private bool firebaseReady = false;

    async void Start()
    {
        var result = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (result == DependencyStatus.Available)
        {
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            firebaseReady = true;
            Debug.Log("Firebase Ready");
            LoadMap("-Ovp5AsV8-f-1C7VUy7Z");
        }
        else
        {
            Debug.LogError("Firebase lỗi dependency: " + result);
        }
    }

    public async void LoadMap(string mapId)
    {
        if (!firebaseReady)
        {
            Debug.LogError("Firebase chưa khởi tạo xong");
            return;
        }

        try
        {
            DataSnapshot snapshot = await dbRef.Child("maps").Child(mapId).GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError("Không tìm thấy map: " + mapId);
                return;
            }

            string json = snapshot.GetRawJsonValue();
            Debug.Log("Map JSON: " + json);

            MapData mapData = JsonUtility.FromJson<MapData>(json);

            tilemap.ClearAllTiles();

            foreach (TileData tileData in mapData.tiles)
            {
                TileBase tile = GetTileById(tileData.type);

                if (tile != null)
                {
                    Vector3Int pos = new Vector3Int(tileData.x, tileData.y, 0);
                    tilemap.SetTile(pos, tile);
                }
            }

            Debug.Log("Load map thành công: " + mapData.mapName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Load map thất bại: " + e);
        }
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