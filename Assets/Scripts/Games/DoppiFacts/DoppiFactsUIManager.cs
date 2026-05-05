using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DoppiFactsUIManager : MonoBehaviour
{
    [Header("Holes (parent of N child Hole objects, each with a 'Mole' child)")]
    public Transform holeGrid;

    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text waveText;
    public TMP_Text streakText;

    [Header("Banner")]
    public GameObject bannerPanel;
    public TMP_Text bannerText;
    public float bannerDuration = 1.0f;

    [Header("End Screen")]
    public GameObject endScreenPanel;
    public TMP_Text finalScoreText;
    public TMP_Text endMessageText;
    public Button restartButton;
    public Button hubButton;

    [Header("Mole colors")]
    public Color moleNeutralColor = new Color(0.95f, 0.78f, 0.45f);
    public Color hitCorrectColor  = new Color(0.18f, 0.80f, 0.44f);
    public Color hitWrongColor    = new Color(0.91f, 0.30f, 0.24f);

    [Header("Tween")]
    public float popInDuration  = 0.18f;
    public float popOutDuration = 0.14f;

    Image[] moleBackgrounds;
    TMP_Text[] moleTexts;
    Button[] moleButtons;
    Transform[] moleTransforms;
    Coroutine[] moleAnimations;

    DoppiFactsGameManager gameManager;

    void Awake()
    {
        gameManager = FindFirstObjectByType<DoppiFactsGameManager>();

        int n = holeGrid != null ? holeGrid.childCount : 0;
        moleBackgrounds = new Image[n];
        moleTexts = new TMP_Text[n];
        moleButtons = new Button[n];
        moleTransforms = new Transform[n];
        moleAnimations = new Coroutine[n];

        for (int i = 0; i < n; i++)
        {
            var hole = holeGrid.GetChild(i);
            var mole = hole.Find("Mole");
            if (mole == null) continue;
            moleTransforms[i] = mole;
            moleBackgrounds[i] = mole.GetComponent<Image>();
            moleTexts[i] = mole.GetComponentInChildren<TMP_Text>(true);
            moleButtons[i] = mole.GetComponent<Button>();

            int idx = i;
            if (moleButtons[i] != null)
                moleButtons[i].onClick.AddListener(() => OnMoleClicked(idx));

            mole.localScale = Vector3.zero;
        }

        if (bannerPanel != null) bannerPanel.SetActive(false);
        if (endScreenPanel != null) endScreenPanel.SetActive(false);
    }

    void OnEnable()
    {
        DoppiFactsGameManager.OnGameStart  += HandleGameStart;
        DoppiFactsGameManager.OnWaveStart  += HandleWaveStart;
        DoppiFactsGameManager.OnMoleAppear += HandleMoleAppear;
        DoppiFactsGameManager.OnMoleHide   += HandleMoleHide;
        DoppiFactsGameManager.OnHitResult  += HandleHitResult;
        DoppiFactsGameManager.OnGameEnd    += HandleGameEnd;
    }

    void OnDisable()
    {
        DoppiFactsGameManager.OnGameStart  -= HandleGameStart;
        DoppiFactsGameManager.OnWaveStart  -= HandleWaveStart;
        DoppiFactsGameManager.OnMoleAppear -= HandleMoleAppear;
        DoppiFactsGameManager.OnMoleHide   -= HandleMoleHide;
        DoppiFactsGameManager.OnHitResult  -= HandleHitResult;
        DoppiFactsGameManager.OnGameEnd    -= HandleGameEnd;
    }

    void HandleGameStart(int totalWaves)
    {
        if (scoreText != null) scoreText.text = "Score: 0";
        if (streakText != null) streakText.text = "";
        if (waveText != null) waveText.text = $"Wave 0 / {totalWaves}";
    }

    void HandleWaveStart(int waveNumber, int total)
    {
        if (waveText != null) waveText.text = $"Wave {waveNumber} / {total}";
        StartCoroutine(ShowBanner($"WAVE {waveNumber}"));
    }

    void HandleMoleAppear(int holeIdx, DoppiFactData fact, float linger)
    {
        if (holeIdx < 0 || holeIdx >= moleTransforms.Length) return;
        if (moleTexts[holeIdx] != null) moleTexts[holeIdx].text = fact.statement;
        if (moleBackgrounds[holeIdx] != null) moleBackgrounds[holeIdx].color = moleNeutralColor;
        if (moleButtons[holeIdx] != null) moleButtons[holeIdx].interactable = true;

        if (moleAnimations[holeIdx] != null) StopCoroutine(moleAnimations[holeIdx]);
        moleAnimations[holeIdx] = StartCoroutine(TweenScale(moleTransforms[holeIdx], Vector3.one, popInDuration));
    }

    void HandleMoleHide(int holeIdx)
    {
        if (holeIdx < 0 || holeIdx >= moleTransforms.Length) return;
        if (moleButtons[holeIdx] != null) moleButtons[holeIdx].interactable = false;
        if (moleAnimations[holeIdx] != null) StopCoroutine(moleAnimations[holeIdx]);
        moleAnimations[holeIdx] = StartCoroutine(TweenScale(moleTransforms[holeIdx], Vector3.zero, popOutDuration));
    }

    void HandleHitResult(bool correct, int holeIdx, int delta, int score, int streak)
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
        if (streakText != null) streakText.text = streak > 1 ? $"x{streak} streak!" : "";

        if (holeIdx >= 0 && holeIdx < moleBackgrounds.Length && moleBackgrounds[holeIdx] != null)
            moleBackgrounds[holeIdx].color = correct ? hitCorrectColor : hitWrongColor;
    }

    void HandleGameEnd(int finalScore)
    {
        if (endScreenPanel != null) endScreenPanel.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = finalScore.ToString();
        if (endMessageText != null) endMessageText.text = GetEndMessage(finalScore);
    }

    void OnMoleClicked(int holeIdx)
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<DoppiFactsGameManager>();
        if (gameManager != null) gameManager.OnPlayerHit(holeIdx);
    }

    IEnumerator ShowBanner(string text)
    {
        if (bannerPanel == null) yield break;
        if (bannerText != null) bannerText.text = text;
        bannerPanel.SetActive(true);
        yield return new WaitForSeconds(bannerDuration);
        bannerPanel.SetActive(false);
    }

    IEnumerator TweenScale(Transform t, Vector3 target, float duration)
    {
        if (t == null) yield break;
        Vector3 start = t.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            t.localScale = Vector3.Lerp(start, target, u);
            yield return null;
        }
        t.localScale = target;
    }

    string GetEndMessage(int score)
    {
        if (score >= 1500) return "Civic champion!";
        if (score >= 1000) return "Sharp eye - well done.";
        if (score >= 600)  return "Solid - keep practising.";
        if (score >= 300)  return "Not bad - try again!";
        return "Don't fall for the myths.";
    }
}
