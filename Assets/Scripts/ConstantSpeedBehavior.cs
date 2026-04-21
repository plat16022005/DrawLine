using UnityEngine;
using System.Collections.Generic;

public class ConstantSpeedBehavior : MonoBehaviour
{
    private Dictionary<Rigidbody2D, float> activeBalls = new Dictionary<Rigidbody2D, float>();

    void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.collider.attachedRigidbody;
        if (rb != null && !activeBalls.ContainsKey(rb))
        {
            // Ghi nhận vận tốc hiện tại lúc vừa chạm vào đường màu xanh dương (không đổi)
            float targetSpeed = rb.velocity.magnitude;
            
            // Cập nhật lại ngay
            if (targetSpeed > 0.001f)
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
            // Giữ vận tốc không đổi trên đường xanh dương
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
            // Quả bóng rời khỏi đường xanh dương
            activeBalls.Remove(rb);
        }
    }
}
