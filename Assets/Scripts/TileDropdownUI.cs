using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class TileDropdownUI : MonoBehaviour
{
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
            options.Add(new TMP_Dropdown.OptionData(item.tileName, item.icon));
        }

        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener(ChangeTile);

        ChangeTile(0);
    }

    void ChangeTile(int index)
    {
        TileOption item = tileDatabase.tileOptions[index];

        selectedTile = item.tile;
        previewImage.sprite = item.icon;
        infoText.text = item.description;
    }
}