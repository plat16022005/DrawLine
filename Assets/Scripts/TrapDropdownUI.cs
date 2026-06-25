using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TrapDropdownUI : MonoBehaviour, IPointerClickHandler
{
    public Image selectedImage;
    public TMP_Dropdown dropdown;
    public Image previewImage;
    public TMP_Text infoText;

    public TrapDatabase trapDatabase;

    public GameObject selectedTrapPrefab;
    public TrapPlacementController placementController;
    public void OnPointerClick(PointerEventData eventData)
    {
        PlacementModeManager.CurrentMode = PlacementMode.Trap;

        if (placementController != null)
            placementController.CreatePreview();
        Debug.Log(PlacementModeManager.CurrentMode.ToString());
    }
    void Start()
    {
        dropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new();

        foreach (TrapOption item in trapDatabase.trapOptions)
        {
            TMP_Dropdown.OptionData optionData = new();
            optionData.text = item.trapName;
            optionData.image = item.icon;
            options.Add(optionData);
        }

        dropdown.AddOptions(options);
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(ChangeTrap);

        AddDropdownClickEvent();

        if (trapDatabase.trapOptions.Count > 0)
            ChangeTrap(0);
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
            SetTrapMode();
        });

        trigger.triggers.Add(entry);
    }

    void SetTrapMode()
    {
        PlacementModeManager.CurrentMode = PlacementMode.Trap;
        Debug.Log("Mode: Trap");

        if (placementController != null)
            placementController.CreatePreview();
    }

    void ChangeTrap(int index)
    {
        TrapOption item = trapDatabase.trapOptions[index];

        selectedTrapPrefab = item.editorPrefab;

        previewImage.sprite = item.icon;
        selectedImage.sprite = item.icon;
        infoText.text = item.description;

        SetTrapMode();
    }
}