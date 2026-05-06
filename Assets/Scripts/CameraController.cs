using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Kéo thả Player/Ball vào đây. Nếu để trống, script sẽ tự tìm object có tag 'Player' hoặc tên 'Ball'.")]
    public Transform target;
    
    [Header("Camera Settings")]
    [Tooltip("Tốc độ camera di chuyển theo mục tiêu")]
    public float followSpeed = 5f;
    [Tooltip("Mức độ zoom của camera khi focus vào player (dành cho Orthographic Camera)")]
    public float followZoom = 2.5f;
    [Tooltip("Tốc độ zoom của camera")]
    public float zoomSpeed = 5f;
    
    private bool isFollowing = false;
    private bool isReturning = false;
    [Tooltip("Kéo thả Camera vào đây. Nếu để trống, script sẽ tự tìm Main Camera.")]
    public Camera targetCamera;
    
    // Original states
    private Vector3 originalPosition;
    private float originalZoom;

    void Start()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (targetCamera == null) targetCamera = Camera.main;
        
        if (targetCamera != null)
        {
            originalPosition = targetCamera.transform.position;
            originalZoom = targetCamera.orthographicSize;
        }

        FindTarget();
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        if (isFollowing)
        {
            if (target != null)
            {
                // Di chuyển mượt mà tới vị trí của player, giữ nguyên trục Z của camera
                Vector3 targetPosition = new Vector3(target.position.x, target.position.y, originalPosition.z);
                targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, targetPosition, followSpeed * Time.unscaledDeltaTime);
            }
            else
            {
                // Thử tìm lại target nếu bị mất (ví dụ bị destroy)
                FindTarget();
            }
            
            // Zoom mượt mà vào player
            if (targetCamera.orthographic)
            {
                targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, followZoom, zoomSpeed * Time.unscaledDeltaTime);
            }
        }
        else if (isReturning)
        {
            // Trở về vị trí và kích thước tổng thể map ban đầu
            targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, originalPosition, followSpeed * Time.unscaledDeltaTime);
            
            if (targetCamera.orthographic)
            {
                targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, originalZoom, zoomSpeed * Time.unscaledDeltaTime);
            }

            // Dừng lerp khi đã về gần sát vị trí/zoom ban đầu
            if (Vector3.Distance(targetCamera.transform.position, originalPosition) < 0.01f && 
                Mathf.Abs(targetCamera.orthographicSize - originalZoom) < 0.01f)
            {
                isReturning = false;
            }
        }
    }

    public void CancelReturn()
    {
        isReturning = false;
    }

    // Hàm này được gọi từ UI Button OnClick
    public void ToggleCameraFollow()
    {
        isFollowing = !isFollowing;
        
        if (isFollowing)
        {
            // Nếu chưa có target, thử tìm lại trước khi follow
            if (target == null) FindTarget();
            isReturning = false;
            Debug.Log("Camera: Đang focus và follow Player.");
        }
        else
        {
            isReturning = true;
            Debug.Log("Camera: Trở về chế độ tổng thể map.");
        }
    }

    private void FindTarget()
    {
        if (target != null) return;

        // Ưu tiên tìm theo tag Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        // Nếu không có, thử tìm object tên Ball
        if (playerObj == null) playerObj = GameObject.Find("Ball");
        
        if (playerObj != null)
        {
            target = playerObj.transform;
        }
    }
}
