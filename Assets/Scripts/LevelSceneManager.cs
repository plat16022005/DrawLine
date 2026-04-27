using UnityEngine;

public class LevelSceneManager : MonoBehaviour
{
    public Sprite[] sprites;
    GameObject player;

    void Start()
    {
        player = GameObject.FindWithTag("Player");

        SpriteRenderer spritePlayer = player.GetComponent<SpriteRenderer>();
        spritePlayer.sprite = sprites[DataGame.instance.CurrentSkin];
    }
}