using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public Image StartPause;
    public Sprite[] sprites;
    public static bool isPlaying = false;

    // --- Snapshot cho các vật thể vật lý (Rigidbody2D) ---
    private struct RigidbodySnapshot
    {
        public Rigidbody2D rb;
        public Vector2 position;
        public float rotation;
    }
    private List<RigidbodySnapshot> rigidbodySnapshots = new List<RigidbodySnapshot>();

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
        // Chụp trạng thái ban đầu của tất cả Rigidbody2D trong scene
        TakeSnapshot();

        // Chụp mức mực hiện tại để có thể restore khi Stop
        if (InkManager.Instance != null)
            InkManager.Instance.SnapshotInk();

        // Khôi phục thời gian trở lại bình thường, trọng lực bắt đầu hoạt động!
        Time.timeScale = 1f;
        isPlaying = true;
        Debug.Log("Trò chơi bắt đầu!");
    }

    public void StopSimulation()
    {
        // 1. Đóng băng thời gian ngay lập tức
        Time.timeScale = 0f;
        isPlaying = false;

        // 2. Khôi phục tất cả đường Rubber (tím) về dạng đường vẽ ban đầu
        RubberBehavior[] rubbers = FindObjectsOfType<RubberBehavior>();
        foreach (RubberBehavior rubber in rubbers)
        {
            rubber.ResetToDrawnState();
        }

        // 3. Khôi phục vị trí/góc quay ban đầu của tất cả Rigidbody2D
        RestoreSnapshot();

        // 4. Khôi phục lượng mực về mức lúc bấm Start
        if (InkManager.Instance != null)
            InkManager.Instance.RestoreInk();

        // 5. Khôi phục đầy máu cho người chơi
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }

        Debug.Log("Simulation đã dừng, mọi thứ đã về trạng thái ban đầu.");
    }
    public void StartStop()
    {
        if (isPlaying == false)
        {
            StartSimulation();
            StartPause.sprite = sprites[1];
        }
        else
        {
            StopSimulation();
            StartPause.sprite = sprites[0];
        }
    }
    // --- Chụp snapshot tất cả Rigidbody2D (trừ các node của rope vì chúng chưa tồn tại) ---
    private void TakeSnapshot()
    {
        rigidbodySnapshots.Clear();

        // Tìm tất cả Rigidbody2D hiện có trong scene
        Rigidbody2D[] allRbs = FindObjectsOfType<Rigidbody2D>();
        foreach (Rigidbody2D rb in allRbs)
        {
            // Chỉ snapshot những Rigidbody không thuộc rope node (rope node chưa tồn tại lúc này)
            rigidbodySnapshots.Add(new RigidbodySnapshot
            {
                rb = rb,
                position = rb.position,
                rotation = rb.rotation
            });
        }

        Debug.Log($"Đã chụp snapshot cho {rigidbodySnapshots.Count} Rigidbody2D.");
    }

    // --- Khôi phục từ snapshot ---
    private void RestoreSnapshot()
    {
        foreach (RigidbodySnapshot snap in rigidbodySnapshots)
        {
            // Kiểm tra Rigidbody còn tồn tại không (có thể bị Destroy rồi)
            if (snap.rb == null) continue;

            snap.rb.velocity = Vector2.zero;
            snap.rb.angularVelocity = 0f;
            snap.rb.position = snap.position;
            snap.rb.rotation = snap.rotation;

            // Đồng bộ Transform để Unity render đúng vị trí
            snap.rb.transform.position = snap.position;
            snap.rb.transform.rotation = Quaternion.Euler(0f, 0f, snap.rotation);
        }

        rigidbodySnapshots.Clear();
    }
}
