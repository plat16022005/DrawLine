#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase.Auth;
using Firebase.Database;
#endif
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class MapCommentManager : MonoBehaviour
{
    public static MapCommentManager instance;
#if !UNITY_WEBGL || UNITY_EDITOR
    private DatabaseReference db;
    private FirebaseAuth auth;
#endif

    void Awake()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        db = FirebaseDatabase.DefaultInstance.RootReference;
        auth = FirebaseAuth.DefaultInstance;
#endif
        instance = this;
    }

    public void SendComment(string mapId, string playerName, string comment, int point)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        string uid = auth.CurrentUser.UserId;

        Dictionary<string, object> data = new Dictionary<string, object>();
        data["name"] = playerName;
        data["comment"] = comment;
        data["point"] = point;
        data["time"] = ServerValue.Timestamp;

        db.Child("MapComments")
          .Child(mapId)
          .Child(uid)
          .SetValueAsync(data);
        Debug.Log(data["comment"]);
#else
        if (FirebaseJSBridge.instance != null)
        {
            string uid = FirebaseJSBridge.instance.GetCurrentUserId();
            if (!string.IsNullOrEmpty(uid))
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                data["name"] = playerName;
                data["comment"] = comment;
                data["point"] = point;
                data["time"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // Tương đương ServerValue.Timestamp

                string json = JsonConvert.SerializeObject(data);
                FirebaseJSBridge.instance.WriteDatabaseAsync($"MapComments/{mapId}/{uid}", json);
                Debug.Log(data["comment"]);
            }
        }
#endif
    }

public async Task<List<CommentData>> LoadComments(string mapId)
{
    List<CommentData> comments = new();

#if !UNITY_WEBGL || UNITY_EDITOR
    DataSnapshot snapshot = await db.Child("MapComments")
                                    .Child(mapId)
                                    .GetValueAsync();

    foreach (DataSnapshot child in snapshot.Children)
    {
        comments.Add(new CommentData
        {
            uid = child.Key,
            name = child.Child("name").Value?.ToString(),
            comment = child.Child("comment").Value?.ToString(),
            point = int.Parse(child.Child("point").Value.ToString()),
            time = long.Parse(child.Child("time").Value.ToString())
        });
    }
#else
    if (FirebaseJSBridge.instance != null)
    {
        string json = await FirebaseJSBridge.instance.ReadDatabaseAsync($"MapComments/{mapId}");
        if (!string.IsNullOrEmpty(json))
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    var cData = kvp.Value;
                    comments.Add(new CommentData
                    {
                        uid = kvp.Key,
                        name = cData.ContainsKey("name") ? cData["name"]?.ToString() : null,
                        comment = cData.ContainsKey("comment") ? cData["comment"]?.ToString() : null,
                        point = cData.ContainsKey("point") ? Convert.ToInt32(cData["point"]) : 0,
                        time = cData.ContainsKey("time") ? Convert.ToInt64(cData["time"]) : 0
                    });
                }
            }
        }
    }
#endif

    // Sắp xếp mới nhất lên đầu
    comments.Sort((a, b) => b.time.CompareTo(a.time));

    return comments;
}

    public void DeleteMyComment(string mapId)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        string uid = auth.CurrentUser.UserId;

        db.Child("MapComments")
          .Child(mapId)
          .Child(uid)
          .RemoveValueAsync();
#else
        if (FirebaseJSBridge.instance != null)
        {
            string uid = FirebaseJSBridge.instance.GetCurrentUserId();
            if (!string.IsNullOrEmpty(uid))
            {
                FirebaseJSBridge.instance.RemoveDatabaseAsync($"MapComments/{mapId}/{uid}");
            }
        }
#endif
    }
    public async Task<bool> HasMyComment(string mapId)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        string uid = auth.CurrentUser.UserId;

        var snapshot = await FirebaseDatabase.DefaultInstance
            .RootReference
            .Child("MapComments")
            .Child(mapId)
            .Child(uid)
            .GetValueAsync();

        return snapshot.Exists;
#else
        if (FirebaseJSBridge.instance != null)
        {
            string uid = FirebaseJSBridge.instance.GetCurrentUserId();
            if (!string.IsNullOrEmpty(uid))
            {
                string json = await FirebaseJSBridge.instance.ReadDatabaseAsync($"MapComments/{mapId}/{uid}");
                return !string.IsNullOrEmpty(json);
            }
        }
        return false;
#endif
    }
}