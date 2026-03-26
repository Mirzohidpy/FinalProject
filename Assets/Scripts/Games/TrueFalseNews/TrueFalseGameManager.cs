using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Core game logic for True or False News.
/// Attach this to a "GameManager" GameObject in the TrueFalseNews scene.
/// </summary>
public class TrueFalseGameManager : MonoBehaviour
{
    [Header("Data")]
    public HeadlineDatabase headlineDatabase;

    [Header("Settings")]
    public int headlinesPerRound = 10;
    public float timePerQuestion = 5f;
    public float explanationDisplayTime = 3f;

    [Header("Score")]
    public int pointsPerCorrect = 100;
    public int streakBonusMultiplier = 50; // bonus = streak * this

    // --- State ---
    private List<HeadlineData> roundHeadlines = new List<HeadlineData>();
    private int currentIndex = 0;
    private int score = 0;
    private int streak = 0;
    private bool inputAllowed = false;

    private Coroutine timerCoroutine;

    // --- Events (UI listens to these) ---
    public static event System.Action<HeadlineData, int> OnQuestionShown;     // headline, questionNumber
    public static event System.Action<float> OnTimerTick;                     // 0..1 normalized
    public static event System.Action<bool, string, int, int> OnAnswerResult; // correct, explanation, score, streak
    public static event System.Action<int> OnRoundEnd;                        // final score

    void Start()
    {
        StartRound();
    }

    // -------------------------------------------------------
    // Round setup
    // -------------------------------------------------------

    void StartRound()
    {
        score = 0;
        streak = 0;
        currentIndex = 0;
        roundHeadlines = PickRandomHeadlines(headlinesPerRound);
        ShowNextQuestion();
    }

    List<HeadlineData> PickRandomHeadlines(int count)
    {
        List<HeadlineData> pool = new List<HeadlineData>(headlineDatabase.headlines);
        List<HeadlineData> picked = new List<HeadlineData>();

        count = Mathf.Min(count, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int rand = Random.Range(0, pool.Count);
            picked.Add(pool[rand]);
            pool.RemoveAt(rand);
        }
        return picked;
    }

    // -------------------------------------------------------
    // Question flow
    // -------------------------------------------------------

    void ShowNextQuestion()
    {
        if (currentIndex >= roundHeadlines.Count)
        {
            EndRound();
            return;
        }

        HeadlineData headline = roundHeadlines[currentIndex];
        OnQuestionShown?.Invoke(headline, currentIndex + 1);

        inputAllowed = true;

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(QuestionTimer());
    }

    IEnumerator QuestionTimer()
    {
        float elapsed = 0f;

        while (elapsed < timePerQuestion)
        {
            elapsed += Time.deltaTime;
            float normalized = 1f - (elapsed / timePerQuestion);
            OnTimerTick?.Invoke(normalized);
            yield return null;
        }

        // Time's up — treat as wrong answer
        if (inputAllowed)
        {
            inputAllowed = false;
            streak = 0;
            HeadlineData current = roundHeadlines[currentIndex];
            OnAnswerResult?.Invoke(false, current.explanation, score, streak);
            yield return new WaitForSeconds(explanationDisplayTime);
            currentIndex++;
            ShowNextQuestion();
        }
    }

    // -------------------------------------------------------
    // Player input
    // -------------------------------------------------------

    /// <summary>Call from UI buttons: TrueButton.onClick and FalseButton.onClick</summary>
    public void OnPlayerAnswer(bool answeredReal)
    {
        if (!inputAllowed) return;
        inputAllowed = false;

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        HeadlineData current = roundHeadlines[currentIndex];
        bool correct = (answeredReal == current.isReal);

        if (correct)
        {
            streak++;
            int streakBonus = streak > 1 ? (streak - 1) * streakBonusMultiplier : 0;
            score += pointsPerCorrect + streakBonus;
        }
        else
        {
            streak = 0;
        }

        OnAnswerResult?.Invoke(correct, current.explanation, score, streak);
        StartCoroutine(AdvanceAfterDelay());
    }

    IEnumerator AdvanceAfterDelay()
    {
        yield return new WaitForSeconds(explanationDisplayTime);
        currentIndex++;
        ShowNextQuestion();
    }

    // -------------------------------------------------------
    // Round end
    // -------------------------------------------------------

    void EndRound()
    {
        OnRoundEnd?.Invoke(score);
    }

    // -------------------------------------------------------
    // Scene navigation (called by UI buttons)
    // -------------------------------------------------------

    public void ReturnToHub()
    {
        SceneManager.LoadScene("HubScene");
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
