using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase.Auth;
using Firebase.Database;
#endif
using Newtonsoft.Json;
using UnityEngine;

public class DataGame : MonoBehaviour
{
    public static DataGame instance;
#if !UNITY_WEBGL || UNITY_EDITOR
    private FirebaseUser user;
#endif
    public Users users = null;
    public CurrentLevel CurrentLevel;
    public int CurrentSkin = 0;
    public MySkin MySkin;
    public List<Level> levels = new List<Level>();
    public TotalPoint totalPoint;
    public List<TotalPoint> TotalPointRank;
    public List<CurrentLevel> LevelRank;
    public List<Level> LvXRank;
    public bool Tutorial = false;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
#if !UNITY_WEBGL || UNITY_EDITOR
        user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null) InitializeNative();
#else
        StartCoroutine(WebGLInitSequence());
#endif
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    private async void InitializeNative()
    {
        FindUsers();
        await FindCurrentLevel();
        FindCurrentSkin();
        FindMySkin();
        await LoadAllLevel();
        LoadTotalPoint();
        FindTutorial();
    }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    private IEnumerator WebGLInitSequence()
    {
        while (FirebaseJSBridge.instance == null || !FirebaseJSBridge.instance.IsFirebaseReady())
            yield return new WaitForSeconds(0.5f);
        string uid = FirebaseJSBridge.instance.GetCurrentUserId();
        if (!string.IsNullOrEmpty(uid)) LoadDataWebGL();
    }

    private async void LoadDataWebGL()
    {
        string uid = FirebaseJSBridge.instance.GetCurrentUserId();
        await FindUsersWebGL(uid);
        await FindCurrentLevelWebGL(uid);
        await FindCurrentSkinWebGL(uid);
        await FindMySkinWebGL(uid);
        await LoadAllLevelWebGL(uid);
        await LoadTotalPointWebGL(uid);
        await FindTutorialWebGL(uid);
    }
#endif

    // ─────────────────────────────────────────────────────────
    //  RANKING METHODS (Now implemented for WebGL)
    // ─────────────────────────────────────────────────────────

    public async Task LoadTotalPointRank() {
#if !UNITY_WEBGL || UNITY_EDITOR
        TotalPointRank = await FirebaseDataManager.instance.GetTop10TotalPoint();
#else
        string json = GetCleanJson(await QueryDatabaseAsync("TotalPoint", "point", 10));
        if (json != null) {
            TotalPointRank = JsonConvert.DeserializeObject<List<TotalPoint>>(json);
            TotalPointRank = TotalPointRank.OrderByDescending(x => x.point).ToList();
        }
#endif
    }

    public async Task<int> FindMyRank() {
#if !UNITY_WEBGL || UNITY_EDITOR
        return await FirebaseDataManager.instance.GetMyRank();
#else
        string json = GetCleanJson(await QueryDatabaseAsync("TotalPoint", "point", 0));
        if (json != null) {
            var all = JsonConvert.DeserializeObject<List<TotalPoint>>(json);
            all = all.OrderByDescending(x => x.point).ToList();
            for (int i = 0; i < all.Count; i++) {
                if (all[i].name == totalPoint.name && all[i].point == totalPoint.point) return i + 1;
            }
        }
        return 0;
#endif
    }

    public async Task LoadLevelRank() {
#if !UNITY_WEBGL || UNITY_EDITOR
        LevelRank = await FirebaseDataManager.instance.GetTop10Level();
#else
        string json = GetCleanJson(await ReadDatabaseAsync("CurrentLevel"));
        if (json != null) {
            // ReadDatabase trả về object dict, cần convert sang list
            var dict = JsonConvert.DeserializeObject<Dictionary<string, CurrentLevel>>(json);
            LevelRank = dict.Values.OrderByDescending(x => x.level).ThenBy(x => x.time).Take(10).ToList();
        }
#endif
    }

    public async Task<int> FindMyLevelRank() {
#if !UNITY_WEBGL || UNITY_EDITOR
        return await FirebaseDataManager.instance.GetMyLevelRank();
#else
        string uid = FirebaseJSBridge.instance.GetCurrentUserId();
        string json = GetCleanJson(await ReadDatabaseAsync("CurrentLevel"));
        if (json != null) {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, CurrentLevel>>(json);
            var list = dict.Select(kvp => new { uid = kvp.Key, data = kvp.Value })
                           .OrderByDescending(x => x.data.level).ThenBy(x => x.data.time).ToList();
            for (int i = 0; i < list.Count; i++) { if (list[i].uid == uid) return i + 1; }
        }
        return 0;
#endif
    }

    public async Task LoadTop10Level(int lv) {
#if !UNITY_WEBGL || UNITY_EDITOR
        LvXRank = await FirebaseDataManager.instance.GetTop10Level("Lv" + lv.ToString());
#else
        string json = GetCleanJson(await QueryDatabaseAsync("Lv" + lv, "point", 10));
        if (json != null) {
            LvXRank = JsonConvert.DeserializeObject<List<Level>>(json);
            LvXRank = LvXRank.OrderByDescending(x => x.point).ToList();
        }
#endif
    }

    public async Task<int> FindMyLevelXRank(int lv) {
#if !UNITY_WEBGL || UNITY_EDITOR
        return await FirebaseDataManager.instance.GetMyLevelRank("Lv" + lv.ToString());
#else
        string uid = FirebaseJSBridge.instance.GetCurrentUserId();
        string json = GetCleanJson(await QueryDatabaseAsync("Lv" + lv, "point", 0));
        if (json != null) {
            var all = JsonConvert.DeserializeObject<List<Level>>(json);
            // Thêm field _key vào model nếu cần, ở đây ta so sánh UID hoặc name
            all = all.OrderByDescending(x => x.point).ToList();
            for (int i = 0; i < all.Count; i++) {
                // Giả sử có UID trong Level hoặc so sánh tên
                if (all[i].namePlayer == users.name) return i + 1;
            }
        }
        return -1;
#endif
    }

    // ─────────────────────────────────────────────────────────
    //  WEBGL IMPLEMENTATION HELPERS
    // ─────────────────────────────────────────────────────────

#if UNITY_WEBGL && !UNITY_EDITOR
    private TaskCompletionSource<string> _readTaskSource;

    private async Task<string> ReadDatabaseAsync(string path) {
        _readTaskSource = new TaskCompletionSource<string>();
        FirebaseJSBridge.instance.ReadDatabase(path, gameObject.name, "OnReadResult");
        return await _readTaskSource.Task;
    }

    private async Task<string> QueryDatabaseAsync(string path, string orderBy, int limit) {
        _readTaskSource = new TaskCompletionSource<string>();
        FirebaseJSBridge.instance.QueryDatabase(path, orderBy, limit, gameObject.name, "OnReadResult");
        return await _readTaskSource.Task;
    }

    public void OnReadResult(string result) { if (_readTaskSource != null) _readTaskSource.TrySetResult(result); }

    private string GetCleanJson(string result) {
        if (string.IsNullOrEmpty(result) || result.StartsWith("ERROR|") || result.StartsWith("NULL|")) return null;
        return result.StartsWith("OK|") ? result.Substring(3) : result;
    }

    async Task FindUsersWebGL(string uid) {
        string json = GetCleanJson(await ReadDatabaseAsync("Users/" + uid));
        if (json != null) users = JsonConvert.DeserializeObject<Users>(json);
    }
    async Task FindTutorialWebGL(string uid) {
        string json = GetCleanJson(await ReadDatabaseAsync("Tutorial/" + uid));
        if (json != null) Tutorial = JsonConvert.DeserializeObject<bool>(json);
    }
    async Task FindCurrentLevelWebGL(string uid) {
        string json = GetCleanJson(await ReadDatabaseAsync("CurrentLevel/" + uid));
        if (json != null) CurrentLevel = JsonConvert.DeserializeObject<CurrentLevel>(json);
    }
    async Task FindCurrentSkinWebGL(string uid) {
        string json = GetCleanJson(await ReadDatabaseAsync("CurrentSkin/" + uid));
        if (json != null) CurrentSkin = JsonConvert.DeserializeObject<int>(json);
    }
    async Task FindMySkinWebGL(string uid) {
        string json = GetCleanJson(await ReadDatabaseAsync("MySkin/" + uid));
        if (json != null) MySkin = JsonConvert.DeserializeObject<MySkin>(json);
    }
    async Task LoadAllLevelWebGL(string uid) {
        if (CurrentLevel == null) return;
        levels.Clear();
        for (int i = 1; i < CurrentLevel.level; i++) {
            string json = GetCleanJson(await ReadDatabaseAsync($"Lv{i}/{uid}"));
            if (json != null) levels.Add(JsonConvert.DeserializeObject<Level>(json));
        }
    }
    async Task LoadTotalPointWebGL(string uid) {
        string json = GetCleanJson(await ReadDatabaseAsync("TotalPoint/" + uid));
        if (json != null) totalPoint = JsonConvert.DeserializeObject<TotalPoint>(json);
    }
#endif

#if !UNITY_WEBGL || UNITY_EDITOR
    async void FindUsers() {
        DataSnapshot snap = await FirebaseDataManager.instance.ReadDatabase("Users", user.UserId);
        if (snap != null && snap.Exists) users = JsonConvert.DeserializeObject<Users>(snap.GetRawJsonValue());
    }
    async void FindTutorial() {
        DataSnapshot snap = await FirebaseDataManager.instance.ReadDatabase("Tutorial", user.UserId);
        if (snap != null && snap.Exists) Tutorial = JsonConvert.DeserializeObject<bool>(snap.GetRawJsonValue());
    }
    async Task FindCurrentLevel() {
        DataSnapshot snap = await FirebaseDataManager.instance.ReadDatabase("CurrentLevel", user.UserId);
        if (snap != null && snap.Exists) CurrentLevel = JsonConvert.DeserializeObject<CurrentLevel>(snap.GetRawJsonValue());
    }
    async void FindCurrentSkin() {
        DataSnapshot snap = await FirebaseDataManager.instance.ReadDatabase("CurrentSkin", user.UserId);
        if (snap != null && snap.Exists) CurrentSkin = JsonConvert.DeserializeObject<int>(snap.GetRawJsonValue());
    }
    async void FindMySkin() {
        DataSnapshot snap = await FirebaseDataManager.instance.ReadDatabase("MySkin", user.UserId);
        if (snap != null && snap.Exists) MySkin = JsonConvert.DeserializeObject<MySkin>(snap.GetRawJsonValue());
    }
    async Task LoadAllLevel() {
        if (CurrentLevel == null) return;
        levels.Clear();
        for (int i = 1; i < CurrentLevel.level; i++) {
            Level level = await FindPointofLevel(i);
            if (level != null) levels.Add(level);
        }
    }
    async Task<Level> FindPointofLevel(int lv) {
        DataSnapshot snap = await FirebaseDataManager.instance.ReadDatabase("Lv" + lv.ToString(), user.UserId);
        if (snap != null && snap.Exists) return JsonConvert.DeserializeObject<Level>(snap.GetRawJsonValue());
        return null;
    }
    async void LoadTotalPoint() {
        DataSnapshot snap = await FirebaseDataManager.instance.ReadDatabase("TotalPoint", user.UserId);
        if (snap != null && snap.Exists) totalPoint = JsonConvert.DeserializeObject<TotalPoint>(snap.GetRawJsonValue());
    }
#endif
}
