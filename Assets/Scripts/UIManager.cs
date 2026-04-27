using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase.Auth;
using System.Linq;

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

    [Header("Game Win UI")]
    [Tooltip("Panel UI hiển thị khi chiến thắng")]
    public GameObject winPanel;
    public Image Star1;
    public Image Star2;
    public Image Star3;
    public TextMeshProUGUI Point;
    public TextMeshProUGUI Reward;
    public Button NextLevel;
    private FirebaseUser user;
    private void OnEnable()
    {
        // Đăng ký lắng nghe sự kiện từ PlayerHealth
        PlayerHealth.OnHealthChanged += UpdateHealthUI;
        PlayerHealth.OnPlayerDied += ShowGameOverScreen;
        // Đăng ký lắng nghe sự kiện từ Princess
        Princess.OnPlayerWin += HandlePlayerWin;
    }

    private void OnDisable()
    {
        // Hủy đăng ký khi UIManager bị tắt/destroy để tránh lỗi rò rỉ bộ nhớ
        PlayerHealth.OnHealthChanged -= UpdateHealthUI;
        PlayerHealth.OnPlayerDied -= ShowGameOverScreen;
        Princess.OnPlayerWin -= HandlePlayerWin;
    }

    void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
        user = FirebaseAuth.DefaultInstance.CurrentUser;
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
        GameController.isGameOver = true;
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

    private void HandlePlayerWin(Vector3 winPosition)
    {
        StartCoroutine(WinRoutine(winPosition));
    }

    private System.Collections.IEnumerator WinRoutine(Vector3 winPosition)
    {

        // 2. Chờ 1 giây
        yield return new WaitForSeconds(1f);

        // 3. Dừng game
        Time.timeScale = 0f;
        GameController.isPlaying = false;
        GameController.isGameOver = true;

        SetWinThisLv();
    }
    void SetWinThisLv()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        int star;
        int money = 0;
        if (InkManager.CurrentInk >= 700)
        {
            star = 3;
        }
        else if (InkManager.CurrentInk < 700 && InkManager.CurrentInk >= 500)
        {
            star = 2;
        }
        else if (InkManager.CurrentInk < 500 && InkManager.CurrentInk >= 300)
        {
            star = 1;
        }
        else
        {
            star = 0;
        }
        Level foundLevel = DataGame.instance.levels
            .Find(l => l != null && l.level == sceneName);

        int index = DataGame.instance.levels
            .FindIndex(l => l != null && l.level == sceneName);
        int value = Mathf.RoundToInt(InkManager.CurrentInk * PlayerHealth.currentHealth);
        Level level = new Level(sceneName, DataGame.instance.users.name, star, value);
        if (foundLevel != null)
        {
            if (foundLevel.point < level.point)
            {
                FirebaseDataManager.instance.WriteDatabase(sceneName, user.UserId, level.ToString());
                DataGame.instance.levels[index] = level;
                DataGame.instance.totalPoint.point -= foundLevel.point;
                DataGame.instance.totalPoint.point += level.point;
                FirebaseDataManager.instance.WriteDatabase("TotalPoint", user.UserId, DataGame.instance.totalPoint.ToString());
                money = 0;
            }
        }
        else
        {
            FirebaseDataManager.instance.WriteDatabase(sceneName, user.UserId, level.ToString());
            DataGame.instance.levels.Add(level);
            DataGame.instance.CurrentLevel = new CurrentLevel(DataGame.instance.users.name, DataGame.instance.CurrentLevel.level + 1);
            DataGame.instance.totalPoint.point += value;
            FirebaseDataManager.instance.WriteDatabase("CurrentLevel", user.UserId, DataGame.instance.CurrentLevel.ToString());
            FirebaseDataManager.instance.WriteDatabase("TotalPoint", user.UserId, DataGame.instance.totalPoint.ToString());
            money = 50;
        }
        OpenWinPanel(sceneName, star, value, money);
    }
    void OpenWinPanel(string sceneName, int star, int point, int money)
    {
        winPanel.SetActive(true);
        DataGame.instance.users.coin += money;
        FirebaseDataManager.instance.WriteDatabase("Users", user.UserId, DataGame.instance.users.ToString());
        if (star >= 1)
        {
            Star1.color = Color.white;
        }
        if (star >= 2)
        {
            Star2.color = Color.white;
        }
        if (star >= 3)
        {
            Star3.color = Color.white;
        }
        Point.text = point.ToString();
        Reward.text = "+" + money.ToString() + " vàng";

        string prefix = new string(sceneName.TakeWhile(c => !char.IsDigit(c)).ToArray());
        string numberPart = new string(sceneName.SkipWhile(c => !char.IsDigit(c)).ToArray());

        int levelNumber = int.Parse(numberPart);
        NextLevel.onClick.AddListener(() => NextToLevel(prefix + (levelNumber + 1).ToString()));
    }
    void NextToLevel(string level)
    {
        SceneManager.LoadScene(level);
    }
}
