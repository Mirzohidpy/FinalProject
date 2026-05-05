using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimelineSortGameManager : MonoBehaviour
{
    [Header("Data")]
    public TimelineEventDatabase eventDatabase;

    [Header("Round Composition")]
    public int modernEvents = 2;
    public int earlyModernEvents = 2;
    public int ancientEvents = 1;

    [Header("Score")]
    public int pointsForFullyCorrect = 500;
    public int pointsPerCorrectPair = 100;

    public static event System.Action<List<TimelineEventData>> OnRoundStart;
    public static event System.Action<bool, int, List<TimelineEventData>, List<TimelineEventData>> OnSubmitResult;

    void Start() { StartRound(); }

    void StartRound()
    {
        if (eventDatabase == null || eventDatabase.events == null || eventDatabase.events.Length == 0)
        {
            Debug.LogError("TimelineSortGameManager: eventDatabase missing or empty.");
            return;
        }

        var events = PickEvents();
        Shuffle(events);
        OnRoundStart?.Invoke(events);
    }

    List<TimelineEventData> PickEvents()
    {
        var modern      = FilterByEra(TimelineEra.Modern);
        var earlyModern = FilterByEra(TimelineEra.EarlyModern);
        var ancient     = FilterByEra(TimelineEra.Ancient);

        var result = new List<TimelineEventData>();
        result.AddRange(PickN(modern, modernEvents));
        result.AddRange(PickN(earlyModern, earlyModernEvents));
        result.AddRange(PickN(ancient, ancientEvents));
        return result;
    }

    List<TimelineEventData> FilterByEra(TimelineEra era)
    {
        var list = new List<TimelineEventData>();
        foreach (var e in eventDatabase.events)
            if (e != null && e.era == era) list.Add(e);
        return list;
    }

    List<TimelineEventData> PickN(List<TimelineEventData> pool, int n)
    {
        var copy = new List<TimelineEventData>(pool);
        var picks = new List<TimelineEventData>();
        n = Mathf.Min(n, copy.Count);
        for (int i = 0; i < n; i++)
        {
            int idx = Random.Range(0, copy.Count);
            picks.Add(copy[idx]);
            copy.RemoveAt(idx);
        }
        return picks;
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void OnPlayerSubmit(List<TimelineEventData> playerOrder)
    {
        if (playerOrder == null || playerOrder.Count == 0) return;

        bool fullyCorrect = true;
        int score = 0;
        for (int i = 0; i < playerOrder.Count - 1; i++)
        {
            if (playerOrder[i].year <= playerOrder[i + 1].year)
                score += pointsPerCorrectPair;
            else
                fullyCorrect = false;
        }
        if (fullyCorrect) score = pointsForFullyCorrect;

        var sortedCorrect = new List<TimelineEventData>(playerOrder);
        sortedCorrect.Sort((a, b) => a.year.CompareTo(b.year));

        OnSubmitResult?.Invoke(fullyCorrect, score, playerOrder, sortedCorrect);
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
