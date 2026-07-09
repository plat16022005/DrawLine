using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TrapPlacementController : MonoBehaviour
{
    public TrapDropdownUI trapDropdownUI;
    public TrapInfoPanelUI trapInfoPanelUI;

    public Camera mainCamera;
    public Transform trapParent;

    private GameObject previewObject;
    private float currentRotationZ = 0f;
    public RotationCircleUI rotationCircleUI;
    
    private GameObject lastSelectedTrapPrefab;
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (PlacementModeManager.CurrentMode != PlacementMode.Trap)
        {
            HidePreview();
            trapInfoPanelUI.Hide();
            return;
        }

        if (trapDropdownUI.selectedTrapPrefab == null)
        {
            trapInfoPanelUI.Hide();
            return;
        }

        if (trapDropdownUI.selectedTrapPrefab != lastSelectedTrapPrefab)
        {
            lastSelectedTrapPrefab = trapDropdownUI.selectedTrapPrefab;
            trapInfoPanelUI.isHiddenByUser = false;
        }

        UpdatePreviewPosition();
        currentRotationZ = rotationCircleUI.Angle;
        previewObject.transform.rotation = Quaternion.Euler(0, 0, currentRotationZ);
        
        if (!trapInfoPanelUI.isHiddenByUser)
        {
            UpdateInfoPanel();
        }

        if (WasPointerPressed())
        {
            PlaceTrap();
        }
    }


void UpdateInfoPanel()
{
    trapInfoPanelUI.SetTarget(previewObject.transform);

    trapInfoPanelUI.Show(
        previewObject.transform.position,
        currentRotationZ,
        previewObject.transform.localScale
    );
}

    void HidePreview()
    {
        if (previewObject != null)
            previewObject.SetActive(false);
    }

    public void CreatePreview()
    {
        if (previewObject != null)
            Destroy(previewObject);

        previewObject = Instantiate(trapDropdownUI.selectedTrapPrefab);

        MonoBehaviour[] scripts = previewObject.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
            script.enabled = false;

        Collider2D[] colliders = previewObject.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
            col.enabled = false;

        SpriteRenderer[] renderers = previewObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            Color c = sr.color;
            c.a = 0.5f;
            sr.color = c;
        }
    }

    void UpdatePreviewPosition()
    {
        if (previewObject == null)
            CreatePreview();

        previewObject.SetActive(true);

        Vector3 mousePos = mainCamera.ScreenToWorldPoint(GetPointerPosition());
        mousePos.z = 0;

        previewObject.transform.position = mousePos;
    }

    void PlaceTrap()
    {
        if (IsPointerOverUI())
            return;

        // Apply giá trị đã nhập trong config panel vào preview object trước
        if (trapInfoPanelUI != null && trapInfoPanelUI.trapConfigPanel != null)
            trapInfoPanelUI.trapConfigPanel.Apply();

        // Lấy configJson từ preview (nếu có)
        string configJson = null;
        ITrapConfig previewConfig = previewObject.GetComponentInChildren<ITrapConfig>();
        if (previewConfig != null)
            configJson = previewConfig.ToJson();

        Vector3 pos = previewObject.transform.position;

        GameObject trap = Instantiate(
            trapDropdownUI.selectedTrapPrefab,
            pos,
            Quaternion.Euler(0, 0, currentRotationZ),
            trapParent
        );

        trap.transform.localScale = previewObject.transform.localScale;

        // Copy config vào trap vừa tạo
        if (!string.IsNullOrEmpty(configJson))
        {
            ITrapConfig trapConfig = trap.GetComponentInChildren<ITrapConfig>();
            if (trapConfig != null)
                trapConfig.FromJson(configJson);
        }
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
}