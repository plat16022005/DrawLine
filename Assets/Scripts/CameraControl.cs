using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Điều khiển camera: zoom (scroll chuột / pinch) + pan (giữ chuột giữa / 1 ngón chạm).
/// Chỉ hoạt động khi người dùng chọn chế độ Camera từ UI.
/// Gắn script này lên Main Camera.
/// </summary>
public class CameraControl : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static CameraControl Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Zoom Settings")]
    [Tooltip("Tốc độ zoom bằng scroll chuột")]
    public float scrollZoomSpeed = 1.5f;

    [Tooltip("Tốc độ zoom bằng pinch (mobile)")]
    public float pinchZoomSpeed = 0.025f;

    [Tooltip("Kích thước camera tối thiểu (không zoom nhỏ hơn)")]
    public float minOrthographicSize = 2.5f;

    [Header("Cursor Settings")]
    [Tooltip("Texture con trỏ khi ở chế độ Camera (để trống = con trỏ mặc định)")]
    public Texture2D cameraCursor;
    [Tooltip("Điểm tác động của con trỏ camera")]
    public Vector2 cameraHotSpot = Vector2.zero;

    [Header("Pan Settings")]
    [Tooltip("Tốc độ pan khi dùng chuột giữa (PC)")]
    public float mousePanSpeed = 1f;

    // ─── Private ──────────────────────────────────────────────────────────────
    private Camera _cam;

    /// <summary>Script CameraController cũ — sẽ bị tạm disable khi camera mode bật.</summary>
    private CameraController _legacyCameraController;

    /// <summary>Orthographic size tối đa — ghi nhận khi scene bắt đầu.</summary>
    private float _maxOrthographicSize;

    /// <summary>Chế độ điều khiển camera có đang bật không.</summary>
    private bool _isCameraModeActive = false;

    // PC – pan bằng chuột giữa
    private bool  _isPanning;
    private Vector3 _panOrigin;
    private Vector3 _camPosAtPanStart;

    // Mobile – pan bằng 1 ngón
    private bool _isTouchPanning;
    private Vector2 _touchPanOrigin;
    private Vector3 _camPosAtTouchStart;

    // Mobile – pinch
    private float _prevPinchDistance;
    private bool  _isPinching;

    // Boundary
    private Bounds _cameraBounds;
    private bool _hasBounds = false;

    // ─── Unity ────────────────────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
    }

    private void Start()
    {
        if (_cam != null)
        {
            _maxOrthographicSize = _cam.orthographicSize;
            CalculateInitialBounds();
        }

        // Tìm CameraController cũ để quản lý xung đột
        _legacyCameraController = FindObjectOfType<CameraController>();
    }

    private void CalculateInitialBounds()
    {
        if (_cam == null || !_cam.orthographic) return;

        float vertExtent = _cam.orthographicSize;
        float horzExtent = vertExtent * _cam.aspect;

        Vector3 center = _cam.transform.position;
        Vector3 size = new Vector3(horzExtent * 2f, vertExtent * 2f, 0f);

        _cameraBounds = new Bounds(center, size);
        _hasBounds = true;
    }

    private void Update()
    {
        if (!_isCameraModeActive || _cam == null) return;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        HandlePCInput();
#endif

#if UNITY_ANDROID || UNITY_IOS
        HandleMobileInput();
#elif UNITY_EDITOR
        // Trong Editor cũng test touch nếu có (Unity Remote / Simulator)
        if (Input.touchCount > 0) HandleMobileInput();
#endif

        ClampCameraPosition();
    }

    // ─── PC Input ─────────────────────────────────────────────────────────────
    private void HandlePCInput()
    {
        HandleScrollZoom();
        HandleLeftMousePan();
    }

    private void HandleScrollZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        // Bỏ qua nếu cuộn chuột trên UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!_cam.orthographic) return;

        // Vị trí con trỏ trong world trước khi zoom
        Vector3 mouseWorldBefore = _cam.ScreenToWorldPoint(Input.mousePosition);

        float newSize = _cam.orthographicSize - scroll * scrollZoomSpeed;
        newSize = Mathf.Clamp(newSize, minOrthographicSize, _maxOrthographicSize);
        _cam.orthographicSize = newSize;

        // Điều chỉnh vị trí camera để con trỏ giữ nguyên vị trí world sau khi zoom
        Vector3 mouseWorldAfter = _cam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 diff = mouseWorldBefore - mouseWorldAfter;
        diff.z = 0f;
        _cam.transform.position += diff;
    }

    private Vector3 _lastMouseScreenPos;

    private void HandleLeftMousePan()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Bỏ qua nếu click trúng UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            _isPanning = true;
            _lastMouseScreenPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isPanning = false;
        }

        if (_isPanning)
        {
            Vector3 currentMouseScreenPos = Input.mousePosition;

            // Tính world delta dựa trên sự chênh lệch screen position để không bị vòng lặp feedback
            Vector3 worldOld = _cam.ScreenToWorldPoint(_lastMouseScreenPos);
            Vector3 worldNew = _cam.ScreenToWorldPoint(currentMouseScreenPos);
            Vector3 delta = worldOld - worldNew;
            delta.z = 0f;

            _cam.transform.position += delta;

            _lastMouseScreenPos = currentMouseScreenPos;
        }
    }

    // ─── Mobile Input ─────────────────────────────────────────────────────────
    private void HandleMobileInput()
    {
        int touchCount = Input.touchCount;

        if (touchCount == 2)
        {
            // ── Pinch zoom ──
            _isTouchPanning = false; // tắt pan khi đang pinch

            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (!_isPinching || t1.phase == TouchPhase.Began)
            {
                _prevPinchDistance = Vector2.Distance(t0.position, t1.position);
                _isPinching = true;
                return;
            }

            float currentDist = Vector2.Distance(t0.position, t1.position);
            float delta = currentDist - _prevPinchDistance;
            _prevPinchDistance = currentDist;

            if (!_cam.orthographic) return;

            // Tâm 2 ngón trong world trước khi zoom
            Vector2 midScreen = (t0.position + t1.position) / 2f;
            Vector3 midWorldBefore = _cam.ScreenToWorldPoint(midScreen);

            float newSize = _cam.orthographicSize - delta * pinchZoomSpeed;
            newSize = Mathf.Clamp(newSize, minOrthographicSize, _maxOrthographicSize);
            _cam.orthographicSize = newSize;

            // Giữ tâm pinch cố định trong world
            Vector3 midWorldAfter = _cam.ScreenToWorldPoint(midScreen);
            Vector3 diff = midWorldBefore - midWorldAfter;
            diff.z = 0f;
            _cam.transform.position += diff;
        }
        else if (touchCount == 1)
        {
            _isPinching = false;

            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                // Bỏ qua nếu chạm trúng UI
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                    return;

                _isTouchPanning = true;
                _touchPanOrigin = t.position;
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                _isTouchPanning = false;
            }

            if (_isTouchPanning && t.phase == TouchPhase.Moved)
            {
                // Sử dụng t.deltaPosition để tính lượng di chuyển chính xác
                Vector2 prevPos = t.position - t.deltaPosition;

                Vector3 worldOld = _cam.ScreenToWorldPoint(new Vector3(prevPos.x, prevPos.y, 0f));
                Vector3 worldNew = _cam.ScreenToWorldPoint(new Vector3(t.position.x, t.position.y, 0f));
                
                Vector3 delta = worldOld - worldNew;
                delta.z = 0f;
                
                _cam.transform.position += delta;
            }
        }
        else
        {
            _isPinching = false;
            _isTouchPanning = false;
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Bật chế độ điều khiển camera. Gọi từ nút UI Camera.
    /// </summary>
    public void EnableCameraMode()
    {
        _isCameraModeActive = true;

        // Tắt CameraController cũ để ngăn nó ghi đè orthographicSize
        if (_legacyCameraController != null)
        {
            _legacyCameraController.CancelReturn(); // Hủy lệnh reset vị trí nếu có
            _legacyCameraController.enabled = false;
        }

        // Đổi con trỏ sang cursor camera
        if (cameraCursor != null)
            Cursor.SetCursor(cameraCursor, cameraHotSpot, CursorMode.Auto);
        else
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        // Reset trạng thái pan để tránh giật khi chuyển chế độ
        _isPanning = false;
        _isTouchPanning = false;
        _isPinching = false;
        Debug.Log("[CameraControl] Chế độ Camera đã bật.");
    }

    /// <summary>
    /// Tắt chế độ điều khiển camera. Gọi khi chọn công cụ khác (vẽ, tẩy...).
    /// </summary>
    public void DisableCameraMode()
    {
        _isCameraModeActive = false;

        // Bật lại CameraController cũ
        if (_legacyCameraController != null)
            _legacyCameraController.enabled = true;

        // Trả con trỏ về mặc định (LineCreator sẽ set cursor bút/tẩy ngay sau)
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        _isPanning = false;
        _isTouchPanning = false;
        _isPinching = false;
        Debug.Log("[CameraControl] Chế độ Camera đã tắt.");
    }

    /// <summary>Trả về true nếu đang ở chế độ Camera.</summary>
    public bool IsActive => _isCameraModeActive;

    /// <summary>
    /// Cập nhật lại maxOrthographicSize theo giá trị hiện tại của camera.
    /// Gọi sau khi reset scene (ví dụ GameController.StopSimulation).
    /// </summary>
    public void ResetMaxZoom()
    {
        if (_cam != null)
        {
            _maxOrthographicSize = _cam.orthographicSize;
            CalculateInitialBounds();
        }
    }

    private void ClampCameraPosition()
    {
        if (!_hasBounds || _cam == null || !_cam.orthographic) return;

        float vertExtent = _cam.orthographicSize;
        float horzExtent = vertExtent * _cam.aspect;

        float minX = _cameraBounds.min.x + horzExtent;
        float maxX = _cameraBounds.max.x - horzExtent;
        float minY = _cameraBounds.min.y + vertExtent;
        float maxY = _cameraBounds.max.y - vertExtent;

        // Nếu zoom đang là max (hoặc hơn) thì ép cứng camera ở tâm
        if (minX > maxX) minX = maxX = _cameraBounds.center.x;
        if (minY > maxY) minY = maxY = _cameraBounds.center.y;

        Vector3 pos = _cam.transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        _cam.transform.position = pos;
    }
}
