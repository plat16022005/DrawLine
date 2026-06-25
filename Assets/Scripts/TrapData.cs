[System.Serializable]
public class TrapData
{
    public string trapId;
    public float x;
    public float y;
    public float angle;
    public float scaleX;
    public float scaleY;
    public string configJson;

    public TrapData(string trapId, float x, float y, float angle, float scaleX, float scaleY, string configJson)
    {
        this.trapId = trapId;
        this.x = x;
        this.y = y;
        this.angle = angle;
        this.scaleX = scaleX;
        this.scaleY = scaleY;
        this.configJson = configJson;
    }
}