using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Firebase JavaScript SDK Bridge for WebGL builds.
/// Tự động tạo singleton — KHÔNG cần tạo thủ công trong scene.
/// </summary>
public class FirebaseJSBridge : MonoBehaviour
{
    public static FirebaseJSBridge instance { get; private set; }

    // ─────────────────────────────────────────────────────────
    //  Auto-create singleton before any scene loads
    // ─────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (instance != null) return;

        var go = new GameObject("FirebaseJSBridge");
        instance = go.AddComponent<FirebaseJSBridge>();
        DontDestroyOnLoad(go);
        Debug.Log("[FirebaseJSBridge] Auto-created singleton.");
    }

    private void Awake()
    {
        // Nếu có instance khác, hủy cái này
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("[FirebaseJSBridge] Running on WebGL — initializing Firebase JS SDK...");
        FB_Initialize(); // Gọi JS để load Firebase Compat SDK
#else
        Debug.Log("[FirebaseJSBridge] Running in Editor/non-WebGL — bridge in stub mode.");
#endif
    }

    // ─────────────────────────────────────────────────────────
    //  Native JS Imports (only compiled on WebGL)
    // ─────────────────────────────────────────────────────────

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void   FB_Initialize();
    [DllImport("__Internal")] private static extern int    FB_Auth_IsSignedIn();
    [DllImport("__Internal")] private static extern void   FB_Auth_SignIn(string email, string password, string gameObj, string successMethod, string failMethod);
    [DllImport("__Internal")] private static extern void   FB_Auth_SignUp(string email, string password, string gameObj, string successMethod, string failMethod);
    [DllImport("__Internal")] private static extern void   FB_Auth_SignOut();
    [DllImport("__Internal")] private static extern void   FB_Auth_SendPasswordReset(string email, string gameObj, string successMethod, string failMethod);
    [DllImport("__Internal")] private static extern string FB_Auth_GetCurrentUserId();
    [DllImport("__Internal")] private static extern string FB_Auth_GetCurrentUserEmail();
    [DllImport("__Internal")] private static extern void   FB_DB_Write(string path, string json, string gameObj, string successMethod, string failMethod);
    [DllImport("__Internal")] private static extern void   FB_DB_Read(string path, string gameObj, string callbackMethod);
    [DllImport("__Internal")] private static extern void   FB_DB_Query(string path, string orderBy, int limit, string gameObj, string callbackMethod);
#endif

    // ─────────────────────────────────────────────────────────
    //  Pending action queue — chờ Firebase load xong
    // ─────────────────────────────────────────────────────────

    private System.Action _pendingAction = null;
    private float         _pendingRetryTimer = 0f;
    private int           _pendingRetryCount = 0;
    private const int     MaxRetries = 10; // tối đa 10 giây

    private void Update()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (_pendingAction != null)
        {
            _pendingRetryTimer += Time.unscaledDeltaTime;
            if (_pendingRetryTimer >= 1f)
            {
                _pendingRetryTimer = 0f;
                _pendingRetryCount++;

                if (FB_Auth_IsSignedIn() >= 0 && IsFirebaseReady())
                {
                    Debug.Log("[FirebaseJSBridge] Firebase ready — executing pending action.");
                    var action = _pendingAction;
                    _pendingAction = null;
                    _pendingRetryCount = 0;
                    action?.Invoke();
                }
                else if (_pendingRetryCount >= MaxRetries)
                {
                    Debug.LogError("[FirebaseJSBridge] Firebase failed to initialize after 10s.");
                    _pendingAction = null;
                    _pendingRetryCount = 0;
                    OnSignInFailure?.Invoke("TIMEOUT", "Firebase khong khoi tao duoc. Kiem tra ket noi mang.");
                    OnSignUpFailure?.Invoke("TIMEOUT", "Firebase khong khoi tao duoc. Kiem tra ket noi mang.");
                }
                else
                {
                    Debug.Log($"[FirebaseJSBridge] Waiting for Firebase... ({_pendingRetryCount}/{MaxRetries})");
                }
            }
        }
#endif
    }

    /// <summary>Kiểm tra Firebase đã sẵn sàng chưa (chỉ hoạt động trên WebGL)</summary>
    public bool IsFirebaseReady()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Nếu IsSignedIn() không throw exception, Firebase đã ready
        try { FB_Auth_IsSignedIn(); return true; } catch { return false; }
#else
        return false;
#endif
    }

    // ─────────────────────────────────────────────────────────
    //  Auth — Public API
    // ─────────────────────────────────────────────────────────

    /// <summary>Đăng nhập. Nếu Firebase chưa sẵn sàng, tự retry sau 1 giây.</summary>
    public void SignIn(string email, string password)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!IsFirebaseReady())
        {
            Debug.Log("[FirebaseJSBridge] Firebase not ready yet, queuing SignIn...");
            _pendingAction = () => SignIn(email, password);
            return;
        }
        FB_Auth_SignIn(email, password, gameObject.name, "OnSignInSuccess", "OnSignInFailed");
#else
        Debug.Log($"[FirebaseJSBridge STUB] SignIn: {email}");
#endif
    }

    /// <summary>Đăng ký. Nếu Firebase chưa sẵn sàng, tự retry sau 1 giây.</summary>
    public void SignUp(string email, string password)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!IsFirebaseReady())
        {
            Debug.Log("[FirebaseJSBridge] Firebase not ready yet, queuing SignUp...");
            _pendingAction = () => SignUp(email, password);
            return;
        }
        FB_Auth_SignUp(email, password, gameObject.name, "OnSignUpSuccess", "OnSignUpFailed");
#else
        Debug.Log($"[FirebaseJSBridge STUB] SignUp: {email}");
#endif
    }

    /// <summary>Đăng xuất.</summary>
    public void SignOutUser()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        FB_Auth_SignOut();
#else
        Debug.Log("[FirebaseJSBridge STUB] SignOut");
#endif
    }

    /// <summary>Gửi email đặt lại mật khẩu. Kết quả qua event OnPasswordResetSucceeded / OnPasswordResetFailure.</summary>
    public void SendPasswordReset(string email)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        FB_Auth_SendPasswordReset(email, gameObject.name, "OnPasswordResetSuccess", "OnPasswordResetFailed");
#else
        Debug.Log($"[FirebaseJSBridge STUB] SendPasswordReset: {email}");
#endif
    }

    /// <summary>Lấy UID user hiện tại (null nếu chưa đăng nhập).</summary>
    public string GetCurrentUserId()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return FB_Auth_GetCurrentUserId();
#else
        return null;
#endif
    }

    /// <summary>Lấy Email user hiện tại.</summary>
    public string GetCurrentUserEmail()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return FB_Auth_GetCurrentUserEmail();
#else
        return null;
#endif
    }

    /// <summary>Kiểm tra user đã đăng nhập chưa.</summary>
    public bool IsSignedIn()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return FB_Auth_IsSignedIn() == 1;
#else
        return false;
#endif
    }

    // ─────────────────────────────────────────────────────────
    //  Database — Public API
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Ghi dữ liệu vào Realtime Database.
    /// path: "NodeName/uid" (ví dụ "Users/uid123")
    /// jsonData: chuỗi JSON
    /// </summary>
    public void WriteDatabase(string path, string jsonData)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        FB_DB_Write(path, jsonData, gameObject.name, "OnDBWriteSuccess", "OnDBWriteFailed");
#else
        Debug.Log($"[FirebaseJSBridge STUB] WriteDatabase path={path}");
#endif
    }

    /// <summary>
    /// Đọc dữ liệu từ Realtime Database.
    /// Callback nhận chuỗi "OK|{json}" hoặc "NULL|" hoặc "ERROR|msg"
    /// </summary>
    public void ReadDatabase(string path, string callbackGameObject, string callbackMethod)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        FB_DB_Read(path, callbackGameObject, callbackMethod);
#else
        Debug.Log($"[FirebaseJSBridge STUB] ReadDatabase path={path}");
#endif
    }

    /// <summary>
    /// Truy vấn dữ liệu (Ranking).
    /// </summary>
    public void QueryDatabase(string path, string orderBy, int limit, string callbackGameObject, string callbackMethod)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        FB_DB_Query(path, orderBy, limit, callbackGameObject, callbackMethod);
#else
        Debug.Log($"[FirebaseJSBridge STUB] QueryDatabase path={path}, orderBy={orderBy}, limit={limit}");
#endif
    }

    // ─────────────────────────────────────────────────────────
    //  Events (cho UI scripts đăng ký)
    // ─────────────────────────────────────────────────────────

    public event Action<string, string> OnSignInSucceeded;      // uid, email
    public event Action<string, string> OnSignInFailure;        // errorCode, message
    public event Action<string, string> OnSignUpSucceeded;      // uid, email
    public event Action<string, string> OnSignUpFailure;        // errorCode, message
    public event Action                 OnPasswordResetSucceeded;
    public event Action<string, string> OnPasswordResetFailure; // errorCode, message

    // ─────────────────────────────────────────────────────────
    //  Callbacks được gọi bởi JS qua SendMessage
    // ─────────────────────────────────────────────────────────

    private void OnSignInSuccess(string result)
    {
        var parts = result.Split(new char[]{'|'}, 2);
        string uid   = parts.Length > 0 ? parts[0] : "";
        string email = parts.Length > 1 ? parts[1] : "";
        Debug.Log($"[FirebaseJSBridge] SignIn success — UID: {uid}");
        OnSignInSucceeded?.Invoke(uid, email);
    }

    private void OnSignInFailed(string result)
    {
        var parts = result.Split(new char[]{'|'}, 2);
        string code    = parts.Length > 0 ? parts[0] : "UNKNOWN";
        string message = parts.Length > 1 ? parts[1] : result;
        Debug.LogWarning($"[FirebaseJSBridge] SignIn failed — {code}: {message}");
        OnSignInFailure?.Invoke(code, message);
    }

    private void OnSignUpSuccess(string result)
    {
        var parts = result.Split(new char[]{'|'}, 2);
        string uid   = parts.Length > 0 ? parts[0] : "";
        string email = parts.Length > 1 ? parts[1] : "";
        Debug.Log($"[FirebaseJSBridge] SignUp success — UID: {uid}");
        OnSignUpSucceeded?.Invoke(uid, email);
    }

    private void OnSignUpFailed(string result)
    {
        var parts = result.Split(new char[]{'|'}, 2);
        string code    = parts.Length > 0 ? parts[0] : "UNKNOWN";
        string message = parts.Length > 1 ? parts[1] : result;
        Debug.LogWarning($"[FirebaseJSBridge] SignUp failed — {code}: {message}");
        OnSignUpFailure?.Invoke(code, message);
    }

    private void OnPasswordResetSuccess(string result)
    {
        Debug.Log("[FirebaseJSBridge] Password reset email sent.");
        OnPasswordResetSucceeded?.Invoke();
    }

    private void OnPasswordResetFailed(string result)
    {
        var parts = result.Split(new char[]{'|'}, 2);
        string code    = parts.Length > 0 ? parts[0] : "UNKNOWN";
        string message = parts.Length > 1 ? parts[1] : result;
        Debug.LogWarning($"[FirebaseJSBridge] Password reset failed — {code}: {message}");
        OnPasswordResetFailure?.Invoke(code, message);
    }

    private void OnDBWriteSuccess(string result)
    {
        Debug.Log("[FirebaseJSBridge] DB write success.");
    }

    private void OnDBWriteFailed(string result)
    {
        Debug.LogWarning($"[FirebaseJSBridge] DB write failed: {result}");
    }
}
