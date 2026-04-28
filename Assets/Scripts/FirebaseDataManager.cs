using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using UnityEngine;

public class FirebaseDataManager : MonoBehaviour
{
    public static FirebaseDataManager instance;
    private DatabaseReference reference;
    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
        FirebaseApp app = FirebaseApp.DefaultInstance;
        reference = FirebaseDatabase.DefaultInstance.RootReference;
    }
    public void WriteDatabase<T>(string child, string id, T data)
    {
        // Nếu là kiểu đơn giản → lưu trực tiếp
        if (data is int ||
            data is float ||
            data is double ||
            data is bool ||
            data is string ||
            data is long)
        {
            reference.Child(child)
                .Child(id)
                .SetValueAsync(data)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                        Debug.Log("Ghi dữ liệu thành công");
                    else
                        Debug.LogError("Lỗi: " + task.Exception);
                });

            return;
        }

        // Nếu là object/class → lưu dạng JSON object thật
        string json = JsonConvert.SerializeObject(
            data,
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include
            });

        reference.Child(child)
            .Child(id)
            .SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    Debug.Log("Ghi dữ liệu thành công");
                else
                    Debug.LogError("Lỗi: " + task.Exception);
            });
    }
    public async Task<DataSnapshot> ReadDatabase(string child, string id)
    {
        var snapshot = await reference.Child(child).Child(id).GetValueAsync();
        return snapshot;
    }
    public async Task<List<TotalPoint>> GetTop10TotalPoint()
    {
        List<TotalPoint> result = new List<TotalPoint>();

        try
        {
            DataSnapshot snapshot = await reference
                .Child("TotalPoint")
                .OrderByChild("point")
                .LimitToLast(10)
                .GetValueAsync();

            foreach (DataSnapshot child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();

                if (!string.IsNullOrEmpty(json))
                {
                    TotalPoint player =
                        JsonConvert.DeserializeObject<TotalPoint>(json);

                    if (player != null)
                    {
                        result.Add(player);
                    }
                }
            }

            // Firebase trả về từ thấp → cao nên đảo lại
            result = result
                .OrderByDescending(x => x.point)
                .ToList();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi lấy Top 10: " + e.Message);
        }

        return result;
    }
    public async Task<int> GetMyRank()
    {
        int rank = 0;

        try
        {
            string myUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

            DataSnapshot snapshot = await reference
                .Child("TotalPoint")
                .OrderByChild("point")
                .GetValueAsync();

            List<TotalPoint> players = new List<TotalPoint>();

            foreach (DataSnapshot child in snapshot.Children)
            {
                string uid = child.Key;
                string json = child.GetRawJsonValue();

                if (!string.IsNullOrEmpty(json))
                {
                    TotalPoint player =
                        JsonConvert.DeserializeObject<TotalPoint>(json);

                    if (player != null)
                    {
                        players.Add(player);

                        if (uid == myUid)
                        {
                            // lưu lại uid nếu cần
                        }
                    }
                }
            }

            // Firebase trả về tăng dần → đảo lại giảm dần
            players = players
                .OrderByDescending(x => x.point)
                .ToList();

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].name == DataGame.instance.totalPoint.name &&
                    players[i].point == DataGame.instance.totalPoint.point)
                {
                    rank = i + 1;
                    break;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi lấy rank: " + e);
        }

        return rank;
    }
    public async Task<List<CurrentLevel>> GetTop10Level()
    {
        List<CurrentLevel> result = new List<CurrentLevel>();

        try
        {
            DataSnapshot snapshot = await reference
                .Child("CurrentLevel")
                .GetValueAsync();

            foreach (DataSnapshot child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();

                if (!string.IsNullOrEmpty(json))
                {
                    CurrentLevel player =
                        JsonConvert.DeserializeObject<CurrentLevel>(json);

                    if (player != null)
                    {
                        result.Add(player);
                    }
                }
            }

            // Sắp xếp:
            // 1. level giảm dần (cao hơn đứng trước)
            // 2. time tăng dần (sớm hơn đứng trước)
            result = result
                .OrderByDescending(x => x.level)
                .ThenBy(x => x.time)
                .Take(10)
                .ToList();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi lấy Top Level: " + e);
        }

        return result;
    }
    public async Task<int> GetMyLevelRank()
    {
        int myRank = 0;

        try
        {
            string myUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

            DataSnapshot snapshot = await reference
                .Child("CurrentLevel")
                .GetValueAsync();

            List<(string uid, CurrentLevel data)> players =
                new List<(string, CurrentLevel)>();

            foreach (DataSnapshot child in snapshot.Children)
            {
                string uid = child.Key;
                string json = child.GetRawJsonValue();

                if (!string.IsNullOrEmpty(json))
                {
                    CurrentLevel player =
                        JsonConvert.DeserializeObject<CurrentLevel>(json);

                    if (player != null)
                    {
                        players.Add((uid, player));
                    }
                }
            }

            // Sắp xếp:
            // 1. level giảm dần
            // 2. time tăng dần (sớm hơn đứng trước)
            players = players
                .OrderByDescending(x => x.data.level)
                .ThenBy(x => x.data.time)
                .ToList();

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].uid == myUid)
                {
                    myRank = i + 1;
                    break;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi lấy rank bản thân: " + e);
        }

        return myRank;
    }
}
