using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase.Auth;
using Firebase.Database;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class SelectLevelManager : MonoBehaviour
{
    public AudioSource AudioSource;
    public AudioClip[] SoundClip;
    [SerializeField] private Sprite[] spritesSkin;
    [SerializeField] private String[] namesSkin;
    [SerializeField] private int[] costSkin;

    private string currentUserId; // Dùng chung cho cả 2 nền tảng

    [Header("Panel Set Name")]
    public GameObject PanelSetName;
    public TMP_InputField Name;
    [Header("Panel Select Level")]
    public TextMeshProUGUI NamePlayer;
    public TextMeshProUGUI Coin;
    [Header("Level")]
    public Transform ContentLevel;
    [Header("Panel Shop")]
    public GameObject PanelShop;
    public Image CurrentSkin;
    public TextMeshProUGUI NameSkin;
    public Transform ContentSkin;
    public TextMeshProUGUI notificationSkin;
    [Header("Panel Rank")]
    public GameObject PanelRank;
    public Transform ContentRank;
    public GameObject RankPrefabs;
    public TextMeshProUGUI RankPlayer;
    public TextMeshProUGUI NamePlayerRank;
    public TextMeshProUGUI PointPlayerRank;
    [Header("Panel Level Detail")]
    public GameObject PanelLevelDetail;
    public Image ImageLevelDetail;
    public Sprite[] ImageDetail;
    public Image Star1Level;
    public Image Star2Level;
    public Image Star3Level;
    public TextMeshProUGUI PointLevel;
    public Button EnterLevelButton;
    public Transform ContentLevelDetail;
    public GameObject LevelDetailPrefab;
    public TextMeshProUGUI XHLevel;
    public TextMeshProUGUI NamePlayerLevel;
    public TextMeshProUGUI PointPlayerLevel;
    [Header("Panel Answer")]
    public GameObject PanelAnswer;
    [Header("Panel Tutorial")]
    public GameObject PanelTutorial;
    public Transform ButtonsPlayVideo;
    public GameObject PanelPlayVideo;
    public VideoPlayer videoPlayer;
    public VideoClip[] videoClip;
    public TextMeshProUGUI DetailVideo;
    public string[] Details;
    [Header("Panel Ask Tutorial")]
    public GameObject PanelAskTutorial;
    [Header("Map")]
    public GameObject GameObjectMap;
    public GameObject GameObjectLevel;
    public FirebaseMapSaver firebaseMapSaver;
    public TMP_InputField MapNameInput;
    public TextMeshProUGUI MapNotificationText;
    
    [Header("Map List UI")]
    public Transform ContentMapList;
    public GameObject MapButtonPrefab;

    [Header("Panel Detail Map")]
    public GameObject PanelDetailMap;
    public TextMeshProUGUI DetailMapName;
    public TMP_Dropdown DetailMapStatusDropdown;
    // Dropdown thứ tự: 0=Publish, 1=Private, 2=Maintenance

    [Header("Panel Detail Map Community")]
    public GameObject PanelDetailMapCommunity;
    public TextMeshProUGUI NameMapCommunity;
    public Button PlayNowCommunityButton;

    [Header("Tìm kiếm Map Community")]
    public TMP_InputField SearchMapNameInput;
    public TMP_InputField SearchOwnerNameInput;

    [Header("Panel Thông Báo")]
    public GameObject BangThongBao;
    public TextMeshProUGUI TextThongBao;

    [Header("Panel Xác Nhận Xóa")]
    public GameObject PanelConfirmDelete;
    public TextMeshProUGUI ConfirmDeleteText;
    // Gán nút "Xác nhận" → ConfirmDeleteMap()
    // Gán nút "Hủy"      → CancelDeleteMap()

    private string _selectedMapId;
    private string _selectedMapName;
    private string _selectedMapStatus; // track status hiện tại để revert dropdown nếu cần

    // Cache cho tìm kiếm map community
    private struct CommunityMapEntry
    {
        public string mapId;
        public string mapName;
        public string ownerId;
        public string ownerName; // resolved async
    }
    private List<CommunityMapEntry> _cachedCommunityMaps = new List<CommunityMapEntry>();

    private void Awake()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null) currentUserId = user.UserId;
#else
        if (FirebaseJSBridge.instance != null) currentUserId = FirebaseJSBridge.instance.GetCurrentUserId();
#endif
    }

    void Start()
    {
        Name.characterLimit = 15;
        Name.onValueChanged.AddListener(ValidateName);

        if (DataGame.instance.users != null && DataGame.instance.users.name == "")
        {
            PanelSetName.SetActive(true);
        }
        else if (DataGame.instance.users != null)
        {
            NamePlayer.text = DataGame.instance.users.name;
            Coin.text = DataGame.instance.users.coin.ToString();
        }

        if (DataGame.instance.CurrentLevel != null && DataGame.instance.CurrentLevel.level > 0)
        {
            LoadLevel();
        }

        if (DataGame.instance.Tutorial == false)
        {
            PanelAskTutorial.SetActive(true);
        }
    }

    void ValidateName(string value)
    {
        string filtered = Regex.Replace(value, @"[^a-zA-Z0-9À-ỹ\s]", "");
        if (filtered != value) Name.text = filtered;
    }

    void LoadLevel()
    {
        int current = 1;
        foreach (Transform item in ContentLevel)
        {
            Transform bd = item.Find("Bd");
            Transform lockObj = item.Find("Lock");
            TextMeshProUGUI textLv = bd?.Find("LV")?.GetComponent<TextMeshProUGUI>();
            Image star1 = bd?.Find("1")?.GetComponent<Image>();
            Image star2 = bd?.Find("2")?.GetComponent<Image>();
            Image star3 = bd?.Find("3")?.GetComponent<Image>();

            bool isUnlocked = current <= DataGame.instance.CurrentLevel.level;
            if (bd != null) bd.gameObject.SetActive(isUnlocked);
            if (lockObj != null) lockObj.gameObject.SetActive(!isUnlocked);
            if (textLv != null) textLv.text = current.ToString();

            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                int levelIndex = current;
                btn.interactable = isUnlocked;
                btn.onClick.AddListener(() => OpenPanelDetailLevel(levelIndex));
            }

            if (star1 != null) star1.color = Color.black;
            if (star2 != null) star2.color = Color.black;
            if (star3 != null) star3.color = Color.black;

            if (current != DataGame.instance.CurrentLevel.level)
            {
                string levelName = "Lv" + current;
                Level lvData = DataGame.instance.levels.Find(l => l != null && l.level == levelName);
                if (lvData != null)
                {
                    if (lvData.star >= 1 && star1 != null) star1.color = Color.white;
                    if (lvData.star >= 2 && star2 != null) star2.color = Color.white;
                    if (lvData.star >= 3 && star3 != null) star3.color = Color.white;
                }
            }
            current++;
            if (current > DataGame.instance.CurrentLevel.level) break;
        }
    }

    void EnterLevel(int level)
    {
        SceneManager.LoadScene("Lv" + level);
    }

    public void SetName()
    {
        if (string.IsNullOrEmpty(currentUserId)) return;

        Users users = new Users(Name.text, 0);
        MySkin myskin = new MySkin(new List<int>(){-1});
        TotalPoint totalPoint = new TotalPoint(Name.text, 0);
        CurrentLevel currentLevel = new CurrentLevel(Name.text, 1);

        DataGame.instance.users = users;
        NamePlayer.text = Name.text;
        Coin.text = users.coin.ToString();
        PanelSetName.SetActive(false);

        // Ghi dữ liệu (Hàm WriteDatabase trong FirebaseDataManager đã tự xử lý WebGL/Native)
        FirebaseDataManager.instance.WriteDatabase("Users", currentUserId, users);
        FirebaseDataManager.instance.WriteDatabase("CurrentLevel", currentUserId, currentLevel);
        FirebaseDataManager.instance.WriteDatabase("CurrentSkin", currentUserId, 0);
        FirebaseDataManager.instance.WriteDatabase("MySkin", currentUserId, myskin);
        FirebaseDataManager.instance.WriteDatabase("TotalPoint", currentUserId, totalPoint);

        DataGame.instance.CurrentLevel = currentLevel;
        DataGame.instance.CurrentSkin = 0;
        DataGame.instance.MySkin = myskin;
        DataGame.instance.totalPoint = totalPoint;
        
        AudioSource.PlayOneShot(SoundClip[1]);
        LoadLevel();
    }

    public void OpenShop()
    {
        PanelShop.SetActive(true);
        CurrentSkin.sprite = spritesSkin[DataGame.instance.CurrentSkin];
        NameSkin.text = namesSkin[DataGame.instance.CurrentSkin];
        ResetShop();
    }

    void ResetShop()
    {
        notificationSkin.text = "";
        foreach (Transform item in ContentSkin)
        {
            int index = item.GetSiblingIndex() + 1;
            Image img = item.Find("Image")?.GetComponent<Image>();
            TextMeshProUGUI txtCost = item.Find("Buy/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            Button btnBuy = item.Find("Buy")?.GetComponent<Button>();
            int tempIndex = index;
            btnBuy.onClick.RemoveAllListeners();
            btnBuy.onClick.AddListener(() => BuySkin(tempIndex));
            img.sprite = spritesSkin[index];
            txtCost.text = costSkin[index].ToString();            
        }

        if (DataGame.instance.MySkin == null || DataGame.instance.MySkin.myskin == null) return;

        foreach (int index in DataGame.instance.MySkin.myskin)
        {
            if (index <= 0) continue;
            Transform item = ContentSkin.GetChild(index - 1);
            Image img = item.Find("Image")?.GetComponent<Image>();
            TextMeshProUGUI txtCost = item.Find("Buy/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            Button btnBuy = item.Find("Buy")?.GetComponent<Button>();
            Button btnEquip = item.Find("Equip")?.GetComponent<Button>();
            Button btnCancel = item.Find("Cancel")?.GetComponent<Button>();

            btnBuy.gameObject.SetActive(false);
            if (DataGame.instance.CurrentSkin == index)
            {
                btnEquip.gameObject.SetActive(false);
                btnCancel.gameObject.SetActive(true);
                btnCancel.onClick.RemoveAllListeners();
                btnCancel.onClick.AddListener(CancelSkin);
            }
            else
            {
                btnEquip.gameObject.SetActive(true);
                btnEquip.onClick.RemoveAllListeners();
                btnEquip.onClick.AddListener(() => EquipSkin(index));
                btnCancel.gameObject.SetActive(false);                
            }
            img.sprite = spritesSkin[index];
            txtCost.text = costSkin[index].ToString();
        }        
    }

    void BuySkin(int index)
    {
        if (string.IsNullOrEmpty(currentUserId)) return;
        if (DataGame.instance.MySkin.myskin.Contains(index)) return;

        if (DataGame.instance.users.coin >= costSkin[index])
        {
            DataGame.instance.users.coin -= costSkin[index];
            DataGame.instance.MySkin.myskin.Add(index);

            FirebaseDataManager.instance.WriteDatabase("Users", currentUserId, DataGame.instance.users);
            FirebaseDataManager.instance.WriteDatabase("MySkin", currentUserId, DataGame.instance.MySkin);

            ResetShop();
            notificationSkin.text = "Bạn đã mua thành công skin: " + namesSkin[index];
            notificationSkin.color = Color.green;
            notificationSkin.gameObject.SetActive(true);
            Coin.text = DataGame.instance.users.coin.ToString();
            AudioSource.PlayOneShot(SoundClip[1]);
        }
        else
        {
            notificationSkin.text = "Bạn không đủ tiền để mua skin này!";
            notificationSkin.color = Color.red;
            notificationSkin.gameObject.SetActive(true);
            AudioSource.PlayOneShot(SoundClip[0]);
        }
    }

    void EquipSkin(int index)
    {
        if (string.IsNullOrEmpty(currentUserId)) return;
        if (DataGame.instance.MySkin.myskin.Contains(index))
        {
            DataGame.instance.CurrentSkin = index;
            CurrentSkin.sprite = spritesSkin[index];
            FirebaseDataManager.instance.WriteDatabase("CurrentSkin", currentUserId, index);
            NameSkin.text = namesSkin[index];
            ResetShop();
            notificationSkin.text = "Bạn đang mặc skin: " + namesSkin[index];
            notificationSkin.color = Color.green;
            notificationSkin.gameObject.SetActive(true);
        }
    }

    void CancelSkin()
    {
        if (string.IsNullOrEmpty(currentUserId)) return;
        DataGame.instance.CurrentSkin = 0;
        CurrentSkin.sprite = spritesSkin[0];
        FirebaseDataManager.instance.WriteDatabase("CurrentSkin", currentUserId, 0);
        NameSkin.text = namesSkin[0];
        ResetShop();
        notificationSkin.text = "Bạn đã hủy mặc skin và trở về mặc định";
        notificationSkin.color = Color.green;
        notificationSkin.gameObject.SetActive(true);
    }

    public void CloseShop() { PanelShop.SetActive(false); }

    public void OpenRankPanel() { PanelRank.SetActive(true); LoadPointRank(); }

    public async void LoadPointRank()
    {
        foreach (Transform item in ContentRank) Destroy(item.gameObject);
        
        await DataGame.instance.LoadTotalPointRank();
        int currentRantPlayer = 0;
        if (DataGame.instance.TotalPointRank != null)
        {
            foreach (TotalPoint player in DataGame.instance.TotalPointRank)
            {
                currentRantPlayer++;
                GameObject obj = Instantiate(RankPrefabs, ContentRank);
                obj.transform.Find("XH").GetComponent<TextMeshProUGUI>().text = currentRantPlayer.ToString();
                obj.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = player.name;
                obj.transform.Find("Point").GetComponent<TextMeshProUGUI>().text = player.point.ToString();
            }
        }
        int myRank = await DataGame.instance.FindMyRank();
        RankPlayer.text = myRank.ToString();
        NamePlayerRank.text = DataGame.instance.users.name;
        PointPlayerRank.text = DataGame.instance.totalPoint != null ? DataGame.instance.totalPoint.point.ToString() : "0";
    }

    public async void LoadLevelRank()
    {
        foreach (Transform item in ContentRank) Destroy(item.gameObject);
        await DataGame.instance.LoadLevelRank();
        int currentRantPlayer = 0;
        if (DataGame.instance.LevelRank != null)
        {
            foreach (CurrentLevel level in DataGame.instance.LevelRank)
            {
                currentRantPlayer++;
                GameObject obj = Instantiate(RankPrefabs, ContentRank);
                obj.transform.Find("XH").GetComponent<TextMeshProUGUI>().text = currentRantPlayer.ToString();
                obj.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = level.name;
                obj.transform.Find("Point").GetComponent<TextMeshProUGUI>().text = "LV." + (level.level - 1).ToString();
            }
        }
        int myRank = await DataGame.instance.FindMyLevelRank();
        RankPlayer.text = myRank.ToString();
        NamePlayerRank.text = DataGame.instance.users.name;
        PointPlayerRank.text = "LV." + (DataGame.instance.CurrentLevel.level - 1).ToString();        
    }

    public void CloseRankPanel() { PanelRank.SetActive(false); }

    void OpenPanelDetailLevel(int level)
    {
        PanelLevelDetail.SetActive(true);
        Level result = DataGame.instance.levels.Find(l => l.level == "Lv" + level);
        EnterLevelButton.onClick.RemoveAllListeners();
        EnterLevelButton.onClick.AddListener(() => EnterLevel(level));

        if (result != null)
        {
            ImageLevelDetail.sprite = ImageDetail[0];
            Star1Level.color = result.star >= 1 ? Color.white : Color.black;
            Star2Level.color = result.star >= 2 ? Color.white : Color.black;
            Star3Level.color = result.star >= 3 ? Color.white : Color.black;
            PointLevel.text = result.point.ToString();
        }
        else
        {
            ImageLevelDetail.sprite = ImageDetail[1];
            Star1Level.color = Star2Level.color = Star3Level.color = Color.black;
            PointLevel.text = "0";
        }
        LoadLevelXRank(level);
    }

    public async void LoadLevelXRank(int lv)
    {
        foreach (Transform item in ContentLevelDetail) Destroy(item.gameObject);
        await DataGame.instance.LoadTop10Level(lv);
        int currentRantPlayer = 0;
        if (DataGame.instance.LvXRank != null)
        {
            foreach (Level level in DataGame.instance.LvXRank)
            {
                currentRantPlayer++;
                GameObject obj = Instantiate(LevelDetailPrefab, ContentLevelDetail);
                obj.transform.Find("XH").GetComponent<TextMeshProUGUI>().text = currentRantPlayer.ToString();
                obj.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = level.namePlayer;
                obj.transform.Find("Point").GetComponent<TextMeshProUGUI>().text = level.point.ToString();
            }
        }
        int myRank = await DataGame.instance.FindMyLevelXRank(lv);
        Level myLevel = DataGame.instance.levels.Find(l => l.level == "Lv" + lv.ToString());
        XHLevel.text = myRank > 0 ? myRank.ToString() : "???";
        NamePlayerLevel.text = DataGame.instance.users.name;
        PointPlayerLevel.text = myLevel != null ? myLevel.point.ToString() : "0";        
    }

    public void ClosePanelDetailLevel() { PanelLevelDetail.SetActive(false); }
    public void OpenPanelAnswer() { PanelAnswer.SetActive(true); }
    public void ClosePanelAnswer() { PanelAnswer.SetActive(false); }
    public void OpenPanelTutorial()
    {
        PanelTutorial.SetActive(true);
        int currentVideo = 0;
        foreach (Transform item in ButtonsPlayVideo)
        {
            Button btn = item.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            int videoIndex = currentVideo;
            btn.onClick.AddListener(() => OpenTutorial(videoIndex));
            currentVideo++;
        }
    }
    public void ClosePanelTutorial() { PanelTutorial.SetActive(false); }
    public void OpenTutorial(int index)
    {
        if (index < 0 || index >= videoClip.Length) return;
        PanelPlayVideo.SetActive(true);
        videoPlayer.Stop();
        videoPlayer.clip = videoClip[index];
        videoPlayer.Play();
        DetailVideo.text = Details[index];
    }
    public void CloseTutorial() { videoPlayer.Stop(); PanelPlayVideo.SetActive(false); }
    public void AcceptTutorial() { SceneManager.LoadScene("Tutorial1"); }
    public void DeniedTutorial()
    {
        if (string.IsNullOrEmpty(currentUserId)) return;
        DataGame.instance.Tutorial = true;
        FirebaseDataManager.instance.WriteDatabase("Tutorial", currentUserId, true);
        PanelAskTutorial.gameObject.SetActive(false);
    }
    public async void CreateNewMap()
    {
        if (firebaseMapSaver == null)
        {
            Debug.LogError("Chưa gán FirebaseMapSaver vào SelectLevelManager!");
            return;
        }

        string mapName = MapNameInput != null ? MapNameInput.text.Trim() : "";

        if (string.IsNullOrWhiteSpace(mapName))
        {
            if (MapNotificationText != null)
            {
                MapNotificationText.color = Color.red;
                MapNotificationText.text = "Vui lòng nhập tên map!";
            }
            return;
        }

        string mapId = await firebaseMapSaver.CreateMap(mapName);

        if (!string.IsNullOrEmpty(mapId))
        {
            if (MapNotificationText != null)
            {
                MapNotificationText.color = Color.green;
                MapNotificationText.text = $"Tạo map \"{ mapName}\" thành công!";
            }
            Debug.Log("Map mới được tạo với ID: " + mapId);
            if (DataGame.instance != null)
            {
                DataGame.instance.currentEditMapId = mapId;
            }
            SceneManager.LoadScene("MakeMap");
        }
        else
        {
            if (MapNotificationText != null)
            {
                MapNotificationText.color = Color.red;
                MapNotificationText.text = "Tạo map thất bại. Vui lòng thử lại!";
            }
        }
    }

    public void OpenMap()
    {
        GameObjectLevel.SetActive(false);
        GameObjectMap.SetActive(true);
        LoadCommunityMaps();
    }
    private async void LoadCreatorName(string ownerId, TextMeshProUGUI txtChuMap)
    {
        if (string.IsNullOrEmpty(ownerId) || txtChuMap == null) return;
#if !UNITY_WEBGL || UNITY_EDITOR
        try
        {
            DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            DataSnapshot snapshot = await dbRef.Child("Users").Child(ownerId).Child("name").GetValueAsync();
            if (snapshot.Exists && txtChuMap != null)
            {
                txtChuMap.text = snapshot.Value.ToString();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Community] Error loading creator name: " + e);
        }
#endif
    }

    /// <summary>
    /// Load tất cả map community có status=publish từ mapscommunity.
    /// Tạo button cho mỗi map — khi nhấn sẽ chuyển sang scene LvMap.
    /// </summary>
    public async void LoadCommunityMaps()
    {
        if (ContentMapList == null || MapButtonPrefab == null) return;

        foreach (Transform child in ContentMapList)
            Destroy(child.gameObject);

        _cachedCommunityMaps.Clear();

#if !UNITY_WEBGL || UNITY_EDITOR
        try
        {
            DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.RootReference;

            DataSnapshot snapshot = await dbRef.Child("mapscommunity")
                .OrderByChild("status").EqualTo("publish").GetValueAsync();

            if (snapshot == null || !snapshot.Exists)
            {
                Debug.Log("[Community] Chưa có map nào được publish.");
                return;
            }

            // Bước 1: Thu thập tất cả mapId + mapName + ownerId
            var entries = new List<CommunityMapEntry>();
            var ownerTasks = new List<Task<DataSnapshot>>();

            foreach (DataSnapshot child in snapshot.Children)
            {
                var entry = new CommunityMapEntry
                {
                    mapId   = child.Key,
                    mapName = child.HasChild("mapName") ? child.Child("mapName").Value.ToString() : "Unnamed",
                    ownerId = child.HasChild("ownerId") ? child.Child("ownerId").Value.ToString() : "",
                    ownerName = ""
                };
                entries.Add(entry);

                if (!string.IsNullOrEmpty(entry.ownerId))
                    ownerTasks.Add(dbRef.Child("Users").Child(entry.ownerId).Child("name").GetValueAsync());
                else
                    ownerTasks.Add(Task.FromResult<DataSnapshot>(null));
            }

            // Bước 2: Resolve tất cả owner name song song
            await Task.WhenAll(ownerTasks);

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var snap = ownerTasks[i].Result;
                if (snap != null && snap.Exists)
                    e.ownerName = snap.Value.ToString();
                entries[i] = e;
            }

            _cachedCommunityMaps = entries;
            RenderCommunityMapList(_cachedCommunityMaps);
        }
        catch (Exception e)
        {
            Debug.LogError("[Community] Lỗi load community maps: " + e);
        }
#else
        Debug.LogWarning("[Community] LoadCommunityMaps chưa hỗ trợ WebGL native.");
#endif
    }

    /// <summary>
    /// Tìm kiếm map theo tên map và/hoặc tên chủ map (partial, case-insensitive).
    /// Nếu cả 2 đều trống, hiển thị toàn bộ.
    /// Gọn vào Button Tìm kiếm trong Inspector.
    /// </summary>
    public void SearchCommunityMaps()
    {
        string mapNameQuery  = SearchMapNameInput  != null ? SearchMapNameInput.text.Trim().ToLower()  : "";
        string ownerQuery    = SearchOwnerNameInput != null ? SearchOwnerNameInput.text.Trim().ToLower() : "";

        bool mapEmpty   = string.IsNullOrEmpty(mapNameQuery);
        bool ownerEmpty = string.IsNullOrEmpty(ownerQuery);

        List<CommunityMapEntry> results = _cachedCommunityMaps.FindAll(entry =>
        {
            bool mapMatch   = mapEmpty   || entry.mapName.ToLower().Contains(mapNameQuery);
            bool ownerMatch = ownerEmpty || entry.ownerName.ToLower().Contains(ownerQuery);
            return mapMatch && ownerMatch;
        });

        RenderCommunityMapList(results);
    }

    private void RenderCommunityMapList(List<CommunityMapEntry> list)
    {
        foreach (Transform child in ContentMapList)
            Destroy(child.gameObject);

        foreach (var entry in list)
        {
            GameObject btnObj = Instantiate(MapButtonPrefab, ContentMapList);

            Transform tenMapTrans = btnObj.transform.Find("TenMap");
            if (tenMapTrans != null)
            {
                TextMeshProUGUI txtName = tenMapTrans.GetComponent<TextMeshProUGUI>();
                if (txtName != null) txtName.text = entry.mapName;
            }
            else
            {
                TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = entry.mapName;
            }

            Transform chuMapTrans = btnObj.transform.Find("ChuMap");
            if (chuMapTrans != null)
            {
                TextMeshProUGUI txtChuMap = chuMapTrans.GetComponent<TextMeshProUGUI>();
                if (txtChuMap != null)
                    txtChuMap.text = string.IsNullOrEmpty(entry.ownerName) ? "..." : entry.ownerName;

                Transform statusTrans = chuMapTrans.Find("TrangThai");
                if (statusTrans != null) statusTrans.gameObject.SetActive(false);
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                string cId   = entry.mapId;
                string cName = entry.mapName;
                btn.onClick.AddListener(() => OpenCommunityMapDetail(cId, cName));
            }
        }
    }


    public async void LoadUserMaps()
    {
        if (ContentMapList == null || MapButtonPrefab == null) return;

        // Xóa danh sách cũ
        foreach (Transform child in ContentMapList)
        {
            Destroy(child.gameObject);
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        try
        {
            string userId = string.IsNullOrEmpty(currentUserId) ? "guest_maps" : currentUserId;
            DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.RootReference;

            // Bước 1: Lấy danh sách map từ maps/<userId>
            DataSnapshot snapshot = await dbRef.Child("maps").Child(userId).GetValueAsync();
            Debug.Log(userId);
            if (snapshot != null && snapshot.Exists)
            {
                // Bước 2: Đọc song song status từ mapscommunity cho từng mapId
                var mapIds   = new List<string>();
                var mapNames = new List<string>();
                var statusTasks = new List<Task<DataSnapshot>>();

                foreach (DataSnapshot child in snapshot.Children)
                {
                    string mapId   = child.Key;
                    string mapName = child.HasChild("mapName") ? child.Child("mapName").Value.ToString() : "Unnamed Map";
                    mapIds.Add(mapId);
                    mapNames.Add(mapName);
                    statusTasks.Add(dbRef.Child("mapscommunity").Child(mapId).Child("status").GetValueAsync());
                }

                await Task.WhenAll(statusTasks);

                // Bước 3: Tạo button cho từng map
                for (int i = 0; i < mapIds.Count; i++)
                {
                    string mapId     = mapIds[i];
                    string mapName   = mapNames[i];
                    string mapStatus = statusTasks[i].Result != null && statusTasks[i].Result.Exists
                        ? statusTasks[i].Result.Value.ToString()
                        : "private";

                    string statusIcon = mapStatus switch
                    {
                        "publish"     => "Công khai",
                        "maintenance" => "Bảo trì",
                        _             => "Bảo mật"
                    };

                    GameObject btnObj = Instantiate(MapButtonPrefab, ContentMapList);

                    Transform tenMapTrans = btnObj.transform.Find("TenMap");
                    if (tenMapTrans != null)
                    {
                        TextMeshProUGUI txtName = tenMapTrans.GetComponent<TextMeshProUGUI>();
                        if (txtName != null) txtName.text = mapName;
                    }
                    else
                    {
                        TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                        if (txt != null) txt.text = $"{mapName}";
                    }

                    Transform chuMapTrans = btnObj.transform.Find("ChuMap");
                    if (chuMapTrans != null)
                    {
                        // TextMeshProUGUI txtChuMap = chuMapTrans.GetComponent<TextMeshProUGUI>();
                        // if (txtChuMap != null)
                        // {
                        //     txtChuMap.text = DataGame.instance.users != null ? DataGame.instance.users.name : "Me";
                        // }

                        // Transform statusTrans = chuMapTrans.Find("TrangThai");
                        // if (statusTrans != null)
                        // {
                        //     statusTrans.gameObject.SetActive(true);
                        //     TextMeshProUGUI txtStatus = statusTrans.GetComponent<TextMeshProUGUI>();
                        //     if (txtStatus != null) txtStatus.text = statusIcon;
                        chuMapTrans.gameObject.SetActive(false);
                    }                        
                    Transform statusTrans = btnObj.transform.Find("TrangThai");
                    if (statusTrans != null)
                    {
                        statusTrans.gameObject.SetActive(true);
                        TextMeshProUGUI txtStatus = statusTrans.GetComponent<TextMeshProUGUI>();
                        if (txtStatus != null) txtStatus.text = statusIcon;
                        txtStatus.color = mapStatus switch
                        {
                            "publish"     => Color.green,
                            "maintenance" => Color.yellow,
                            _             => Color.red
                        };
                    }

                    // Khi click → mở Panel Detail
                    Button btn = btnObj.GetComponent<Button>();
                    if (btn != null)
                    {
                        string cId     = mapId;
                        string cName   = mapName;
                        string cStatus = mapStatus;
                        btn.onClick.AddListener(() => OpenMapDetail(cId, cName, cStatus));
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Lỗi khi load danh sách map: " + e);
        }
#else
        Debug.LogWarning("LoadUserMaps chưa được hỗ trợ hoàn toàn trên WebGL native.");
#endif
    }
    public void OpenLevel()
    {
        GameObjectMap.SetActive(false);
        GameObjectLevel.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────
    // Panel Detail Map
    // ─────────────────────────────────────────────────────────

    void OpenMapDetail(string mapId, string mapName, string mapStatus)
    {
        _selectedMapId     = mapId;
        _selectedMapName   = mapName;
        _selectedMapStatus = mapStatus;

        if (PanelDetailMap != null) PanelDetailMap.SetActive(true);
        if (DetailMapName  != null) DetailMapName.text = mapName;

        // Cập nhật dropdown theo trạng thái hiện tại từ mapscommunity
        // Thứ tự dropdown: 0=Publish, 1=Private, 2=Maintenance
        if (DetailMapStatusDropdown != null)
        {
            int dropdownIndex = mapStatus switch
            {
                "publish"     => 0,
                "maintenance" => 2,
                _             => 1 // private
            };

            // Gán giá trị trước khi add listener để tránh trigger
            DetailMapStatusDropdown.onValueChanged.RemoveAllListeners();
            DetailMapStatusDropdown.value = dropdownIndex;
            DetailMapStatusDropdown.RefreshShownValue();

            // Người chơi chỉ được chọn Private / Maintenance — KHÔNG được chọn Publish từ dropdown
            DetailMapStatusDropdown.onValueChanged.AddListener(OnDropdownStatusChanged);
        }
    }

    /// <summary>
    /// Xử lý khi người chơi đổi dropdown trạng thái.
    /// Chỉ cho phép Private (1) và Maintenance (2).
    /// Publish (0) chỉ được đặt qua nút "Đăng tải".
    /// </summary>
    async void OnDropdownStatusChanged(int newIndex)
    {
        // Ngăn chọn Publish từ dropdown
        if (newIndex == 0)
        {
            // Revert về trạng thái hợp lệ trước đó
            int revert = _selectedMapStatus switch
            {
                "maintenance" => 2,
                _             => 1 // private
            };
            DetailMapStatusDropdown.onValueChanged.RemoveAllListeners();
            DetailMapStatusDropdown.value = revert;
            DetailMapStatusDropdown.RefreshShownValue();
            DetailMapStatusDropdown.onValueChanged.AddListener(OnDropdownStatusChanged);
            Debug.Log("[DetailMap] Không thể chọn Publish từ dropdown. Dùng nút Đăng tải!");
            return;
        }

        string newStatus = newIndex == 2 ? "maintenance" : "private";
        _selectedMapStatus = newStatus;

#if !UNITY_WEBGL || UNITY_EDITOR
        try
        {
            DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            await dbRef.Child("mapscommunity").Child(_selectedMapId).Child("status").SetValueAsync(newStatus);
            Debug.Log($"[DetailMap] Cập nhật status mapscommunity/{_selectedMapId} → {newStatus}");
        }
        catch (Exception e)
        {
            Debug.LogError("[DetailMap] Lỗi cập nhật status: " + e);
        }
#endif
    }

    public void CloseMapDetail()
    {
        if (PanelDetailMap != null) PanelDetailMap.SetActive(false);
    }

    public void OpenCommunityMapDetail(string mapId, string mapName)
    {
        if (PanelDetailMapCommunity != null) PanelDetailMapCommunity.SetActive(true);
        if (NameMapCommunity != null) NameMapCommunity.text = mapName;

        if (PlayNowCommunityButton != null)
        {
            PlayNowCommunityButton.onClick.RemoveAllListeners();
            PlayNowCommunityButton.onClick.AddListener(() => 
            {
                if (DataGame.instance != null)
                    DataGame.instance.currentCommunityMapId = mapId;
                SceneManager.LoadScene("LvMap");
            });
        }
    }

    public void CloseCommunityMapDetail()
    {
        if (PanelDetailMapCommunity != null) PanelDetailMapCommunity.SetActive(false);
    }

    /// <summary>Nút "Chỉnh sửa" — vào scene MakeMap với map đang chọn.</summary>
    public void OnDetailEditMap()
    {
        if (string.IsNullOrEmpty(_selectedMapId)) return;
        if (DataGame.instance != null)
            DataGame.instance.currentEditMapId = _selectedMapId;
        SceneManager.LoadScene("MakeMap");
    }

    /// <summary>
    /// Nút "Đăng tải" — copy toàn bộ dữ liệu từ maps/<userId>/<mapId>
    /// vào mapscommunity/<mapId> với status="publish".
    /// Đây là cách DUY NHẤT để map lên trạng thái Publish.
    /// </summary>
    public async void OnDetailSaveStatus()
    {
        if (string.IsNullOrEmpty(_selectedMapId)) return;

#if !UNITY_WEBGL || UNITY_EDITOR
        try
        {
            string userId = string.IsNullOrEmpty(currentUserId) ? "guest_maps" : currentUserId;
            DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.RootReference;

            // Đọc toàn bộ dữ liệu map của người chơi
            DataSnapshot mapSnap = await dbRef.Child("maps").Child(userId).Child(_selectedMapId).GetValueAsync();
            if (mapSnap == null || !mapSnap.Exists)
            {
                Debug.LogError($"[DetailMap] Không tìm thấy map {_selectedMapId} trong maps/{userId}");
                return;
            }

            // ✅ Kiểm tra các vị trí spawn bắt buộc
            bool knightOk   = SpawnIsValid(mapSnap, "knightSpawn");
            bool demonOk    = SpawnIsValid(mapSnap, "demonSpawn");
            bool princessOk = SpawnIsValid(mapSnap, "princessSpawn");

            if (!knightOk || !demonOk || !princessOk)
            {
                var missing = new System.Text.StringBuilder("Map chưa đầy đủ vị trí spawn: ");
                if (!knightOk)   missing.Append("[Người chơi] ");
                if (!demonOk)    missing.Append("[Demon] ");
                if (!princessOk) missing.Append("[Princess] ");
                missing.Append("\nVui lòng vào Chỉnh sửa và đặt đủ spawn point trước khi Đăng tải!");
                Debug.LogWarning("[DetailMap] " + missing);
                ShowNotification(missing.ToString(), false);
                return;
            }

            // Chuyển snapshot thành dictionary (giữ đủ dữ liệu tile/trap/spawn)
            var communityData = new Dictionary<string, object>();
            foreach (DataSnapshot field in mapSnap.Children)
                communityData[field.Key] = field.Value;

            // Ghi đè các field community
            communityData["ownerId"] = userId;
            communityData["status"]  = "publish";

            // Ghi vào mapscommunity (ghi đè toàn bộ entry)
            await dbRef.Child("mapscommunity").Child(_selectedMapId).SetValueAsync(communityData);
            _selectedMapStatus = "publish";

            Debug.Log($"[DetailMap] Đã đăng tải map {_selectedMapId} lên mapscommunity với status=publish");
            LoadUserMaps();
            CloseMapDetail();
        }
        catch (Exception e)
        {
            Debug.LogError("[DetailMap] Lỗi đăng tải: " + e);
        }
#endif
    }

    /// <summary>
    /// Kiểm tra spawn point trong Firebase snapshot có hợp lệ không.
    /// Hợp lệ = tồn tại và có ít nhất x hoặc y khác 0.
    /// </summary>
    static bool SpawnIsValid(DataSnapshot mapSnap, string spawnKey)
    {
        if (!mapSnap.HasChild(spawnKey)) return false;
        DataSnapshot sp = mapSnap.Child(spawnKey);
        float x = 0f, y = 0f;
        try
        {
            if (sp.HasChild("x") && sp.Child("x").Value != null)
                x = Convert.ToSingle(sp.Child("x").Value);
            if (sp.HasChild("y") && sp.Child("y").Value != null)
                y = Convert.ToSingle(sp.Child("y").Value);
        }
        catch { return false; }
        // (0,0) được coi là chưa đặt (giá trị mặc định của Vector2)
        return x != 0f || y != 0f;
    }

    /// <summary>Nút "Xóa map" — hiện panel xác nhận trước.</summary>
    public void OnDetailDeleteMap()
    {
        if (string.IsNullOrEmpty(_selectedMapId)) return;

        // Hiện panel xác nhận với tên map
        if (PanelConfirmDelete != null)
        {
            PanelConfirmDelete.SetActive(true);
            if (ConfirmDeleteText != null)
                ConfirmDeleteText.text = $"Bạn có chắc muốn xóa map\n\"{_selectedMapName}\" không?\nHành động này không thể hoàn tác!";
        }
    }

    /// <summary>Nút "Xác nhận" — thực hiện xóa map khỏi cả maps và mapscommunity.</summary>
    public async void ConfirmDeleteMap()
    {
        if (PanelConfirmDelete != null) PanelConfirmDelete.SetActive(false);
        if (string.IsNullOrEmpty(_selectedMapId)) return;

#if !UNITY_WEBGL || UNITY_EDITOR
        try
        {
            string userId = string.IsNullOrEmpty(currentUserId) ? "guest_maps" : currentUserId;
            DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.RootReference;

            // Xóa song song cả 2 collections
            await Task.WhenAll(
                dbRef.Child("maps").Child(userId).Child(_selectedMapId).RemoveValueAsync(),
                dbRef.Child("mapscommunity").Child(_selectedMapId).RemoveValueAsync()
            );

            Debug.Log($"[DetailMap] Đã xóa map {_selectedMapId} khỏi maps và mapscommunity");
            LoadUserMaps();
            CloseMapDetail();
        }
        catch (Exception e)
        {
            Debug.LogError("[DetailMap] Lỗi xóa map: " + e);
            ShowNotification("Xóa map thất bại. Vui lòng thử lại!", false);
        }
#endif
    }

    /// <summary>Nút "Hủy" — đóng panel xác nhận, không làm gì.</summary>
    public void CancelDeleteMap()
    {
        if (PanelConfirmDelete != null) PanelConfirmDelete.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────
    // Bảng Thông Báo chung
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Hiển bảng thông báo với nội dung và màu theo kết quả.
    /// success=true → xanh lá, success=false → đỏ.
    /// </summary>
    public void ShowNotification(string message, bool success)
    {
        if (BangThongBao == null) return;
        BangThongBao.SetActive(true);
        if (TextThongBao != null)
        {
            TextThongBao.text  = message;
            TextThongBao.color = success ? UnityEngine.Color.green : UnityEngine.Color.red;
        }
    }

    /// <summary>Nút đóng bảng thông báo.</summary>
    public void CloseNotification()
    {
        if (BangThongBao != null) BangThongBao.SetActive(false);
    }
}

