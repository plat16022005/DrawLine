using UnityEngine;

/// <summary>
/// Gắn script này vào khối gai (Spike) có Collider2D và bật Is Trigger.
/// Khi Player chạm vào thì sẽ chết ngay.
/// </summary>
public class SpikeTrap : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra đối tượng chạm vào có phải Player không
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            // Gây lượng sát thương cực lớn để kích hoạt Die()
            playerHealth.TakeDamage(999999f);

            Debug.Log("Player đã chạm gai và chết ngay!");
        }
    }
}