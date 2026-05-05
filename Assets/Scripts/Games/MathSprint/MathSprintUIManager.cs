using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// All UI for Math Sprint. Subscribes to MathSprintGameManager events.
/// </summary>
public class MathSprintUIManager : MonoBehaviour
{
    [Header("Question")]
    public TMP_Text equationText;
    public TMP_Text questionCounterText;
    public TMP_Text levelText;

    [Header("Timer")]
    public Slider timerSlider;
    public Image timerFill;
    public Color timerFullColor = new Color(0.180f, 0.800f, 0.443f);
    public Color timerLowColor = new Color(0.906f, 0.298f, 0.235f);

    [Header("Answer Buttons")]
    public Transform answerGrid;
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
    public TMP_Text bestStreakText;
    public Button restartButton;
    public Button hubButton;

    [Header("Colors")]
    public Color correctColor = new Color(0.180f, 0.800f, 0.443f);
    public Color wrongColor = new Color(0.906f, 0.298f, 0.235f);
    public Color suddenDeathColor = new Color(0.906f, 0.298f, 0.235f);
    public Color regularLevelColor = Color.white;

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
            Debug.LogError("MathSprintUIManager: answerGrid is not assigned.");
        }
    }

    void OnEnable()
    {
        MathSprintGameManager.OnQuestionShown += HandleQuestionShown;
        MathSprintGameManager.OnTimerTick     += HandleTimerTick;
        MathSprintGameManager.OnAnswerResult  += HandleAnswerResult;
        MathSprintGameManager.OnLevelStarted  += HandleLevelStarted;
        MathSprintGameManager.OnRoundEnd      += HandleRoundEnd;
    }

    void OnDisable()
    {
        MathSprintGameManager.OnQuestionShown -= HandleQuestionShown;
        MathSprintGameManager.OnTimerTick     -= HandleTimerTick;
        MathSprintGameManager.OnAnswerResult  -= HandleAnswerResult;
        MathSprintGameManager.OnLevelStarted  -= HandleLevelStarted;
        MathSprintGameManager.OnRoundEnd      -= HandleRoundEnd;
    }

    void HandleQuestionShown(string equation, int[] choices, int correctIdx,
        int qNum, int totalQ, MathLevelData level)
    {
        feedbackPanel.SetActive(false);
        SetButtonsInteractable(true);

        equationText.text = equation;
        questionCounterText.text = $"Question {qNum} / {totalQ}";
        levelText.text = level.levelName.ToUpper();
        levelText.color = level.suddenDeath ? suddenDeathColor : regularLevelColor;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < choices.Length)
            {
                answerTexts[i].text = choices[i].ToString();
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

    void HandleAnswerResult(bool correct, int correctAnswer, int score, int streak,
        int streakBonus, int timeBonus, bool gameOver)
    {
        SetButtonsInteractable(false);

        scoreText.text = $"Score: {score}";
        streakHUDText.text = streak > 1 ? $"x{streak} Streak!" : "";

        feedbackPanel.SetActive(true);
        feedbackResultText.text = gameOver ? "GAME OVER!" : (correct ? "CORRECT!" : "WRONG!");
        feedbackResultText.color = correct ? correctColor : wrongColor;
        correctAnswerText.text = correct ? "" : $"Answer: {correctAnswer}";

        var bits = new List<string>();
        if (timeBonus > 0)   bits.Add($"+{timeBonus} time");
        if (streakBonus > 0) bits.Add($"+{streakBonus} streak");
        bonusText.text = bits.Count > 0 ? string.Join("    ", bits) : "";
    }

    void HandleLevelStarted(MathLevelData level, int levelIndex, int totalLevels)
    {
        // Visual cue handled by HandleQuestionShown's level color/text
    }

    void HandleRoundEnd(int finalScore, int bestStreak)
    {
        feedbackPanel.SetActive(false);
        endScreenPanel.SetActive(true);
        finalScoreText.text = $"{finalScore}";
        bestStreakText.text = $"Best Streak: x{bestStreak}";
        endMessageText.text = GetEndMessage(finalScore);
    }

    void SetButtonsInteractable(bool b)
    {
        foreach (var btn in answerButtons)
            if (btn != null) btn.interactable = b;
    }

    string GetEndMessage(int score)
    {
        if (score >= 5000) return "Math wizard!";
        if (score >= 3000) return "Sharp mind — well done.";
        if (score >= 1500) return "Solid effort — keep training!";
        if (score >= 800)  return "Not bad — practice makes perfect.";
        return "Keep practicing those math skills!";
    }
}
