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

    private Transform targetTrap;
    private bool isUpdatingUI = false;

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
    }
}