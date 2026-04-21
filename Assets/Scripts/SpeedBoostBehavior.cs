using UnityEngine;
using System.Collections.Generic;

public class SpeedBoostBehavior : MonoBehaviour
{
    private Dictionary<Rigidbody2D, float> activeBalls = new Dictionary<Rigidbody2D, float>();

    void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.collider.attachedRigidbody;
        if (rb != null && !activeBalls.ContainsKey(rb))
        {
            // Lấy vận tốc lúc vừa lăn vào x2
            float currentSpeed = rb.velocity.magnitude;
            float targetSpeed = currentSpeed * 1.8f;
            
            // Ngay lập tức áp dụng vận tốc mới
            if (currentSpeed > 0.001f)
            {
                rb.velocity = rb.velocity.normalized * targetSpeed;
            }

            activeBalls[rb] = targetSpeed;
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.collider.attachedRigidbody;
        if (rb != null && activeBalls.ContainsKey(rb))
        {
            float targetSpeed = activeBalls[rb];
            
            float currentSpeed = rb.velocity.magnitude;
            // Giữ vận tốc không đổi trên đường đỏ
            if (currentSpeed > 0.001f)
            {
                rb.velocity = rb.velocity.normalized * targetSpeed;
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.collider.attachedRigidbody;
        if (rb != null && activeBalls.ContainsKey(rb))
        {
            // Quả bóng rời khỏi đường đỏ
            activeBalls.Remove(rb);
        }
    }
}
