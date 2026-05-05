using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// All UI for Memory Match. Subscribes to MemoryMatchGameManager events.
/// Spawns one MemoryCard per cell on each level start, then forwards
/// flip / match / reveal events to those instances.
/// </summary>
public class MemoryMatchUIManager : MonoBehaviour
{
    [Header("Cards")]
    public GridLayoutGroup cardsLayout;
    public GameObject cardTemplate;

    [Header("Layout Sizing")]
    public Vector2 boardSize = new Vector2(1180, 800);
    public Vector2 cellSpacing = new Vector2(14, 14);

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text comboText;
    public TMP_Text levelText;
    public TMP_Text matchCountText;

    [Header("Timer")]
    public Slider timerSlider;
    public Image timerFill;
    public Color timerFullColor = new Color(0.180f, 0.800f, 0.443f);
    public Color timerLowColor = new Color(0.906f, 0.298f, 0.235f);

    [Header("Toast")]
    public GameObject toastPanel;
    public TMP_Text toastText;
    public float toastSeconds = 1.4f;

    [Header("End Screen")]
    public GameObject endScreenPanel;
    public TMP_Text endTitleText;
    public TMP_Text finalScoreText;
    public TMP_Text endMessageText;
    public TMP_Text bestComboText;
    public Button restartButton;
    public Button hubButton;

    [Header("Card Colors")]
    public Color cardFaceDownColor = new Color(0.118f, 0.153f, 0.380f);
    public Color cardFaceUpColor = Color.white;
    public Color cardMatchedColor = new Color(0.180f, 0.800f, 0.443f);

    readonly List<MemoryCard> spawnedCards = new List<MemoryCard>();
    Coroutine toastRoutine;
    int latestScore;

    void OnEnable()
    {
        MemoryMatchGameManager.OnLevelStarted    += HandleLevelStarted;
        MemoryMatchGameManager.OnTimerTick       += HandleTimerTick;
        MemoryMatchGameManager.OnRevealStart     += HandleRevealStart;
        MemoryMatchGameManager.OnRevealEnd       += HandleRevealEnd;
        MemoryMatchGameManager.OnCardFlipped     += HandleCardFlipped;
        MemoryMatchGameManager.OnPairMatched     += HandlePairMatched;
        MemoryMatchGameManager.OnPairMissed      += HandlePairMissed;
        MemoryMatchGameManager.OnLevelCompleted  += HandleLevelCompleted;
        MemoryMatchGameManager.OnRoundEnd        += HandleRoundEnd;
    }

    void OnDisable()
    {
        MemoryMatchGameManager.OnLevelStarted    -= HandleLevelStarted;
        MemoryMatchGameManager.OnTimerTick       -= HandleTimerTick;
        MemoryMatchGameManager.OnRevealStart     -= HandleRevealStart;
        MemoryMatchGameManager.OnRevealEnd       -= HandleRevealEnd;
        MemoryMatchGameManager.OnCardFlipped     -= HandleCardFlipped;
        MemoryMatchGameManager.OnPairMatched     -= HandlePairMatched;
        MemoryMatchGameManager.OnPairMissed      -= HandlePairMissed;
        MemoryMatchGameManager.OnLevelCompleted  -= HandleLevelCompleted;
        MemoryMatchGameManager.OnRoundEnd        -= HandleRoundEnd;
    }

    void HandleLevelStarted(MemoryMatchGameManager.LevelConfig lvl,
        int levelIdx, int totalLevels, MemoryCardData[] cards)
    {
        if (endScreenPanel != null) endScreenPanel.SetActive(false);
        if (toastPanel != null) toastPanel.SetActive(false);

        levelText.text = $"LEVEL {levelIdx + 1}: {lvl.levelName.ToUpper()}";
        comboText.text = "";
        matchCountText.text = $"0 / {cards.Length / 2}";
        scoreText.text = $"Score: {latestScore}";

        cardsLayout.constraintCount = lvl.columns;
        cardsLayout.cellSize = ComputeCellSize(lvl.columns, lvl.rows);
        cardsLayout.spacing = cellSpacing;
        cardsLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        cardsLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        cardsLayout.childAlignment = TextAnchor.MiddleCenter;

        foreach (var sc in spawnedCards)
            if (sc != null && sc.gameObject != cardTemplate) Destroy(sc.gameObject);
        spawnedCards.Clear();

        cardTemplate.SetActive(false);
        for (int i = 0; i < cards.Length; i++)
        {
            var go = Instantiate(cardTemplate, cardsLayout.transform);
            go.SetActive(true);
            go.name = $"Card_{i}";
            var memCard = go.GetComponent<MemoryCard>();
            memCard.backgroundFaceDownColor = cardFaceDownColor;
            memCard.backgroundFaceUpColor = cardFaceUpColor;
            memCard.backgroundMatchedColor = cardMatchedColor;
            memCard.Setup(i, cards[i].cardSprite);
            spawnedCards.Add(memCard);
        }

        timerSlider.value = 1f;
        timerFill.color = timerFullColor;
    }

    Vector2 ComputeCellSize(int cols, int rows)
    {
        float w = (boardSize.x - cellSpacing.x * (cols - 1)) / cols;
        float h = (boardSize.y - cellSpacing.y * (rows - 1)) / rows;
        return new Vector2(Mathf.Max(40f, w), Mathf.Max(40f, h));
    }

    void HandleRevealStart()
    {
        foreach (var c in spawnedCards) if (c != null) c.Flip(true);
    }

    void HandleRevealEnd()
    {
        foreach (var c in spawnedCards) if (c != null) c.Flip(false);
    }

    void HandleCardFlipped(int index, bool faceUp)
    {
        if (index < 0 || index >= spawnedCards.Count) return;
        if (spawnedCards[index] != null) spawnedCards[index].Flip(faceUp);
    }

    void HandleTimerTick(float normalized)
    {
        timerSlider.value = normalized;
        timerFill.color = Color.Lerp(timerLowColor, timerFullColor, normalized);
    }

    void HandlePairMatched(int a, int b, int score, int combo, int comboBonus, int matched, int total)
    {
        latestScore = score;
        scoreText.text = $"Score: {score}";
        matchCountText.text = $"{matched} / {total}";
        comboText.text = combo > 1 ? $"x{combo} Combo!" : "";

        if (a >= 0 && a < spawnedCards.Count) spawnedCards[a].MarkMatched();
        if (b >= 0 && b < spawnedCards.Count) spawnedCards[b].MarkMatched();

        if (combo > 1 && comboBonus > 0)
            ShowToast($"COMBO x{combo}   +{comboBonus}");
    }

    void HandlePairMissed(int a, int b, int score)
    {
        comboText.text = "";
        if (a >= 0 && a < spawnedCards.Count) spawnedCards[a].Flip(false);
        if (b >= 0 && b < spawnedCards.Count) spawnedCards[b].Flip(false);
    }

    void HandleLevelCompleted(int levelIndex, int score, int timeBonus)
    {
        latestScore = score;
        scoreText.text = $"Score: {score}";
        if (timeBonus > 0)
            ShowToast($"LEVEL {levelIndex + 1} CLEAR!   +{timeBonus} time");
        else
            ShowToast($"LEVEL {levelIndex + 1} CLEAR!");
    }

    void HandleRoundEnd(int finalScore, int bestCombo, bool allPassed)
    {
        if (toastPanel != null) toastPanel.SetActive(false);
        endScreenPanel.SetActive(true);
        endTitleText.text = allPassed ? "ALL LEVELS COMPLETE" : "TIME'S UP";
        finalScoreText.text = $"{finalScore}";
        bestComboText.text = $"Best Combo: x{bestCombo}";
        endMessageText.text = allPassed
            ? "Memory master — well done."
            : "Time ran out. Practice makes perfect!";
    }

    void ShowToast(string message)
    {
        if (toastPanel == null || toastText == null) return;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastText.text = message;
        toastPanel.SetActive(true);
        toastRoutine = StartCoroutine(HideToast());
    }

    IEnumerator HideToast()
    {
        yield return new WaitForSeconds(toastSeconds);
        if (toastPanel != null) toastPanel.SetActive(false);
        toastRoutine = null;
    }
}
