using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Game logic for Flag Quiz.
/// Round = N easy + N medium + N hard questions, each with 4 multiple-choice options.
/// Scoring: base + time bonus + streak bonus.
/// </summary>
public class FlagQuizGameManager : MonoBehaviour
{
    [Header("Data")]
    public FlagDatabase flagDatabase;

    [Header("Round Composition")]
    public int easyQuestions = 3;
    public int mediumQuestions = 4;
    public int hardQuestions = 3;

    [Header("Settings")]
    public float timePerQuestion = 8f;
    public float feedbackDisplayTime = 2f;

    [Header("Score")]
    public int pointsPerCorrect = 100;
    public int timeBonusMultiplier = 10;     // score += round(timeRemaining * this)
    public int streakBonusMultiplier = 50;   // bonus = (streak - 1) * this

    // --- State ---
    List<FlagData> roundFlags = new List<FlagData>();
    FlagData[] currentChoices;
    int correctChoiceIndex;
    int currentIndex = 0;
    int score = 0;
    int streak = 0;
    bool inputAllowed = false;
    float timeRemaining = 0f;
    Coroutine timerCoroutine;

    // --- Events ---
    public static event System.Action<FlagData, FlagData[], int, int, int> OnQuestionShown;       // correct, choices, correctIdx, qNum, totalQ
    public static event System.Action<float> OnTimerTick;                                          // 0..1
    public static event System.Action<bool, FlagData, int, int, int, int> OnAnswerResult;          // correct, correctFlag, score, streak, streakBonus, timeBonus
    public static event System.Action<int> OnRoundEnd;                                             // final score

    void Start()
    {
        StartRound();
    }

    void StartRound()
    {
        if (flagDatabase == null || flagDatabase.flags == null || flagDatabase.flags.Length == 0)
        {
            Debug.LogError("FlagQuizGameManager: flagDatabase is missing or empty. " +
                "Run BrainCitizen > Build FlagQuiz Game.");
            return;
        }

        score = 0;
        streak = 0;
        currentIndex = 0;
        roundFlags = PickRoundFlags();
        ShowNextQuestion();
    }

    List<FlagData> PickRoundFlags()
    {
        var easy = FilterByDifficulty(FlagDifficulty.Easy);
        var medium = FilterByDifficulty(FlagDifficulty.Medium);
        var hard = FilterByDifficulty(FlagDifficulty.Hard);

        var result = new List<FlagData>();
        result.AddRange(PickN(easy, easyQuestions));
        result.AddRange(PickN(medium, mediumQuestions));
        result.AddRange(PickN(hard, hardQuestions));
        return result;
    }

    List<FlagData> FilterByDifficulty(FlagDifficulty d)
    {
        var list = new List<FlagData>();
        foreach (var f in flagDatabase.flags)
            if (f != null && f.difficulty == d) list.Add(f);
        return list;
    }

    List<FlagData> PickN(List<FlagData> pool, int n)
    {
        var copy = new List<FlagData>(pool);
        var picks = new List<FlagData>();
        n = Mathf.Min(n, copy.Count);
        for (int i = 0; i < n; i++)
        {
            int idx = Random.Range(0, copy.Count);
            picks.Add(copy[idx]);
            copy.RemoveAt(idx);
        }
        return picks;
    }

    void ShowNextQuestion()
    {
        if (currentIndex >= roundFlags.Count)
        {
            OnRoundEnd?.Invoke(score);
            return;
        }

        var correct = roundFlags[currentIndex];
        currentChoices = BuildChoices(correct);
        correctChoiceIndex = System.Array.IndexOf(currentChoices, correct);

        OnQuestionShown?.Invoke(correct, currentChoices, correctChoiceIndex,
            currentIndex + 1, roundFlags.Count);

        inputAllowed = true;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(QuestionTimer());
    }

    FlagData[] BuildChoices(FlagData correct)
    {
        var sameTier = new List<FlagData>();
        foreach (var f in flagDatabase.flags)
        {
            if (f != null && f != correct && f.difficulty == correct.difficulty)
                sameTier.Add(f);
        }

        var distractors = PickN(sameTier, 3);
        var all = new List<FlagData>(distractors) { correct };

        // Fisher-Yates shuffle
        for (int i = all.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (all[i], all[j]) = (all[j], all[i]);
        }
        return all.ToArray();
    }

    IEnumerator QuestionTimer()
    {
        timeRemaining = timePerQuestion;
        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            OnTimerTick?.Invoke(Mathf.Clamp01(timeRemaining / timePerQuestion));
            yield return null;
        }

        if (inputAllowed)
        {
            inputAllowed = false;
            streak = 0;
            var correct = roundFlags[currentIndex];
            OnAnswerResult?.Invoke(false, correct, score, streak, 0, 0);
            yield return new WaitForSeconds(feedbackDisplayTime);
            currentIndex++;
            ShowNextQuestion();
        }
    }

    /// <summary>Called by UI buttons — choiceIndex is 0..3.</summary>
    public void OnPlayerAnswer(int choiceIndex)
    {
        if (!inputAllowed) return;
        inputAllowed = false;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        var correct = roundFlags[currentIndex];
        bool isCorrect = (choiceIndex == correctChoiceIndex);

        int timeBonus = 0;
        int streakBonus = 0;

        if (isCorrect)
        {
            streak++;
            timeBonus = Mathf.RoundToInt(timeRemaining * timeBonusMultiplier);
            streakBonus = streak > 1 ? (streak - 1) * streakBonusMultiplier : 0;
            score += pointsPerCorrect + timeBonus + streakBonus;
        }
        else
        {
            streak = 0;
        }

        OnAnswerResult?.Invoke(isCorrect, correct, score, streak, streakBonus, timeBonus);
        StartCoroutine(AdvanceAfterDelay());
    }

    IEnumerator AdvanceAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDisplayTime);
        currentIndex++;
        ShowNextQuestion();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToHub()
    {
        if (Application.CanStreamedLevelBeLoaded("HubScene"))
        {
            SceneManager.LoadScene("HubScene");
        }
        else
        {
            Debug.LogWarning("HubScene is not in Build Settings yet. Restarting current scene instead.");
            RestartGame();
        }
    }
}
