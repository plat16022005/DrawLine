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
    public int CurrentLevel = 0;
    public int CurrentSkin = 0;
    public MySkin MySkin;
    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
        user = FirebaseAuth.DefaultInstance.CurrentUser;
        FindUsers();
        FindCurrentLevel();
        FindCurrentSkin();
        FindMySkin();
    }
    // async void Start()
    // {
        
    // }
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
    async void FindCurrentLevel()
    {
        DataSnapshot currentLevel = await FirebaseDataManager.instance.ReadDatabase("CurrentLevel", user.UserId);

        if (currentLevel == null || !currentLevel.Exists)
        {
            Debug.Log("Không tìm thấy dữ liệu");
            // isLoaded = true;
            return;
        }

        try 
        {
            string raw = currentLevel.GetRawJsonValue();
            string cleanJson = raw;

            if (raw.StartsWith("\"") && raw.EndsWith("\""))
            {
                cleanJson = JsonConvert.DeserializeObject<string>(raw);
            }

            CurrentLevel = JsonConvert.DeserializeObject<int>(cleanJson);

            Debug.Log("Load thành công! Level hiện tại: " + CurrentLevel);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi Parse Json: " + e.Message);
        }

        // isLoaded = true;
    }
    async void FindCurrentSkin()
    {
        DataSnapshot currentSkin = await FirebaseDataManager.instance.ReadDatabase("CurrentSkin", user.UserId);

        if (currentSkin == null || !currentSkin.Exists)
        {
            Debug.Log("Không tìm thấy dữ liệu");
            // isLoaded = true;
            return;
        }

        try 
        {
            string raw = currentSkin.GetRawJsonValue();
            string cleanJson = raw;

            if (raw.StartsWith("\"") && raw.EndsWith("\""))
            {
                cleanJson = JsonConvert.DeserializeObject<string>(raw);
            }

            CurrentSkin = JsonConvert.DeserializeObject<int>(cleanJson);

            Debug.Log("Load thành công! Skin hiện tại: " + CurrentSkin);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi Parse Json: " + e.Message);
        }

        // isLoaded = true;
    }
    async void FindMySkin()
    {
        DataSnapshot mySkin = await FirebaseDataManager.instance.ReadDatabase("MySkin", user.UserId);

        if (mySkin == null || !mySkin.Exists)
        {
            Debug.Log("Không tìm thấy dữ liệu");
            // isLoaded = true;
            return;
        }

        try 
        {
            string raw = mySkin.GetRawJsonValue();
            string cleanJson = raw;

            if (raw.StartsWith("\"") && raw.EndsWith("\""))
            {
                cleanJson = JsonConvert.DeserializeObject<string>(raw);
            }

            MySkin = JsonConvert.DeserializeObject<MySkin>(cleanJson);

            Debug.Log("Load thành công! Skin hiện tại: " + MySkin.myskin);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi Parse Json: " + e.Message);
        }       
    }
}
