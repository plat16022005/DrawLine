using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class SelectLevelManager : MonoBehaviour
{
    private FirebaseUser user;
    [Header("Panel Set Name")]
    public GameObject PanelSetName;
    public TMP_InputField Name;
    [Header("Panel Select Level")]
    public TextMeshProUGUI NamePlayer;
    public TextMeshProUGUI Coin;
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
    }
    public void SetName()
    {
        Users users = new Users(Name.text, 0);
        DataGame.instance.users = users;
        NamePlayer.text = Name.text;
        Coin.text = users.coin.ToString();
        PanelSetName.SetActive(false);
        FirebaseDataManager.instance.WriteDatabase("Users", user.UserId, users.ToString());
    }
}
