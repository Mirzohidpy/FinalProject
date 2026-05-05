using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimelineSortUIManager : MonoBehaviour
{
    [Header("Card list")]
    public Transform cardListContainer;
    public GameObject cardTemplate;

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text resultText;

    [Header("Buttons")]
    public Button submitButton;
    public Button restartButton;
    public Button hubButton;

    [Header("Reveal colors")]
    public Color correctColor = new Color(0.18f, 0.80f, 0.44f);
    public Color wrongColor   = new Color(0.91f, 0.30f, 0.24f);

    readonly List<TimelineCard> activeCards = new List<TimelineCard>();
    TimelineSortGameManager gameManager;

    void Awake()
    {
        gameManager = FindFirstObjectByType<TimelineSortGameManager>();

        if (cardTemplate != null) cardTemplate.SetActive(false);
        if (resultText != null) resultText.text = "";
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmitClicked);
        if (restartButton != null) restartButton.gameObject.SetActive(false);
        if (hubButton != null) hubButton.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        TimelineSortGameManager.OnRoundStart    += HandleRoundStart;
        TimelineSortGameManager.OnSubmitResult  += HandleSubmitResult;
    }

    void OnDisable()
    {
        TimelineSortGameManager.OnRoundStart    -= HandleRoundStart;
        TimelineSortGameManager.OnSubmitResult  -= HandleSubmitResult;
    }

    void HandleRoundStart(List<TimelineEventData> events)
    {
        // Clear any prior cards
        foreach (var c in activeCards)
            if (c != null) Destroy(c.gameObject);
        activeCards.Clear();

        if (cardTemplate == null || cardListContainer == null)
        {
            Debug.LogError("TimelineSortUIManager: cardTemplate or container not assigned.");
            return;
        }

        foreach (var ev in events)
        {
            var go = Instantiate(cardTemplate, cardListContainer);
            go.SetActive(true);
            var card = go.GetComponent<TimelineCard>();
            if (card == null) card = go.AddComponent<TimelineCard>();
            card.Setup(ev, cardListContainer);
            activeCards.Add(card);
        }

        if (scoreText != null) scoreText.text = "Sort oldest to newest, then submit.";
        if (resultText != null) resultText.text = "";
        if (submitButton != null)
        {
            submitButton.gameObject.SetActive(true);
            submitButton.interactable = true;
        }
    }

    void OnSubmitClicked()
    {
        var order = new List<TimelineEventData>();
        for (int i = 0; i < cardListContainer.childCount; i++)
        {
            var child = cardListContainer.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            var card = child.GetComponent<TimelineCard>();
            if (card == null || card.eventData == null) continue;
            order.Add(card.eventData);
        }
        if (gameManager == null) gameManager = FindFirstObjectByType<TimelineSortGameManager>();
        gameManager?.OnPlayerSubmit(order);
    }

    void HandleSubmitResult(bool fullyCorrect, int score,
        List<TimelineEventData> playerOrder, List<TimelineEventData> correctOrder)
    {
        if (submitButton != null) submitButton.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(true);
        if (hubButton != null) hubButton.gameObject.SetActive(true);

        // Reveal cards
        int playerIdx = 0;
        for (int i = 0; i < cardListContainer.childCount; i++)
        {
            var child = cardListContainer.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            var card = child.GetComponent<TimelineCard>();
            if (card == null || card.eventData == null) continue;
            bool isInCorrectPosition = playerIdx < correctOrder.Count
                && card.eventData == correctOrder[playerIdx];
            card.Reveal(isInCorrectPosition, correctColor, wrongColor);
            playerIdx++;
        }

        if (scoreText != null) scoreText.text = $"Score: {score}";
        if (resultText != null)
        {
            resultText.text = fullyCorrect
                ? "Perfect order!"
                : "Some events out of order. Years revealed below.";
            resultText.color = fullyCorrect ? correctColor : wrongColor;
        }
    }
}
