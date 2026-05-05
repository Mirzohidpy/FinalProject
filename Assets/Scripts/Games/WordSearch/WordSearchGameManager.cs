using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Game logic for Word Search.
/// One round: pick N civic words, place them in a 10x10 grid (4 directions),
/// fill rest with random letters, run a single round timer.
/// Drag selections come in via WordSearchGrid.OnPlayerSelection.
/// </summary>
public class WordSearchGameManager : MonoBehaviour
{
    [Header("Data")]
    public WordDatabase wordDatabase;

    [Header("Round Settings")]
    public int gridSize = 10;
    public int wordsPerRound = 7;
    public float roundSeconds = 120f;

    [Header("Score")]
    public int pointsPerWord = 100;
    public int timeBonusMultiplier = 5;
    public int allFoundBonus = 200;

    public struct WordPlacement
    {
        public string word;
        public string definition;
        public int startRow, startCol;
        public int endRow, endCol;
    }

    char[,] grid;
    readonly List<WordPlacement> placements = new List<WordPlacement>();
    readonly HashSet<string> foundWords = new HashSet<string>();
    int score;
    bool inputAllowed;
    float timeRemaining;
    Coroutine timerCoroutine;

    public static event System.Action<char[,], List<WordPlacement>> OnRoundStarted;
    public static event System.Action<float> OnTimerTick;
    public static event System.Action<WordPlacement, int, int, int> OnWordFound;       // placement, score, found, total
    public static event System.Action OnSelectionRejected;
    public static event System.Action<int, bool, float, int, int> OnRoundEnd;          // score, allFound, timeRemaining, found, total

    void OnEnable()
    {
        WordSearchGrid.OnPlayerSelection += HandlePlayerSelection;
    }

    void OnDisable()
    {
        WordSearchGrid.OnPlayerSelection -= HandlePlayerSelection;
    }

    void Start()
    {
        StartRound();
    }

    void StartRound()
    {
        if (wordDatabase == null || wordDatabase.words == null || wordDatabase.words.Length == 0)
        {
            Debug.LogError("WordSearchGameManager: wordDatabase missing or empty. " +
                "Run BrainCitizen > Build WordSearch Game.");
            return;
        }

        score = 0;
        foundWords.Clear();
        placements.Clear();
        BuildGrid();
        OnRoundStarted?.Invoke(grid, new List<WordPlacement>(placements));

        inputAllowed = true;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(RoundTimer());
    }

    void BuildGrid()
    {
        var pool = new List<WordData>();
        foreach (var w in wordDatabase.words)
            if (w != null && !string.IsNullOrEmpty(w.word) && w.word.Length <= gridSize)
                pool.Add(w);

        Shuffle(pool);
        var picks = new List<WordData>();
        for (int i = 0; i < pool.Count && picks.Count < wordsPerRound; i++)
            picks.Add(pool[i]);
        picks.Sort((a, b) => b.word.Length.CompareTo(a.word.Length));

        grid = new char[gridSize, gridSize];

        var directions = new[]
        {
            new Vector2Int(0, 1),    // right
            new Vector2Int(1, 0),    // down
            new Vector2Int(1, 1),    // down-right
            new Vector2Int(-1, 1),   // up-right
        };

        foreach (var wd in picks)
        {
            string upper = wd.word.ToUpperInvariant();
            bool placed = false;
            for (int attempt = 0; attempt < 200 && !placed; attempt++)
            {
                var dir = directions[Random.Range(0, directions.Length)];
                int len = upper.Length;
                int sr = Random.Range(0, gridSize);
                int sc = Random.Range(0, gridSize);
                int er = sr + dir.x * (len - 1);
                int ec = sc + dir.y * (len - 1);
                if (er < 0 || er >= gridSize || ec < 0 || ec >= gridSize) continue;

                bool conflict = false;
                for (int i = 0; i < len; i++)
                {
                    int rr = sr + dir.x * i;
                    int cc = sc + dir.y * i;
                    if (grid[rr, cc] != '\0' && grid[rr, cc] != upper[i])
                    {
                        conflict = true; break;
                    }
                }
                if (conflict) continue;

                for (int i = 0; i < len; i++)
                {
                    int rr = sr + dir.x * i;
                    int cc = sc + dir.y * i;
                    grid[rr, cc] = upper[i];
                }
                placements.Add(new WordPlacement
                {
                    word = upper,
                    definition = wd.definition,
                    startRow = sr, startCol = sc,
                    endRow = er, endCol = ec,
                });
                placed = true;
            }
            if (!placed)
                Debug.LogWarning($"WordSearchGameManager: couldn't place '{upper}' after 200 attempts.");
        }

        for (int r = 0; r < gridSize; r++)
            for (int c = 0; c < gridSize; c++)
                if (grid[r, c] == '\0')
                    grid[r, c] = (char)('A' + Random.Range(0, 26));
    }

    void HandlePlayerSelection(int sr, int sc, int er, int ec)
    {
        if (!inputAllowed) return;
        if (sr == er && sc == ec) { OnSelectionRejected?.Invoke(); return; }

        WordPlacement match = default;
        bool matched = false;
        foreach (var p in placements)
        {
            if (foundWords.Contains(p.word)) continue;
            bool fwd = (p.startRow == sr && p.startCol == sc && p.endRow == er && p.endCol == ec);
            bool rev = (p.startRow == er && p.startCol == ec && p.endRow == sr && p.endCol == sc);
            if (fwd || rev)
            {
                match = p;
                matched = true;
                break;
            }
        }

        if (!matched)
        {
            OnSelectionRejected?.Invoke();
            return;
        }

        foundWords.Add(match.word);
        score += pointsPerWord;
        OnWordFound?.Invoke(match, score, foundWords.Count, placements.Count);

        if (foundWords.Count >= placements.Count)
            EndRound(allFound: true);
    }

    IEnumerator RoundTimer()
    {
        timeRemaining = roundSeconds;
        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
            OnTimerTick?.Invoke(Mathf.Clamp01(timeRemaining / roundSeconds));
            yield return null;
        }
        timeRemaining = 0f;
        OnTimerTick?.Invoke(0f);
        timerCoroutine = null;
        EndRound(allFound: false);
    }

    void EndRound(bool allFound)
    {
        if (!inputAllowed) return;
        inputAllowed = false;
        if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; }

        if (allFound)
        {
            int timeBonus = Mathf.RoundToInt(Mathf.Max(0f, timeRemaining) * timeBonusMultiplier);
            score += timeBonus + allFoundBonus;
        }

        OnRoundEnd?.Invoke(score, allFound, Mathf.Max(0f, timeRemaining),
            foundWords.Count, placements.Count);
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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
