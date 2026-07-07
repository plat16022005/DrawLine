using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
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
    private Vector3 mapCenter; // Tâm gốc của map để giới hạn pan

    // Pan state
    private Vector3 dragOrigin;

    void Start()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (targetCamera == null) targetCamera = Camera.main;
        
        if (targetCamera != null)
        {
            originalPosition = targetCamera.transform.position;
            originalZoom = targetCamera.orthographicSize;
            mapCenter = targetCamera.transform.position;
        }

        FindTarget();
    }
    public void LoadCamera(){
        targetCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        originalPosition = targetCamera.transform.position;
        originalZoom = targetCamera.orthographicSize;
        mapCenter = targetCamera.transform.position;
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
        else
        {
            // Chỉ cho phép pan camera trong scene MakeMap (Edit Map)
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MakeMap")
            {
                // Cho phép pan camera bằng chuột giữa (chỉ khi không follow/return)
                if (Input.GetMouseButtonDown(2))
            {
                dragOrigin = targetCamera.ScreenToWorldPoint(Input.mousePosition);
            }
            else if (Input.GetMouseButton(2))
            {
                Vector3 difference = dragOrigin - targetCamera.ScreenToWorldPoint(Input.mousePosition);
                // Giữ nguyên trục Z của camera
                difference.z = 0; 
                targetCamera.transform.position += difference;
                
                ClampCameraPosition();
            }

            // Hỗ trợ cảm ứng cho Mobile (PE) - Dùng 2 ngón tay để kéo thả, tránh trùng lấp với việc đặt bẫy/vẽ bằng 1 ngón
            if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                // Lấy điểm chính giữa 2 ngón tay
                Vector2 midPoint = (t0.position + t1.position) / 2f;

                if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
                {
                    dragOrigin = targetCamera.ScreenToWorldPoint(midPoint);
                }
                else if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
                {
                    Vector3 difference = dragOrigin - targetCamera.ScreenToWorldPoint(midPoint);
                    difference.z = 0;
                    targetCamera.transform.position += difference;
                    
                    ClampCameraPosition();
                }
            }
        }
    }
}

    private void ClampCameraPosition()
    {
        if (targetCamera == null || !targetCamera.orthographic) return;

        float maxLens = 25f;
        float currentLens = targetCamera.orthographicSize;
        float aspect = targetCamera.aspect;
        
        // Tính khoảng cách tối đa camera được phép lệch khỏi mapCenter
        float maxY = Mathf.Max(0, maxLens - currentLens);
        float maxX = Mathf.Max(0, (maxLens - currentLens) * aspect);
        
        Vector3 pos = targetCamera.transform.position;
        pos.x = Mathf.Clamp(pos.x, mapCenter.x - maxX, mapCenter.x + maxX);
        pos.y = Mathf.Clamp(pos.y, mapCenter.y - maxY, mapCenter.y + maxY);
        
        targetCamera.transform.position = pos;
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

    public void UpdateOriginalZoom(float newZoom)
    {
        originalZoom = newZoom;
        if (targetCamera != null && !isFollowing)
        {
            targetCamera.orthographicSize = newZoom;
        }
    }
}
