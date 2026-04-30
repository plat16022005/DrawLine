using UnityEngine;

public class Demon : MonoBehaviour
{
    [Header("Effects")]
    public GameObject smokeEffectPrefab;
    public float smokeYOffset = 0.5f; // Chỉnh thông số này trên Inspector để khói cao/thấp tùy ý

    private GameObject cage;
    private bool isTriggered = false;
    private GameObject currentSmoke; // Track smoke instance để có thể xóa khi StopSimulation

    // Lưu vị trí ban đầu để có thể restore khi StopSimulation
    private Vector3 demonStartPosition;
    private Quaternion demonStartRotation;
    private Vector3 cageStartPosition;
    private Quaternion cageStartRotation;

    void Start()
    {
        cage = GameObject.FindGameObjectWithTag("Cage");

        // Lưu vị trí ban đầu của Demon
        demonStartPosition = transform.position;
        demonStartRotation = transform.rotation;

        // Lưu vị trí ban đầu của Cage (nếu có)
        if (cage != null)
        {
            cageStartPosition = cage.transform.position;
            cageStartRotation = cage.transform.rotation;
        }
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

            // Ẩn Cage thay vì Destroy để có thể khôi phục khi StopSimulation
            if (cage != null)
            {
                cage.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Không tìm thấy Cage");
            }

            // Tạo hiệu ứng khói trước khi biến mất
            if (smokeEffectPrefab != null)
            {
                Vector3 spawnPosition = transform.position + new Vector3(0f, smokeYOffset, 0f);
                currentSmoke = Instantiate(smokeEffectPrefab, spawnPosition, Quaternion.identity);
                Destroy(currentSmoke, 1f);
            }

            // Ẩn Demon thay vì Destroy để có thể khôi phục khi StopSimulation
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Gọi hàm này khi StopSimulation để khôi phục Demon và Cage về trạng thái ban đầu.
    /// </summary>
    public void ResetDemon()
    {
        isTriggered = false;

        // Xóa smoke effect nếu vẫn còn tồn tại
        if (currentSmoke != null)
        {
            Destroy(currentSmoke);
            currentSmoke = null;
        }

        // Khôi phục Demon về vị trí ban đầu và hiện lại
        transform.position = demonStartPosition;
        transform.rotation = demonStartRotation;
        gameObject.SetActive(true);

        // Khôi phục Cage về vị trí ban đầu và hiện lại
        if (cage != null)
        {
            cage.transform.position = cageStartPosition;
            cage.transform.rotation = cageStartRotation;
            cage.SetActive(true);
        }
        else
        {
            // Trường hợp cage bị Destroy trước khi lưu được ref (fallback)
            cage = GameObject.FindGameObjectWithTag("Cage");
            if (cage != null)
            {
                cage.transform.position = cageStartPosition;
                cage.transform.rotation = cageStartRotation;
                cage.SetActive(true);
            }
        }
    }
}