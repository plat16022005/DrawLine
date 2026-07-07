using UnityEngine;

public class LevelSceneManager : MonoBehaviour
{
    public Sprite[] sprites;
    GameObject player;

    public static LevelSceneManager instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            SpriteRenderer spritePlayer = player.GetComponent<SpriteRenderer>();
            spritePlayer.sprite = sprites[DataGame.instance.CurrentSkin];
        }
    }
    public void LoadSkin()
    {
        player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            SpriteRenderer spritePlayer = player.GetComponent<SpriteRenderer>();
            spritePlayer.sprite = sprites[DataGame.instance.CurrentSkin];
        }
    }
}