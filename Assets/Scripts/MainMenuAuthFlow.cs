using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles main menu auth navigation:
/// - When clicking DangNhap: slide BTN group to the right, disable it, then slide PanelDangNhap in from the left.
/// - When clicking DangKy: same, but show PanelDangKy.
/// - When clicking QuayLai on a panel: slide panel out to the left, disable it, then slide BTN group in from the right and enable.
/// 
/// Uses unscaled time so it works regardless of Time.timeScale.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuAuthFlow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform btnGroup; // Canvas/MainMenuUI/BTN
    [SerializeField] private RectTransform panelDangNhap; // Canvas/PanelDangNhap
    [SerializeField] private RectTransform panelDangKy; // Canvas/PanelDangKy

    [Header("Buttons")]
    [SerializeField] private Button dangNhapButton; // BTN/DangNhap
    [SerializeField] private Button dangKyButton; // BTN/DangKy
    [SerializeField] private Button quayLaiDangNhapButton; // PanelDangNhap/.../QuayLai
    [SerializeField] private Button quayLaiDangKyButton; // PanelDangKy/.../QuayLai

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float duration = 0.25f;
    [SerializeField] private AnimationCurve ease = null;

    private RectTransform _canvasRect;
    private Vector2 _btnShown;
    private Vector2 _btnHiddenRight;

    private Vector2 _panelShown;
    private Vector2 _panelHiddenLeft;

    private Coroutine _routine;

    private void Reset()
    {
        ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
        duration = 0.25f;
    }

    private void Awake()
    {
        if (ease == null || ease.length == 0)
            ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("MainMenuAuthFlow: could not find parent Canvas.", this);
            enabled = false;
            return;
        }

        _canvasRect = canvas.GetComponent<RectTransform>();

        if (btnGroup == null || panelDangNhap == null || panelDangKy == null)
        {
            Debug.LogError("MainMenuAuthFlow: missing references (btnGroup/panels).", this);
            enabled = false;
            return;
        }

        CachePositions();

        // Ensure panels start hidden.
        if (panelDangNhap.gameObject.activeSelf)
            panelDangNhap.gameObject.SetActive(false);
        if (panelDangKy.gameObject.activeSelf)
            panelDangKy.gameObject.SetActive(false);

        if (dangNhapButton != null) dangNhapButton.onClick.AddListener(ShowDangNhap);
        if (dangKyButton != null) dangKyButton.onClick.AddListener(ShowDangKy);
        if (quayLaiDangNhapButton != null) quayLaiDangNhapButton.onClick.AddListener(BackToMain);
        if (quayLaiDangKyButton != null) quayLaiDangKyButton.onClick.AddListener(BackToMain);
    }

    private void OnEnable()
    {
        if (_canvasRect != null && btnGroup != null)
            CachePositions();
    }

    private void CachePositions()
    {
        float w = _canvasRect.rect.width;

        _btnShown = btnGroup.anchoredPosition;
        _btnHiddenRight = _btnShown + new Vector2(w, 0f);

        // Panels are full-screen; keep their shown position as (0,0) based on current.
        _panelShown = Vector2.zero;
        _panelHiddenLeft = new Vector2(-w, 0f);
    }

    public void ShowDangNhap() => StartFlow(panelDangNhap);
    public void ShowDangKy() => StartFlow(panelDangKy);

    private void StartFlow(RectTransform targetPanel)
    {
        if (targetPanel == null) return;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowPanelRoutine(targetPanel));
    }

    public void BackToMain()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(BackRoutine());
    }

    private IEnumerator ShowPanelRoutine(RectTransform targetPanel)
    {
        // Slide BTN group out to the right.
        btnGroup.gameObject.SetActive(true);
        yield return Slide(btnGroup, _btnShown, _btnHiddenRight);
        btnGroup.gameObject.SetActive(false);

        // Ensure both panels are off.
        if (panelDangNhap != targetPanel) panelDangNhap.gameObject.SetActive(false);
        if (panelDangKy != targetPanel) panelDangKy.gameObject.SetActive(false);

        // Slide target panel in from the left.
        targetPanel.gameObject.SetActive(true);
        targetPanel.anchoredPosition = _panelHiddenLeft;
        yield return Slide(targetPanel, _panelHiddenLeft, _panelShown);

        _routine = null;
    }

    private IEnumerator BackRoutine()
    {
        // Slide any active panel out to the left.
        RectTransform activePanel = null;
        if (panelDangNhap.gameObject.activeSelf) activePanel = panelDangNhap;
        else if (panelDangKy.gameObject.activeSelf) activePanel = panelDangKy;

        if (activePanel != null)
        {
            yield return Slide(activePanel, activePanel.anchoredPosition, _panelHiddenLeft);
            activePanel.gameObject.SetActive(false);
        }

        // Slide BTN group back in from the right.
        btnGroup.gameObject.SetActive(true);
        btnGroup.anchoredPosition = _btnHiddenRight;
        yield return Slide(btnGroup, _btnHiddenRight, _btnShown);

        _routine = null;
    }

    private IEnumerator Slide(RectTransform rt, Vector2 from, Vector2 to)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            float e = ease.Evaluate(u);
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            yield return null;
        }
        rt.anchoredPosition = to;
    }
}
