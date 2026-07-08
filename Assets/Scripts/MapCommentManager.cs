using Firebase.Auth;
using Firebase.Database;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class MapCommentManager : MonoBehaviour
{
    public static MapCommentManager instance;
    private DatabaseReference db;
    private FirebaseAuth auth;

    void Awake()
    {
        db = FirebaseDatabase.DefaultInstance.RootReference;
        auth = FirebaseAuth.DefaultInstance;
        instance = this;
    }

    public void SendComment(string mapId, string playerName, string comment, int point)
    {
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
    }

public async Task<List<CommentData>> LoadComments(string mapId)
{
    List<CommentData> comments = new();

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

    // Sắp xếp mới nhất lên đầu
    comments.Sort((a, b) => b.time.CompareTo(a.time));

    return comments;
}

    public void DeleteMyComment(string mapId)
    {
        string uid = auth.CurrentUser.UserId;

        db.Child("MapComments")
          .Child(mapId)
          .Child(uid)
          .RemoveValueAsync();
    }
    public async Task<bool> HasMyComment(string mapId)
    {
        string uid = auth.CurrentUser.UserId;

        var snapshot = await FirebaseDatabase.DefaultInstance
            .RootReference
            .Child("MapComments")
            .Child(mapId)
            .Child(uid)
            .GetValueAsync();

        return snapshot.Exists;
    }
}