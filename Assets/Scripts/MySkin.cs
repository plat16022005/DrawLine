using System.Collections.Generic;
using Newtonsoft.Json;
[System.Serializable]
public class MySkin
{
    public List<int> myskin = new List<int>();
    public MySkin()
    {
        
    }
    public MySkin(List<int> myskin)
    {
        this.myskin = myskin;
    }
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
