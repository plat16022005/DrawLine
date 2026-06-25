using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class RotationCircleUI : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    public RectTransform circle;
    public RectTransform handle;
    public TMP_InputField angleText;

    public float Angle { get; private set; }

    public System.Action<float> OnAngleChanged;

    void Start()
    {
        if (angleText != null)
            angleText.onValueChanged.AddListener(OnInputChanged);

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateAngle(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateAngle(eventData);
    }

    void UpdateAngle(PointerEventData eventData)
    {
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            circle,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        float angle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;

        SetAngle(angle);
    }

    void OnInputChanged(string value)
    {
        if (!float.TryParse(value, out float angle))
            return;

        SetAngle(angle);
    }

    public void SetAngle(float angle, bool notify = true)
    {
        angle %= 360f;

        if (angle < 0)
            angle += 360f;

        Angle = angle;

        float radius = circle.rect.width / 2f - handle.rect.width / 2f;

        Vector2 dir = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );

        handle.anchoredPosition = dir * radius;

        if (angleText != null)
            angleText.SetTextWithoutNotify(angle.ToString("0"));

        if (notify)
            OnAngleChanged?.Invoke(Angle);
    }
}