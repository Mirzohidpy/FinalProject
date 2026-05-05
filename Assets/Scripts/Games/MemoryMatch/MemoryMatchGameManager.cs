using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Game logic for Memory Match.
/// Plays each level in order: pick `pairs` cards from the database, duplicate
/// them, shuffle, briefly reveal all, then run a per-level round timer.
/// Player clicks two cards; if names match they stay up, otherwise flip back.
/// </summary>
public class MemoryMatchGameManager : MonoBehaviour
{
    [System.Serializable]
    public struct LevelConfig
    {
        public string levelName;
        public int columns;
        public int rows;
        public float roundSeconds;
    }

    [Header("Data")]
    public MemoryCardDatabase cardDatabase;

    [Header("Levels")]
    public LevelConfig[] levels;

    [Header("Settings")]
    public float revealSeconds = 3f;
    public float evaluationDelay = 0.5f;
    public float flipBackDelay = 0.4f;
    public float interLevelDelay = 1.5f;

    [Header("Score")]
    public int pointsPerMatch = 100;
    public int comboBonusMultiplier = 50;
    public int levelClearBonus = 200;
    public int timeBonusMultiplier = 5;

    enum CardState { FaceDown, FaceUp, Matched }

    int currentLevelIndex;
    MemoryCardData[] currentCards;
    CardState[] currentStates;
    int firstFlipped = -1;
    int matchedPairs;
    int totalPairs;
    int score;
    int combo;
    int bestCombo;
    bool inputAllowed;
    float timeRemaining;
    Coroutine timerCoroutine;

    public static event System.Action<LevelConfig, int, int, MemoryCardData[]> OnLevelStarted;
    // level, levelIndex, totalLevels, cards (post-shuffle, length = pairs * 2)
    public static event System.Action<float> OnTimerTick;
    public static event System.Action OnRevealStart;
    public static event System.Action OnRevealEnd;
    public static event System.Action<int, bool> OnCardFlipped;
    public static event System.Action<int, int, int, int, int, int, int> OnPairMatched;
    // a, b, score, combo, comboBonus, matched, total
    public static event System.Action<int, int, int> OnPairMissed;
    // a, b, score (combo just reset)
    public static event System.Action<int, int, int> OnLevelCompleted;
    // levelIndex, score, timeBonus
    public static event System.Action<int, int, bool> OnRoundEnd;
    // finalScore, bestCombo, allLevelsPassed

    void OnEnable()
    {
        MemoryCard.OnClicked += HandleCardClicked;
    }

    void OnDisable()
    {
        MemoryCard.OnClicked -= HandleCardClicked;
    }

    void Start()
    {
        StartGame();
    }

    void StartGame()
    {
        if (cardDatabase == null || cardDatabase.cards == null || cardDatabase.cards.Length == 0)
        {
            Debug.LogError("MemoryMatchGameManager: cardDatabase missing or empty. " +
                "Run BrainCitizen > Build MemoryMatch Game.");
            return;
        }
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("MemoryMatchGameManager: levels not configured.");
            return;
        }
        currentLevelIndex = 0;
        score = 0;
        combo = 0;
        bestCombo = 0;
        StartLevel();
    }

    void StartLevel()
    {
        var lvl = levels[currentLevelIndex];
        int cellCount = lvl.columns * lvl.rows;
        if (cellCount % 2 != 0)
        {
            Debug.LogWarning($"Level {currentLevelIndex}: odd cell count {cellCount}, skipping last cell.");
            cellCount--;
        }
        totalPairs = cellCount / 2;
        matchedPairs = 0;
        firstFlipped = -1;
        combo = 0;

        currentCards = BuildCardSequence(totalPairs);
        currentStates = new CardState[currentCards.Length];

        OnLevelStarted?.Invoke(lvl, currentLevelIndex, levels.Length, currentCards);
        StartCoroutine(LevelRoutine(lvl));
    }

    MemoryCardData[] BuildCardSequence(int pairs)
    {
        var pool = new List<MemoryCardData>();
        foreach (var c in cardDatabase.cards)
            if (c != null && c.cardSprite != null) pool.Add(c);

        if (pool.Count < pairs)
        {
            Debug.LogWarning($"Card pool has {pool.Count} cards but level needs {pairs} pairs. " +
                "Reducing pair count to fit.");
            pairs = pool.Count;
            totalPairs = pairs;
        }

        Shuffle(pool);
        var picks = new List<MemoryCardData>();
        for (int i = 0; i < pairs; i++) picks.Add(pool[i]);

        var seq = new List<MemoryCardData>(pairs * 2);
        foreach (var c in picks) { seq.Add(c); seq.Add(c); }
        Shuffle(seq);
        return seq.ToArray();
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    IEnumerator LevelRoutine(LevelConfig lvl)
    {
        inputAllowed = false;
        // Wait one frame for UIManager to spawn the card visuals
        yield return null;

        OnRevealStart?.Invoke();
        for (int i = 0; i < currentStates.Length; i++) currentStates[i] = CardState.FaceUp;
        yield return new WaitForSeconds(revealSeconds);

        OnRevealEnd?.Invoke();
        for (int i = 0; i < currentStates.Length; i++) currentStates[i] = CardState.FaceDown;
        yield return new WaitForSeconds(0.4f);

        inputAllowed = true;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(RoundTimer(lvl.roundSeconds));
    }

    IEnumerator RoundTimer(float seconds)
    {
        timeRemaining = seconds;
        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
            OnTimerTick?.Invoke(Mathf.Clamp01(timeRemaining / seconds));
            yield return null;
        }
        timeRemaining = 0f;
        OnTimerTick?.Invoke(0f);
        timerCoroutine = null;
        EndGame(allPassed: false);
    }

    void HandleCardClicked(int idx)
    {
        if (!inputAllowed) return;
        if (idx < 0 || idx >= currentStates.Length) return;
        if (currentStates[idx] != CardState.FaceDown) return;

        currentStates[idx] = CardState.FaceUp;
        OnCardFlipped?.Invoke(idx, true);

        if (firstFlipped == -1)
        {
            firstFlipped = idx;
        }
        else
        {
            int first = firstFlipped;
            int second = idx;
            firstFlipped = -1;
            inputAllowed = false;
            StartCoroutine(EvaluatePair(first, second));
        }
    }

    IEnumerator EvaluatePair(int first, int second)
    {
        yield return new WaitForSeconds(evaluationDelay);
        bool matched = currentCards[first].cardName == currentCards[second].cardName;

        if (matched)
        {
            currentStates[first] = CardState.Matched;
            currentStates[second] = CardState.Matched;
            combo++;
            if (combo > bestCombo) bestCombo = combo;
            int comboBonus = combo > 1 ? (combo - 1) * comboBonusMultiplier : 0;
            score += pointsPerMatch + comboBonus;
            matchedPairs++;
            OnPairMatched?.Invoke(first, second, score, combo, comboBonus, matchedPairs, totalPairs);

            if (matchedPairs >= totalPairs)
            {
                yield return new WaitForSeconds(0.6f);
                LevelComplete();
                yield break;
            }
        }
        else
        {
            combo = 0;
            currentStates[first] = CardState.FaceDown;
            currentStates[second] = CardState.FaceDown;
            OnPairMissed?.Invoke(first, second, score);
            yield return new WaitForSeconds(flipBackDelay);
        }
        inputAllowed = true;
    }

    void LevelComplete()
    {
        inputAllowed = false;
        if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; }
        int timeBonus = Mathf.RoundToInt(Mathf.Max(0f, timeRemaining) * timeBonusMultiplier);
        score += timeBonus + levelClearBonus;
        OnLevelCompleted?.Invoke(currentLevelIndex, score, timeBonus);

        currentLevelIndex++;
        if (currentLevelIndex >= levels.Length)
            StartCoroutine(DelayedEnd(allPassed: true, interLevelDelay));
        else
            StartCoroutine(NextLevelAfterDelay(interLevelDelay));
    }

    IEnumerator NextLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartLevel();
    }

    IEnumerator DelayedEnd(bool allPassed, float delay)
    {
        yield return new WaitForSeconds(delay);
        EndGame(allPassed);
    }

    void EndGame(bool allPassed)
    {
        inputAllowed = false;
        if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; }
        OnRoundEnd?.Invoke(score, bestCombo, allPassed);
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
