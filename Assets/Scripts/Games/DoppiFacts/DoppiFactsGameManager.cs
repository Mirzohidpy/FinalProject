using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoppiFactsGameManager : MonoBehaviour
{
    [Header("Data")]
    public DoppiFactDatabase factDatabase;

    [Header("Wave settings")]
    public int totalWaves = 4;
    public int holeCount = 9;
    public float interWaveDelay = 1.4f;

    [Header("Per-wave tuning (clamped to last value if shorter than totalWaves)")]
    public int[] molesPerWave = new[] { 6, 8, 10, 12 };
    public float[] lingerSecondsPerWave = new[] { 2.6f, 2.1f, 1.7f, 1.3f };
    public float[] spawnIntervalPerWave = new[] { 1.4f, 1.1f, 0.85f, 0.65f };

    [Header("Score")]
    public int pointsForTrueHit = 100;
    public int penaltyForFalseHit = 75;
    public int penaltyForMissedTrue = 25;
    public int streakBonusPerStep = 25;

    int score;
    int streak;
    bool gameOver;

    DoppiFactData[] moleFacts;
    Coroutine[] moleHideRoutines;
    bool[] moleHandled;

    public static event System.Action<int> OnGameStart;
    public static event System.Action<int, int> OnWaveStart;
    public static event System.Action<int, DoppiFactData, float> OnMoleAppear;
    public static event System.Action<int> OnMoleHide;
    public static event System.Action<bool, int, int, int, int> OnHitResult;
    public static event System.Action<int> OnGameEnd;

    void Start()
    {
        if (factDatabase == null || factDatabase.facts == null || factDatabase.facts.Length == 0)
        {
            Debug.LogError("DoppiFactsGameManager: factDatabase is missing or empty. " +
                "Run BrainCitizen > Build DoppiFacts Game.");
            return;
        }

        moleFacts = new DoppiFactData[holeCount];
        moleHideRoutines = new Coroutine[holeCount];
        moleHandled = new bool[holeCount];

        StartCoroutine(RunGame());
    }

    IEnumerator RunGame()
    {
        OnGameStart?.Invoke(totalWaves);
        yield return new WaitForSeconds(0.5f);

        for (int w = 0; w < totalWaves; w++)
        {
            OnWaveStart?.Invoke(w + 1, totalWaves);
            yield return RunWave(w);
            if (gameOver) yield break;
            if (w < totalWaves - 1)
                yield return new WaitForSeconds(interWaveDelay);
        }

        gameOver = true;
        OnGameEnd?.Invoke(score);
    }

    IEnumerator RunWave(int waveIdx)
    {
        int molesThisWave = ClampedAt(molesPerWave, waveIdx);
        float linger = ClampedAt(lingerSecondsPerWave, waveIdx);
        float interval = ClampedAt(spawnIntervalPerWave, waveIdx);

        for (int i = 0; i < molesThisWave; i++)
        {
            int hole = PickFreeHole();
            while (hole == -1)
            {
                yield return null;
                hole = PickFreeHole();
            }
            SpawnMole(hole, linger);
            yield return new WaitForSeconds(interval);
        }

        while (HasActiveMole())
            yield return null;
    }

    int PickFreeHole()
    {
        var free = new List<int>();
        for (int i = 0; i < holeCount; i++)
            if (moleFacts[i] == null) free.Add(i);
        return free.Count == 0 ? -1 : free[Random.Range(0, free.Count)];
    }

    bool HasActiveMole()
    {
        for (int i = 0; i < holeCount; i++)
            if (moleFacts[i] != null) return true;
        return false;
    }

    void SpawnMole(int holeIdx, float linger)
    {
        var fact = factDatabase.facts[Random.Range(0, factDatabase.facts.Length)];
        moleFacts[holeIdx] = fact;
        moleHandled[holeIdx] = false;
        OnMoleAppear?.Invoke(holeIdx, fact, linger);
        moleHideRoutines[holeIdx] = StartCoroutine(HideAfter(holeIdx, linger));
    }

    IEnumerator HideAfter(int holeIdx, float linger)
    {
        yield return new WaitForSeconds(linger);
        if (moleFacts[holeIdx] != null && !moleHandled[holeIdx])
        {
            var fact = moleFacts[holeIdx];
            if (fact.isTrue)
            {
                streak = 0;
                int delta = -penaltyForMissedTrue;
                score = Mathf.Max(0, score + delta);
                OnHitResult?.Invoke(false, holeIdx, delta, score, streak);
            }
            moleHandled[holeIdx] = true;
        }
        moleFacts[holeIdx] = null;
        OnMoleHide?.Invoke(holeIdx);
    }

    public void OnPlayerHit(int holeIdx)
    {
        if (holeIdx < 0 || holeIdx >= holeCount) return;
        var fact = moleFacts[holeIdx];
        if (fact == null || moleHandled[holeIdx]) return;
        moleHandled[holeIdx] = true;

        bool correct = fact.isTrue;
        int delta;
        if (correct)
        {
            streak++;
            int bonus = streak > 1 ? (streak - 1) * streakBonusPerStep : 0;
            delta = pointsForTrueHit + bonus;
            score += delta;
        }
        else
        {
            streak = 0;
            delta = -penaltyForFalseHit;
            score = Mathf.Max(0, score + delta);
        }
        OnHitResult?.Invoke(correct, holeIdx, delta, score, streak);

        if (moleHideRoutines[holeIdx] != null) StopCoroutine(moleHideRoutines[holeIdx]);
        moleFacts[holeIdx] = null;
        OnMoleHide?.Invoke(holeIdx);
    }

    public void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void ReturnToHub()
    {
        if (Application.CanStreamedLevelBeLoaded("HubScene"))
            SceneManager.LoadScene("HubScene");
        else
            RestartGame();
    }

    static int ClampedAt(int[] arr, int i) => arr[Mathf.Min(i, arr.Length - 1)];
    static float ClampedAt(float[] arr, int i) => arr[Mathf.Min(i, arr.Length - 1)];
}
