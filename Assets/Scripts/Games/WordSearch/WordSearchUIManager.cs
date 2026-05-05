using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// All UI for Word Search. Subscribes to WordSearchGameManager events.
/// </summary>
public class WordSearchUIManager : MonoBehaviour
{
    [Header("Grid")]
    public WordSearchGrid grid;

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text foundCountText;

    [Header("Timer")]
    public Slider timerSlider;
    public Image timerFill;
    public Color timerFullColor = new Color(0.180f, 0.800f, 0.443f);
    public Color timerLowColor = new Color(0.906f, 0.298f, 0.235f);

    [Header("Word List")]
    public Transform wordListParent;
    public TMP_Text wordEntryTemplate;

    [Header("Toast")]
    public GameObject toastPanel;
    public TMP_Text toastText;
    public float toastSeconds = 1.2f;

    [Header("End Screen")]
    public GameObject endScreenPanel;
    public TMP_Text finalScoreText;
    public TMP_Text endMessageText;
    public Button restartButton;
    public Button hubButton;

    [Header("Colors")]
    public Color foundWordColor = new Color(0.180f, 0.800f, 0.443f);
    public Color unfoundWordColor = Color.white;

    readonly Dictionary<string, TMP_Text> wordEntries = new Dictionary<string, TMP_Text>();
    Coroutine toastRoutine;

    void OnEnable()
    {
        WordSearchGameManager.OnRoundStarted     += HandleRoundStarted;
        WordSearchGameManager.OnTimerTick        += HandleTimerTick;
        WordSearchGameManager.OnWordFound        += HandleWordFound;
        WordSearchGameManager.OnSelectionRejected += HandleSelectionRejected;
        WordSearchGameManager.OnRoundEnd         += HandleRoundEnd;
    }

    void OnDisable()
    {
        WordSearchGameManager.OnRoundStarted     -= HandleRoundStarted;
        WordSearchGameManager.OnTimerTick        -= HandleTimerTick;
        WordSearchGameManager.OnWordFound        -= HandleWordFound;
        WordSearchGameManager.OnSelectionRejected -= HandleSelectionRejected;
        WordSearchGameManager.OnRoundEnd         -= HandleRoundEnd;
    }

    void HandleRoundStarted(char[,] gridChars,
        List<WordSearchGameManager.WordPlacement> placements)
    {
        if (endScreenPanel != null) endScreenPanel.SetActive(false);
        if (toastPanel != null) toastPanel.SetActive(false);
        scoreText.text = "Score: 0";
        foundCountText.text = $"0 / {placements.Count}";

        grid.BuildLetters(gridChars);

        if (wordEntryTemplate != null) wordEntryTemplate.gameObject.SetActive(false);

        foreach (var entry in wordEntries.Values)
            if (entry != null && entry != wordEntryTemplate) Destroy(entry.gameObject);
        wordEntries.Clear();

        foreach (var p in placements)
        {
            var entry = Instantiate(wordEntryTemplate, wordListParent);
            entry.text = p.word;
            entry.color = unfoundWordColor;
            entry.fontStyle = FontStyles.Bold;
            entry.gameObject.SetActive(true);
            wordEntries[p.word] = entry;
        }

        timerSlider.value = 1f;
        timerFill.color = timerFullColor;
    }

    void HandleTimerTick(float normalized)
    {
        timerSlider.value = normalized;
        timerFill.color = Color.Lerp(timerLowColor, timerFullColor, normalized);
    }

    void HandleWordFound(WordSearchGameManager.WordPlacement p,
        int score, int found, int total)
    {
        scoreText.text = $"Score: {score}";
        foundCountText.text = $"{found} / {total}";

        if (wordEntries.TryGetValue(p.word, out var entry) && entry != null)
        {
            entry.color = foundWordColor;
            entry.fontStyle = FontStyles.Bold | FontStyles.Strikethrough;
        }
        grid.MarkFound(p.startRow, p.startCol, p.endRow, p.endCol);
        ShowToast($"FOUND: {p.word}");
    }

    void HandleSelectionRejected()
    {
        grid.ClearSelection();
    }

    void HandleRoundEnd(int finalScore, bool allFound, float timeRemaining,
        int found, int total)
    {
        if (endScreenPanel == null) return;
        endScreenPanel.SetActive(true);
        finalScoreText.text = $"{finalScore}";
        if (allFound)
        {
            int seconds = Mathf.RoundToInt(timeRemaining);
            endMessageText.text = $"All {total} words found! {seconds}s left.\nCivic genius.";
        }
        else
        {
            endMessageText.text = $"Time's up — you found {found} of {total} words.";
        }
    }

    void ShowToast(string message)
    {
        if (toastPanel == null || toastText == null) return;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastText.text = message;
        toastPanel.SetActive(true);
        toastRoutine = StartCoroutine(HideToast());
    }

    IEnumerator HideToast()
    {
        yield return new WaitForSeconds(toastSeconds);
        toastPanel.SetActive(false);
        toastRoutine = null;
    }
}
