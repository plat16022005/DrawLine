using System;
using System.Drawing;
using Newtonsoft.Json;
[System.Serializable]
public class CurrentLevel
{
    public string name;
    public int level;
    public long time;
    public string timeText;
    public CurrentLevel()
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        this.time = timestamp;

        this.timeText = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");               
    }
    public CurrentLevel(string name, int level)
    {
        this.name = name;
        this.level = level;
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        this.time = timestamp;

        this.timeText = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");       
    }
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
