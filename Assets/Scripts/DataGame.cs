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
    public CurrentLevel CurrentLevel;
    public int CurrentSkin = 0;
    public MySkin MySkin;
    public List<Level> levels;
    public TotalPoint totalPoint;
    private async void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
        user = FirebaseAuth.DefaultInstance.CurrentUser;
        FindUsers();
        await FindCurrentLevel();
        FindCurrentSkin();
        FindMySkin();
        await LoadAllLevel();
        LoadTotalPoint();
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
    async Task FindCurrentLevel()
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

            CurrentLevel = JsonConvert.DeserializeObject<CurrentLevel>(cleanJson);

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

            Debug.Log("Load thành công! Skin hiện tại: " + string.Join(",", MySkin.myskin));
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi Parse Json: " + e.Message);
        }       
    }
    async Task LoadAllLevel()
    {
        Debug.Log(CurrentLevel);
        for (int i = 1; i < CurrentLevel.level; i++)
        {
            Debug.Log("Lấy thông tin lv: " + i);
            Level level = await FindPointofLevel(i);
            levels.Add(level);
        }
    }
    async Task<Level> FindPointofLevel(int lv)
    {
        Level currentLevel = new Level();
        string level = "Lv" + lv.ToString();
        DataSnapshot myLevel = await FirebaseDataManager.instance.ReadDatabase(level, user.UserId);

        if (myLevel == null || !myLevel.Exists)
        {
            Debug.Log("Không tìm thấy dữ liệu");
            // isLoaded = true;
            return null;
        }

        try 
        {
            string raw = myLevel.GetRawJsonValue();
            string cleanJson = raw;

            if (raw.StartsWith("\"") && raw.EndsWith("\""))
            {
                cleanJson = JsonConvert.DeserializeObject<string>(raw);
            }

            currentLevel = JsonConvert.DeserializeObject<Level>(cleanJson);

            Debug.Log("Load thành công! Level đang được load hiện tại: " + currentLevel.level);
            return currentLevel;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi Parse Json: " + e.Message);
            return null;
        }             
    }
    async void LoadTotalPoint()
    {
        DataSnapshot myTotalPoint = await FirebaseDataManager.instance.ReadDatabase("TotalPoint", user.UserId);

        if (myTotalPoint == null || !myTotalPoint.Exists)
        {
            Debug.Log("Không tìm thấy dữ liệu");
            // isLoaded = true;
            return;
        }

        try 
        {
            string raw = myTotalPoint.GetRawJsonValue();
            string cleanJson = raw;

            if (raw.StartsWith("\"") && raw.EndsWith("\""))
            {
                cleanJson = JsonConvert.DeserializeObject<string>(raw);
            }

            totalPoint = JsonConvert.DeserializeObject<TotalPoint>(cleanJson);

            Debug.Log("Load thành công! Tổng điểm hiện tại: " + totalPoint.point);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi Parse Json: " + e.Message);
        }             
    }
}
