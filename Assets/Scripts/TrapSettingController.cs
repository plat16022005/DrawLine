using UnityEngine;
using UnityEngine.EventSystems;

public class TrapSettingController : MonoBehaviour
{
    public Camera mainCamera;
    public LayerMask trapLayer;
    public TrapSettingPanelUI settingPanelUI;

    private Transform selectedTrap;
    private PlacementMode lastMode = PlacementMode.None;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        PlacementMode currentMode = PlacementModeManager.CurrentMode;

        // Phát hiện khi vừa thoát khỏi TrapSetting mode → tự đóng panel
        if (lastMode == PlacementMode.TrapSetting && currentMode != PlacementMode.TrapSetting)
        {
            settingPanelUI.Close();
        }

        lastMode = currentMode;

        if (currentMode != PlacementMode.TrapSetting)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            SelectTrap();
        }
    }

    void SelectTrap()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Collider2D hit = Physics2D.OverlapPoint(mousePos, trapLayer);

        if (hit == null)
            return;

        TrapEditorObject editor = hit.GetComponentInParent<TrapEditorObject>();

        if (editor == null)
            return;

        selectedTrap = editor.transform;

        settingPanelUI.Open(selectedTrap);
    }
}