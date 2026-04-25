using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
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
    public void WriteDatabase (string child, string id, string message)
    {
        reference.Child(child).Child(id).SetValueAsync(message).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Ghi dữ liệu thành công");
            }
            else
            {
                Debug.Log("Ghi dữ liệu thất bại: " + task.Exception);
            }
        });
    }
    public async Task<DataSnapshot> ReadDatabase(string child, string id)
    {
        var snapshot = await reference.Child(child).Child(id).GetValueAsync();
        return snapshot;
    }
}
