using Firebase.Database;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    private string sessionToken;
    private string uid;

    async void Start()
    {
        DontDestroyOnLoad(this);
        if (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            Debug.LogError("Chưa đăng nhập!");
            return;
        }

        uid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        // tạo token ngẫu nhiên cho thiết bị hiện tại
        sessionToken = System.Guid.NewGuid().ToString();

        // lưu vào UserSession thay vì Users
        await FirebaseDatabase.DefaultInstance
            .RootReference
            .Child("UserSession")
            .Child(uid)
            .Child("sessionToken")
            .SetValueAsync(sessionToken);

        // kiểm tra định kỳ
        InvokeRepeating(nameof(CheckSession), 5f, 5f);
    }

    async void CheckSession()
    {
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
            // TODO: chuyển về scene login
            // SceneManager.LoadScene("Login");
        }
    }
}