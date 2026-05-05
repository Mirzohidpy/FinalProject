using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MazeRunnerUIManager : MonoBehaviour
{
    [Header("HUD")]
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text gatesText;

    [Header("Question Popup")]
    public GameObject questionPanel;
    public TMP_Text questionText;
    public TMP_Text questionCounterText;
    public Transform answerGrid;

    [Header("Feedback")]
    public TMP_Text feedbackText;
    public Color correctColor = new Color(0.18f, 0.80f, 0.44f);
    public Color wrongColor   = new Color(0.91f, 0.30f, 0.24f);
    public float popupHideDelay = 0.6f;

    [Header("Win Screen")]
    public GameObject winPanel;
    public TMP_Text finalScoreText;
    public TMP_Text finalTimeText;
    public Button restartButton;
    public Button hubButton;

    Button[] answerButtons = new Button[4];
    TMP_Text[] answerTexts = new TMP_Text[4];
    int totalGates;
    MazeRunnerGameManager gameManager;

    void Awake()
    {
        gameManager = FindFirstObjectByType<MazeRunnerGameManager>();

        if (answerGrid != null)
        {
            int n = Mathf.Min(4, answerGrid.childCount);
            for (int i = 0; i < n; i++)
            {
                var child = answerGrid.GetChild(i);
                answerButtons[i] = child.GetComponent<Button>();
                answerTexts[i] = child.GetComponentInChildren<TMP_Text>(true);
                int idx = i;
                if (answerButtons[i] != null)
                    answerButtons[i].onClick.AddListener(() => OnAnswerClicked(idx));
            }
        }

        if (questionPanel != null) questionPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (feedbackText != null) feedbackText.text = "";
    }

    void OnEnable()
    {
        MazeRunnerGameManager.OnGameStart      += HandleGameStart;
        MazeRunnerGameManager.OnTimerTick      += HandleTimerTick;
        MazeRunnerGameManager.OnGateApproached += HandleGateApproached;
        MazeRunnerGameManager.OnGateAnswered   += HandleGateAnswered;
        MazeRunnerGameManager.OnGameWon        += HandleGameWon;
    }

    void OnDisable()
    {
        MazeRunnerGameManager.OnGameStart      -= HandleGameStart;
        MazeRunnerGameManager.OnTimerTick      -= HandleTimerTick;
        MazeRunnerGameManager.OnGateApproached -= HandleGateApproached;
        MazeRunnerGameManager.OnGateAnswered   -= HandleGateAnswered;
        MazeRunnerGameManager.OnGameWon        -= HandleGameWon;
    }

    void HandleGameStart(int total)
    {
        totalGates = total;
        if (scoreText != null) scoreText.text = "Score: 0";
        if (gatesText != null) gatesText.text = $"Gates: 0 / {total}";
    }

    void HandleTimerTick(float secs)
    {
        if (timerText == null) return;
        int m = (int)(secs / 60);
        int s = (int)(secs % 60);
        timerText.text = $"{m:D2}:{s:D2}";
    }

    void HandleGateApproached(MazeQuestionData q, int gateNumber, int total)
    {
        if (questionPanel == null) return;
        if (questionText != null) questionText.text = q.question;
        if (questionCounterText != null) questionCounterText.text = $"Gate {gateNumber} / {total}";

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (q.options != null && i < q.options.Length)
            {
                if (answerTexts[i] != null) answerTexts[i].text = q.options[i];
                if (answerButtons[i] != null)
                {
                    answerButtons[i].gameObject.SetActive(true);
                    answerButtons[i].interactable = true;
                }
            }
            else if (answerButtons[i] != null)
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        if (feedbackText != null) feedbackText.text = "";
        questionPanel.SetActive(true);
    }

    void HandleGateAnswered(bool correct, int score, int gatesPassed)
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
        if (gatesText != null) gatesText.text = $"Gates: {gatesPassed} / {totalGates}";

        if (feedbackText != null)
        {
            feedbackText.text = correct ? "Correct! Gate opens." : "Wrong - try another answer.";
            feedbackText.color = correct ? correctColor : wrongColor;
        }

        if (correct) StartCoroutine(HidePopupAfterDelay());
    }

    IEnumerator HidePopupAfterDelay()
    {
        yield return new WaitForSeconds(popupHideDelay);
        if (questionPanel != null) questionPanel.SetActive(false);
        if (feedbackText != null) feedbackText.text = "";
    }

    void HandleGameWon(int finalScore, float timeTaken)
    {
        if (winPanel != null) winPanel.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = finalScore.ToString();
        if (finalTimeText != null)
        {
            int m = (int)(timeTaken / 60);
            int s = (int)(timeTaken % 60);
            finalTimeText.text = $"Time: {m:D2}:{s:D2}";
        }
    }

    void OnAnswerClicked(int idx)
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<MazeRunnerGameManager>();
        if (gameManager != null) gameManager.OnPlayerAnswer(idx);
    }
}
