using UnityEngine;

public class SlowDownBehavior : MonoBehaviour
{
    [Tooltip("Hệ số làm chậm, càng lớn bóng càng mau dừng lại (VD: 1.0 là giảm 100% vận tốc mỗi giây)")]
    public float slowRate = 0.9f;

    void OnCollisionStay2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.collider.attachedRigidbody;
        if (rb != null)
        {
            // Lấy vận tốc hiện tại
            Vector2 velocity = rb.velocity;
            
            // Trừ dần đi một lượng vận tốc theo thời gian
            velocity -= velocity * slowRate * Time.fixedDeltaTime;
            
            // Khóa chết lại nếu vận tốc quá nhỏ (tránh việc trôi nhẹ cực chậm)
            if (velocity.magnitude < 0.05f)
            {
                velocity = Vector2.zero;
                rb.angularVelocity = 0f; // Ngừng xoay luôn cho thực tế như đang mắc lầy
            }
            
            rb.velocity = velocity;
        }
    }
}
