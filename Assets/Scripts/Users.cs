using Newtonsoft.Json;
[System.Serializable]
public class Users
{
    public string name;
    public int coin = 999;
    public Users()
    {
        
    }
    public Users(string name, int coin)
    {
        this.name = name;
        this.coin = coin;
    }
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
