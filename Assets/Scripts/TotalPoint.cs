using System.Drawing;
using Newtonsoft.Json;
[System.Serializable]
public class TotalPoint
{
    public string name;
    public int point;
    public TotalPoint()
    {
        
    }
    public TotalPoint(string name, int point)
    {
        this.name = name;
        this.point = point;
    }
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
