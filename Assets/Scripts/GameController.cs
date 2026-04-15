using UnityEngine;

public class GameController : MonoBehaviour
{
    public static bool isPlaying = false;

    void Start()
    {
        // Khi game vừa mở lên, đóng băng thời gian (các vật thể sẽ lơ lửng trên không/không chịu lực hấp dẫn)
        Time.timeScale = 0f;
        isPlaying = false;
        Debug.Log("Thời gian đã dừng, bạn có thể bắt đầu vẽ.");
    }

    // Hàm này sẽ được liên kết và gọi bởi nút bấm giao diện (UI Button)
    public void StartSimulation()
    {
        // Khôi phục thời gian trở lại bình thường, trọng lực bắt đầu hoạt động!
        Time.timeScale = 1f;
        isPlaying = true;
        Debug.Log("Trò chơi bắt đầu!");
    }
}
