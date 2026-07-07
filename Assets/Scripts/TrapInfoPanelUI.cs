using TMPro;
using UnityEngine;

public class TrapInfoPanelUI : MonoBehaviour
{
    public GameObject panel;

    public TMP_InputField posXText;
    public TMP_InputField posYText;
    public TMP_InputField scaleXText;
    public TMP_InputField scaleYText;

    public TMP_Text rotationText;

    [Header("Config panel cho thuộc tính riêng của từng loại bẫy")]
    public TrapConfigPanelUI trapConfigPanel;

    private Transform targetTrap;
    private Transform lastConfigTarget; // cache tránh rebuild mỗi frame
    private bool isUpdatingUI = false;

    [HideInInspector]
    public bool isHiddenByUser = false;

    void Start()
    {
        if (scaleXText != null)
            scaleXText.onValueChanged.AddListener(OnScaleChanged);

        if (scaleYText != null)
            scaleYText.onValueChanged.AddListener(OnScaleChanged);
    }

    public void SetTarget(Transform target)
    {
        targetTrap = target;

        // Chỉ rebuild khi target thực sự thay đổi — tránh destroy/recreate InputField mỗi frame
        if (trapConfigPanel != null && target != lastConfigTarget)
        {
            lastConfigTarget = target;
            ITrapConfig config = target != null
                ? target.GetComponentInChildren<ITrapConfig>(true) // true = include disabled
                : null;
            trapConfigPanel.SetConfig(config);
        }
    }

    public void Show(Vector3 position, float rotationZ, Vector3 scale)
    {
        panel.SetActive(true);

        isUpdatingUI = true;

        posXText.SetTextWithoutNotify(position.x.ToString("0.00"));
        posYText.SetTextWithoutNotify(position.y.ToString("0.00"));

        scaleXText.SetTextWithoutNotify(scale.x.ToString("0.00"));
        scaleYText.SetTextWithoutNotify(scale.y.ToString("0.00"));

        if (rotationText != null)
            rotationText.text = rotationZ.ToString("0") + "°";

        isUpdatingUI = false;
    }

    void OnScaleChanged(string value)
    {
        if (isUpdatingUI) return;
        if (targetTrap == null) return;

        float scaleX = targetTrap.localScale.x;
        float scaleY = targetTrap.localScale.y;

        if (float.TryParse(scaleXText.text, out float x))
            scaleX = x;

        if (float.TryParse(scaleYText.text, out float y))
            scaleY = y;

        targetTrap.localScale = new Vector3(scaleX, scaleY, 1);
    }

    public void Hide()
    {
        panel.SetActive(false);
        targetTrap = null;
        lastConfigTarget = null;

        // Luôn ẩn TrapConfigPanel khi InfoPanel ẩn
        // TrapSettingPanelUI sẽ tự hiện lại sau đó (trong LateUpdate) nếu đang mở trap cụ thể
        if (trapConfigPanel != null)
            trapConfigPanel.SetConfig(null);
    }
    public void Close()
    {
        panel.SetActive(false);
        isHiddenByUser = true;

        if (trapConfigPanel != null)
            trapConfigPanel.SetConfig(null);
    }
}