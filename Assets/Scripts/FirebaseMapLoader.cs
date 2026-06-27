using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FirebaseMapLoader : MonoBehaviour
{
    public Tilemap tilemap;
    public TileDatabase tileDatabase;

    public TrapDatabase trapDatabase;
    public Transform trapParent;

    [Header("Nhân vật trong gameplay (tìm kiếm theo tên hoặc assign)")]
    public Transform knightTransform;
    public Transform demonTransform;
    public Transform princessTransform;
    public GameObject princessCagePrefab; // Prefab lồng cho công chúa

    [Header("Editor spawn marker (chỉ dùng khi load vào map editor)")]
    public SpawnPointEditor spawnPointEditor;

    private DatabaseReference dbRef;
    private bool firebaseReady = false;
    private GameObject currentCage;

    async void Start()
    {
        var result = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (result == DependencyStatus.Available)
        {
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            firebaseReady = true;
            Debug.Log("Firebase Ready");

            LoadMap("-Ow99XnVeAddnhlg7--g");
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
                Debug.Log(trapData.configJson);
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
        }
        catch (System.Exception e)
        {
            Debug.LogError("Load map thất bại: " + e);
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