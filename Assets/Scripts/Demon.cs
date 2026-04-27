using UnityEngine;

public class Demon : MonoBehaviour
{
    [Header("Effects")]
    public GameObject smokeEffectPrefab;
    public float smokeYOffset = 0.5f; // Chỉnh thông số này trên Inspector để khói cao/thấp tùy ý

    private GameObject cage;
    private bool isTriggered = false;

    void Start()
    {
        cage = GameObject.FindGameObjectWithTag("Cage");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckCollision(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        CheckCollision(collider.gameObject);
    }

    private void CheckCollision(GameObject other)
    {
        Debug.Log("Va chạm với: " + other.name + " | Tag: " + other.tag);
        if (isTriggered) return;

        if (other.CompareTag("Player"))
        {
            isTriggered = true;

            if (cage != null)
            {
                Destroy(cage);
            }
            else
            {
                Debug.LogWarning("Không tìm thấy Cage");
            }

            // Tạo hiệu ứng khói trước khi biến mất
            if (smokeEffectPrefab != null)
            {
                // Nâng vị trí tạo khói lên một khoảng Y bằng smokeYOffset
                Vector3 spawnPosition = transform.position + new Vector3(0f, smokeYOffset, 0f);
                GameObject smoke = Instantiate(smokeEffectPrefab, spawnPosition, Quaternion.identity);
                // Xóa object khói sau 1 giây (bạn có thể điều chỉnh thời gian này tùy theo độ dài của animation/particle khói)
                Destroy(smoke, 1f); 
            }

            Destroy(gameObject, 0.01f);
        }
    }
}