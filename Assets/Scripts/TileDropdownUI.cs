using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

public class TileDropdownUI : MonoBehaviour
{
    public Image selectedImage;
    public TMP_Dropdown dropdown;
    public Image previewImage;
    public TMP_Text infoText;

    public TileDatabase tileDatabase;

    public TileBase selectedTile;

    void Start()
    {
        dropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new();

        foreach (TileOption item in tileDatabase.tileOptions)
        {
            TMP_Dropdown.OptionData optionData = new();
            optionData.text = item.tileName;
            optionData.image = item.icon;
            options.Add(optionData);
        }

        dropdown.AddOptions(options);
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(ChangeTile);

        AddDropdownClickEvent();

        if (tileDatabase.tileOptions.Count > 0)
            ChangeTile(0);
    }

    void ChangeTile(int index)
    {
        TileOption item = tileDatabase.tileOptions[index];

        selectedTile = item.tile;
        previewImage.sprite = item.icon;
        selectedImage.sprite = item.icon;
        infoText.text = item.description;

        SetTileMode();
    }

    void SetTileMode()
    {
        PlacementModeManager.CurrentMode = PlacementMode.Tile;
        Debug.Log("Mode: Tile");
    }

    void AddDropdownClickEvent()
    {
        EventTrigger trigger = dropdown.gameObject.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger = dropdown.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;

        entry.callback.AddListener((data) =>
        {
            SetTileMode();
        });

        trigger.triggers.Add(entry);
    }
}