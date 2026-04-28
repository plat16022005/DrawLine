using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Firebase.Auth;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectLevelManager : MonoBehaviour
{
    [SerializeField] private Sprite[] spritesSkin;
    [SerializeField] private String[] namesSkin;
    [SerializeField] private int[] costSkin;
    private FirebaseUser user;
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
    private void Awake()
    {
        user = FirebaseAuth.DefaultInstance.CurrentUser;
    }
    void Start()
    {
        // Giới hạn tối đa 15 ký tự
        Name.characterLimit = 15;

        // Mỗi khi người chơi nhập sẽ tự kiểm tra
        Name.onValueChanged.AddListener(ValidateName);
        if (DataGame.instance.users.name == "")
        {
            PanelSetName.SetActive(true);
        }
        else
        {
            NamePlayer.text = DataGame.instance.users.name;
            Coin.text = DataGame.instance.users.coin.ToString();
        }
        if (DataGame.instance.CurrentLevel.level > 0)
        {
            LoadLevel();
        }
    }
    void ValidateName(string value)
    {
        /*
         Cho phép:
         - Chữ thường + chữ hoa
         - Chữ có dấu tiếng Việt
         - Số
         - Dấu cách

         Không cho:
         - Ký tự đặc biệt: @ # $ % ^ & * ...
        */

        string filtered = Regex.Replace(
            value,
            @"[^a-zA-Z0-9À-ỹ\s]",
            ""
        );

        if (filtered != value)
        {
            Name.text = filtered;
        }
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

            if (textLv != null)
            {
                textLv.text = current.ToString();
            }

            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();

                int levelIndex = current; // 🔥 FIX closure bug

                btn.interactable = isUnlocked;

                btn.onClick.AddListener(() => OpenPanelDetailLevel(levelIndex));
            }
            star1.color = Color.black;
            star2.color = Color.black;
            star3.color = Color.black;
            if (current != DataGame.instance.CurrentLevel.level)
            {
                string levelName = "Lv" + current;

                Level lvData = DataGame.instance.levels
                    .Find(l => l != null && l.level == levelName);

                if (lvData != null)
                {
                    if (lvData.star >= 1)
                        star1.color = Color.white;

                    if (lvData.star >= 2)
                        star2.color = Color.white;

                    if (lvData.star >= 3)
                        star3.color = Color.white;
                }
            }

            current++;

            if (current > DataGame.instance.CurrentLevel.level)
            {
                break;
            }
        }
    }
    void EnterLevel(int level)
    {
        string lv = "Lv" + level; 
        SceneManager.LoadScene(lv);
    }
    public void SetName()
    {
        Users users = new Users(Name.text, 0);
        MySkin myskin = new MySkin(new List<int>(){-1});
        TotalPoint totalPoint = new TotalPoint(Name.text, 0);
        CurrentLevel currentLevel= new CurrentLevel(Name.text, 1);
        DataGame.instance.users = users;
        NamePlayer.text = Name.text;
        Coin.text = users.coin.ToString();
        PanelSetName.SetActive(false);
        FirebaseDataManager.instance.WriteDatabase("Users", user.UserId, users);
        FirebaseDataManager.instance.WriteDatabase("CurrentLevel", user.UserId, currentLevel);
        FirebaseDataManager.instance.WriteDatabase("CurrentSkin", user.UserId, 0);
        FirebaseDataManager.instance.WriteDatabase("MySkin", user.UserId, myskin);
        FirebaseDataManager.instance.WriteDatabase("TotalPoint", user.UserId, totalPoint);
        DataGame.instance.CurrentLevel = new CurrentLevel(Name.text, 1);
        DataGame.instance.CurrentSkin = 0;
        DataGame.instance.MySkin = myskin;
        DataGame.instance.totalPoint = totalPoint;
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
            btnBuy.onClick.AddListener(() => BuySkin(index));

            img.sprite = spritesSkin[index];
            txtCost.text = costSkin[index].ToString();            
        }
        if (DataGame.instance.MySkin.myskin == null)
        {
            return;
        }
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
                btnCancel.onClick.AddListener(CancelSkin);
            }
            else
            {
                btnEquip.gameObject.SetActive(true);
                btnEquip.onClick.AddListener(() => EquipSkin(index));
                btnCancel.gameObject.SetActive(false);                
            }

            img.sprite = spritesSkin[index];
            txtCost.text = costSkin[index].ToString();
            
        }        
    }
    void BuySkin(int index)
    {
        if (DataGame.instance.users.coin >= costSkin[index])
        {
            DataGame.instance.users.coin -= costSkin[index];
            DataGame.instance.MySkin.myskin.Add(index);
            FirebaseDataManager.instance.WriteDatabase("Users", user.UserId, DataGame.instance.users);
            FirebaseDataManager.instance.WriteDatabase("MySkin", user.UserId, DataGame.instance.MySkin);
            ResetShop();
            notificationSkin.text = "Bạn đã mua thành công skin: " + namesSkin[index];
            notificationSkin.color = Color.green;
            notificationSkin.gameObject.SetActive(true);
            Coin.text = DataGame.instance.users.coin.ToString();
        }
        else
        {
            notificationSkin.text = "Bạn không đủ tiền để mua skin này!";
            notificationSkin.color = Color.red;
            notificationSkin.gameObject.SetActive(true);
        }
    }
    void EquipSkin(int index)
    {
        if (DataGame.instance.MySkin.myskin.Contains(index))
        {
            DataGame.instance.CurrentSkin = index;
            CurrentSkin.sprite = spritesSkin[index];
            FirebaseDataManager.instance.WriteDatabase("CurrentSkin", user.UserId, index);
            NameSkin.text = namesSkin[index];
            ResetShop();
            notificationSkin.text = "Bạn đang mặc skin: " + namesSkin[index];
            notificationSkin.color = Color.green;
            notificationSkin.gameObject.SetActive(true);
        }
    }
    void CancelSkin()
    {
        DataGame.instance.CurrentSkin = 0;
        CurrentSkin.sprite = spritesSkin[0];
        FirebaseDataManager.instance.WriteDatabase("CurrentSkin", user.UserId, 0);
        NameSkin.text = namesSkin[0];
        ResetShop();
        notificationSkin.text = "Bạn đã hủy mặc skin và trở về mặc định";
        notificationSkin.color = Color.green;
        notificationSkin.gameObject.SetActive(true);
    }
    public void CloseShop()
    {
        PanelShop.SetActive(false);
    }
    public async void OpenRankPanel()
    {
        PanelRank.SetActive(true);
        LoadPointRank();
    }
    public async void LoadPointRank()
    {
        foreach (Transform item in ContentRank)
        {
            Destroy(item.gameObject);
        }
        await DataGame.instance.LoadTotalPointRank();
        int currentRantPlayer = 0;
        foreach (TotalPoint player in DataGame.instance.TotalPointRank)
        {
            currentRantPlayer++;
            GameObject obj = Instantiate(RankPrefabs, ContentRank);
            TextMeshProUGUI XH = obj.transform.Find("XH")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI Name = obj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI Point = obj.transform.Find("Point")?.GetComponent<TextMeshProUGUI>();

            XH.text = currentRantPlayer.ToString();
            Name.text = player.name;
            Point.text = player.point.ToString();
        }
        int myRank = await DataGame.instance.FindMyRank();

        RankPlayer.text = myRank.ToString();
        NamePlayerRank.text = DataGame.instance.users.name;
        PointPlayerRank.text = DataGame.instance.totalPoint.point.ToString();        
    }
    public async void LoadLevelRank()
    {
        foreach (Transform item in ContentRank)
        {
            Destroy(item.gameObject);
        }
        await DataGame.instance.LoadLevelRank();
        int currentRantPlayer = 0;
        foreach (CurrentLevel level in DataGame.instance.LevelRank)
        {
            currentRantPlayer++;
            GameObject obj = Instantiate(RankPrefabs, ContentRank);
            TextMeshProUGUI XH = obj.transform.Find("XH")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI Name = obj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI Point = obj.transform.Find("Point")?.GetComponent<TextMeshProUGUI>();

            XH.text = currentRantPlayer.ToString();
            Name.text = level.name;
            Point.text = "LV." + (level.level - 1).ToString();
        }
        int myRank = await DataGame.instance.FindMyLevelRank();

        RankPlayer.text = myRank.ToString();
        NamePlayerRank.text = DataGame.instance.users.name;
        PointPlayerRank.text = "LV." + (DataGame.instance.CurrentLevel.level - 1).ToString();        
    }
    public void CloseRankPanel()
    {
        PanelRank.SetActive(false);
    }
    void OpenPanelDetailLevel(int level)
    {
        PanelLevelDetail.SetActive(true);
        Level result = DataGame.instance.levels.Find(l => l.level == "Lv" + level);
        if (result != null)
        {
            ImageLevelDetail.sprite = ImageDetail[0];
            if (result.star >= 1)
            {
                Star1Level.color = Color.white;
            }
            if (result.star >= 2)
            {
                Star2Level.color = Color.white;
            }
            if (result.star >= 3)
            {
                Star3Level.color = Color.white;
            }
            PointLevel.text = result.point.ToString();
            EnterLevelButton.onClick.AddListener(() => EnterLevel(level));
        }
        else
        {
            ImageLevelDetail.sprite = ImageDetail[1];
            Star1Level.color = Color.black;
            Star2Level.color = Color.black;
            Star3Level.color = Color.black;
            PointLevel.text = "0";
            EnterLevelButton.onClick.AddListener(() => EnterLevel(level));
        }
    }
    public void ClosePanelDetailLevel()
    {
        PanelLevelDetail.SetActive(false);
    }
}
