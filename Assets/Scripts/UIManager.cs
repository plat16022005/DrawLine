using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Health UI")]
    [Tooltip("Thanh máu (Slider)")]
    public Slider healthBar;
    [Tooltip("Text hiển thị lượng máu (tuỳ chọn)")]
    public TextMeshProUGUI hpText; 
    
    [Header("Game Over UI")]
    [Tooltip("Panel UI hiển thị chữ Game Over và nút Restart")]
    public GameObject gameOverPanel;

    private void OnEnable()
    {
        // Đăng ký lắng nghe sự kiện từ PlayerHealth
        PlayerHealth.OnHealthChanged += UpdateHealthUI;
        PlayerHealth.OnPlayerDied += ShowGameOverScreen;
    }

    private void OnDisable()
    {
        // Hủy đăng ký khi UIManager bị tắt/destroy để tránh lỗi rò rỉ bộ nhớ
        PlayerHealth.OnHealthChanged -= UpdateHealthUI;
        PlayerHealth.OnPlayerDied -= ShowGameOverScreen;
    }

    void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        if (hpText != null)
        {
            hpText.text = Mathf.CeilToInt(currentHealth).ToString() + " / " + maxHealth.ToString();
        }
    }

    private void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    // Hàm này sẽ được gán vào nút "Chơi lại" (Restart) trong màn hình Game Over UI
    public void RestartGame()
    {
        Time.timeScale = 1f; // Khôi phục lại thời gian về bình thường
        
        // Khôi phục trạng thái của GameController
        GameController.isPlaying = false; 
        
        // Tải lại Scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
