using UnityEngine;

public class FireDamage : MonoBehaviour
{
    [Header("Fire Damage Settings")]
    [Tooltip("Sát thương gây ra khi Player chạm vào lửa")]
    public float damageAmount = 5f;

    [Tooltip("Thời gian giữa mỗi lần gây sát thương (nếu đứng trong lửa liên tục)")]
    public float damageCooldown = 1f;

    private float lastDamageTime;

    private void OnTriggerStay2D(Collider2D other)
    {
        // Kiểm tra đúng Player chưa
        if (!other.CompareTag("Player"))
            return;

        // Lấy script PlayerHealth
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        // Cooldown tránh trừ máu mỗi frame
        if (Time.time >= lastDamageTime + damageCooldown)
        {
            playerHealth.TakeDamage(damageAmount);
            lastDamageTime = Time.time;

            Debug.Log($"Player chạm lửa! Mất {damageAmount} HP");
        }
    }
}