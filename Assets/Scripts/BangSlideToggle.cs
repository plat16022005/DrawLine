using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggle a UI panel (Bang) by sliding it to the left and disabling it, while keeping the toggle button visible.
/// Attach this script to the On/Off button GameObject (the one with a Button component).
/// </summary>
[DisallowMultipleComponent]
public class BangSlideToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform bang; // Panel to slide
    [SerializeField] private RectTransform toggleButton; // The button rect that should move with the panel

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float duration = 0.25f;
    [SerializeField] private AnimationCurve ease = null;
    [SerializeField, Min(0f)] private float rightMargin = 16f;

    private Button _button;
    private Canvas _canvas;
    private RectTransform _canvasRect;

    private Vector2 _bangShownPos;
    private Vector2 _bangHiddenPos;

    private Vector2 _btnShownPos;
    private Vector2 _btnHiddenPos;

    private bool _isHidden;
    private Coroutine _routine;

    private void Reset()
    {
        // Reasonable default curve.
        ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    private void Awake()
    {
        if (ease == null || ease.length == 0)
            ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

        _button = GetComponent<Button>();
        if (_button == null)
        {
            Debug.LogError("BangSlideToggle must be attached to a GameObject with a UnityEngine.UI.Button.", this);
            enabled = false;
            return;
        }

        if (toggleButton == null)
            toggleButton = transform as RectTransform;

        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError("BangSlideToggle could not find a parent Canvas.", this);
            enabled = false;
            return;
        }

        _canvasRect = _canvas.GetComponent<RectTransform>();

        // Auto-find Bang as a sibling under the same canvas.
        if (bang == null)
        {
            var t = _canvas.transform.Find("Bang");
            if (t != null)
                bang = t as RectTransform;
        }

        if (bang == null)
        {
            Debug.LogError("BangSlideToggle: assign the Bang RectTransform (panel) in the inspector.", this);
            enabled = false;
            return;
        }

        CachePositions();

        // Determine initial hidden state.
        _isHidden = !bang.gameObject.activeSelf;

        _button.onClick.AddListener(Toggle);
    }

    private void OnEnable()
    {
        // If resolution/layout changes in edit mode or runtime, keep positions consistent.
        if (_canvasRect != null && bang != null && toggleButton != null)
            CachePositions();
    }

    private void CachePositions()
    {
        _bangShownPos = bang.anchoredPosition;
        _btnShownPos = toggleButton.anchoredPosition;

        float canvasHalfWidth = _canvasRect.rect.width * 0.5f;

        // Hide Bang fully past the RIGHT edge (so it slides to the right when hiding).
        float bangWidth = bang.rect.width;
        float bangHiddenX = canvasHalfWidth + bangWidth; // fully off-screen to the right
        _bangHiddenPos = new Vector2(bangHiddenX, _bangShownPos.y);

        // Keep the toggle button visible near the RIGHT edge.
        float btnWidth = toggleButton.rect.width;
        float btnHiddenX = canvasHalfWidth - (btnWidth * 0.5f) - rightMargin;
        _btnHiddenPos = new Vector2(btnHiddenX, _btnShownPos.y);
    }

    public void Toggle()
    {
        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(_isHidden ? ShowRoutine() : HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        // Ensure active while animating out.
        if (!bang.gameObject.activeSelf)
            bang.gameObject.SetActive(true);

        yield return Slide(_bangShownPos, _bangHiddenPos, _btnShownPos, _btnHiddenPos);

        bang.gameObject.SetActive(false);
        _isHidden = true;
        _routine = null;
    }

    private IEnumerator ShowRoutine()
    {
        bang.gameObject.SetActive(true);

        // Start from hidden positions.
        bang.anchoredPosition = _bangHiddenPos;
        toggleButton.anchoredPosition = _btnHiddenPos;

        yield return Slide(_bangHiddenPos, _bangShownPos, _btnHiddenPos, _btnShownPos);

        _isHidden = false;
        _routine = null;
    }

    private IEnumerator Slide(Vector2 bangFrom, Vector2 bangTo, Vector2 btnFrom, Vector2 btnTo)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            float e = ease.Evaluate(u);

            bang.anchoredPosition = Vector2.LerpUnclamped(bangFrom, bangTo, e);
            toggleButton.anchoredPosition = Vector2.LerpUnclamped(btnFrom, btnTo, e);

            yield return null;
        }

        bang.anchoredPosition = bangTo;
        toggleButton.anchoredPosition = btnTo;
    }
}
