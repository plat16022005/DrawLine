using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class StorySlide
{
    public Sprite image;

    [TextArea(3, 10)]
    public string text;

    [Tooltip("Thời gian chờ trước khi tự động chuyển sang slide tiếp theo")]
    public float waitTime = 2f;
}

public class StoryController : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImageA;
    public Image backgroundImageB;
    public TextMeshProUGUI storyText;

    [Header("Skip Button")]
    public GameObject skipButton; // kéo nút Skip vào đây

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip typingClip;

    [Range(0f, 1f)]
    public float typingVolume = 1f;

    [Header("Story Settings")]
    public List<StorySlide> slides;
    public float fadeDuration = 1.5f;
    public float typingSpeed = 0.05f;
    public float erasingSpeed = 0.02f;

    private bool useImageA = true;

    void Awake()
    {
        if (backgroundImageA != null)
            backgroundImageA.color = new Color(1, 1, 1, 0);

        if (backgroundImageB != null)
            backgroundImageB.color = new Color(1, 1, 1, 0);

        if (storyText != null)
            storyText.text = "";

        // Ẩn nút Skip khi mới vào game
        if (skipButton != null)
            skipButton.SetActive(false);

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();

            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }
    }

    void Start()
    {
        // Sau 3 giây hiện nút Skip
        StartCoroutine(ShowSkipButtonAfterDelay());

        if (slides.Count > 0)
        {
            StartCoroutine(PlayStory());
        }
    }

    // =========================
    // HÀM CHỜ 3 GIÂY HIỆN SKIP
    // =========================
    IEnumerator ShowSkipButtonAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        if (skipButton != null)
        {
            skipButton.SetActive(true);
        }
    }

    // Hàm gán vào OnClick của nút Skip
    public void SkipStory()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }

    IEnumerator PlayStory()
    {
        for (int i = 0; i < slides.Count; i++)
        {
            StorySlide slide = slides[i];

            Image currentImage = useImageA ? backgroundImageA : backgroundImageB;
            Image previousImage = useImageA ? backgroundImageB : backgroundImageA;

            if (slide.image != null)
            {
                currentImage.sprite = slide.image;
            }

            Coroutine imageFade = StartCoroutine(
                CrossFadeImages(currentImage, previousImage, fadeDuration));

            if (storyText.text.Length > 0)
            {
                yield return StartCoroutine(EraseTextCoroutine());
            }

            yield return imageFade;

            yield return StartCoroutine(TypeTextCoroutine(slide.text));

            yield return new WaitForSeconds(slide.waitTime);

            useImageA = !useImageA;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }

    IEnumerator CrossFadeImages(Image imageIn, Image imageOut, float duration)
    {
        float time = 0;

        Color colorIn = imageIn.color;
        colorIn.a = 0f;
        imageIn.color = colorIn;

        Color colorOut = imageOut != null ? imageOut.color : Color.clear;

        while (time < duration)
        {
            time += Time.deltaTime;
            float normalizedTime = time / duration;

            if (imageIn.sprite != null)
            {
                colorIn.a = Mathf.Lerp(0, 1, normalizedTime);
                imageIn.color = colorIn;
            }

            if (imageOut != null && imageOut.sprite != null)
            {
                colorOut.a = Mathf.Lerp(1, 0, normalizedTime);
                imageOut.color = colorOut;
            }

            yield return null;
        }
    }

    IEnumerator TypeTextCoroutine(string textToType)
    {
        storyText.text = "";

        if (typingClip != null && sfxSource != null)
        {
            sfxSource.clip = typingClip;
            sfxSource.volume = typingVolume;
            sfxSource.loop = true;

            if (!sfxSource.isPlaying)
                sfxSource.Play();
        }

        foreach (char c in textToType.ToCharArray())
        {
            storyText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        if (sfxSource != null && sfxSource.isPlaying)
            sfxSource.Stop();
    }

    IEnumerator EraseTextCoroutine()
    {
        while (storyText.text.Length > 0)
        {
            storyText.text =
                storyText.text.Substring(0, storyText.text.Length - 1);

            yield return new WaitForSeconds(erasingSpeed);
        }
    }
}