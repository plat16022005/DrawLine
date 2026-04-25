using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using Newtonsoft.Json;
using UnityEngine;

public class DataGame : MonoBehaviour
{
    public static DataGame instance;
    private FirebaseUser user;
    public Users users = null;
    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
        user = FirebaseAuth.DefaultInstance.CurrentUser;
        FindUsers();
    }
    async void Start()
    {
        
    }
    // public bool isLoaded;
    async void FindUsers()
    {
        DataSnapshot dataUsers = await FirebaseDataManager.instance.ReadDatabase("Users", user.UserId);

        if (dataUsers == null || !dataUsers.Exists)
        {
            Debug.Log("Không tìm thấy dữ liệu");
            // isLoaded = true;
            return;
        }

        try 
        {
            string raw = dataUsers.GetRawJsonValue();
            string cleanJson = raw;

            if (raw.StartsWith("\"") && raw.EndsWith("\""))
            {
                cleanJson = JsonConvert.DeserializeObject<string>(raw);
            }

            users = JsonConvert.DeserializeObject<Users>(cleanJson);

            Debug.Log("Load thành công! Tên: " + users.name);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi Parse Json: " + e.Message);
        }

        // isLoaded = true;
    }
}
