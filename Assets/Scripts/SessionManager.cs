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
        await System.Threading.Tasks.Task.Yield();
#endif
    }

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
        await System.Threading.Tasks.Task.Yield();
#endif
    }
}