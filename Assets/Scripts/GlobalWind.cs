using UnityEngine;

public class GlobalWind : MonoBehaviour
{
    [Header("Wind Settings")]
    [Tooltip("Lực thổi của gió")]
    public float windForce = 15f;
    
    [Tooltip("Góc thổi (0: Phải, 90: Lên, 180: Trái, 270: Xuống)")]
    [Range(0f, 360f)]
    public float windAngle = 180f; // Mặc định thổi sang trái

    [Header("Visual Effects")]
    public ParticleSystem windParticleSystem;

    private Vector2 windVector;
    private Rigidbody2D[] playerRbs;

    void Start()
    {
        CalculateWindVector();
        UpdateVisuals();
        FindPlayers();
    }

    void OnValidate()
    {
        // Tự động tính toán lại vector gió và hình ảnh mỗi khi bạn kéo thanh trượt góc trong Editor
        CalculateWindVector();
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        // Xoay toàn bộ GameObject cha (chứa script này) theo hướng gió
        transform.rotation = Quaternion.Euler(0, 0, windAngle);

        if (windParticleSystem != null)
        {
            var main = windParticleSystem.main;
            // Giả sử lực gió tiêu chuẩn là 15f. 
            // - Lực nhẹ (vd: 5): simulationSpeed = 0.33 (gió bay rất chậm, hạt sinh ra thưa thớt)
            // - Lực mạnh (vd: 30): simulationSpeed = 2 (gió bay vèo vèo, hạt sinh ra dày đặc)
            // Cách này không làm hỏng các setting có sẵn của bạn!
            main.simulationSpeed = Mathf.Max(0.1f, windForce / 15f);
        }
    }

    void CalculateWindVector()
    {
        // Chuyển đổi góc từ độ sang radian và tính toán vector hướng gió
        // Hệ trục: 0 = Lên, 90 = Trái, 180 = Xuống, 270 = Phải (Khớp với góc quay Z của Transform)
        float radian = windAngle * Mathf.Deg2Rad;
        windVector = new Vector2(-Mathf.Sin(radian), Mathf.Cos(radian)) * windForce;
    }

    void Update()
    {
        // Tìm lại danh sách player mỗi 60 frame (phòng trường hợp nhân vật mới được spawn sinh ra)
        if (Time.frameCount % 60 == 0)
        {
            FindPlayers();
        }
    }

    void FindPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        playerRbs = new Rigidbody2D[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            playerRbs[i] = players[i].GetComponent<Rigidbody2D>();
        }
    }

    void FixedUpdate()
    {
        if (playerRbs == null) return;

        // Quét tất cả các player và liên tục đẩy họ theo hướng gió
        // Nhân thêm với hệ số nhỏ (0.1f) để lực đẩy vật lý trên thanh trượt không bị quá gắt
        foreach(var rb in playerRbs)
        {
            if (rb != null && rb.gameObject.activeInHierarchy)
            {
                rb.AddForce(windVector * 0.1f, ForceMode2D.Force);
            }
        }
    }
}
