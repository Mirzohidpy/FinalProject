#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-click scene builder for the True or False News mini-game.
/// Run via menu: BrainCitizen > Build TrueFalseNews Scene.
/// Creates a new scene with the full UI hierarchy, wires up GameManager + UIManager,
/// links the HeadlineDatabase, and saves to Assets/Scenes/TrueFalseNews.unity.
/// </summary>
public static class TrueFalseSceneBuilder
{
    const string ScenePath = "Assets/Scenes/TrueFalseNews.unity";
    const string DatabasePath = "Assets/Data/Headlines/HeadlineDatabase.asset";

    static readonly Color NavyBg     = new Color(0.118f, 0.153f, 0.380f); // #1E2761
    static readonly Color White      = Color.white;
    static readonly Color GreenReal  = new Color(0.153f, 0.682f, 0.376f); // #27AE60
    static readonly Color RedFake    = new Color(0.906f, 0.298f, 0.235f); // #E74C3C
    static readonly Color TimerFull  = new Color(0.180f, 0.800f, 0.443f); // #2ECC71
    static readonly Color TimerLow   = new Color(0.906f, 0.298f, 0.235f);
    static readonly Color PanelDim   = new Color(0f, 0f, 0f, 0.65f);

    [MenuItem("BrainCitizen/Build TrueFalseNews Scene")]
    public static void Build()
    {
        if (System.IO.File.Exists(ScenePath))
        {
            if (!EditorUtility.DisplayDialog(
                "Scene exists",
                $"{ScenePath} already exists. Overwrite?",
                "Overwrite", "Cancel"))
            {
                return;
            }
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // EventSystem (required for UI input)
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // Canvas
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Background
        var bg = MakeImage("Background", canvasGO.transform, NavyBg);
        Stretch(bg);

        // ----- Question Panel -----
        var questionPanel = MakeImage("QuestionPanel", canvasGO.transform, White);
        SetAnchor(questionPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        questionPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -250);
        questionPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 380);

        var categoryText = MakeText("CategoryText", questionPanel.transform, "POLITICS",
            48, FontStyles.Bold, NavyBg);
        SetAnchor(categoryText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        categoryText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        categoryText.GetComponent<RectTransform>().sizeDelta = new Vector2(1300, 60);
        categoryText.alignment = TextAlignmentOptions.Center;

        var headlineText = MakeText("HeadlineText", questionPanel.transform,
            "Headline text appears here…", 56, FontStyles.Bold, NavyBg);
        SetAnchor(headlineText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        headlineText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);
        headlineText.GetComponent<RectTransform>().sizeDelta = new Vector2(1300, 200);
        headlineText.alignment = TextAlignmentOptions.Center;
        headlineText.textWrappingMode = TextWrappingModes.Normal;

        var sourceHintText = MakeText("SourceHintText", questionPanel.transform,
            "Source: …", 32, FontStyles.Italic, NavyBg);
        SetAnchor(sourceHintText, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        sourceHintText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
        sourceHintText.GetComponent<RectTransform>().sizeDelta = new Vector2(1300, 40);
        sourceHintText.alignment = TextAlignmentOptions.Center;

        var questionCounterText = MakeText("QuestionCounterText", canvasGO.transform,
            "Question 1 / 10", 36, FontStyles.Bold, White);
        SetAnchor(questionCounterText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        questionCounterText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -40);
        questionCounterText.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 50);
        questionCounterText.alignment = TextAlignmentOptions.Center;

        // ----- Timer -----
        var timerSlider = MakeSlider("TimerSlider", canvasGO.transform, TimerFull);
        var timerRT = timerSlider.GetComponent<RectTransform>();
        SetAnchor(timerRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        timerRT.anchoredPosition = new Vector2(0, -50);
        timerRT.sizeDelta = new Vector2(1200, 28);
        var timerFill = timerSlider.fillRect.GetComponent<Image>();

        // ----- Answer Buttons -----
        var realButton = MakeButton("RealButton", canvasGO.transform, "REAL", GreenReal, White);
        SetAnchor(realButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        realButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-340, 250);
        realButton.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 200);
        var realButtonText = realButton.GetComponentInChildren<TextMeshProUGUI>();

        var fakeButton = MakeButton("FakeButton", canvasGO.transform, "FAKE", RedFake, White);
        SetAnchor(fakeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        fakeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(340, 250);
        fakeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 200);
        var fakeButtonText = fakeButton.GetComponentInChildren<TextMeshProUGUI>();

        // ----- HUD -----
        var scoreText = MakeText("ScoreText", canvasGO.transform, "Score: 0", 40, FontStyles.Bold, White);
        SetAnchor(scoreText, new Vector2(0f, 1f), new Vector2(0f, 1f));
        scoreText.GetComponent<RectTransform>().anchoredPosition = new Vector2(60, -40);
        scoreText.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 60);
        scoreText.alignment = TextAlignmentOptions.Left;

        var streakHUDText = MakeText("StreakHUDText", canvasGO.transform, "", 36, FontStyles.Bold, GreenReal);
        SetAnchor(streakHUDText, new Vector2(1f, 1f), new Vector2(1f, 1f));
        streakHUDText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-60, -40);
        streakHUDText.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 60);
        streakHUDText.alignment = TextAlignmentOptions.Right;

        // ----- Feedback Panel (hidden by default) -----
        var feedbackPanel = MakeImage("FeedbackPanel", canvasGO.transform, PanelDim);
        Stretch(feedbackPanel);

        var feedbackResultText = MakeText("FeedbackResultText", feedbackPanel.transform,
            "CORRECT!", 96, FontStyles.Bold, GreenReal);
        SetAnchor(feedbackResultText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        feedbackResultText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);
        feedbackResultText.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 130);
        feedbackResultText.alignment = TextAlignmentOptions.Center;

        var explanationText = MakeText("ExplanationText", feedbackPanel.transform,
            "Explanation appears here.", 36, FontStyles.Normal, White);
        SetAnchor(explanationText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        explanationText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        explanationText.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 200);
        explanationText.alignment = TextAlignmentOptions.Center;
        explanationText.textWrappingMode = TextWrappingModes.Normal;

        var streakText = MakeText("StreakText", feedbackPanel.transform, "", 36, FontStyles.Bold, GreenReal);
        SetAnchor(streakText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        streakText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -160);
        streakText.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 60);
        streakText.alignment = TextAlignmentOptions.Center;

        feedbackPanel.gameObject.SetActive(false);

        // ----- End Screen Panel (hidden by default) -----
        var endScreenPanel = MakeImage("EndScreenPanel", canvasGO.transform, NavyBg);
        Stretch(endScreenPanel);

        var endTitle = MakeText("EndTitle", endScreenPanel.transform, "ROUND COMPLETE", 72,
            FontStyles.Bold, White);
        SetAnchor(endTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        endTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
        endTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 100);
        endTitle.alignment = TextAlignmentOptions.Center;

        var finalScoreText = MakeText("FinalScoreText", endScreenPanel.transform, "0", 144,
            FontStyles.Bold, GreenReal);
        SetAnchor(finalScoreText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        finalScoreText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);
        finalScoreText.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 200);
        finalScoreText.alignment = TextAlignmentOptions.Center;

        var endMessageText = MakeText("EndMessageText", endScreenPanel.transform,
            "Great job!", 40, FontStyles.Normal, White);
        SetAnchor(endMessageText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        endMessageText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -80);
        endMessageText.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 100);
        endMessageText.alignment = TextAlignmentOptions.Center;
        endMessageText.textWrappingMode = TextWrappingModes.Normal;

        var restartButton = MakeButton("RestartButton", endScreenPanel.transform, "PLAY AGAIN",
            GreenReal, White);
        SetAnchor(restartButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        restartButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-260, 250);
        restartButton.GetComponent<RectTransform>().sizeDelta = new Vector2(440, 140);

        var hubButton = MakeButton("HubButton", endScreenPanel.transform, "BACK TO HUB",
            new Color(0.3f, 0.3f, 0.5f), White);
        SetAnchor(hubButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        hubButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(260, 250);
        hubButton.GetComponent<RectTransform>().sizeDelta = new Vector2(440, 140);

        endScreenPanel.gameObject.SetActive(false);

        // ----- Managers -----
        var gameManagerGO = new GameObject("GameManager");
        var gameManager = gameManagerGO.AddComponent<TrueFalseGameManager>();
        var db = AssetDatabase.LoadAssetAtPath<HeadlineDatabase>(DatabasePath);
        if (db == null)
        {
            Debug.LogWarning($"HeadlineDatabase not found at {DatabasePath}. " +
                             "Drag it into GameManager.headlineDatabase manually.");
        }
        else
        {
            var so = new SerializedObject(gameManager);
            so.FindProperty("headlineDatabase").objectReferenceValue = db;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var uiManagerGO = new GameObject("UIManager");
        var uiManager = uiManagerGO.AddComponent<TrueFalseUIManager>();
        uiManager.headlineText        = headlineText;
        uiManager.categoryText        = categoryText;
        uiManager.sourceHintText      = sourceHintText;
        uiManager.questionCounterText = questionCounterText;
        uiManager.timerSlider         = timerSlider;
        uiManager.timerFill           = timerFill;
        uiManager.timerColorFull      = TimerFull;
        uiManager.timerColorLow       = TimerLow;
        uiManager.realButton          = realButton;
        uiManager.fakeButton          = fakeButton;
        uiManager.realButtonText      = realButtonText;
        uiManager.fakeButtonText      = fakeButtonText;
        uiManager.feedbackPanel       = feedbackPanel.gameObject;
        uiManager.feedbackResultText  = feedbackResultText;
        uiManager.explanationText     = explanationText;
        uiManager.streakText          = streakText;
        uiManager.scoreText           = scoreText;
        uiManager.streakHUDText       = streakHUDText;
        uiManager.endScreenPanel      = endScreenPanel.gameObject;
        uiManager.finalScoreText      = finalScoreText;
        uiManager.endMessageText      = endMessageText;
        uiManager.restartButton       = restartButton;
        uiManager.hubButton           = hubButton;
        uiManager.correctColor        = GreenReal;
        uiManager.wrongColor          = RedFake;

        // Wire up button events as persistent listeners
        UnityAction<bool> onAnswer = gameManager.OnPlayerAnswer;
        UnityEventTools.AddBoolPersistentListener(realButton.onClick, onAnswer, true);
        UnityEventTools.AddBoolPersistentListener(fakeButton.onClick, onAnswer, false);
        UnityEventTools.AddPersistentListener(restartButton.onClick, gameManager.RestartGame);
        UnityEventTools.AddPersistentListener(hubButton.onClick, gameManager.ReturnToHub);

        // Save scene
        if (!System.IO.Directory.Exists("Assets/Scenes"))
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorUtility.DisplayDialog(
            "Scene built",
            $"TrueFalseNews scene saved to {ScenePath}\n\n" +
            "Next: add this scene to Build Settings (File > Build Settings).",
            "OK");
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

    static Image MakeImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI MakeText(string name, Transform parent, string text,
        float fontSize, FontStyles style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    static Button MakeButton(string name, Transform parent, string label,
        Color bgColor, Color textColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bgColor;
        var btn = go.GetComponent<Button>();

        var labelTmp = MakeText("Label", go.transform, label, 56, FontStyles.Bold, textColor);
        var rt = labelTmp.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return btn;
    }

    static Slider MakeSlider(string name, Transform parent, Color fillColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

        var slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;

        // Fill area
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0);
        faRT.anchorMax = new Vector2(1, 1);
        faRT.offsetMin = new Vector2(4, 4);
        faRT.offsetMax = new Vector2(-4, -4);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.GetComponent<Image>();
        fillImg.color = fillColor;
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0);
        fillRT.anchorMax = new Vector2(1, 1);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        slider.fillRect = fillRT;

        return slider;
    }

    static void Stretch(Image img)
    {
        var rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetAnchor(Component c, Vector2 min, Vector2 max)
    {
        var rt = c.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
    }
}
#endif
