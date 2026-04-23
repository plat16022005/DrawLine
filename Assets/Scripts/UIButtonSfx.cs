using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Plays UI SFX for hover/click.
/// Attach to a UI element that has a Selectable (Button, Toggle, etc.).
/// </summary>
[DisallowMultipleComponent]
public class UIButtonSfx : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    public void Configure(AudioSource source, AudioClip hover, AudioClip click, float vol = 1f)
    {
        sfxSource = source;
        hoverClip = hover;
        clickClip = click;
        volume = Mathf.Clamp01(vol);
    }

    private Selectable _selectable;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        if (sfxSource == null)
            sfxSource = FindFirstObjectByType<AudioSource>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        if (hoverClip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(hoverClip, volume);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!IsInteractable()) return;
        if (clickClip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clickClip, volume);
    }

    private bool IsInteractable()
    {
        return _selectable == null || _selectable.IsInteractable();
    }
}
