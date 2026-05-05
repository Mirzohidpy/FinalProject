using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmotionUIManager : MonoBehaviour
{
    [Header("Question Panel")]
    public Image faceImage;
    public TMP_Text questionCounterText;
    public TMP_Text difficultyText;

    [Header("Timer")]
    public Slider timerSlider;
    public Image timerFill;
    public Color timerFullColor = Color.green;
    public Color timerLowColor = Color.red;

    [Header("Answer Buttons")]
    public Transform answerGrid;
    Button[] answerButtons = new Button[4];
    TMP_Text[] answerTexts = new TMP_Text[4];

    [Header("Feedback Panel")]
    public GameObject feedbackPanel;
    public TMP_Text feedbackResultText;
    public TMP_Text correctAnswerText;
    public TMP_Text bonusText;
    public TMP_Text mentalTipText;

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
                answerTexts[i] = child.GetComponentInChildren<TMP_Text>(true);
            }
        }
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        if (endScreenPanel != null) endScreenPanel.SetActive(false);
    }

    void OnEnable()
    {
        EmotionGameManager.OnQuestionShown += HandleQuestionShown;
        EmotionGameManager.OnTimerTick     += HandleTimerTick;
        EmotionGameManager.OnAnswerResult  += HandleAnswerResult;
        EmotionGameManager.OnRoundEnd      += HandleRoundEnd;
    }

    void OnDisable()
    {
        EmotionGameManager.OnQuestionShown -= HandleQuestionShown;
        EmotionGameManager.OnTimerTick     -= HandleTimerTick;
        EmotionGameManager.OnAnswerResult  -= HandleAnswerResult;
        EmotionGameManager.OnRoundEnd      -= HandleRoundEnd;
    }

    void HandleQuestionShown(EmotionData correct, EmotionData[] choices, int correctIdx,
        int qNum, int totalQ)
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        SetButtonsInteractable(true);

        if (faceImage != null)
        {
            faceImage.sprite = correct.faceSprite;
            faceImage.color = Color.white;
        }
        if (questionCounterText != null) questionCounterText.text = $"Question {qNum} / {totalQ}";
        if (difficultyText != null)
        {
            difficultyText.text = correct.difficulty.ToString().ToUpper();
            difficultyText.color = ColorFor(correct.difficulty);
        }

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < choices.Length)
            {
                if (answerTexts[i] != null) answerTexts[i].text = choices[i].emotionName;
                if (answerButtons[i] != null) answerButtons[i].gameObject.SetActive(true);
            }
            else if (answerButtons[i] != null)
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        if (timerSlider != null) timerSlider.value = 1f;
        if (timerFill != null) timerFill.color = timerFullColor;
    }

    void HandleTimerTick(float normalized)
    {
        if (timerSlider != null) timerSlider.value = normalized;
        if (timerFill != null) timerFill.color = Color.Lerp(timerLowColor, timerFullColor, normalized);
    }

    void HandleAnswerResult(bool correct, EmotionData correctEmotion, int score, int streak,
        int streakBonus, int timeBonus)
    {
        SetButtonsInteractable(false);

        if (scoreText != null) scoreText.text = $"Score: {score}";
        if (streakHUDText != null) streakHUDText.text = streak > 1 ? $"x{streak} Streak!" : "";

        if (feedbackPanel != null) feedbackPanel.SetActive(true);
        if (feedbackResultText != null)
        {
            feedbackResultText.text = correct ? "CORRECT!" : "WRONG!";
            feedbackResultText.color = correct ? correctColor : wrongColor;
        }
        if (correctAnswerText != null)
            correctAnswerText.text = correct ? "" : $"Correct answer: {correctEmotion.emotionName}";

        var bits = new List<string>();
        if (timeBonus > 0)   bits.Add($"+{timeBonus} time");
        if (streakBonus > 0) bits.Add($"+{streakBonus} streak");
        if (bonusText != null) bonusText.text = bits.Count > 0 ? string.Join("    ", bits) : "";

        if (mentalTipText != null)
            mentalTipText.text = !string.IsNullOrEmpty(correctEmotion.mentalHealthTip)
                ? "Tip: " + correctEmotion.mentalHealthTip
                : "";
    }

    void HandleRoundEnd(int finalScore)
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        if (endScreenPanel != null) endScreenPanel.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = $"{finalScore}";
        if (endMessageText != null) endMessageText.text = GetEndMessage(finalScore);
    }

    void SetButtonsInteractable(bool b)
    {
        foreach (var btn in answerButtons)
            if (btn != null) btn.interactable = b;
    }

    Color ColorFor(EmotionDifficulty d)
    {
        switch (d)
        {
            case EmotionDifficulty.Easy:   return easyColor;
            case EmotionDifficulty.Medium: return mediumColor;
            case EmotionDifficulty.Hard:   return hardColor;
            default: return Color.white;
        }
    }

    string GetEndMessage(int score)
    {
        if (score >= 1500) return "Excellent emotional awareness!";
        if (score >= 1000) return "Great empathy - well done.";
        if (score >= 600)  return "Solid - keep paying attention to feelings.";
        if (score >= 300)  return "Not bad - emotions take practice.";
        return "Keep watching faces around you.";
    }
}
