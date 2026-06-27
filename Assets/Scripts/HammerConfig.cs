using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HammerConfig : MonoBehaviour, ITrapConfig
{
    public float rotationSpeed = 0f;
    public float traiphai = 1f;
    public string ToJson()
    {
        HammerConfigData data = new HammerConfigData();
        data.rotationSpeed = rotationSpeed;
        data.traiphai = traiphai;

        return JsonUtility.ToJson(data);
    }

    public void FromJson(string json)
    {
        HammerConfigData data = JsonUtility.FromJson<HammerConfigData>(json);

        rotationSpeed = data.rotationSpeed;
        traiphai = data.traiphai;
    }
}
