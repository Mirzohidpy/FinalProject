#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-click builder for Math Sprint.
/// Creates 5 MathLevelData assets + MathLevelDatabase, builds the scene,
/// and unlocks Game 4 in the hub.
///
/// Run via: BrainCitizen > Build MathSprint Game
/// </summary>
public static class MathSprintSceneBuilder
{
    const string ScenePath = "Assets/Scenes/MathSprint.unity";
    const string DataDir = "Assets/Data/Math";
    const string DatabasePath = DataDir + "/MathLevelDatabase.asset";
    const string HubGameInfoPath = "Assets/Data/Hub/Game04_MathSprint.asset";

    static readonly Color NavyBg        = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color NavyBgDark    = new Color(0.063f, 0.082f, 0.220f);
    static readonly Color White         = Color.white;
    static readonly Color GreenAccent   = new Color(0.180f, 0.800f, 0.443f);
    static readonly Color GreenSolid    = new Color(0.153f, 0.682f, 0.376f);
    static readonly Color RedAccent     = new Color(0.906f, 0.298f, 0.235f);
    static readonly Color TextPrimary   = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color PanelDim      = new Color(0f, 0f, 0f, 0.65f);

    struct LevelDef
    {
        public string Name;
        public MathOperation Op;
        public int Min;
        public int Max;
        public int Questions;
        public float Seconds;
        public bool SuddenDeath;
    }

    static readonly LevelDef[] LEVELS = new[]
    {
        new LevelDef { Name = "Warm Up",       Op = MathOperation.Add,         Min = 1, Max = 10, Questions = 5, Seconds = 8f, SuddenDeath = false },
        new LevelDef { Name = "Subtraction",   Op = MathOperation.Subtract,    Min = 1, Max = 20, Questions = 5, Seconds = 7f, SuddenDeath = false },
        new LevelDef { Name = "Mix It Up",     Op = MathOperation.MixedAddSub, Min = 1, Max = 50, Questions = 5, Seconds = 6f, SuddenDeath = false },
        new LevelDef { Name = "Times Tables",  Op = MathOperation.Multiply,    Min = 2, Max = 9,  Questions = 5, Seconds = 6f, SuddenDeath = false },
        new LevelDef { Name = "Sudden Death",  Op = MathOperation.MixedAll,    Min = 1, Max = 30, Questions = 5, Seconds = 5f, SuddenDeath = true  },
    };

    [MenuItem("BrainCitizen/Build MathSprint Game")]
    public static void Build()
    {
        if (File.Exists(ScenePath))
        {
            if (!EditorUtility.DisplayDialog(
                "Scene exists",
                $"{ScenePath} already exists. Overwrite?",
                "Overwrite", "Cancel"))
            {
                return;
            }
        }

        var db = BuildLevelAssets();
        if (db == null) return;
        BuildScene(db);
        UnlockInHub();
        AddToBuildSettings();

        EditorUtility.DisplayDialog(
            "Math Sprint built",
            $"Scene saved to {ScenePath}\n" +
            $"{db.levels.Length} levels loaded.\n\n" +
            "Press Play.",
            "OK");
    }

    // =======================================================
    // Level data assets
    // =======================================================

    static MathLevelDatabase BuildLevelAssets()
    {
        if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);

        var infos = new List<MathLevelData>();
        for (int i = 0; i < LEVELS.Length; i++)
        {
            var def = LEVELS[i];
            string assetPath = $"{DataDir}/Level{(i + 1):D2}_{def.Name.Replace(" ", "")}.asset";

            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var lvl = ScriptableObject.CreateInstance<MathLevelData>();
            lvl.levelName = def.Name;
            lvl.operation = def.Op;
            lvl.minOperand = def.Min;
            lvl.maxOperand = def.Max;
            lvl.questionsInLevel = def.Questions;
            lvl.secondsPerQuestion = def.Seconds;
            lvl.suddenDeath = def.SuddenDeath;
            AssetDatabase.CreateAsset(lvl, assetPath);

            var so = new SerializedObject(lvl);
            so.FindProperty("levelName").stringValue = def.Name;
            so.FindProperty("operation").enumValueIndex = (int)def.Op;
            so.FindProperty("minOperand").intValue = def.Min;
            so.FindProperty("maxOperand").intValue = def.Max;
            so.FindProperty("questionsInLevel").intValue = def.Questions;
            so.FindProperty("secondsPerQuestion").floatValue = def.Seconds;
            so.FindProperty("suddenDeath").boolValue = def.SuddenDeath;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lvl);

            infos.Add(lvl);
        }

        var db = AssetDatabase.LoadAssetAtPath<MathLevelDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<MathLevelDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        db.levels = infos.ToArray();
        EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return db;
    }

    // =======================================================
    // Scene
    // =======================================================

    static void BuildScene(MathLevelDatabase db)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGO = new GameObject("Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = MakeImage("Background", canvasGO.transform, NavyBg);
        Stretch(bg);

        // Score (top-left)
        var scoreText = MakeText("ScoreText", canvasGO.transform, "Score: 0",
            40, FontStyles.Bold, White);
        var scoreRT = scoreText.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0, 1);
        scoreRT.anchorMax = new Vector2(0, 1);
        scoreRT.pivot = new Vector2(0, 0.5f);
        scoreRT.anchoredPosition = new Vector2(60, -55);
        scoreRT.sizeDelta = new Vector2(420, 60);
        scoreText.alignment = TextAlignmentOptions.Left;

        // Streak HUD (top-right)
        var streakHUDText = MakeText("StreakHUDText", canvasGO.transform, "",
            36, FontStyles.Bold, GreenAccent);
        var streakRT = streakHUDText.GetComponent<RectTransform>();
        streakRT.anchorMin = new Vector2(1, 1);
        streakRT.anchorMax = new Vector2(1, 1);
        streakRT.pivot = new Vector2(1, 0.5f);
        streakRT.anchoredPosition = new Vector2(-60, -55);
        streakRT.sizeDelta = new Vector2(400, 60);
        streakHUDText.alignment = TextAlignmentOptions.Right;

        // Question counter (top-center, left of level)
        var questionCounterText = MakeText("QuestionCounterText", canvasGO.transform,
            "Question 1 / 25", 36, FontStyles.Bold, White);
        var qcRT = questionCounterText.GetComponent<RectTransform>();
        qcRT.anchorMin = new Vector2(0.5f, 1);
        qcRT.anchorMax = new Vector2(0.5f, 1);
        qcRT.pivot = new Vector2(1, 0.5f);
        qcRT.anchoredPosition = new Vector2(-30, -130);
        qcRT.sizeDelta = new Vector2(500, 50);
        questionCounterText.alignment = TextAlignmentOptions.Right;

        // Level name (top-center, right of counter)
        var levelText = MakeText("LevelText", canvasGO.transform, "WARM UP",
            36, FontStyles.Bold, White);
        var lvRT = levelText.GetComponent<RectTransform>();
        lvRT.anchorMin = new Vector2(0.5f, 1);
        lvRT.anchorMax = new Vector2(0.5f, 1);
        lvRT.pivot = new Vector2(0, 0.5f);
        lvRT.anchoredPosition = new Vector2(30, -130);
        lvRT.sizeDelta = new Vector2(500, 50);
        levelText.alignment = TextAlignmentOptions.Left;

        // Equation (huge center text)
        var equationText = MakeText("EquationText", canvasGO.transform, "0 + 0 = ?",
            180, FontStyles.Bold, White);
        var eqRT = equationText.GetComponent<RectTransform>();
        eqRT.anchorMin = new Vector2(0.5f, 1);
        eqRT.anchorMax = new Vector2(0.5f, 1);
        eqRT.pivot = new Vector2(0.5f, 0.5f);
        eqRT.anchoredPosition = new Vector2(0, -380);
        eqRT.sizeDelta = new Vector2(1600, 320);

        // Timer slider
        var timerSlider = MakeSlider("TimerSlider", canvasGO.transform, GreenAccent);
        var timerRT = timerSlider.GetComponent<RectTransform>();
        SetAnchor(timerRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        timerRT.anchoredPosition = new Vector2(0, -570);
        timerRT.sizeDelta = new Vector2(900, 24);
        var timerFill = timerSlider.fillRect.GetComponent<Image>();

        // Answer grid (2x2)
        var answerGridGO = new GameObject("AnswerGrid",
            typeof(RectTransform), typeof(GridLayoutGroup));
        answerGridGO.transform.SetParent(canvasGO.transform, false);
        var agRT = answerGridGO.GetComponent<RectTransform>();
        agRT.anchorMin = new Vector2(0.5f, 0f);
        agRT.anchorMax = new Vector2(0.5f, 0f);
        agRT.pivot = new Vector2(0.5f, 0f);
        agRT.anchoredPosition = new Vector2(0, 80);
        agRT.sizeDelta = new Vector2(1240, 320);
        var glg = answerGridGO.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(610, 150);
        glg.spacing = new Vector2(20, 20);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.childAlignment = TextAnchor.MiddleCenter;

        var answerButtons = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            answerButtons[i] = MakeButton($"AnswerButton{i + 1}", answerGridGO.transform,
                "0", new Color(1f, 1f, 1f, 0.92f), TextPrimary, 64);
        }

        // Feedback panel
        var feedbackPanel = MakeImage("FeedbackPanel", canvasGO.transform, PanelDim);
        Stretch(feedbackPanel);
        var feedbackResult = MakeText("FeedbackResultText", feedbackPanel.transform,
            "CORRECT!", 96, FontStyles.Bold, GreenAccent);
        SetAnchor(feedbackResult, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        feedbackResult.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 140);
        feedbackResult.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 130);

        var correctAnswer = MakeText("CorrectAnswerText", feedbackPanel.transform, "",
            56, FontStyles.Normal, White);
        SetAnchor(correctAnswer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        correctAnswer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
        correctAnswer.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 80);

        var bonusText = MakeText("BonusText", feedbackPanel.transform, "",
            36, FontStyles.Bold, GreenAccent);
        SetAnchor(bonusText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        bonusText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -60);
        bonusText.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 60);
        feedbackPanel.gameObject.SetActive(false);

        // End screen
        var endPanel = MakeImage("EndScreenPanel", canvasGO.transform, NavyBg);
        Stretch(endPanel);
        var endTitle = MakeText("EndTitle", endPanel.transform, "RUN COMPLETE",
            72, FontStyles.Bold, White);
        SetAnchor(endTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        endTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
        endTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 100);

        var finalScore = MakeText("FinalScoreText", endPanel.transform, "0",
            144, FontStyles.Bold, GreenAccent);
        SetAnchor(finalScore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        finalScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 130);
        finalScore.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 200);

        var bestStreakLabel = MakeText("BestStreakText", endPanel.transform, "Best Streak: x0",
            44, FontStyles.Bold, GreenAccent);
        SetAnchor(bestStreakLabel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        bestStreakLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
        bestStreakLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 70);

        var endMessage = MakeText("EndMessageText", endPanel.transform, "",
            38, FontStyles.Normal, White);
        SetAnchor(endMessage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        endMessage.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -110);
        endMessage.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 100);

        var restartBtn = MakeButton("RestartButton", endPanel.transform, "PLAY AGAIN",
            GreenSolid, White, 44);
        var rRT = restartBtn.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0.5f, 0f); rRT.anchorMax = new Vector2(0.5f, 0f);
        rRT.pivot = new Vector2(0.5f, 0f);
        rRT.anchoredPosition = new Vector2(-260, 250);
        rRT.sizeDelta = new Vector2(440, 130);

        var hubBtn = MakeButton("HubButton", endPanel.transform, "BACK TO HUB",
            new Color(0.3f, 0.3f, 0.5f), White, 44);
        var hRT = hubBtn.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0.5f, 0f); hRT.anchorMax = new Vector2(0.5f, 0f);
        hRT.pivot = new Vector2(0.5f, 0f);
        hRT.anchoredPosition = new Vector2(260, 250);
        hRT.sizeDelta = new Vector2(440, 130);

        endPanel.gameObject.SetActive(false);

        // Managers
        var gameManagerGO = new GameObject("GameManager");
        var gameManager = gameManagerGO.AddComponent<MathSprintGameManager>();

        var dbLive = AssetDatabase.LoadAssetAtPath<MathLevelDatabase>(DatabasePath);
        if (dbLive == null)
        {
            Debug.LogError($"Failed to reload {DatabasePath}; using stale reference.");
            dbLive = db;
        }
        gameManager.levelDatabase = dbLive;
        var so = new SerializedObject(gameManager);
        so.FindProperty("levelDatabase").objectReferenceValue = dbLive;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);

        var uiManagerGO = new GameObject("UIManager");
        var uiManager = uiManagerGO.AddComponent<MathSprintUIManager>();
        uiManager.equationText = equationText;
        uiManager.questionCounterText = questionCounterText;
        uiManager.levelText = levelText;
        uiManager.timerSlider = timerSlider;
        uiManager.timerFill = timerFill;
        uiManager.timerFullColor = GreenAccent;
        uiManager.timerLowColor = RedAccent;
        uiManager.answerGrid = answerGridGO.transform;
        uiManager.feedbackPanel = feedbackPanel.gameObject;
        uiManager.feedbackResultText = feedbackResult;
        uiManager.correctAnswerText = correctAnswer;
        uiManager.bonusText = bonusText;
        uiManager.scoreText = scoreText;
        uiManager.streakHUDText = streakHUDText;
        uiManager.endScreenPanel = endPanel.gameObject;
        uiManager.finalScoreText = finalScore;
        uiManager.endMessageText = endMessage;
        uiManager.bestStreakText = bestStreakLabel;
        uiManager.restartButton = restartBtn;
        uiManager.hubButton = hubBtn;
        uiManager.correctColor = GreenAccent;
        uiManager.wrongColor = RedAccent;
        uiManager.suddenDeathColor = RedAccent;
        uiManager.regularLevelColor = White;
        EditorUtility.SetDirty(uiManager);

        UnityAction<int> onAnswer = gameManager.OnPlayerAnswer;
        for (int i = 0; i < 4; i++)
            UnityEventTools.AddIntPersistentListener(answerButtons[i].onClick, onAnswer, i);
        UnityEventTools.AddPersistentListener(restartBtn.onClick, gameManager.RestartGame);
        UnityEventTools.AddPersistentListener(hubBtn.onClick, gameManager.ReturnToHub);

        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    // =======================================================
    // Hub registry + build settings
    // =======================================================

    static void UnlockInHub()
    {
        var info = AssetDatabase.LoadAssetAtPath<GameInfo>(HubGameInfoPath);
        if (info != null)
        {
            info.isImplemented = true;
            EditorUtility.SetDirty(info);
            AssetDatabase.SaveAssets();
        }
        else
        {
            Debug.LogWarning(
                "Couldn't find " + HubGameInfoPath + " - run BrainCitizen > Build Hub Scene first.");
        }
    }

    static void AddToBuildSettings()
    {
        var existing = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!existing.Exists(s => s.path == ScenePath))
        {
            existing.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = existing.ToArray();
        }
    }

    // =======================================================
    // UI helpers
    // =======================================================

    static Image MakeImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<Image>();
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
        Color bgColor, Color textColor, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bgColor;
        var btn = go.GetComponent<Button>();

        var tmp = MakeText("Label", go.transform, label, fontSize, FontStyles.Bold, textColor);
        var rt = tmp.GetComponent<RectTransform>();
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

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0);
        faRT.anchorMax = new Vector2(1, 1);
        faRT.offsetMin = new Vector2(4, 4);
        faRT.offsetMax = new Vector2(-4, -4);

        var fill = new GameObject("Fill",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = fillColor;
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
