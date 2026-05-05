using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MazeRunnerGameManager : MonoBehaviour
{
    [Header("Data")]
    public MazeQuestionDatabase questionDatabase;
    public MazeRunnerPlayer player;

    [Header("Score")]
    public int pointsPerGate = 200;
    public int penaltyPerWrongAnswer = 50;
    public int timeBonusBase = 1500;
    public float timeBonusDecaySeconds = 90f;

    [Header("Layout (set by scene builder)")]
    public int totalGates = 5;

    int gatesPassed;
    int score;
    float startTime;
    bool gameOver;
    bool answering;
    MazeRunnerGate currentGate;
    MazeQuestionData currentQuestion;
    readonly HashSet<int> usedQuestionIndices = new HashSet<int>();

    public static event System.Action<int> OnGameStart;
    public static event System.Action<float> OnTimerTick;
    public static event System.Action<MazeQuestionData, int, int> OnGateApproached;
    public static event System.Action<bool, int, int> OnGateAnswered;
    public static event System.Action<int, float> OnGameWon;

    void Start()
    {
        startTime = Time.time;
        OnGameStart?.Invoke(totalGates);
        StartCoroutine(TimerLoop());
    }

    IEnumerator TimerLoop()
    {
        while (!gameOver)
        {
            OnTimerTick?.Invoke(Time.time - startTime);
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void OnGateEntered(MazeRunnerGate gate)
    {
        if (answering || gameOver) return;
        if (questionDatabase == null || questionDatabase.questions == null || questionDatabase.questions.Length == 0)
        {
            Debug.LogError("MazeRunnerGameManager: questionDatabase missing or empty.");
            gate.Open();
            return;
        }
        answering = true;
        currentGate = gate;
        currentQuestion = PickFreshQuestion();
        if (player != null) player.SetInputEnabled(false);
        OnGateApproached?.Invoke(currentQuestion, gatesPassed + 1, totalGates);
    }

    public void OnPlayerAnswer(int choiceIndex)
    {
        if (!answering || currentQuestion == null) return;
        bool correct = choiceIndex == currentQuestion.correctIndex;
        if (correct)
        {
            gatesPassed++;
            score += pointsPerGate;
            if (currentGate != null) currentGate.Open();
            answering = false;
            currentGate = null;
            currentQuestion = null;
            if (player != null) player.SetInputEnabled(true);
        }
        else
        {
            score = Mathf.Max(0, score - penaltyPerWrongAnswer);
        }
        OnGateAnswered?.Invoke(correct, score, gatesPassed);
    }

    public void OnGoalReached()
    {
        if (gameOver) return;
        gameOver = true;
        float elapsed = Time.time - startTime;
        int timeBonus = Mathf.Max(0, Mathf.RoundToInt(timeBonusBase * (1f - elapsed / timeBonusDecaySeconds)));
        score += timeBonus;
        if (player != null) player.SetInputEnabled(false);
        OnGameWon?.Invoke(score, elapsed);
    }

    MazeQuestionData PickFreshQuestion()
    {
        int n = questionDatabase.questions.Length;
        if (usedQuestionIndices.Count >= n) usedQuestionIndices.Clear();
        int idx;
        int safety = 0;
        do
        {
            idx = Random.Range(0, n);
            if (++safety > 200) break;
        } while (usedQuestionIndices.Contains(idx));
        usedQuestionIndices.Add(idx);
        return questionDatabase.questions[idx];
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
