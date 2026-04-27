using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    // --- EVENTS CHO UI LẮNG NGHE ---
    public static event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)
    public static event Action OnPlayerDied;

    [Header("Health Settings")]
    [Tooltip("Lượng máu tối đa của Player")]
    public float maxHealth = 100f;
    public static float currentHealth;

    [Header("Fall Damage Settings")]
    [Tooltip("Vận tốc va chạm tối thiểu để bắt đầu bị trừ máu")]
    public float fallDamageThreshold = 10f; 
    [Tooltip("Hệ số nhân sát thương. VD: (Vận tốc - Threshold) * Multiplier = Sát thương")]
    public float damageMultiplier = 2f;

    private bool isDead = false;

    [Header("Instant Death Settings")]
    [Tooltip("Nếu player rơi xuống dưới giá trị Y này thì chết ngay")]
    public float deathY = -7f;

    void Start()
    {
        ResetHealth();
    }
    void Update()
    {
        if (isDead) return;

        if (transform.position.y < deathY)
        {
            Die();
        }
    }
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        // Bắn event để UI cập nhật
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        // Chỉ tính sát thương nếu va chạm với đối tượng thuộc layer "Ground"
        if (collision.gameObject.layer != LayerMask.NameToLayer("Ground"))
        {
            return;
        }

        // Lấy pháp tuyến (normal) của bề mặt va chạm
        if (collision.contactCount > 0)
        {
            Vector2 normal = collision.GetContact(0).normal;
            
            // Nếu mặt phẳng va chạm không hướng lên (ví dụ: đập vào bức tường thẳng đứng)
            // normal.y < 0.5f tức là góc dốc quá đứng, ta sẽ không tính là rơi chạm đất
            if (normal.y < 0.5f)
            {
                return; // Bỏ qua, không trừ máu
            }
        }

        // Tính toán lực va chạm dựa trên vận tốc tương đối theo trục Y (chỉ tính lực rơi dọc)
        // Bỏ qua lực ngang (trục X) để không bị trừ máu khi lăn nhanh đập vào tường
        float impactVelocity = Mathf.Abs(collision.relativeVelocity.y);

        if (impactVelocity > fallDamageThreshold)
        {
            float damage = (impactVelocity - fallDamageThreshold) * damageMultiplier;
            TakeDamage(damage);
            
            Debug.Log($"Rơi chạm đất! Lực dọc: {impactVelocity:F1}. Mất {damage:F1} HP.");
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        // Thông báo cho UIManager biết máu vừa thay đổi
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Kiểm tra nếu hết máu thì chết
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player đã chết do rơi quá mạnh!");

        // Dừng thời gian game để mọi thứ không rơi nữa
        Time.timeScale = 0f;

        // Thông báo cho UIManager biết player đã chết để hiện màn hình Game Over
        OnPlayerDied?.Invoke();
    }
}
