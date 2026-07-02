using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapSettingsPanelUI : MonoBehaviour
{
    [Header("UI Controls")]
    public Slider cameraLensSlider;
    public TextMeshProUGUI cameraLensText;

    public TMP_InputField inkCostInput;

    public TMP_Dropdown weatherDropdown;

    [Header("Wind Settings UI")]
    public Toggle enableWindToggle;
    public GameObject windSettingsPanel;
    public Slider windForceSlider;
    public TextMeshProUGUI windForceText;

    public Slider windAngleSlider;
    public TextMeshProUGUI windAngleText;

    [Header("Applied Values (Saved to Map)")]
    public float currentCameraLens = 5f;
    public float currentInkCostPerUnit = 30f;
    public int currentWeatherType = 0;
    public bool currentEnableWind = false;
    public float currentWindForce = 15f;
    public float currentWindAngle = 180f;

    public GlobalWind globalWind; // Kéo thả từ Inspector

    public GameObject panel;

    // Các giá trị tạm thời khi đang chỉnh sửa UI (chưa bấm Xác nhận)
    private float tempCameraLens;
    private float tempInkCostPerUnit;
    private int tempWeatherType;
    private bool tempEnableWind;
    private float tempWindForce;
    private float tempWindAngle;

    void Awake()
    {
        // Tự động rót dữ liệu cho Weather Dropdown dựa trên enum WeatherType
        if (weatherDropdown != null)
        {
            weatherDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>(System.Enum.GetNames(typeof(WeatherType)));
            weatherDropdown.AddOptions(options);
            weatherDropdown.onValueChanged.AddListener(OnWeatherChanged);
        }

        // Đảm bảo các slider có Min/Max đúng để không bị lỗi clamp về 1
        if (cameraLensSlider != null)
        {
            cameraLensSlider.minValue = 3f;
            cameraLensSlider.maxValue = 20f;
            cameraLensSlider.onValueChanged.AddListener(OnCameraLensChanged);
        }

        if (inkCostInput != null) inkCostInput.onValueChanged.AddListener(OnInkCostChanged);
        
        if (enableWindToggle != null) enableWindToggle.onValueChanged.AddListener(OnEnableWindChanged);
        
        if (windForceSlider != null)
        {
            windForceSlider.minValue = 0f;
            windForceSlider.maxValue = 50f;
            windForceSlider.onValueChanged.AddListener(OnWindForceChanged);
        }

        if (windAngleSlider != null)
        {
            windAngleSlider.minValue = 0f;
            windAngleSlider.maxValue = 360f;
            windAngleSlider.onValueChanged.AddListener(OnWindAngleChanged);
        }
    }



    private void UpdateUI()
    {
        if (cameraLensSlider != null) cameraLensSlider.value = tempCameraLens;
        if (inkCostInput != null) inkCostInput.text = tempInkCostPerUnit.ToString();
        if (weatherDropdown != null) weatherDropdown.value = tempWeatherType;
        
        if (enableWindToggle != null) enableWindToggle.isOn = tempEnableWind;
        if (windSettingsPanel != null) windSettingsPanel.SetActive(tempEnableWind);

        if (windForceSlider != null) windForceSlider.value = tempWindForce;
        if (windAngleSlider != null) windAngleSlider.value = tempWindAngle;

        if (cameraLensText != null) cameraLensText.text = tempCameraLens.ToString("F1");
        if (windForceText != null) windForceText.text = tempWindForce.ToString("F1");
        if (windAngleText != null) windAngleText.text = tempWindAngle.ToString("F0") + "°";
    }

    public void OnEnableWindChanged(bool isOn)
    {
        tempEnableWind = isOn;
        if (windSettingsPanel != null) windSettingsPanel.SetActive(isOn);
    }

    public void OnCameraLensChanged(float value)
    {
        tempCameraLens = value;
        if (cameraLensText != null) cameraLensText.text = tempCameraLens.ToString("F1");
    }

    public void OnInkCostChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            tempInkCostPerUnit = result;
        }
    }

    public void OnWeatherChanged(int value)
    {
        tempWeatherType = value;
    }

    public void OnWindForceChanged(float value)
    {
        tempWindForce = value;
        if (windForceText != null) windForceText.text = tempWindForce.ToString("F1");
    }

    public void OnWindAngleChanged(float value)
    {
        tempWindAngle = value;
        if (windAngleText != null) windAngleText.text = tempWindAngle.ToString("F0") + "°";
    }

    // Gán hàm này vào nút Xác nhận (Confirm)
    public void OnConfirmSettings()
    {
        // 1. Chốt các giá trị
        currentCameraLens = tempCameraLens;
        currentInkCostPerUnit = tempInkCostPerUnit;
        currentWeatherType = tempWeatherType;
        currentEnableWind = tempEnableWind;
        currentWindForce = tempWindForce;
        currentWindAngle = tempWindAngle;

        // 2. Áp dụng ngay vào môi trường (nếu đang ở Editor Scene)
        if (Camera.main != null)
        {
            Camera.main.orthographicSize = currentCameraLens;
            CameraController camCtrl = Camera.main.GetComponent<CameraController>();
            if (camCtrl != null)
            {
                camCtrl.UpdateOriginalZoom(currentCameraLens);
            }
        }

        if (InkManager.Instance != null)
        {
            InkManager.Instance.inkCostPerUnit = currentInkCostPerUnit;
        }

        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.SetWeather((WeatherType)currentWeatherType);
        }

        if (globalWind != null)
        {
            globalWind.gameObject.SetActive(currentEnableWind);
            if (currentEnableWind)
            {
                globalWind.ApplySettings(currentWindForce, currentWindAngle);
            }
        }

        // 3. Tắt panel
        ClosePanel();
    }

    // Hàm gọi khi vừa load map (FirebaseMapLoader)
    public void LoadFromData(MapData data)
    {
        if (data == null) return;
        currentCameraLens = data.cameraLens > 0 ? data.cameraLens : 5f;
        currentInkCostPerUnit = data.inkCostPerUnit > 0 ? data.inkCostPerUnit : 30f;
        currentWeatherType = data.weatherType;
        currentEnableWind = data.enableWind;
        currentWindForce = data.windForce;
        currentWindAngle = data.windAngle;
        
        // Cập nhật lại UI nếu panel đang mở
        if (panel != null && panel.activeInHierarchy)
        {
            tempCameraLens = currentCameraLens;
            tempInkCostPerUnit = currentInkCostPerUnit;
            tempWeatherType = currentWeatherType;
            tempEnableWind = currentEnableWind;
            tempWindForce = currentWindForce;
            tempWindAngle = currentWindAngle;
            UpdateUI();
        }
    }

    public void OpenPanel()
    {
        // Khi mở panel, copy các giá trị đã chốt (hoặc mặc định) vào biến tạm để hiển thị lên UI
        tempCameraLens = currentCameraLens;
        tempInkCostPerUnit = currentInkCostPerUnit;
        tempWeatherType = currentWeatherType;
        tempEnableWind = currentEnableWind;
        tempWindForce = currentWindForce;
        tempWindAngle = currentWindAngle;

        UpdateUI();

        if (panel != null) panel.SetActive(true);
    }
    public void ClosePanel()
    {
        panel.SetActive(false);
    }
}
