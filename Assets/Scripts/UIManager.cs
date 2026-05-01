using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase.Auth;
using System.Linq;
using Unity.VisualScripting;

public class UIManager : MonoBehaviour
{
    public AudioSource MusicBackGround;
    public AudioSource SoundEffect;
    public AudioClip[] SoundClip;
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
    [Header("Star Ink")]
    public Image Star1Ink;
    public Image Star2Ink;
    public Image Star3Ink;
    [Header("Detail Rank")]
    public Transform ContentLevelDetail;
    public GameObject LevelDetailPrefab;
    public TextMeshProUGUI XHLevel;
    public TextMeshProUGUI NamePlayerLevel;
    public TextMeshProUGUI PointPlayerLevel;
    void Update()
    {
        if (InkManager.CurrentInk < 700)
        {
            Star1Ink.color = Color.black;
        }
        else
        {
            Star1Ink.color = Color.white;
        }
        if (InkManager.CurrentInk < 500)
        {
            Star2Ink.color = Color.black;
        }
        else
        {
            Star2Ink.color = Color.white;
        }
        if (InkManager.CurrentInk < 300)
        {
            Star3Ink.color = Color.black;
        }
        else
        {
            Star3Ink.color = Color.white;
        }
    }
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
        MusicBackGround.Stop();
        SoundEffect.PlayOneShot(SoundClip[1]);
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
        MusicBackGround.Stop();
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
                FirebaseDataManager.instance.WriteDatabase(sceneName, user.UserId, level);
                DataGame.instance.levels[index] = level;
                DataGame.instance.totalPoint.point -= foundLevel.point;
                DataGame.instance.totalPoint.point += level.point;
                FirebaseDataManager.instance.WriteDatabase("TotalPoint", user.UserId, DataGame.instance.totalPoint);
                money = 0;
            }
        }
        else
        {
            FirebaseDataManager.instance.WriteDatabase(sceneName, user.UserId, level);
            DataGame.instance.levels.Add(level);
            DataGame.instance.CurrentLevel = new CurrentLevel(DataGame.instance.users.name, DataGame.instance.CurrentLevel.level + 1);
            DataGame.instance.totalPoint.point += value;
            FirebaseDataManager.instance.WriteDatabase("CurrentLevel", user.UserId, DataGame.instance.CurrentLevel);
            FirebaseDataManager.instance.WriteDatabase("TotalPoint", user.UserId, DataGame.instance.totalPoint);
            money = 50;
        }
        OpenWinPanel(sceneName, star, value, money);
    }
    void OpenWinPanel(string sceneName, int star, int point, int money)
    {
        winPanel.SetActive(true);
        DataGame.instance.users.coin += money;
        MusicBackGround.Stop();
        SoundEffect.PlayOneShot(SoundClip[0]);
        FirebaseDataManager.instance.WriteDatabase("Users", user.UserId, DataGame.instance.users);
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
        LoadLevelXRank(levelNumber);
    }
    void NextToLevel(string level)
    {
        SceneManager.LoadScene(level);
    }
    public async void LoadLevelXRank(int lv)
    {
        foreach (Transform item in ContentLevelDetail)
        {
            Destroy(item.gameObject);
        }
        await DataGame.instance.LoadTop10Level(lv);
        int currentRantPlayer = 0;
        foreach (Level level in DataGame.instance.LvXRank)
        {
            currentRantPlayer++;
            GameObject obj = Instantiate(LevelDetailPrefab, ContentLevelDetail);
            TextMeshProUGUI XH = obj.transform.Find("XH")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI Name = obj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI Point = obj.transform.Find("Point")?.GetComponent<TextMeshProUGUI>();

            XH.text = currentRantPlayer.ToString();
            Name.text = level.namePlayer;
            Point.text = level.point.ToString();
        }
        int myRank = await DataGame.instance.FindMyLevelXRank(lv);

        Level myLevel = DataGame.instance.levels.Find(level => (level.level == "Lv" + lv.ToString()));
        if (myLevel != null)
        {
            XHLevel.text = myRank.ToString();
            NamePlayerLevel.text = DataGame.instance.users.name;
            PointPlayerLevel.text = myLevel.point.ToString();        
        }
        else
        {
            XHLevel.text = "???";
            NamePlayerLevel.text = DataGame.instance.users.name;
            PointPlayerLevel.text = "0";                  
        }
    }
}
