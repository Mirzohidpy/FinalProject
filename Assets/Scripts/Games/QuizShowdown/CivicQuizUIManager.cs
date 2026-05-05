using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CivicQuizUIManager : MonoBehaviour
{
    [Header("HUD")]
    public TMP_Text questionCounterText;
    public TMP_Text scoreText;
    public TMP_Text streakText;
    public TMP_Text basePointsText;

    [Header("Question")]
    public TMP_Text questionText;
    public Slider timerSlider;
    public Image timerFill;
    public Color timerFullColor = Color.green;
    public Color timerLowColor = Color.red;

    [Header("Answers (parent must have 4 button children)")]
    public Transform answerGrid;
    public Color answerNormalColor = new Color(0.96f, 0.97f, 1f);
    public Color answerCorrectColor = new Color(0.18f, 0.80f, 0.44f);
    public Color answerWrongColor = new Color(0.91f, 0.30f, 0.24f);
    public Color answerHiddenColor = new Color(0.5f, 0.5f, 0.6f, 0.45f);

    [Header("Lifelines")]
    public Button fiftyFiftyButton;
    public Button hintButton;
    public Button pauseButton;
    public Color lifelineActive = new Color(0.95f, 0.70f, 0.10f);
    public Color lifelineUsed = new Color(0.4f, 0.4f, 0.45f);

    [Header("Hint Modal")]
    public GameObject hintPanel;
    public TMP_Text hintText;
    public Button hintCloseButton;

    [Header("End Screen")]
    public GameObject endScreenPanel;
    public TMP_Text endTitleText;
    public TMP_Text finalScoreText;
    public TMP_InputField nameInputField;
    public Button saveScoreButton;
    public Transform leaderboardContainer;
    public GameObject leaderboardEntryTemplate;
    public Button restartButton;
    public Button hubButton;

    [Header("Win/Lose colors")]
    public Color winColor = new Color(0.18f, 0.80f, 0.44f);
    public Color loseColor = new Color(0.91f, 0.30f, 0.24f);

    Button[] answerButtons = new Button[4];
    Image[] answerImages = new Image[4];
    TMP_Text[] answerTexts = new TMP_Text[4];

    CivicQuizGameManager gameManager;
    int finalScore;
    bool scoreSaved;

    void Awake()
    {
        gameManager = FindFirstObjectByType<CivicQuizGameManager>();

        if (answerGrid != null)
        {
            int n = Mathf.Min(4, answerGrid.childCount);
            for (int i = 0; i < n; i++)
            {
                var child = answerGrid.GetChild(i);
                answerButtons[i] = child.GetComponent<Button>();
                answerImages[i] = child.GetComponent<Image>();
                answerTexts[i] = child.GetComponentInChildren<TMP_Text>(true);
                int idx = i;
                if (answerButtons[i] != null)
                    answerButtons[i].onClick.AddListener(() => OnAnswerClicked(idx));
            }
        }

        if (fiftyFiftyButton != null) fiftyFiftyButton.onClick.AddListener(OnFiftyFiftyClicked);
        if (hintButton != null) hintButton.onClick.AddListener(OnHintClicked);
        if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
        if (hintCloseButton != null) hintCloseButton.onClick.AddListener(CloseHint);
        if (saveScoreButton != null) saveScoreButton.onClick.AddListener(OnSaveScoreClicked);

        if (hintPanel != null) hintPanel.SetActive(false);
        if (endScreenPanel != null) endScreenPanel.SetActive(false);
        if (leaderboardEntryTemplate != null) leaderboardEntryTemplate.SetActive(false);
    }

    void OnEnable()
    {
        CivicQuizGameManager.OnQuestionShown      += HandleQuestionShown;
        CivicQuizGameManager.OnTimerTick          += HandleTimerTick;
        CivicQuizGameManager.OnFiftyFiftyApplied  += HandleFiftyFifty;
        CivicQuizGameManager.OnHintShown          += HandleHintShown;
        CivicQuizGameManager.OnPauseUsed          += HandlePauseUsed;
        CivicQuizGameManager.OnAnswerResult       += HandleAnswerResult;
        CivicQuizGameManager.OnGameEnd            += HandleGameEnd;
    }

    void OnDisable()
    {
        CivicQuizGameManager.OnQuestionShown      -= HandleQuestionShown;
        CivicQuizGameManager.OnTimerTick          -= HandleTimerTick;
        CivicQuizGameManager.OnFiftyFiftyApplied  -= HandleFiftyFifty;
        CivicQuizGameManager.OnHintShown          -= HandleHintShown;
        CivicQuizGameManager.OnPauseUsed          -= HandlePauseUsed;
        CivicQuizGameManager.OnAnswerResult       -= HandleAnswerResult;
        CivicQuizGameManager.OnGameEnd            -= HandleGameEnd;
    }

    static readonly string[] LetterPrefix = { "A.  ", "B.  ", "C.  ", "D.  " };

    void HandleQuestionShown(CivicQuizQuestion q, int qNum, int totalQ, int basePts, int currentScore)
    {
        if (questionText != null) questionText.text = q.question;
        if (questionCounterText != null) questionCounterText.text = $"Question {qNum} / {totalQ}";
        if (scoreText != null) scoreText.text = $"Score: {currentScore:N0}";
        if (basePointsText != null) basePointsText.text = $"Worth: {basePts:N0}";

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] == null) continue;
            if (i < q.options.Length)
            {
                if (answerTexts[i] != null) answerTexts[i].text = LetterPrefix[i] + q.options[i];
                if (answerImages[i] != null) answerImages[i].color = answerNormalColor;
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].interactable = true;
            }
            else
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

    void HandleFiftyFifty(HashSet<int> hidden)
    {
        foreach (int i in hidden)
        {
            if (i < 0 || i >= answerButtons.Length) continue;
            if (answerImages[i] != null) answerImages[i].color = answerHiddenColor;
            if (answerButtons[i] != null) answerButtons[i].interactable = false;
            if (answerTexts[i] != null) answerTexts[i].text = "";
        }
        if (fiftyFiftyButton != null)
        {
            fiftyFiftyButton.interactable = false;
            var img = fiftyFiftyButton.GetComponent<Image>();
            if (img != null) img.color = lifelineUsed;
        }
    }

    void HandleHintShown(string hint)
    {
        if (hintPanel == null) return;
        if (hintText != null) hintText.text = string.IsNullOrEmpty(hint) ? "(no hint)" : hint;
        hintPanel.SetActive(true);
        if (hintButton != null)
        {
            hintButton.interactable = false;
            var img = hintButton.GetComponent<Image>();
            if (img != null) img.color = lifelineUsed;
        }
    }

    void CloseHint()
    {
        if (hintPanel != null) hintPanel.SetActive(false);
    }

    void HandlePauseUsed()
    {
        if (pauseButton != null)
        {
            pauseButton.interactable = false;
            var img = pauseButton.GetComponent<Image>();
            if (img != null) img.color = lifelineUsed;
        }
    }

    void HandleAnswerResult(bool correct, int correctIdx, int points, int score, int streak, int multPercent)
    {
        if (scoreText != null) scoreText.text = $"Score: {score:N0}";
        if (streakText != null)
            streakText.text = streak > 1 ? $"x{multPercent / 100f:0.0} streak" : "";

        if (correctIdx >= 0 && correctIdx < answerImages.Length && answerImages[correctIdx] != null)
            answerImages[correctIdx].color = answerCorrectColor;

        for (int i = 0; i < answerButtons.Length; i++)
            if (answerButtons[i] != null) answerButtons[i].interactable = false;
    }

    void HandleGameEnd(int finalScoreVal, int answered, bool wonAll)
    {
        finalScore = finalScoreVal;
        scoreSaved = false;
        if (endScreenPanel != null) endScreenPanel.SetActive(true);
        if (endTitleText != null)
        {
            endTitleText.text = wonAll ? "PERFECT GAME!" : "GAME OVER";
            endTitleText.color = wonAll ? winColor : loseColor;
        }
        if (finalScoreText != null) finalScoreText.text = $"{finalScoreVal:N0}";
        if (nameInputField != null)
        {
            nameInputField.text = "";
            nameInputField.gameObject.SetActive(true);
        }
        if (saveScoreButton != null) saveScoreButton.gameObject.SetActive(true);
        RenderLeaderboard(-1);
    }

    void OnAnswerClicked(int idx)
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<CivicQuizGameManager>();
        if (gameManager != null) gameManager.OnPlayerAnswer(idx);
    }

    void OnFiftyFiftyClicked()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<CivicQuizGameManager>();
        if (gameManager != null) gameManager.UseFiftyFifty();
    }

    void OnHintClicked()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<CivicQuizGameManager>();
        if (gameManager != null) gameManager.UseHint();
    }

    void OnPauseClicked()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<CivicQuizGameManager>();
        if (gameManager != null) gameManager.UsePause();
    }

    void OnSaveScoreClicked()
    {
        if (scoreSaved) return;
        scoreSaved = true;
        string name = nameInputField != null ? nameInputField.text : "Player";
        int rank = CivicQuizGameManager.SaveLeaderboardEntry(name, finalScore);
        if (nameInputField != null) nameInputField.gameObject.SetActive(false);
        if (saveScoreButton != null) saveScoreButton.gameObject.SetActive(false);
        RenderLeaderboard(rank);
    }

    void RenderLeaderboard(int highlightRank)
    {
        if (leaderboardContainer == null || leaderboardEntryTemplate == null) return;

        for (int i = leaderboardContainer.childCount - 1; i >= 0; i--)
        {
            var child = leaderboardContainer.GetChild(i).gameObject;
            if (child == leaderboardEntryTemplate) continue;
            Destroy(child);
        }

        var entries = CivicQuizGameManager.LoadLeaderboard();
        for (int i = 0; i < entries.Count; i++)
        {
            var go = Instantiate(leaderboardEntryTemplate, leaderboardContainer);
            go.SetActive(true);
            var rankT = go.transform.Find("RankText")?.GetComponent<TMP_Text>();
            var nameT = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
            var scoreT = go.transform.Find("ScoreText")?.GetComponent<TMP_Text>();
            if (rankT != null) rankT.text = $"{i + 1}.";
            if (nameT != null) nameT.text = entries[i].Key;
            if (scoreT != null) scoreT.text = $"{entries[i].Value:N0}";
            if (i == highlightRank)
            {
                var img = go.GetComponent<Image>();
                if (img != null) img.color = new Color(0.95f, 0.70f, 0.10f, 0.35f);
            }
        }
    }
}
