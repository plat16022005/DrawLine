using UnityEngine;

/// <summary>
/// Gắn vào editor prefab của BreakablePlatform.
/// Implements ITrapConfig để Save/Load breakDelay từ Firebase.
/// </summary>
public class BreakablePlatformConfig : MonoBehaviour, ITrapConfig
{
    [Header("Thời gian trước khi vỡ (giây)")]
    public float breakDelay = 0f;

    public string ToJson()
    {
        BreakablePlatformConfigData data = new BreakablePlatformConfigData();
        data.breakDelay = breakDelay;
        return JsonUtility.ToJson(data);
    }

    public void FromJson(string json)
    {
        BreakablePlatformConfigData data = JsonUtility.FromJson<BreakablePlatformConfigData>(json);
        breakDelay = data.breakDelay;
    }
}
