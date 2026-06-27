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

        // 1. Ưu tiên kiểm tra xem có click trúng Trap nào không (dùng Collider & Layer)
        Collider2D hit = Physics2D.OverlapPoint(mousePos, trapLayer);

        if (hit != null)
        {
            selectedTrap = hit.transform;
            offset = selectedTrap.position - mousePos;
            return;
        }

        // 2. Nếu không trúng Trap, kiểm tra xem có click trúng Spawn Point marker không
        // (Tìm marker gần nhất trong bán kính 1 đơn vị)
        SpawnPointEditor spEditor = FindObjectOfType<SpawnPointEditor>();
        if (spEditor != null)
        {
            Transform closest = null;
            float minDist = 1.0f; // Bán kính click (world unit)

            Transform[] markers = new Transform[] {
                spEditor.GetKnightInstance()?.transform,
                spEditor.GetDemonInstance()?.transform,
                spEditor.GetPrincessInstance()?.transform
            };

            foreach (Transform t in markers)
            {
                if (t != null && t.gameObject.activeSelf)
                {
                    float dist = Vector2.Distance(mousePos, t.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = t;
                    }
                }
            }

            if (closest != null)
            {
                selectedTrap = closest;
                offset = selectedTrap.position - mousePos;
            }
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