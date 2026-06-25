using UnityEngine;
using UnityEngine.EventSystems;

public class TrapMoveController : MonoBehaviour
{ 
    public Camera mainCamera;
    public LayerMask trapLayer;

    private Transform selectedTrap;
    private Vector3 offset;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (PlacementModeManager.CurrentMode != PlacementMode.MoveTrap)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            SelectTrap();
        }

        if (Input.GetMouseButton(0) && selectedTrap != null)
        {
            DragTrap();
        }

        if (Input.GetMouseButtonUp(0))
        {
            selectedTrap = null;
        }
    }

    void SelectTrap()
    {
        Vector3 mousePos = GetMouseWorldPos();

        Collider2D hit = Physics2D.OverlapPoint(mousePos, trapLayer);

        if (hit != null)
        {
            selectedTrap = hit.transform;
            offset = selectedTrap.position - mousePos;
        }
    }

    void DragTrap()
    {
        Vector3 mousePos = GetMouseWorldPos();
        selectedTrap.position = mousePos + offset;
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        return mousePos;
    }
}