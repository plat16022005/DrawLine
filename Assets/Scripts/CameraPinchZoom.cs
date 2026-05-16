using UnityEngine;

public class CameraPinchZoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 0.01f;

    [Header("Limit Zoom")]
    public float minZoom = 3f;
    public float maxZoom = 10f;

    private Camera cam;

    public static CameraPinchZoom instance;

    private void Awake()
    {
        instance = this;
    }

    public void ZoomFromJS(float difference)
    {
        Zoom(difference * zoomSpeed);
    }

    private void Start()
    {
        // Khởi tạo Bridge trực tiếp để tránh bị IL2CPP strip code
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLZoomBridge.Init();
#endif

        // Tự động lấy Main Camera
        cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("Không tìm thấy Main Camera!");
        }
    }

    private void Update()
    {
        if (cam == null) return;

        // Xử lý zoom bằng cuộn chuột (PC) hoặc khi trình duyệt giả lập pinch thành scroll
        if (Input.mouseScrollDelta.y != 0)
        {
            Zoom(Input.mouseScrollDelta.y * zoomSpeed * 50f);
        }

        // Chỉ xử lý khi có đúng 2 ngón chạm (Mobile)
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // Vị trí frame trước
            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            // Khoảng cách cũ và mới
            float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
            float currentMagnitude = (touch0.position - touch1.position).magnitude;

            // Chênh lệch pinch
            float difference = currentMagnitude - prevMagnitude;

            Zoom(difference * zoomSpeed);
        }
    }

    private void Zoom(float increment)
    {
        if (cam.orthographic)
        {
            // Camera 2D
            cam.orthographicSize -= increment;

            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize,
                minZoom,
                maxZoom
            );
        }
        else
        {
            // Camera 3D
            cam.fieldOfView -= increment;

            cam.fieldOfView = Mathf.Clamp(
                cam.fieldOfView,
                minZoom,
                maxZoom
            );
        }
    }
}