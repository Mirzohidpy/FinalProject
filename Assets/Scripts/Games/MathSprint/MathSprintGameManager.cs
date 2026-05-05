using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Game logic for Math Sprint.
/// Plays through every level in the database in order. Each level has its
/// own operand range, operation set, question count, and time-per-question.
/// A level marked suddenDeath ends the run on the first wrong answer.
/// </summary>
public class MathSprintGameManager : MonoBehaviour
{
    [Header("Data")]
    public MathLevelDatabase levelDatabase;

    [Header("Settings")]
    public float feedbackDisplayTime = 1.4f;

    [Header("Score")]
    public int pointsPerCorrect = 100;
    public int streakBonusMultiplier = 20;
    public int timeBonusMultiplier = 10;

    int currentLevelIndex;
    int currentQuestionInLevel;
    int totalQuestionsAnswered;
    int totalQuestionsTotal;
    int score;
    int streak;
    int bestStreak;

    string currentEquation;
    int[] currentChoices;
    int correctChoiceIndex;
    int currentCorrectAnswer;

    bool inputAllowed;
    float timeRemaining;
    Coroutine timerCoroutine;

    public static event System.Action<string, int[], int, int, int, MathLevelData> OnQuestionShown;
    // equation, choices, correctIdx, qNum, totalQ, level
    public static event System.Action<float> OnTimerTick;
    public static event System.Action<bool, int, int, int, int, int, bool> OnAnswerResult;
    // correct, correctAnswer, score, streak, streakBonus, timeBonus, gameOver
    public static event System.Action<MathLevelData, int, int> OnLevelStarted;
    // level, levelIndex, totalLevels
    public static event System.Action<int, int> OnRoundEnd;
    // finalScore, bestStreak

    void Start()
    {
        StartGame();
    }

    void StartGame()
    {
        if (levelDatabase == null || levelDatabase.levels == null || levelDatabase.levels.Length == 0)
        {
            Debug.LogError("MathSprintGameManager: levelDatabase missing or empty. " +
                "Run BrainCitizen > Build MathSprint Game.");
            return;
        }

        currentLevelIndex = 0;
        currentQuestionInLevel = 0;
        totalQuestionsAnswered = 0;
        score = 0;
        streak = 0;
        bestStreak = 0;

        totalQuestionsTotal = 0;
        foreach (var lvl in levelDatabase.levels)
            if (lvl != null) totalQuestionsTotal += lvl.questionsInLevel;

        StartLevel();
    }

    void StartLevel()
    {
        var lvl = levelDatabase.levels[currentLevelIndex];
        currentQuestionInLevel = 0;
        OnLevelStarted?.Invoke(lvl, currentLevelIndex, levelDatabase.levels.Length);
        ShowNextQuestion();
    }

    void ShowNextQuestion()
    {
        var lvl = levelDatabase.levels[currentLevelIndex];
        if (currentQuestionInLevel >= lvl.questionsInLevel)
        {
            currentLevelIndex++;
            if (currentLevelIndex >= levelDatabase.levels.Length)
            {
                EndGame();
                return;
            }
            StartLevel();
            return;
        }

        GenerateQuestion();
        OnQuestionShown?.Invoke(currentEquation, currentChoices, correctChoiceIndex,
            totalQuestionsAnswered + 1, totalQuestionsTotal, lvl);

        inputAllowed = true;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(QuestionTimer(lvl.secondsPerQuestion));
    }

    void GenerateQuestion()
    {
        var lvl = levelDatabase.levels[currentLevelIndex];
        int a = Random.Range(lvl.minOperand, lvl.maxOperand + 1);
        int b = Random.Range(lvl.minOperand, lvl.maxOperand + 1);

        var chosenOp = lvl.operation;
        if (chosenOp == MathOperation.MixedAddSub)
            chosenOp = Random.value < 0.5f ? MathOperation.Add : MathOperation.Subtract;
        else if (chosenOp == MathOperation.MixedAll)
        {
            int r = Random.Range(0, 3);
            chosenOp = r == 0 ? MathOperation.Add : (r == 1 ? MathOperation.Subtract : MathOperation.Multiply);
        }

        char op = '+';
        int correct = 0;
        switch (chosenOp)
        {
            case MathOperation.Add:
                op = '+';
                correct = a + b;
                break;
            case MathOperation.Subtract:
                if (b > a) (a, b) = (b, a);
                op = '-';
                correct = a - b;
                break;
            case MathOperation.Multiply:
                op = 'x';
                correct = a * b;
                break;
        }

        var choices = new List<int> { correct };
        int range = Mathf.Max(5, Mathf.Abs(correct) / 4);
        int attempts = 0;
        while (choices.Count < 4 && attempts < 100)
        {
            attempts++;
            int delta = Random.Range(-range, range + 1);
            if (delta == 0) continue;
            int distractor = correct + delta;
            if (distractor < 0) continue;
            if (choices.Contains(distractor)) continue;
            choices.Add(distractor);
        }
        int filler = correct + 1;
        while (choices.Count < 4)
        {
            if (filler >= 0 && !choices.Contains(filler)) choices.Add(filler);
            filler++;
        }

        for (int i = choices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (choices[i], choices[j]) = (choices[j], choices[i]);
        }

        currentEquation = $"{a} {op} {b} = ?";
        currentChoices = choices.ToArray();
        currentCorrectAnswer = correct;
        correctChoiceIndex = System.Array.IndexOf(currentChoices, correct);
    }

    IEnumerator QuestionTimer(float seconds)
    {
        timeRemaining = seconds;
        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
            OnTimerTick?.Invoke(Mathf.Clamp01(timeRemaining / seconds));
            yield return null;
        }

        if (inputAllowed)
        {
            inputAllowed = false;
            streak = 0;
            var lvl = levelDatabase.levels[currentLevelIndex];
            bool isSuddenDeath = lvl.suddenDeath;
            OnAnswerResult?.Invoke(false, currentCorrectAnswer, score, streak, 0, 0, isSuddenDeath);
            yield return new WaitForSeconds(feedbackDisplayTime);

            if (isSuddenDeath)
            {
                EndGame();
            }
            else
            {
                totalQuestionsAnswered++;
                currentQuestionInLevel++;
                ShowNextQuestion();
            }
        }
    }

    /// <summary>Called by UI buttons — choiceIndex is 0..3.</summary>
    public void OnPlayerAnswer(int choiceIndex)
    {
        if (!inputAllowed) return;
        inputAllowed = false;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        var lvl = levelDatabase.levels[currentLevelIndex];
        bool correct = (choiceIndex == correctChoiceIndex);
        int timeBonus = 0;
        int streakBonus = 0;

        if (correct)
        {
            streak++;
            if (streak > bestStreak) bestStreak = streak;
            timeBonus = Mathf.RoundToInt(timeRemaining * timeBonusMultiplier);
            streakBonus = streak > 1 ? (streak - 1) * streakBonusMultiplier : 0;
            score += pointsPerCorrect + timeBonus + streakBonus;
        }
        else
        {
            streak = 0;
        }

        bool isSuddenDeathFail = lvl.suddenDeath && !correct;
        OnAnswerResult?.Invoke(correct, currentCorrectAnswer, score, streak,
            streakBonus, timeBonus, isSuddenDeathFail);
        StartCoroutine(AdvanceAfterDelay(isSuddenDeathFail));
    }

    IEnumerator AdvanceAfterDelay(bool gameOver)
    {
        yield return new WaitForSeconds(feedbackDisplayTime);
        if (gameOver)
        {
            EndGame();
        }
        else
        {
            totalQuestionsAnswered++;
            currentQuestionInLevel++;
            ShowNextQuestion();
        }
    }

    void EndGame()
    {
        inputAllowed = false;
        if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; }
        OnRoundEnd?.Invoke(score, bestStreak);
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
            Debug.LogWarning("HubScene not in Build Settings. Restarting current scene.");
            RestartGame();
        }
    }
}
