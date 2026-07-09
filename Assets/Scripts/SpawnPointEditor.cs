using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum SpawnCharacterType
{
    Knight,
    Demon,
    Princess
}

/// <summary>
/// Quản lý đặt/di chuyển spawn point marker cho các nhân vật trong map editor.
/// Gắn vào một Manager GameObject trong scene.
/// </summary>
public class SpawnPointEditor : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera;

    [Header("Marker Prefabs (gán prefab từ cửa sổ Project)")]
    public GameObject knightPrefab;
    public GameObject demonPrefab;
    public GameObject princessPrefab;

    // Các instance đã được tạo ra
    private GameObject knightInstance;
    private GameObject demonInstance;
    private GameObject princessInstance;

    // Preview
    private GameObject previewObject;
    private SpawnCharacterType lastPreviewType;

    [Header("Nhân vật đang được chọn để đặt spawn")]
    public SpawnCharacterType selectedCharacter = SpawnCharacterType.Knight;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (PlacementModeManager.CurrentMode != PlacementMode.SpawnPoint)
        {
            if (previewObject != null) previewObject.SetActive(false);
            return;
        }

        if (IsPointerOverUI())
        {
            if (previewObject != null) previewObject.SetActive(false);
            return;
        }

        UpdatePreview();

        if (WasPointerPressed())
        {
            PlaceSpawnPoint();
        }
    }

    void UpdatePreview()
    {
        GameObject prefabToPreview = null;
        switch (selectedCharacter)
        {
            case SpawnCharacterType.Knight: prefabToPreview = knightPrefab; break;
            case SpawnCharacterType.Demon: prefabToPreview = demonPrefab; break;
            case SpawnCharacterType.Princess: prefabToPreview = princessPrefab; break;
        }

        if (prefabToPreview == null)
        {
            if (previewObject != null) previewObject.SetActive(false);
            return;
        }

        // Tạo lại preview nếu đổi nhân vật
        if (previewObject == null || lastPreviewType != selectedCharacter)
        {
            if (previewObject != null) Destroy(previewObject);
            
            previewObject = Instantiate(prefabToPreview);
            lastPreviewType = selectedCharacter;

            // Làm mờ bóng preview
            SpriteRenderer[] renderers = previewObject.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in renderers)
            {
                Color c = sr.color;
                c.a = 0.5f;
                sr.color = c;
            }
            
            // Tắt script/collider của preview
            MonoBehaviour[] scripts = previewObject.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts) script.enabled = false;
        }

        previewObject.SetActive(true);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(GetPointerPosition());
        worldPos.z = 0;
        previewObject.transform.position = worldPos;
    }

    void PlaceSpawnPoint()
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(GetPointerPosition());
        worldPos.z = 0;

        switch (selectedCharacter)
        {
            case SpawnCharacterType.Knight:
                if (knightInstance == null)
                {
                    if (knightPrefab != null) knightInstance = Instantiate(knightPrefab, worldPos, Quaternion.identity);
                    else Debug.LogWarning("[SpawnPointEditor] knightPrefab chưa được assign!");
                }
                else
                {
                    knightInstance.SetActive(true);
                    knightInstance.transform.position = worldPos;
                }
                break;

            case SpawnCharacterType.Demon:
                if (demonInstance == null)
                {
                    if (demonPrefab != null) demonInstance = Instantiate(demonPrefab, worldPos, Quaternion.identity);
                    else Debug.LogWarning("[SpawnPointEditor] demonPrefab chưa được assign!");
                }
                else
                {
                    demonInstance.SetActive(true);
                    demonInstance.transform.position = worldPos;
                }
                break;

            case SpawnCharacterType.Princess:
                if (princessInstance == null)
                {
                    if (princessPrefab != null) princessInstance = Instantiate(princessPrefab, worldPos, Quaternion.identity);
                    else Debug.LogWarning("[SpawnPointEditor] princessPrefab chưa được assign!");
                }
                else
                {
                    princessInstance.SetActive(true);
                    princessInstance.transform.position = worldPos;
                }
                break;
        }
    }

    // -------------------------------------------------------
    // Chọn nhân vật từ UI Button
    // -------------------------------------------------------

    public void SelectKnight()
    {
        selectedCharacter = SpawnCharacterType.Knight;
        PlacementModeManager.CurrentMode = PlacementMode.SpawnPoint;
    }

    public void SelectDemon()
    {
        selectedCharacter = SpawnCharacterType.Demon;
        PlacementModeManager.CurrentMode = PlacementMode.SpawnPoint;
    }

    public void SelectPrincess()
    {
        selectedCharacter = SpawnCharacterType.Princess;
        PlacementModeManager.CurrentMode = PlacementMode.SpawnPoint;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = GetPointerPosition();
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    private bool WasPointerPressed()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            return touch.phase == TouchPhase.Began;
        }

        return Input.GetMouseButtonDown(0);
    }

    private Vector2 GetPointerPosition()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Canceled)
                return touch.position;
        }

        return Input.mousePosition;
    }

    // -------------------------------------------------------
    // Expose Instances cho TrapMoveController di chuyển
    // -------------------------------------------------------
    public GameObject GetKnightInstance() => knightInstance;
    public GameObject GetDemonInstance() => demonInstance;
    public GameObject GetPrincessInstance() => princessInstance;

    // -------------------------------------------------------
    // Lấy vị trí để Saver ghi vào MapData
    // -------------------------------------------------------

    public Vector2 GetKnightSpawn()
        => knightInstance != null && knightInstance.activeSelf
            ? (Vector2)knightInstance.transform.position
            : Vector2.zero;

    public Vector2 GetDemonSpawn()
        => demonInstance != null && demonInstance.activeSelf
            ? (Vector2)demonInstance.transform.position
            : Vector2.zero;

    public Vector2 GetPrincessSpawn()
        => princessInstance != null && princessInstance.activeSelf
            ? (Vector2)princessInstance.transform.position
            : Vector2.zero;

    // -------------------------------------------------------
    // Load — set marker từ MapData (khi load map vào editor)
    // -------------------------------------------------------

    public void LoadSpawnPoints(Vector2 knight, Vector2 demon, Vector2 princess)
    {
        if (knight != Vector2.zero && knightPrefab != null)
        {
            if (knightInstance == null) knightInstance = Instantiate(knightPrefab);
            knightInstance.transform.position = new Vector3(knight.x, knight.y, 0);
            knightInstance.SetActive(true);
        }
        else if (knightInstance != null) knightInstance.SetActive(false);

        if (demon != Vector2.zero && demonPrefab != null)
        {
            if (demonInstance == null) demonInstance = Instantiate(demonPrefab);
            demonInstance.transform.position = new Vector3(demon.x, demon.y, 0);
            demonInstance.SetActive(true);
        }
        else if (demonInstance != null) demonInstance.SetActive(false);

        if (princess != Vector2.zero && princessPrefab != null)
        {
            if (princessInstance == null) princessInstance = Instantiate(princessPrefab);
            princessInstance.transform.position = new Vector3(princess.x, princess.y, 0);
            princessInstance.SetActive(true);
        }
        else if (princessInstance != null) princessInstance.SetActive(false);
    }
}
