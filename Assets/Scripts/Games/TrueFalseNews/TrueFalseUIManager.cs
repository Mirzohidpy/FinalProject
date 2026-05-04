using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI for the True or False News game.
/// Attach to a "UIManager" GameObject. Wire up all references in the Inspector.
/// </summary>
public class TrueFalseUIManager : MonoBehaviour
{
    [Header("Question Panel")]
    public TMP_Text headlineText;
    public TMP_Text categoryText;
    public TMP_Text sourceHintText;
    public TMP_Text questionCounterText;   // "Question 3 / 10"

    [Header("Timer")]
    public Slider timerSlider;
    public Image timerFill;                // assign the Fill image of the slider
    public Color timerColorFull = Color.green;
    public Color timerColorLow = Color.red;

    [Header("Answer Buttons")]
    public Button realButton;
    public Button fakeButton;
    public TMP_Text realButtonText;
    public TMP_Text fakeButtonText;

    [Header("Feedback Panel")]
    public GameObject feedbackPanel;
    public TMP_Text feedbackResultText;    // "CORRECT!" / "WRONG!" / "TIME'S UP!"
    public TMP_Text explanationText;
    public TMP_Text streakText;

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text streakHUDText;

    [Header("End Screen")]
    public GameObject endScreenPanel;
    public TMP_Text finalScoreText;
    public TMP_Text endMessageText;
    public Button restartButton;
    public Button hubButton;

    [Header("Colors")]
    public Color correctColor = new Color(0.2f, 0.8f, 0.3f);
    public Color wrongColor = new Color(0.9f, 0.2f, 0.2f);

    void OnEnable()
    {
        TrueFalseGameManager.OnQuestionShown  += HandleQuestionShown;
        TrueFalseGameManager.OnTimerTick      += HandleTimerTick;
        TrueFalseGameManager.OnAnswerResult   += HandleAnswerResult;
        TrueFalseGameManager.OnRoundEnd       += HandleRoundEnd;
    }

    void OnDisable()
    {
        TrueFalseGameManager.OnQuestionShown  -= HandleQuestionShown;
        TrueFalseGameManager.OnTimerTick      -= HandleTimerTick;
        TrueFalseGameManager.OnAnswerResult   -= HandleAnswerResult;
        TrueFalseGameManager.OnRoundEnd       -= HandleRoundEnd;
    }

    // -------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------

    void HandleQuestionShown(HeadlineData data, int questionNumber, int totalQuestions)
    {
        feedbackPanel.SetActive(false);
        SetButtonsInteractable(true);

        headlineText.text = data.headline;
        categoryText.text = data.category.ToString().ToUpper();
        sourceHintText.text = data.sourceHint;
        questionCounterText.text = $"Question {questionNumber} / {totalQuestions}";

        timerSlider.value = 1f;
        timerFill.color = timerColorFull;
    }

    void HandleTimerTick(float normalized)
    {
        timerSlider.value = normalized;
        timerFill.color = Color.Lerp(timerColorLow, timerColorFull, normalized);
    }

    void HandleAnswerResult(bool correct, string explanation, int score, int streak, int streakBonus)
    {
        SetButtonsInteractable(false);

        scoreText.text = $"Score: {score}";
        streakHUDText.text = streak > 1 ? $"x{streak} Streak!" : "";

        feedbackPanel.SetActive(true);
        feedbackResultText.text = correct ? "CORRECT!" : "WRONG!";
        feedbackResultText.color = correct ? correctColor : wrongColor;
        explanationText.text = explanation;
        streakText.text = streakBonus > 0 ? $"{streak} in a row! +{streakBonus} bonus" : "";
    }

    void HandleRoundEnd(int finalScore)
    {
        feedbackPanel.SetActive(false);
        endScreenPanel.SetActive(true);

        finalScoreText.text = $"{finalScore}";
        endMessageText.text = GetEndMessage(finalScore);
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

    void SetButtonsInteractable(bool interactable)
    {
        realButton.interactable = interactable;
        fakeButton.interactable = interactable;
    }

    string GetEndMessage(int score)
    {
        if (score >= 1200) return "Outstanding! You're a fact-checking pro!";
        if (score >= 800)  return "Great job! Hard to fool you.";
        if (score >= 400)  return "Not bad — keep sharpening those skills!";
        return "Keep practicing — fake news is everywhere!";
    }
}
