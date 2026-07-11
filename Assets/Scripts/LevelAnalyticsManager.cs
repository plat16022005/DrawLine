using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase.Database;
#endif

public class LevelAnalyticsManager : MonoBehaviour
{
    public static LevelAnalyticsManager instance;
    private string currentLevelName;
    private float sessionStartTime;
    private int currentDeathCount;
    private int currentResetCount;
    private bool levelFinished;

    // --- FPS Tracking ---
    private int totalFramesInSession;
    private float sessionMinFps;
    private float fpsTimer;
    private int framesInCurrentSecond;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            
            PlayerHealth.OnPlayerDied += OnPlayerDied;
            Princess.OnPlayerWin += OnPlayerWin;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(currentLevelName) || levelFinished) return;

        totalFramesInSession++;
        framesInCurrentSecond++;
        fpsTimer += Time.unscaledDeltaTime;

        // Cập nhật Min FPS mỗi giây
        if (fpsTimer >= 1f)
        {
            float currentFps = framesInCurrentSecond / fpsTimer;
            if (currentFps < sessionMinFps) sessionMinFps = currentFps;
            
            framesInCurrentSecond = 0;
            fpsTimer = 0f;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            PlayerHealth.OnPlayerDied -= OnPlayerDied;
            Princess.OnPlayerWin -= OnPlayerWin;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // CHỈ theo dõi các scene là bàn chơi (thường bắt đầu bằng "Lv")
        if (!scene.name.StartsWith("Lv"))
        {
            currentLevelName = "";
            return;
        }

        currentLevelName = scene.name;
        
        // Nếu là map cộng đồng, đổi tên để phân biệt từng map riêng biệt
        if (scene.name == "LvMap" && DataGame.instance != null && !string.IsNullOrEmpty(DataGame.instance.currentCommunityMapId))
        {
            currentLevelName = "CommunityMap_" + DataGame.instance.currentCommunityMapId;
        }
        
        sessionStartTime = Time.unscaledTime;
        currentDeathCount = 0;
        currentResetCount = 0;
        levelFinished = false;
        
        totalFramesInSession = 0;
        sessionMinFps = 999f;
        fpsTimer = 0f;
        framesInCurrentSecond = 0;

        // Ghi nhận bắt đầu chơi (playCount +1)
        if (FirebaseDataManager.instance != null)
        {
            FirebaseDataManager.instance.UpdateLevelStats(currentLevelName, "play", 0, 0, 0, 0, 0);
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (string.IsNullOrEmpty(currentLevelName)) return;

        // Nếu người chơi thoát ra giữa chừng mà chưa chết/thắng (ví dụ bấm nút Back ra Menu)
        if (!levelFinished)
        {
            float timeSpent = Time.unscaledTime - sessionStartTime;
            if (FirebaseDataManager.instance != null)
            {
                FirebaseDataManager.instance.UpdateLevelStats(currentLevelName, "quit", timeSpent, currentDeathCount, currentResetCount, totalFramesInSession, sessionMinFps);
                FirebaseDataManager.instance.LogFPSData(currentLevelName, totalFramesInSession, sessionMinFps, timeSpent);
            }
        }
    }

    private void OnPlayerDied()
    {
        if (string.IsNullOrEmpty(currentLevelName) || levelFinished) return;
        
        currentDeathCount++;
        levelFinished = true; // Kết thúc phiên hiện tại
        float timeSpent = Time.unscaledTime - sessionStartTime;
        
        if (FirebaseDataManager.instance != null)
        {
            FirebaseDataManager.instance.UpdateLevelStats(currentLevelName, "lose", timeSpent, currentDeathCount, currentResetCount, totalFramesInSession, sessionMinFps);
            FirebaseDataManager.instance.LogFPSData(currentLevelName, totalFramesInSession, sessionMinFps, timeSpent);
        }
    }

    private void OnPlayerWin(Vector3 pos)
    {
        if (string.IsNullOrEmpty(currentLevelName) || levelFinished) return;
        
        levelFinished = true;
        float timeSpent = Time.unscaledTime - sessionStartTime;
        
        if (FirebaseDataManager.instance != null)
        {
            FirebaseDataManager.instance.UpdateLevelStats(currentLevelName, "win", timeSpent, currentDeathCount, currentResetCount, totalFramesInSession, sessionMinFps);
            FirebaseDataManager.instance.LogFPSData(currentLevelName, totalFramesInSession, sessionMinFps, timeSpent);
        }
    }

    // Được gọi từ GameController.StopSimulation()
    public void OnResetSimulation()
    {
        if (string.IsNullOrEmpty(currentLevelName) || levelFinished) return;
        currentResetCount++;
    }
}

[Serializable]
public class LevelStats
{
    public int playCount;
    public int winCount;
    public int loseCount;
    public int quitCount;
    public float totalTimeSpent;
    public int deathCount;
    public int resetCount;
    public long totalFrames;
    public float minFps = 999f;
    public float avgFps;
}
