using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
#endif
using Newtonsoft.Json;
using UnityEngine;

public class FirebaseDataManager : MonoBehaviour
{
    public static FirebaseDataManager instance;
#if !UNITY_WEBGL || UNITY_EDITOR
    private DatabaseReference reference;
#endif
    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    private async void Start()
    {
        var result = await FirebaseInitializer.EnsureInitializedAsync();
        if (result == DependencyStatus.Available)
            reference = FirebaseDatabase.DefaultInstance.RootReference;
        else
            Debug.LogError("[FirebaseDataManager] Firebase dependency lỗi: " + result);
    }
#endif

#if !UNITY_WEBGL || UNITY_EDITOR
    public void WriteDatabase<T>(string child, string id, T data)
    {
        if (data is int || data is float || data is double || data is bool || data is string || data is long)
        {
            reference.Child(child).Child(id).SetValueAsync(data).ContinueWithOnMainThread(task => {
                if (task.IsCompleted) Debug.Log("Ghi dữ liệu thành công");
                else Debug.LogError("Lỗi: " + task.Exception);
            });
            return;
        }

        string json = JsonConvert.SerializeObject(data, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });
        reference.Child(child).Child(id).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task => {
            if (task.IsCompleted) Debug.Log("Ghi dữ liệu thành công");
            else Debug.LogError("Lỗi: " + task.Exception);
        });
    }

    public async Task<DataSnapshot> ReadDatabase(string child, string id)
    {
        return await reference.Child(child).Child(id).GetValueAsync();
    }

    public async Task<List<TotalPoint>> GetTop10TotalPoint()
    {
        List<TotalPoint> result = new List<TotalPoint>();
        try {
            DataSnapshot snapshot = await reference.Child("TotalPoint").OrderByChild("point").LimitToLast(10).GetValueAsync();
            foreach (DataSnapshot child in snapshot.Children) {
                string json = child.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json)) {
                    TotalPoint player = JsonConvert.DeserializeObject<TotalPoint>(json);
                    if (player != null) result.Add(player);
                }
            }
            result = result.OrderByDescending(x => x.point).ToList();
        } catch (Exception e) { Debug.LogError("Lỗi lấy Top 10: " + e.Message); }
        return result;
    }

    public async Task<int> GetMyRank()
    {
        int rank = 0;
        try {
            string myUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            DataSnapshot snapshot = await reference.Child("TotalPoint").OrderByChild("point").GetValueAsync();
            List<TotalPoint> players = new List<TotalPoint>();
            foreach (DataSnapshot child in snapshot.Children) {
                string json = child.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json)) {
                    TotalPoint p = JsonConvert.DeserializeObject<TotalPoint>(json);
                    if (p != null) players.Add(p);
                }
            }
            players = players.OrderByDescending(x => x.point).ToList();
            for (int i = 0; i < players.Count; i++) {
                if (players[i].name == DataGame.instance.totalPoint.name && players[i].point == DataGame.instance.totalPoint.point) {
                    rank = i + 1; break;
                }
            }
        } catch (Exception e) { Debug.LogError("Lỗi lấy rank: " + e); }
        return rank;
    }

    public async Task<List<CurrentLevel>> GetTop10Level()
    {
        List<CurrentLevel> result = new List<CurrentLevel>();
        try {
            DataSnapshot snapshot = await reference.Child("CurrentLevel").GetValueAsync();
            foreach (DataSnapshot child in snapshot.Children) {
                string json = child.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json)) {
                    CurrentLevel player = JsonConvert.DeserializeObject<CurrentLevel>(json);
                    if (player != null) result.Add(player);
                }
            }
            result = result.OrderByDescending(x => x.level).ThenBy(x => x.time).Take(10).ToList();
        } catch (Exception e) { Debug.LogError("Lỗi lấy Top Level: " + e); }
        return result;
    }

    public async Task<int> GetMyLevelRank()
    {
        int myRank = 0;
        try {
            string myUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            DataSnapshot snapshot = await reference.Child("CurrentLevel").GetValueAsync();
            List<(string uid, CurrentLevel data)> players = new List<(string, CurrentLevel)>();
            foreach (DataSnapshot child in snapshot.Children) {
                string json = child.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json)) {
                    CurrentLevel p = JsonConvert.DeserializeObject<CurrentLevel>(json);
                    if (p != null) players.Add((child.Key, p));
                }
            }
            players = players.OrderByDescending(x => x.data.level).ThenBy(x => x.data.time).ToList();
            for (int i = 0; i < players.Count; i++) { if (players[i].uid == myUid) { myRank = i + 1; break; } }
        } catch (Exception e) { Debug.LogError("Lỗi lấy rank bản thân: " + e); }
        return myRank;
    }

    public async Task<List<Level>> GetTop10Level(string levelName)
    {
        List<Level> top10 = new List<Level>();
        try {
            var snapshot = await reference.Child(levelName).OrderByChild("point").LimitToLast(10).GetValueAsync();
            foreach (var child in snapshot.Children) {
                string json = child.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json)) {
                    Level ld = JsonConvert.DeserializeObject<Level>(json);
                    if (ld != null) top10.Add(ld);
                }
            }
            top10.Reverse();
        } catch (Exception ex) { Debug.LogError("Lỗi lấy Top 10 Level: " + ex.Message); }
        return top10;
    }

    public async Task<int> GetMyLevelRank(string levelName)
    {
        try {
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null) return -1;
            var allSnapshot = await reference.Child(levelName).OrderByChild("point").GetValueAsync();
            List<string> uids = new List<string>();
            foreach (var child in allSnapshot.Children) {
                uids.Add(child.Key);
            }
            uids.Reverse(); // Sắp xếp giảm dần theo điểm, giữ nguyên thứ tự UID của Firebase
            for (int i = 0; i < uids.Count; i++) {
                if (uids[i] == user.UserId) return i + 1;
            }
        } catch (Exception ex) { Debug.LogError("Lỗi lấy rank: " + ex.Message); }
        return -1;
    }

    public async Task<List<Level>> GetTop10CommunityMap(string mapId)
    {
        List<Level> top10 = new List<Level>();
        try
        {
            var snapshot = await reference
                .Child("CommunityMapLeaderboard")
                .Child(mapId)
                .GetValueAsync();

            foreach (var child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json))
                {
                    Level ld = JsonConvert.DeserializeObject<Level>(json);
                    if (ld != null) top10.Add(ld);
                }
            }

            top10 = top10
                .OrderByDescending(x => x.point) // điểm cao hơn đứng trước
                .ThenBy(x => x.time)            // cùng điểm thì time thấp hơn đứng trước
                .Take(10)
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.LogError("Lỗi lấy Top 10 Map: " + ex.Message);
        }

        return top10;
    }

    public async Task<int> GetMyCommunityMapRank(string mapId)
    {
        try
        {
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null) return -1;

            var snapshot = await reference
                .Child("CommunityMapLeaderboard")
                .Child(mapId)
                .GetValueAsync();

            List<(string uid, Level data)> players = new List<(string, Level)>();

            foreach (var child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json))
                {
                    Level level = JsonConvert.DeserializeObject<Level>(json);
                    if (level != null)
                    {
                        players.Add((child.Key, level));
                    }
                }
            }

            players = players
                .OrderByDescending(x => x.data.point) // Điểm cao hơn
                .ThenBy(x => x.data.time)             // Cùng điểm -> thời gian nhỏ hơn
                .ToList();

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].uid == user.UserId)
                    return i + 1;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Lỗi lấy rank map: " + ex.Message);
        }

        return -1;
    }

    public async Task<Level> GetMyCommunityMapScore(string mapId)
    {
        try {
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null) return null;
            var snapshot = await reference.Child("CommunityMapLeaderboard").Child(mapId).Child(user.UserId).GetValueAsync();
            if (snapshot.Exists) {
                string json = snapshot.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json)) {
                    return JsonConvert.DeserializeObject<Level>(json);
                }
            }
        } catch (Exception ex) { Debug.LogError("Lỗi lấy score map: " + ex.Message); }
        return null;
    }

#else
    // WEBGL IMPLEMENTATION
    public void WriteDatabase<T>(string child, string id, T data)
    {
        string path = $"{child}/{id}";
        string json = JsonConvert.SerializeObject(data);
        if (FirebaseJSBridge.instance != null) FirebaseJSBridge.instance.WriteDatabase(path, json);
    }
    
    // WebGL doesn't use these methods directly, DataGame handles WebGL reads.
    public Task<object> ReadDatabase(string child, string id) => Task.FromResult<object>(null);
    public Task<List<TotalPoint>> GetTop10TotalPoint() => Task.FromResult(new List<TotalPoint>());
    public Task<int> GetMyRank() => Task.FromResult(0);
    public Task<List<CurrentLevel>> GetTop10Level() => Task.FromResult(new List<CurrentLevel>());
    public Task<int> GetMyLevelRank() => Task.FromResult(0);
    public Task<List<Level>> GetTop10Level(string levelName) => Task.FromResult(new List<Level>());
    public Task<int> GetMyLevelRank(string levelName) => Task.FromResult(-1);
    public Task<List<Level>> GetTop10CommunityMap(string mapId) => Task.FromResult(new List<Level>());
    public Task<int> GetMyCommunityMapRank(string mapId) => Task.FromResult(-1);
    public Task<Level> GetMyCommunityMapScore(string mapId) => Task.FromResult<Level>(null);
#endif
}
