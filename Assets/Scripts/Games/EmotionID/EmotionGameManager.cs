using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EmotionGameManager : MonoBehaviour
{
    [Header("Data")]
    public EmotionDatabase emotionDatabase;

    [Header("Round Composition")]
    public int easyQuestions = 5;
    public int mediumQuestions = 3;
    public int hardQuestions = 2;

    [Header("Settings")]
    public float timePerQuestion = 8f;
    public float feedbackDisplayTime = 2.4f;

    [Header("Score")]
    public int pointsPerCorrect = 100;
    public int timeBonusMultiplier = 10;
    public int streakBonusMultiplier = 50;

    List<EmotionData> roundEmotions = new List<EmotionData>();
    EmotionData[] currentChoices;
    int correctChoiceIndex;
    int currentIndex;
    int score;
    int streak;
    bool inputAllowed;
    float timeRemaining;
    Coroutine timerCoroutine;

    public static event System.Action<EmotionData, EmotionData[], int, int, int> OnQuestionShown;
    public static event System.Action<float> OnTimerTick;
    public static event System.Action<bool, EmotionData, int, int, int, int> OnAnswerResult;
    public static event System.Action<int> OnRoundEnd;

    void Start() { StartRound(); }

    void StartRound()
    {
        if (emotionDatabase == null || emotionDatabase.emotions == null || emotionDatabase.emotions.Length == 0)
        {
            Debug.LogError("EmotionGameManager: emotionDatabase missing or empty. " +
                "Run BrainCitizen > Build EmotionID Game.");
            return;
        }
        score = 0;
        streak = 0;
        currentIndex = 0;
        roundEmotions = PickRoundEmotions();
        ShowNextQuestion();
    }

    List<EmotionData> PickRoundEmotions()
    {
        var easy = FilterByDifficulty(EmotionDifficulty.Easy);
        var medium = FilterByDifficulty(EmotionDifficulty.Medium);
        var hard = FilterByDifficulty(EmotionDifficulty.Hard);

        var result = new List<EmotionData>();
        result.AddRange(PickN(easy, easyQuestions));
        result.AddRange(PickN(medium, mediumQuestions));
        result.AddRange(PickN(hard, hardQuestions));
        return result;
    }

    List<EmotionData> FilterByDifficulty(EmotionDifficulty d)
    {
        var list = new List<EmotionData>();
        foreach (var e in emotionDatabase.emotions)
            if (e != null && e.difficulty == d) list.Add(e);
        return list;
    }

    List<EmotionData> PickN(List<EmotionData> pool, int n)
    {
        var copy = new List<EmotionData>(pool);
        var picks = new List<EmotionData>();
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
        if (currentIndex >= roundEmotions.Count)
        {
            OnRoundEnd?.Invoke(score);
            return;
        }
        var correct = roundEmotions[currentIndex];
        currentChoices = BuildChoices(correct);
        correctChoiceIndex = System.Array.IndexOf(currentChoices, correct);

        OnQuestionShown?.Invoke(correct, currentChoices, correctChoiceIndex,
            currentIndex + 1, roundEmotions.Count);

        inputAllowed = true;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(QuestionTimer());
    }

    EmotionData[] BuildChoices(EmotionData correct)
    {
        var sameTier = new List<EmotionData>();
        foreach (var e in emotionDatabase.emotions)
            if (e != null && e != correct && e.difficulty == correct.difficulty)
                sameTier.Add(e);

        // If same-tier doesn't have enough distractors, fall back to all-tier pool.
        List<EmotionData> pool = sameTier.Count >= 3 ? sameTier : new List<EmotionData>();
        if (pool.Count < 3)
        {
            pool = new List<EmotionData>();
            foreach (var e in emotionDatabase.emotions)
                if (e != null && e != correct) pool.Add(e);
        }

        var distractors = PickN(pool, 3);
        var all = new List<EmotionData>(distractors) { correct };

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
            var correct = roundEmotions[currentIndex];
            OnAnswerResult?.Invoke(false, correct, score, streak, 0, 0);
            yield return new WaitForSeconds(feedbackDisplayTime);
            currentIndex++;
            ShowNextQuestion();
        }
    }

    public void OnPlayerAnswer(int choiceIndex)
    {
        if (!inputAllowed) return;
        inputAllowed = false;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        var correct = roundEmotions[currentIndex];
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

    public void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void ReturnToHub()
    {
        if (Application.CanStreamedLevelBeLoaded("HubScene"))
            SceneManager.LoadScene("HubScene");
        else
            RestartGame();
    }
}
