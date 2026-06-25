using UnityEngine;

public class MovingBlockConfig : MonoBehaviour, ITrapConfig
{
    public float moveX = 3f;
    public float moveY = 0f;
    public float speed = 2f;

    public string ToJson()
    {
        MovingBlockConfigData data = new MovingBlockConfigData();
        data.moveX = moveX;
        data.moveY = moveY;
        data.speed = speed;

        return JsonUtility.ToJson(data);
    }

    public void FromJson(string json)
    {
        MovingBlockConfigData data = JsonUtility.FromJson<MovingBlockConfigData>(json);

        moveX = data.moveX;
        moveY = data.moveY;
        speed = data.speed;
    }
}