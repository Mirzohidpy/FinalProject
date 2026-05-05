using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CivicQuizGameManager : MonoBehaviour
{
    [Header("Data")]
    public CivicQuizDatabase questionDatabase;

    [Header("Round")]
    public int questionsPerTier = 3;
    public int tierCount = 5;
    public float timePerQuestion = 30f;
    public float revealDuration = 2.4f;
    public float pauseBonusSeconds = 30f;

    [Header("Scoring (one base value per question slot)")]
    public int[] basePoints = new int[]
    {
        100, 200, 300, 500, 1000,
        2000, 4000, 8000, 16000, 32000,
        64000, 125000, 250000, 500000, 1000000
    };

    public const int LeaderboardSize = 5;
    public const string LeaderboardScoreKey = "CivicQuiz_Score_";
    public const string LeaderboardNameKey = "CivicQuiz_Name_";

    List<CivicQuizQuestion> roundQuestions;
    int currentQuestionIndex;
    int totalScore;
    int streak;
    int answeredCorrectly;
    float timeRemaining;
    bool inputAllowed;
    bool gameOver;
    bool fiftyFiftyUsed;
    bool hintUsed;
    bool pauseUsed;
    Coroutine timerCoroutine;
    readonly HashSet<int> hiddenChoices = new HashSet<int>();

    public static event System.Action<CivicQuizQuestion, int, int, int, int> OnQuestionShown;
    public static event System.Action<float> OnTimerTick;
    public static event System.Action<HashSet<int>> OnFiftyFiftyApplied;
    public static event System.Action<string> OnHintShown;
    public static event System.Action OnPauseUsed;
    public static event System.Action<bool, int, int, int, int, int> OnAnswerResult;
    public static event System.Action<int, int, bool> OnGameEnd;

    void Start() { StartGame(); }

    void StartGame()
    {
        if (questionDatabase == null || questionDatabase.questions == null || questionDatabase.questions.Length == 0)
        {
            Debug.LogError("CivicQuizGameManager: questionDatabase missing or empty.");
            return;
        }
        currentQuestionIndex = 0;
        totalScore = 0;
        streak = 0;
        answeredCorrectly = 0;
        fiftyFiftyUsed = false;
        hintUsed = false;
        pauseUsed = false;
        roundQuestions = PickQuestions();
        ShowNextQuestion();
    }

    List<CivicQuizQuestion> PickQuestions()
    {
        var result = new List<CivicQuizQuestion>();
        for (int tier = 1; tier <= tierCount; tier++)
        {
            var tierPool = new List<CivicQuizQuestion>();
            foreach (var q in questionDatabase.questions)
                if (q != null && q.difficulty == tier) tierPool.Add(q);
            result.AddRange(PickN(tierPool, questionsPerTier));
        }
        return result;
    }

    List<CivicQuizQuestion> PickN(List<CivicQuizQuestion> pool, int n)
    {
        var copy = new List<CivicQuizQuestion>(pool);
        var picks = new List<CivicQuizQuestion>();
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
        if (currentQuestionIndex >= roundQuestions.Count)
        {
            EndGame(true);
            return;
        }
        var q = roundQuestions[currentQuestionIndex];
        hiddenChoices.Clear();
        int basePts = basePoints[Mathf.Min(currentQuestionIndex, basePoints.Length - 1)];
        OnQuestionShown?.Invoke(q, currentQuestionIndex + 1, roundQuestions.Count, basePts, totalScore);
        inputAllowed = true;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(QuestionTimer());
    }

    IEnumerator QuestionTimer()
    {
        timeRemaining = timePerQuestion;
        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
            OnTimerTick?.Invoke(Mathf.Clamp01(timeRemaining / timePerQuestion));
            yield return null;
        }
        if (inputAllowed)
        {
            inputAllowed = false;
            var q = GetCurrent();
            OnAnswerResult?.Invoke(false, q.correctIndex, 0, totalScore, 0, 100);
            yield return new WaitForSeconds(revealDuration);
            EndGame(false);
        }
    }

    public void OnPlayerAnswer(int idx)
    {
        if (!inputAllowed) return;
        if (hiddenChoices.Contains(idx)) return;
        inputAllowed = false;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        var q = GetCurrent();
        bool correct = idx == q.correctIndex;
        int basePts = basePoints[Mathf.Min(currentQuestionIndex, basePoints.Length - 1)];
        int multPercent = 100;
        int points = 0;

        if (correct)
        {
            streak++;
            answeredCorrectly++;
            int multSteps = Mathf.Min(streak - 1, 4);
            multPercent = 100 + multSteps * 50;
            points = (basePts * multPercent) / 100;
            totalScore += points;
        }
        else
        {
            streak = 0;
        }

        OnAnswerResult?.Invoke(correct, q.correctIndex, points, totalScore, streak, multPercent);
        StartCoroutine(AdvanceAfterDelay(correct));
    }

    IEnumerator AdvanceAfterDelay(bool correct)
    {
        yield return new WaitForSeconds(revealDuration);
        if (!correct)
        {
            EndGame(false);
        }
        else
        {
            currentQuestionIndex++;
            ShowNextQuestion();
        }
    }

    public void UseFiftyFifty()
    {
        if (fiftyFiftyUsed || !inputAllowed) return;
        fiftyFiftyUsed = true;
        var q = GetCurrent();
        var wrongs = new List<int>();
        for (int i = 0; i < q.options.Length; i++)
            if (i != q.correctIndex) wrongs.Add(i);
        for (int i = wrongs.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (wrongs[i], wrongs[j]) = (wrongs[j], wrongs[i]);
        }
        hiddenChoices.Clear();
        if (wrongs.Count >= 2)
        {
            hiddenChoices.Add(wrongs[0]);
            hiddenChoices.Add(wrongs[1]);
        }
        OnFiftyFiftyApplied?.Invoke(hiddenChoices);
    }

    public void UseHint()
    {
        if (hintUsed || !inputAllowed) return;
        hintUsed = true;
        var q = GetCurrent();
        OnHintShown?.Invoke(q.hint ?? "");
    }

    public void UsePause()
    {
        if (pauseUsed || !inputAllowed) return;
        pauseUsed = true;
        timeRemaining = Mathf.Min(timePerQuestion + pauseBonusSeconds, timeRemaining + pauseBonusSeconds);
        OnPauseUsed?.Invoke();
    }

    CivicQuizQuestion GetCurrent() => roundQuestions[currentQuestionIndex];

    void EndGame(bool wonAll)
    {
        if (gameOver) return;
        gameOver = true;
        OnGameEnd?.Invoke(totalScore, answeredCorrectly, wonAll);
    }

    public void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void ReturnToHub()
    {
        if (Application.CanStreamedLevelBeLoaded("HubScene"))
            SceneManager.LoadScene("HubScene");
        else
            RestartGame();
    }

    // -------- Leaderboard --------

    public static List<KeyValuePair<string, int>> LoadLeaderboard()
    {
        var list = new List<KeyValuePair<string, int>>();
        for (int i = 0; i < LeaderboardSize; i++)
        {
            if (PlayerPrefs.HasKey(LeaderboardScoreKey + i))
            {
                string n = PlayerPrefs.GetString(LeaderboardNameKey + i, "?");
                int s = PlayerPrefs.GetInt(LeaderboardScoreKey + i, 0);
                list.Add(new KeyValuePair<string, int>(n, s));
            }
        }
        return list;
    }

    public static int SaveLeaderboardEntry(string name, int score)
    {
        var list = LoadLeaderboard();
        string clean = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim();
        if (clean.Length > 12) clean = clean.Substring(0, 12);
        list.Add(new KeyValuePair<string, int>(clean, score));
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        if (list.Count > LeaderboardSize)
            list.RemoveRange(LeaderboardSize, list.Count - LeaderboardSize);
        for (int i = 0; i < list.Count; i++)
        {
            PlayerPrefs.SetString(LeaderboardNameKey + i, list[i].Key);
            PlayerPrefs.SetInt(LeaderboardScoreKey + i, list[i].Value);
        }
        for (int i = list.Count; i < LeaderboardSize; i++)
        {
            PlayerPrefs.DeleteKey(LeaderboardNameKey + i);
            PlayerPrefs.DeleteKey(LeaderboardScoreKey + i);
        }
        PlayerPrefs.Save();

        for (int i = 0; i < list.Count; i++)
            if (list[i].Key == clean && list[i].Value == score) return i;
        return -1;
    }
}
