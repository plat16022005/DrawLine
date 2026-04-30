using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggle UI panel (Bang) bằng cách trượt sang phải để ẩn,
/// đồng thời giữ nút toggle luôn nằm ở mép phải màn hình.
/// Script này ổn định trên nhiều tỉ lệ màn hình:
/// - điện thoại dài
/// - iPad / tablet
/// - màn hình vuông
/// - màn có notch
/// </summary>
[DisallowMultipleComponent]
public class BangSlideToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform bang; // Panel cần ẩn/hiện
    [SerializeField] private RectTransform toggleButton; // Nút On/Off

    [Header("Animation")]
    [SerializeField, Min(0.05f)]
    private float duration = 0.25f;

    [SerializeField]
    private AnimationCurve ease;

    [SerializeField, Min(0f)]
    private float rightMargin = 16f; // khoảng cách nút tới mép phải

    private Button _button;
    private Canvas _canvas;
    private RectTransform _canvasRect;

    // vị trí panel
    private Vector2 _bangShownPos;
    private Vector2 _bangHiddenPos;

    // vị trí nút toggle
    private Vector2 _btnShownPos;
    private Vector2 _btnHiddenPos;

    private bool _isHidden = false;
    private Coroutine _routine;

    private void Reset()
    {
        ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    private void Awake()
    {
        if (ease == null || ease.length == 0)
            ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

        _button = GetComponent<Button>();
        if (_button == null)
        {
            Debug.LogError("BangSlideToggle phải gắn trên GameObject có Button.");
            enabled = false;
            return;
        }

        if (toggleButton == null)
            toggleButton = transform as RectTransform;

        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError("Không tìm thấy Canvas cha.");
            enabled = false;
            return;
        }

        _canvasRect = _canvas.GetComponent<RectTransform>();

        // Tự tìm object tên Bang nếu chưa kéo thả
        if (bang == null)
        {
            Transform t = _canvas.transform.Find("Bang");
            if (t != null)
                bang = t as RectTransform;
        }

        if (bang == null)
        {
            Debug.LogError("Hãy assign Bang RectTransform trong Inspector.");
            enabled = false;
            return;
        }

        SetupAnchors();
        CachePositions();

        _isHidden = !bang.gameObject.activeSelf;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(Toggle);
    }

    private void OnEnable()
    {
        if (_canvasRect != null && bang != null && toggleButton != null)
        {
            SetupAnchors();
            CachePositions();
        }
    }

    /// <summary>
    /// Tự ép Anchor/Pivot về chuẩn để tránh lỗi lệch UI
    /// </summary>
    private void SetupAnchors()
    {
        // Toggle Button:
        // Anchor Right Center
        toggleButton.anchorMin = new Vector2(1f, 0.5f);
        toggleButton.anchorMax = new Vector2(1f, 0.5f);
        toggleButton.pivot = new Vector2(1f, 0.5f);

        // Bang Panel:
        // Anchor Right Center
        bang.anchorMin = new Vector2(1f, 0.5f);
        bang.anchorMax = new Vector2(1f, 0.5f);
        bang.pivot = new Vector2(1f, 0.5f);
    }

    /// <summary>
    /// Tính toán vị trí hiện / ẩn
    /// </summary>
    private void CachePositions()
    {
        // vị trí hiện tại khi panel đang mở
        _bangShownPos = bang.anchoredPosition;
        _btnShownPos = toggleButton.anchoredPosition;

        float bangWidth = bang.rect.width;
        float btnWidth = toggleButton.rect.width;

        // Panel trượt hẳn ra ngoài bên phải
        float bangHiddenX = bangWidth + rightMargin;

        // Nút vẫn nằm sát mép phải
        float btnHiddenX = -rightMargin;

        _bangHiddenPos = new Vector2(
            bangHiddenX,
            _bangShownPos.y
        );

        _btnHiddenPos = new Vector2(
            btnHiddenX,
            _btnShownPos.y
        );
    }

    public void Toggle()
    {
        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(
            _isHidden ? ShowRoutine() : HideRoutine()
        );
    }

    private IEnumerator HideRoutine()
    {
        if (!bang.gameObject.activeSelf)
            bang.gameObject.SetActive(true);

        yield return Slide(
            _bangShownPos,
            _bangHiddenPos,
            _btnShownPos,
            _btnHiddenPos
        );

        bang.gameObject.SetActive(false);
        _isHidden = true;
        _routine = null;
    }

    private IEnumerator ShowRoutine()
    {
        bang.gameObject.SetActive(true);

        // bắt đầu từ vị trí ẩn
        bang.anchoredPosition = _bangHiddenPos;
        toggleButton.anchoredPosition = _btnHiddenPos;

        yield return Slide(
            _bangHiddenPos,
            _bangShownPos,
            _btnHiddenPos,
            _btnShownPos
        );

        _isHidden = false;
        _routine = null;
    }

    private IEnumerator Slide(
        Vector2 bangFrom,
        Vector2 bangTo,
        Vector2 btnFrom,
        Vector2 btnTo)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / duration);
            float e = ease.Evaluate(t);

            bang.anchoredPosition =
                Vector2.LerpUnclamped(
                    bangFrom,
                    bangTo,
                    e
                );

            toggleButton.anchoredPosition =
                Vector2.LerpUnclamped(
                    btnFrom,
                    btnTo,
                    e
                );

            yield return null;
        }

        bang.anchoredPosition = bangTo;
        toggleButton.anchoredPosition = btnTo;
    }
}