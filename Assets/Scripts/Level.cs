using System;
using Newtonsoft.Json;
[System.Serializable]
public class Level
{
    public string level;
    public string namePlayer;
    public int star;
    public int point;
    public long time;
    public string timeText;
    public Level()
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        this.time = timestamp;

        this.timeText = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");        
    }
    public Level(string level, string namePlayer, int star, int point)
    {
        this.level = level;
        this.namePlayer = namePlayer;
        this.star = star;
        this.point = point;
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        this.time = timestamp;

        this.timeText = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");    
    }
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
