using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// All UI for Flag Quiz. Subscribes to FlagQuizGameManager events.
/// </summary>
public class FlagQuizUIManager : MonoBehaviour
{
    [Header("Question Panel")]
    public Image flagImage;
    public TMP_Text questionCounterText;
    public TMP_Text difficultyText;

    [Header("Timer")]
    public Slider timerSlider;
    public Image timerFill;
    public Color timerFullColor = Color.green;
    public Color timerLowColor = Color.red;

    [Header("Answer Buttons")]
    public Transform answerGrid;            // parent of 4 buttons; resolved at runtime
    Button[] answerButtons = new Button[4];
    TMP_Text[] answerTexts = new TMP_Text[4];

    [Header("Feedback Panel")]
    public GameObject feedbackPanel;
    public TMP_Text feedbackResultText;
    public TMP_Text correctAnswerText;
    public TMP_Text bonusText;

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text streakHUDText;

    [Header("End Screen")]
    public GameObject endScreenPanel;
    public TMP_Text finalScoreText;
    public TMP_Text endMessageText;
    public Button restartButton;
    public Button hubButton;

    [Header("Difficulty Tag Colors")]
    public Color easyColor = new Color(0.18f, 0.80f, 0.44f);
    public Color mediumColor = new Color(0.95f, 0.70f, 0.10f);
    public Color hardColor = new Color(0.91f, 0.30f, 0.24f);

    [Header("Feedback Colors")]
    public Color correctColor = new Color(0.18f, 0.80f, 0.44f);
    public Color wrongColor = new Color(0.91f, 0.30f, 0.24f);

    void Awake()
    {
        if (answerGrid != null)
        {
            int n = Mathf.Min(4, answerGrid.childCount);
            for (int i = 0; i < n; i++)
            {
                var child = answerGrid.GetChild(i);
                answerButtons[i] = child.GetComponent<Button>();
                answerTexts[i]   = child.GetComponentInChildren<TMP_Text>(true);
            }
        }
        else
        {
            Debug.LogError("FlagQuizUIManager: answerGrid is not assigned.");
        }
    }

    void OnEnable()
    {
        FlagQuizGameManager.OnQuestionShown += HandleQuestionShown;
        FlagQuizGameManager.OnTimerTick     += HandleTimerTick;
        FlagQuizGameManager.OnAnswerResult  += HandleAnswerResult;
        FlagQuizGameManager.OnRoundEnd      += HandleRoundEnd;
    }

    void OnDisable()
    {
        FlagQuizGameManager.OnQuestionShown -= HandleQuestionShown;
        FlagQuizGameManager.OnTimerTick     -= HandleTimerTick;
        FlagQuizGameManager.OnAnswerResult  -= HandleAnswerResult;
        FlagQuizGameManager.OnRoundEnd      -= HandleRoundEnd;
    }

    // -------------------------------------------------------

    void HandleQuestionShown(FlagData correct, FlagData[] choices, int correctIdx,
        int qNum, int totalQ)
    {
        feedbackPanel.SetActive(false);
        SetButtonsInteractable(true);

        flagImage.sprite = correct.flagSprite;
        flagImage.color = Color.white;

        questionCounterText.text = $"Question {qNum} / {totalQ}";
        difficultyText.text = correct.difficulty.ToString().ToUpper();
        difficultyText.color = ColorFor(correct.difficulty);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < choices.Length)
            {
                answerTexts[i].text = choices[i].countryName;
                answerButtons[i].gameObject.SetActive(true);
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        timerSlider.value = 1f;
        timerFill.color = timerFullColor;
    }

    void HandleTimerTick(float normalized)
    {
        timerSlider.value = normalized;
        timerFill.color = Color.Lerp(timerLowColor, timerFullColor, normalized);
    }

    void HandleAnswerResult(bool correct, FlagData correctFlag, int score, int streak,
        int streakBonus, int timeBonus)
    {
        SetButtonsInteractable(false);

        scoreText.text = $"Score: {score}";
        streakHUDText.text = streak > 1 ? $"x{streak} Streak!" : "";

        feedbackPanel.SetActive(true);
        feedbackResultText.text = correct ? "CORRECT!" : "WRONG!";
        feedbackResultText.color = correct ? correctColor : wrongColor;
        correctAnswerText.text = correct ? "" : $"Correct answer: {correctFlag.countryName}";

        var bits = new List<string>();
        if (timeBonus > 0)   bits.Add($"+{timeBonus} time");
        if (streakBonus > 0) bits.Add($"+{streakBonus} streak");
        bonusText.text = bits.Count > 0 ? string.Join("    ", bits) : "";
    }

    void HandleRoundEnd(int finalScore)
    {
        feedbackPanel.SetActive(false);
        endScreenPanel.SetActive(true);
        finalScoreText.text = $"{finalScore}";
        endMessageText.text = GetEndMessage(finalScore);
    }

    // -------------------------------------------------------

    void SetButtonsInteractable(bool b)
    {
        foreach (var btn in answerButtons)
            if (btn != null) btn.interactable = b;
    }

    Color ColorFor(FlagDifficulty d)
    {
        switch (d)
        {
            case FlagDifficulty.Easy:   return easyColor;
            case FlagDifficulty.Medium: return mediumColor;
            case FlagDifficulty.Hard:   return hardColor;
            default: return Color.white;
        }
    }

    string GetEndMessage(int score)
    {
        if (score >= 1500) return "Geography genius!";
        if (score >= 1000) return "Excellent — you really know your flags.";
        if (score >= 600)  return "Solid effort — keep studying!";
        if (score >= 300)  return "Not bad — there's room to grow.";
        return "Time to brush up on world flags!";
    }
}
