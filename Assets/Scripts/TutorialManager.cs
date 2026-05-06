using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Tutorial Steps")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    [Header("UI")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialText;
    public Image darkMask;
    public RectTransform handPointer;

    private int currentStep = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartTutorial();
    }

    public void StartTutorial()
    {
        currentStep = 0;
        ShowStep();
    }

    void ShowStep()
    {
        if (currentStep >= steps.Count)
        {
            EndTutorial();
            return;
        }

        TutorialStep step = steps[currentStep];

        tutorialPanel.SetActive(true);
        tutorialText.text = step.description;

        HighlightTarget(step.target);

        if (handPointer != null)
        {
            handPointer.position = step.target.position;
            handPointer.gameObject.SetActive(true);
        }
    }

    void HighlightTarget(RectTransform target)
    {
        // Tạm thời đặt pointer vào target
        // Sau này nâng cấp thành UI Mask thật sự
        darkMask.gameObject.SetActive(true);
    }

    public void NextStep()
    {
        currentStep++;
        ShowStep();
    }

    void EndTutorial()
    {
        tutorialPanel.SetActive(false);
        darkMask.gameObject.SetActive(false);
        handPointer.gameObject.SetActive(false);

        PlayerPrefs.SetInt("TutorialDone", 1);
    }
}