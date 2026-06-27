using TMPro;
using UnityEngine;

public class TrapSettingPanelUI : MonoBehaviour
{
    public GameObject panel;

    public TMP_InputField posXInput;
    public TMP_InputField posYInput;
    public TMP_InputField scaleXInput;
    public TMP_InputField scaleYInput;

    public RotationCircleUI rotationCircleUI;

    [Header("Config panel cho thuộc tính riêng của từng loại bẫy")]
    public TrapConfigPanelUI trapConfigPanel;

    private Transform targetTrap;

    void Start()
    {
        panel.SetActive(false);
    }

    public void Open(Transform trap)
    {
        targetTrap = trap;
        panel.SetActive(true);
        RefreshUI();

        if (trapConfigPanel != null)
        {
            ITrapConfig config = trap != null
                ? trap.GetComponentInChildren<ITrapConfig>(true)
                : null;
            trapConfigPanel.SetConfig(config);
        }
    }

    void RefreshUI()
    {
        if (targetTrap == null) return;

        posXInput.SetTextWithoutNotify(targetTrap.position.x.ToString("0.00"));
        posYInput.SetTextWithoutNotify(targetTrap.position.y.ToString("0.00"));
        scaleXInput.SetTextWithoutNotify(targetTrap.localScale.x.ToString("0.00"));
        scaleYInput.SetTextWithoutNotify(targetTrap.localScale.y.ToString("0.00"));

        if (rotationCircleUI != null)
            rotationCircleUI.SetAngle(targetTrap.eulerAngles.z, false);
    }

    public void Confirm()
    {
        if (targetTrap == null) return;

        // Ghi giá trị config riêng vào component trước
        if (trapConfigPanel != null)
            trapConfigPanel.Apply();

        Vector3 pos = targetTrap.position;
        Vector3 scale = targetTrap.localScale;
        float angle = targetTrap.eulerAngles.z;

        if (float.TryParse(posXInput.text, out float x))
            pos.x = x;

        if (float.TryParse(posYInput.text, out float y))
            pos.y = y;

        if (float.TryParse(scaleXInput.text, out float sx))
            scale.x = sx;

        if (float.TryParse(scaleYInput.text, out float sy))
            scale.y = sy;

        if (rotationCircleUI != null)
            angle = rotationCircleUI.Angle;

        targetTrap.position = new Vector3(pos.x, pos.y, 0);
        targetTrap.localScale = new Vector3(scale.x, scale.y, 1);
        targetTrap.rotation = Quaternion.Euler(0, 0, angle);

        Close();
    }

    public void Close()
    {
        targetTrap = null;
        panel.SetActive(false);

        if (trapConfigPanel != null)
            trapConfigPanel.SetConfig(null);
    }
}