using UnityEngine;

[System.Serializable]
public class TrapOption
{
    public string id;
    public string trapName;
    public Sprite icon;
    public string description;

    public GameObject editorPrefab;
    public GameObject runtimePrefab;
}