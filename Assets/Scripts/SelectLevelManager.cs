using System;
using System.Collections;
using System.Collections.Generic;
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
    private void Awake()
    {
        user = FirebaseAuth.DefaultInstance.CurrentUser;
    }
    void Start()
    {
        if (DataGame.instance.users.name == "")
        {
            PanelSetName.SetActive(true);
        }
        else
        {
            NamePlayer.text = DataGame.instance.users.name;
            Coin.text = DataGame.instance.users.coin.ToString();
        }
        if (DataGame.instance.CurrentLevel > 0)
        {
            LoadLevel();
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

            bool isUnlocked = current <= DataGame.instance.CurrentLevel;

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

                btn.onClick.AddListener(() => EnterLevel(levelIndex));
            }

            current++;

            if (current > DataGame.instance.CurrentLevel)
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
        MySkin myskin = new MySkin(new List<int>());
        DataGame.instance.users = users;
        NamePlayer.text = Name.text;
        Coin.text = users.coin.ToString();
        PanelSetName.SetActive(false);
        FirebaseDataManager.instance.WriteDatabase("Users", user.UserId, users.ToString());
        FirebaseDataManager.instance.WriteDatabase("CurrentLevel", user.UserId, "1");
        FirebaseDataManager.instance.WriteDatabase("CurrentSkin", user.UserId, "0");
        FirebaseDataManager.instance.WriteDatabase("MySkin", user.UserId, myskin.ToString());
        DataGame.instance.CurrentLevel = 3;
        DataGame.instance.CurrentSkin = 0;
        DataGame.instance.MySkin = myskin;
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
            Transform item = ContentSkin.GetChild(index-1);
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
            FirebaseDataManager.instance.WriteDatabase("Users", user.UserId, DataGame.instance.users.ToString());
            FirebaseDataManager.instance.WriteDatabase("MySkin", user.UserId, DataGame.instance.MySkin.ToString());
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
            FirebaseDataManager.instance.WriteDatabase("CurrentSkin", user.UserId, index.ToString());
            NameSkin.text = namesSkin[index];
            ResetShop();
        }
    }
    void CancelSkin()
    {
        DataGame.instance.CurrentSkin = 0;
        CurrentSkin.sprite = spritesSkin[0];
        FirebaseDataManager.instance.WriteDatabase("CurrentSkin", user.UserId, "0");
        NameSkin.text = namesSkin[0];
        ResetShop();
    }
    public void CloseShop()
    {
        PanelShop.SetActive(false);
    }
}
