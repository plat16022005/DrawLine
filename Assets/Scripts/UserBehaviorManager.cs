using System;
using System.Collections;
using UnityEngine;
#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase.Auth;
#endif

public class UserBehaviorManager : MonoBehaviour
{
    private string uid;
    private string sessionId;
    private float sessionStartTime;
    
    private int sessionsToday;
    private double timeSinceLastOpen;
    
    private void Start()
    {
        StartCoroutine(InitRoutine());
    }
    
    private IEnumerator InitRoutine()
    {
        // Chờ FirebaseDataManager sẵn sàng
        while (FirebaseDataManager.instance == null)
            yield return new WaitForSeconds(0.5f);

#if !UNITY_WEBGL || UNITY_EDITOR
        while (FirebaseAuth.DefaultInstance == null || FirebaseAuth.DefaultInstance.CurrentUser == null)
            yield return new WaitForSeconds(0.5f);
        uid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
#else
        while (FirebaseJSBridge.instance == null || !FirebaseJSBridge.instance.IsFirebaseReady() || string.IsNullOrEmpty(FirebaseJSBridge.instance.GetCurrentUserId()))
            yield return new WaitForSeconds(0.5f);
        uid = FirebaseJSBridge.instance.GetCurrentUserId();
#endif

        CalculateSessionData();
        
        sessionId = Guid.NewGuid().ToString();
        sessionStartTime = Time.unscaledTime;
        
        // Ghi nhận lần đầu tiên
        SaveSessionData();

        // Ghi định kỳ mỗi 30 giây để tránh mất dữ liệu (đặc biệt trên WebGL)
        InvokeRepeating(nameof(SaveSessionData), 30f, 30f);
    }
    
    private void CalculateSessionData()
    {
        string lastOpenStr = PlayerPrefs.GetString("LastOpenTime", "");
        DateTime now = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(lastOpenStr))
        {
            if (DateTime.TryParse(lastOpenStr, out DateTime lastOpen))
            {
                // DateTime.TryParse tự động chuyển giờ Z về Local, do đó phải đổi lại sang UTC để đồng nhất với now (UtcNow).
                timeSinceLastOpen = (now - lastOpen.ToUniversalTime()).TotalSeconds;
            }
        }
        else
        {
            timeSinceLastOpen = 0;
        }
        
        // Cập nhật LastOpenTime ngay lúc mở
        PlayerPrefs.SetString("LastOpenTime", now.ToString("O"));
        
        string lastDay = PlayerPrefs.GetString("LastDay", "");
        string currentDay = now.ToString("yyyy-MM-dd");
        
        if (lastDay == currentDay)
        {
            sessionsToday = PlayerPrefs.GetInt("SessionsToday", 0) + 1;
        }
        else
        {
            sessionsToday = 1;
        }
        
        PlayerPrefs.SetString("LastDay", currentDay);
        PlayerPrefs.SetInt("SessionsToday", sessionsToday);
        PlayerPrefs.Save();
    }
    
    private void SaveSessionData()
    {
        if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(sessionId)) return;
        
        float currentSessionDuration = Time.unscaledTime - sessionStartTime;
        
        UserSessionLog log = new UserSessionLog
        {
            sessionDuration = currentSessionDuration,
            sessionsToday = sessionsToday,
            timeSinceLastOpen = (float)timeSinceLastOpen,
            date = DateTime.UtcNow.ToString("O"),
            platform = GetPlatformName()
        };
        
        FirebaseDataManager.instance.WriteDatabase($"UserBehavior/{uid}", sessionId, log);
    }

    private string GetPlatformName()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return "webgl";
#elif UNITY_ANDROID || UNITY_IOS
        return "pe";
#else
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            return "pe";
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
            return "webgl";
        else
            return "pc";
#endif
    }
}

[Serializable]
public class UserSessionLog
{
    public float sessionDuration;
    public int sessionsToday;
    public float timeSinceLastOpen;
    public string date;
    public string platform;
}
