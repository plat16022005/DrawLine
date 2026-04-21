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
    [Tooltip("Dùng 2 component Image để tạo hiệu ứng cross-fade (chuyển cảnh mượt)")]
    public Image backgroundImageA;
    public Image backgroundImageB;
    public TextMeshProUGUI storyText;

    [Header("Story Settings")]
    public List<StorySlide> slides;
    public float fadeDuration = 1.5f;
    public float typingSpeed = 0.05f;
    public float erasingSpeed = 0.02f;

    private bool useImageA = true;

    void Awake()
    {
        // Khởi tạo mọi thứ về trạng thái tàng hình (Alpha = 0)
        if (backgroundImageA != null) backgroundImageA.color = new Color(1, 1, 1, 0);
        if (backgroundImageB != null) backgroundImageB.color = new Color(1, 1, 1, 0);
        if (storyText != null) storyText.text = "";
    }

    void Start()
    {
        // Bắt đầu kể chuyện khi script được Active
        if (slides.Count > 0)
        {
            StartCoroutine(PlayStory());
        }
    }

    IEnumerator PlayStory()
    {
        for (int i = 0; i < slides.Count; i++)
        {
            StorySlide slide = slides[i];

            // 1. Xác định Image nào đang dùng để fade in, Image nào fade out
            Image currentImage = useImageA ? backgroundImageA : backgroundImageB;
            Image previousImage = useImageA ? backgroundImageB : backgroundImageA;

            // Gán ảnh mới
            if (slide.image != null)
            {
                currentImage.sprite = slide.image;
            }

            // 2. Chuyển ảnh (Vừa làm mờ ảnh cũ, vừa làm rõ ảnh mới)
            Coroutine imageFade = StartCoroutine(CrossFadeImages(currentImage, previousImage, fadeDuration));

            // 3. Xóa dần chữ cũ đi (nếu đang có chữ)
            if (storyText.text.Length > 0)
            {
                yield return StartCoroutine(EraseTextCoroutine());
            }

            // Đợi cho việc chuyển ảnh hoàn thành (nếu thích chữ hiện ra sau khi ảnh đã rõ)
            yield return imageFade;

            // 4. Bắt đầu gõ phím chữ mới
            yield return StartCoroutine(TypeTextCoroutine(slide.text));

            // 5. Chờ thời gian hiển thị mà bạn đã setup
            yield return new WaitForSeconds(slide.waitTime);

            // Đổi cờ để luân phiên dùng 2 Image component
            useImageA = !useImageA;
        }
        
        // --- KẾT THÚC TRUYỆN ---
        // 1. Xóa dần chữ cuối cùng (nếu còn)
        if (storyText.text.Length > 0)
        {
            yield return StartCoroutine(EraseTextCoroutine());
        }

        // 2. Làm mờ toàn bộ ảnh trên màn hình
        Coroutine fadeA = StartCoroutine(FadeOutSingleImage(backgroundImageA, fadeDuration));
        Coroutine fadeB = StartCoroutine(FadeOutSingleImage(backgroundImageB, fadeDuration));

        // Đợi cho cả 2 ảnh mờ đi hoàn toàn
        yield return fadeA;
        yield return fadeB;

        // 3. Chuyển sang Scene mới
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }

    IEnumerator FadeOutSingleImage(Image img, float duration)
    {
        if (img == null) yield break;
        
        float time = 0;
        Color c = img.color;
        float startAlpha = c.a;

        while (time < duration)
        {
            time += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, 0, time / duration);
            img.color = c;
            yield return null;
        }

        c.a = 0;
        img.color = c;
    }

    IEnumerator CrossFadeImages(Image imageIn, Image imageOut, float duration)
    {
        float time = 0;
        
        // Đặt ảnh mới về alpha 0
        Color colorIn = imageIn.color;
        colorIn.a = 0f;
        imageIn.color = colorIn;

        Color colorOut = imageOut != null ? imageOut.color : Color.clear;

        while (time < duration)
        {
            time += Time.deltaTime;
            float normalizedTime = time / duration;

            // Fade in ảnh mới dần sáng lên
            if (imageIn.sprite != null)
            {
                colorIn.a = Mathf.Lerp(0, 1, normalizedTime);
                imageIn.color = colorIn;
            }

            // Fade out ảnh cũ mờ dần đi
            if (imageOut != null && imageOut.sprite != null)
            {
                colorOut.a = Mathf.Lerp(1, 0, normalizedTime);
                imageOut.color = colorOut;
            }

            yield return null;
        }

        // Chốt lại giá trị chính xác là 1 và 0 sau khi hết vòng lặp
        colorIn.a = 1f;
        if (imageIn.sprite != null) imageIn.color = colorIn;

        if (imageOut != null)
        {
            colorOut.a = 0f;
            imageOut.color = colorOut;
        }
    }

    IEnumerator TypeTextCoroutine(string textToType)
    {
        storyText.text = "";
        foreach (char c in textToType.ToCharArray())
        {
            storyText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    IEnumerator EraseTextCoroutine()
    {
        while (storyText.text.Length > 0)
        {
            // Cắt đi ký tự cuối cùng để tạo hiệu ứng xóa
            storyText.text = storyText.text.Substring(0, storyText.text.Length - 1);
            yield return new WaitForSeconds(erasingSpeed);
        }
    }
}
