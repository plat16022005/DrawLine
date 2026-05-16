#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase.Database;
using Firebase.Auth;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    private string sessionToken;
    private string uid;

    async void Start()
    {
        DontDestroyOnLoad(this);
#if !UNITY_WEBGL || UNITY_EDITOR
        if (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            Debug.LogError("Chưa đăng nhập!");
            return;
        }

        uid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        sessionToken = System.Guid.NewGuid().ToString();

        await FirebaseDatabase.DefaultInstance
            .RootReference
            .Child("UserSession")
            .Child(uid)
            .Child("sessionToken")
            .SetValueAsync(sessionToken);

        InvokeRepeating(nameof(CheckSession), 5f, 5f);
#else
        StartCoroutine(WebGLStartSession());
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private System.Threading.Tasks.TaskCompletionSource<string> _readTaskSource;

    private async System.Threading.Tasks.Task<string> ReadSessionTokenWebGL(string path)
    {
        _readTaskSource = new System.Threading.Tasks.TaskCompletionSource<string>();
        FirebaseJSBridge.instance.ReadDatabase(path, gameObject.name, "OnReadSessionResult");
        return await _readTaskSource.Task;
    }

    public void OnReadSessionResult(string result)
    {
        if (_readTaskSource != null)
        {
            _readTaskSource.TrySetResult(result);
        }
    }

    private System.Collections.IEnumerator WebGLStartSession()
    {
        while (FirebaseJSBridge.instance == null || !FirebaseJSBridge.instance.IsFirebaseReady())
            yield return new WaitForSeconds(0.5f);

        uid = FirebaseJSBridge.instance.GetCurrentUserId();
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("Chưa đăng nhập!");
            yield break;
        }

        sessionToken = System.Guid.NewGuid().ToString();
        // Cần lưu là string JSON nên thêm quotes
        FirebaseJSBridge.instance.WriteDatabase($"UserSession/{uid}/sessionToken", $"\"{sessionToken}\"");

        InvokeRepeating(nameof(CheckSessionWebGL), 5f, 5f);
    }
#endif

    async void CheckSession()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        var snapshot = await FirebaseDatabase.DefaultInstance
            .RootReference
            .Child("UserSession")
            .Child(uid)
            .Child("sessionToken")
            .GetValueAsync();

        if (!snapshot.Exists || snapshot.Value == null)
        {
            Debug.LogWarning("Không tìm thấy sessionToken trên server");
            return;
        }

        string serverToken = snapshot.Value.ToString();

        if (serverToken != sessionToken)
        {
            Debug.Log("Tài khoản đã đăng nhập ở thiết bị khác!");
            CancelInvoke(nameof(CheckSession));
            FirebaseAuth.DefaultInstance.SignOut();
            SceneManager.LoadScene("MainMenu");
        }
#else
        // Dummy function to prevent compiler error for WebGL builds if CheckSession isn't called
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    async void CheckSessionWebGL()
    {
        string json = await ReadSessionTokenWebGL($"UserSession/{uid}/sessionToken");
        if (string.IsNullOrEmpty(json) || json.StartsWith("ERROR|") || json.StartsWith("NULL|"))
        {
            Debug.LogWarning("Không tìm thấy sessionToken trên server");
            return;
        }
        
        string serverToken = json.StartsWith("OK|") ? json.Substring(3) : json;
        // Loại bỏ dấu ngoặc kép do JSON parsing
        serverToken = serverToken.Replace("\"", "").Trim();

        if (serverToken != sessionToken)
        {
            Debug.Log("Tài khoản đã đăng nhập ở thiết bị khác!");
            CancelInvoke(nameof(CheckSessionWebGL));
            FirebaseJSBridge.instance.SignOutUser();
            SceneManager.LoadScene("MainMenu");
        }
    }
#endif
}