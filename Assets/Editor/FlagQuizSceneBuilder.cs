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
/// One-click builder for Flag Quiz.
/// Expects 30 flag PNGs in Assets/Data/Flags/Textures/ (one per country).
/// Imports them as Sprites, creates FlagData + FlagDatabase assets, builds
/// the scene, and unlocks Game 2 in the hub.
///
/// Run via: BrainCitizen > Build FlagQuiz Game
/// </summary>
public static class FlagQuizSceneBuilder
{
    const string ScenePath = "Assets/Scenes/FlagQuiz.unity";
    const string FlagsDir = "Assets/Data/Flags";
    const string TexturesDir = FlagsDir + "/Textures";
    const string DatabasePath = FlagsDir + "/FlagDatabase.asset";
    const string HubGameInfoPath = "Assets/Data/Hub/Game02_FlagQuiz.asset";

    static readonly Color NavyBg     = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color White      = Color.white;
    static readonly Color GreenReal  = new Color(0.153f, 0.682f, 0.376f);
    static readonly Color RedFake    = new Color(0.906f, 0.298f, 0.235f);
    static readonly Color TimerFull  = new Color(0.180f, 0.800f, 0.443f);
    static readonly Color TimerLow   = new Color(0.906f, 0.298f, 0.235f);
    static readonly Color TextPrimary = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color PanelDim   = new Color(0f, 0f, 0f, 0.65f);
    static readonly Color EasyTag    = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color MediumTag  = new Color(0.95f, 0.70f, 0.10f);
    static readonly Color HardTag    = new Color(0.91f, 0.30f, 0.24f);

    struct FlagDef
    {
        public string Country;
        public FlagDifficulty Difficulty;
        public FlagDef(string c, FlagDifficulty d) { Country = c; Difficulty = d; }
    }

    static readonly FlagDef[] FLAGS = new[]
    {
        // EASY (10)
        new FlagDef("Japan",        FlagDifficulty.Easy),
        new FlagDef("France",       FlagDifficulty.Easy),
        new FlagDef("Germany",      FlagDifficulty.Easy),
        new FlagDef("Italy",        FlagDifficulty.Easy),
        new FlagDef("Russia",       FlagDifficulty.Easy),
        new FlagDef("Sweden",       FlagDifficulty.Easy),
        new FlagDef("Switzerland",  FlagDifficulty.Easy),
        new FlagDef("Indonesia",    FlagDifficulty.Easy),
        new FlagDef("Poland",       FlagDifficulty.Easy),
        new FlagDef("Netherlands",  FlagDifficulty.Easy),
        // MEDIUM (10)
        new FlagDef("Belgium",      FlagDifficulty.Medium),
        new FlagDef("Ireland",      FlagDifficulty.Medium),
        new FlagDef("Hungary",      FlagDifficulty.Medium),
        new FlagDef("Bulgaria",     FlagDifficulty.Medium),
        new FlagDef("Austria",      FlagDifficulty.Medium),
        new FlagDef("Denmark",      FlagDifficulty.Medium),
        new FlagDef("Finland",      FlagDifficulty.Medium),
        new FlagDef("Ukraine",      FlagDifficulty.Medium),
        new FlagDef("Bangladesh",   FlagDifficulty.Medium),
        new FlagDef("Lithuania",    FlagDifficulty.Medium),
        // HARD (10)
        new FlagDef("Estonia",      FlagDifficulty.Hard),
        new FlagDef("Latvia",       FlagDifficulty.Hard),
        new FlagDef("Luxembourg",   FlagDifficulty.Hard),
        new FlagDef("Romania",      FlagDifficulty.Hard),
        new FlagDef("Chad",         FlagDifficulty.Hard),
        new FlagDef("Mali",         FlagDifficulty.Hard),
        new FlagDef("Guinea",       FlagDifficulty.Hard),
        new FlagDef("Bolivia",      FlagDifficulty.Hard),
        new FlagDef("Yemen",        FlagDifficulty.Hard),
        new FlagDef("Iran",         FlagDifficulty.Hard),
    };

    [MenuItem("BrainCitizen/Build FlagQuiz Game")]
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

        var db = BuildFlagAssets();
        if (db == null) return;
        BuildScene(db);
        UnlockInHub();
        AddToBuildSettings();

        EditorUtility.DisplayDialog(
            "Flag Quiz built",
            $"Scene saved to {ScenePath}\n" +
            $"{db.flags.Length} flags loaded from {TexturesDir}.\n\n" +
            "Press Play.",
            "OK");
    }

    // =======================================================
    // Flag asset import (no rendering — PNGs come from flagcdn.com)
    // =======================================================

    static FlagDatabase BuildFlagAssets()
    {
        // Verify all PNGs exist
        var missing = new List<string>();
        foreach (var def in FLAGS)
        {
            string pngPath = $"{TexturesDir}/{def.Country}.png";
            if (!File.Exists(pngPath)) missing.Add(pngPath);
        }
        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Missing flag PNGs",
                "The following PNG files are missing from " + TexturesDir + ":\n\n" +
                string.Join("\n", missing) +
                "\n\nThe builder cannot continue without them.",
                "OK");
            return null;
        }

        // Pass 1: configure each PNG as a Sprite
        foreach (var def in FLAGS)
        {
            string pngPath = $"{TexturesDir}/{def.Country}.png";
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            }
            if (importer == null)
            {
                Debug.LogWarning($"No TextureImporter at {pngPath} - is this really a texture?");
                continue;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Pass 2: create FlagData assets pointing at the sprites.
        // We delete any existing asset first so CreateAsset always writes fresh state,
        // and use SerializedObject to ensure field assignments serialize reliably.
        var infos = new List<FlagData>();
        int nullSpriteCount = 0;
        foreach (var def in FLAGS)
        {
            string pngPath  = $"{TexturesDir}/{def.Country}.png";
            string assetPath = $"{FlagsDir}/{def.Country}.asset";

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (sprite == null)
            {
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(pngPath))
                    if (sub is Sprite s) { sprite = s; break; }
            }
            if (sprite == null)
            {
                nullSpriteCount++;
                Debug.LogWarning($"Sprite at {pngPath} returned null - flag will render blank.");
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var flag = ScriptableObject.CreateInstance<FlagData>();
            flag.countryName = def.Country;
            flag.flagSprite = sprite;
            flag.difficulty = def.Difficulty;
            AssetDatabase.CreateAsset(flag, assetPath);

            // Belt-and-braces: re-write via SerializedObject in case CreateAsset
            // serialized before in-memory fields were captured.
            var so = new SerializedObject(flag);
            so.FindProperty("countryName").stringValue = def.Country;
            so.FindProperty("flagSprite").objectReferenceValue = sprite;
            so.FindProperty("difficulty").enumValueIndex = (int)def.Difficulty;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flag);

            infos.Add(flag);
        }
        if (nullSpriteCount > 0)
            Debug.LogError($"FlagQuizSceneBuilder: {nullSpriteCount} of {FLAGS.Length} sprites failed to load.");

        var db = AssetDatabase.LoadAssetAtPath<FlagDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<FlagDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        db.flags = infos.ToArray();
        EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return db;
    }

    // =======================================================
    // Scene
    // =======================================================

    static void BuildScene(FlagDatabase db)
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

        // HUD - score (top-left, anchored to left edge)
        var scoreText = MakeText("ScoreText", canvasGO.transform, "Score: 0",
            40, FontStyles.Bold, White);
        var scoreRT = scoreText.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0, 1);
        scoreRT.anchorMax = new Vector2(0, 1);
        scoreRT.pivot = new Vector2(0, 0.5f);
        scoreRT.anchoredPosition = new Vector2(60, -55);
        scoreRT.sizeDelta = new Vector2(420, 60);
        scoreText.alignment = TextAlignmentOptions.Left;

        var streakHUDText = MakeText("StreakHUDText", canvasGO.transform, "",
            36, FontStyles.Bold, GreenReal);
        var streakRT = streakHUDText.GetComponent<RectTransform>();
        streakRT.anchorMin = new Vector2(1, 1);
        streakRT.anchorMax = new Vector2(1, 1);
        streakRT.pivot = new Vector2(1, 0.5f);
        streakRT.anchoredPosition = new Vector2(-60, -55);
        streakRT.sizeDelta = new Vector2(400, 60);
        streakHUDText.alignment = TextAlignmentOptions.Right;

        // Question header
        var questionCounterText = MakeText("QuestionCounterText", canvasGO.transform,
            "Question 1 / 10", 36, FontStyles.Bold, White);
        var qcRT = questionCounterText.GetComponent<RectTransform>();
        qcRT.anchorMin = new Vector2(0.5f, 1);
        qcRT.anchorMax = new Vector2(0.5f, 1);
        qcRT.pivot = new Vector2(1, 0.5f);
        qcRT.anchoredPosition = new Vector2(-30, -130);
        qcRT.sizeDelta = new Vector2(500, 50);
        questionCounterText.alignment = TextAlignmentOptions.Right;

        var difficultyText = MakeText("DifficultyText", canvasGO.transform, "EASY",
            36, FontStyles.Bold, EasyTag);
        var diffRT = difficultyText.GetComponent<RectTransform>();
        diffRT.anchorMin = new Vector2(0.5f, 1);
        diffRT.anchorMax = new Vector2(0.5f, 1);
        diffRT.pivot = new Vector2(0, 0.5f);
        diffRT.anchoredPosition = new Vector2(30, -130);
        diffRT.sizeDelta = new Vector2(300, 50);
        difficultyText.alignment = TextAlignmentOptions.Left;

        // Flag panel
        var flagPanel = MakeImage("FlagPanel", canvasGO.transform, White);
        SetAnchor(flagPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        flagPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -420);
        flagPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(720, 480);

        var flagImage = MakeImage("FlagImage", flagPanel.transform, White);
        var fiRT = flagImage.GetComponent<RectTransform>();
        fiRT.anchorMin = Vector2.zero;
        fiRT.anchorMax = Vector2.one;
        fiRT.offsetMin = new Vector2(8, 8);
        fiRT.offsetMax = new Vector2(-8, -8);
        flagImage.preserveAspect = true;

        // Timer slider
        var timerSlider = MakeSlider("TimerSlider", canvasGO.transform, TimerFull);
        var timerRT = timerSlider.GetComponent<RectTransform>();
        SetAnchor(timerRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        timerRT.anchoredPosition = new Vector2(0, -690);
        timerRT.sizeDelta = new Vector2(720, 24);
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
        agRT.sizeDelta = new Vector2(1240, 240);
        var glg = answerGridGO.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(610, 110);
        glg.spacing = new Vector2(20, 20);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.childAlignment = TextAnchor.MiddleCenter;

        var answerButtons = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            answerButtons[i] = MakeButton($"AnswerButton{i + 1}", answerGridGO.transform,
                "Country", new Color(1f, 1f, 1f, 0.92f), TextPrimary, 36);
        }

        // Feedback panel
        var feedbackPanel = MakeImage("FeedbackPanel", canvasGO.transform, PanelDim);
        Stretch(feedbackPanel);
        var feedbackResult = MakeText("FeedbackResultText", feedbackPanel.transform,
            "CORRECT!", 96, FontStyles.Bold, GreenReal);
        SetAnchor(feedbackResult, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        feedbackResult.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 140);
        feedbackResult.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 130);

        var correctAnswer = MakeText("CorrectAnswerText", feedbackPanel.transform, "",
            48, FontStyles.Normal, White);
        SetAnchor(correctAnswer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        correctAnswer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
        correctAnswer.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 80);

        var bonusText = MakeText("BonusText", feedbackPanel.transform, "",
            36, FontStyles.Bold, GreenReal);
        SetAnchor(bonusText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        bonusText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -60);
        bonusText.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 60);

        feedbackPanel.gameObject.SetActive(false);

        // End screen panel
        var endPanel = MakeImage("EndScreenPanel", canvasGO.transform, NavyBg);
        Stretch(endPanel);
        var endTitle = MakeText("EndTitle", endPanel.transform, "ROUND COMPLETE",
            72, FontStyles.Bold, White);
        SetAnchor(endTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        endTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
        endTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 100);

        var finalScore = MakeText("FinalScoreText", endPanel.transform, "0",
            144, FontStyles.Bold, GreenReal);
        SetAnchor(finalScore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        finalScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);
        finalScore.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 200);

        var endMessage = MakeText("EndMessageText", endPanel.transform, "",
            40, FontStyles.Normal, White);
        SetAnchor(endMessage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        endMessage.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -80);
        endMessage.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 100);

        var restartBtn = MakeButton("RestartButton", endPanel.transform, "PLAY AGAIN",
            GreenReal, White, 44);
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
        var gameManager = gameManagerGO.AddComponent<FlagQuizGameManager>();

        // Re-load the database from disk - the `db` we were passed is an
        // in-memory instance that AssetDatabase.Refresh may have superseded.
        // Assigning the stale reference produces fileID:0 in the saved scene.
        var dbLive = AssetDatabase.LoadAssetAtPath<FlagDatabase>(DatabasePath);
        if (dbLive == null)
        {
            Debug.LogError($"Failed to reload {DatabasePath} - GameManager will have null database.");
            dbLive = db;
        }
        gameManager.flagDatabase = dbLive;
        var so = new SerializedObject(gameManager);
        so.FindProperty("flagDatabase").objectReferenceValue = dbLive;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);

        var uiManagerGO = new GameObject("UIManager");
        var uiManager = uiManagerGO.AddComponent<FlagQuizUIManager>();
        uiManager.flagImage = flagImage;
        uiManager.questionCounterText = questionCounterText;
        uiManager.difficultyText = difficultyText;
        uiManager.timerSlider = timerSlider;
        uiManager.timerFill = timerFill;
        uiManager.timerFullColor = TimerFull;
        uiManager.timerLowColor = TimerLow;
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
        uiManager.restartButton = restartBtn;
        uiManager.hubButton = hubBtn;
        uiManager.correctColor = GreenReal;
        uiManager.wrongColor = RedFake;
        uiManager.easyColor = EasyTag;
        uiManager.mediumColor = MediumTag;
        uiManager.hardColor = HardTag;
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

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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
