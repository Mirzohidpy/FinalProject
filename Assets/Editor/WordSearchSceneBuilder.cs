#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-click builder for Word Search.
/// Creates WordData + WordDatabase assets, builds the scene, and unlocks
/// Game 3 in the hub.
///
/// Run via: BrainCitizen > Build WordSearch Game
/// </summary>
public static class WordSearchSceneBuilder
{
    const string ScenePath = "Assets/Scenes/WordSearch.unity";
    const string DataDir = "Assets/Data/Words";
    const string DatabasePath = DataDir + "/WordDatabase.asset";
    const string HubGameInfoPath = "Assets/Data/Hub/Game03_WordSearch.asset";

    static readonly Color NavyBg        = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color NavyBgDark    = new Color(0.063f, 0.082f, 0.220f);
    static readonly Color White         = Color.white;
    static readonly Color WhiteSoft     = new Color(1f, 1f, 1f, 0.85f);
    static readonly Color GreenAccent   = new Color(0.180f, 0.800f, 0.443f);
    static readonly Color GreenSolid    = new Color(0.153f, 0.682f, 0.376f);
    static readonly Color RedAccent     = new Color(0.906f, 0.298f, 0.235f);
    static readonly Color TextPrimary   = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color CellBg        = Color.white;
    static readonly Color CellSelectedBg = new Color(0.180f, 0.800f, 0.443f);
    static readonly Color CellFoundBg   = new Color(0.180f, 0.800f, 0.443f, 0.85f);
    static readonly Color PanelBg       = new Color(0f, 0f, 0f, 0.18f);
    static readonly Color ToastBg       = new Color(0.118f, 0.153f, 0.380f, 0.92f);

    struct WordDef
    {
        public string Word;
        public string Definition;
        public WordDef(string w, string d) { Word = w; Definition = d; }
    }

    static readonly WordDef[] WORDS = new[]
    {
        new WordDef("DEMOCRACY", "Government by the people, often through elected representatives."),
        new WordDef("JUSTICE",   "Fair and equal treatment under the law."),
        new WordDef("VOTING",    "The act of choosing leaders or policies in an election."),
        new WordDef("RIGHTS",    "Freedoms and protections every person is entitled to."),
        new WordDef("FREEDOM",   "The power to act, speak, or think without unjust restraint."),
        new WordDef("LIBERTY",   "The state of being free within society."),
        new WordDef("EQUALITY",  "Treating all people the same in status, rights, and opportunity."),
        new WordDef("CITIZEN",   "A legally recognized member of a country."),
        new WordDef("COURT",     "A place where legal cases are heard and decided."),
        new WordDef("ELECTION",  "An organized process for choosing public officials by vote."),
        new WordDef("PEACE",     "Freedom from disturbance, war, or violence."),
        new WordDef("DUTY",      "A responsibility a citizen owes to country and community."),
        new WordDef("HONOR",     "Acting with integrity, respect, and ethical character."),
        new WordDef("TRUTH",     "What is real and accurate; the foundation of informed citizenship."),
        new WordDef("VOICE",     "The expression of opinion in civic life."),
        new WordDef("POLICY",    "A course of action set by a government or organization."),
    };

    [MenuItem("BrainCitizen/Build WordSearch Game")]
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

        var db = BuildWordAssets();
        if (db == null) return;
        BuildScene(db);
        UnlockInHub();
        AddToBuildSettings();

        EditorUtility.DisplayDialog(
            "Word Search built",
            $"Scene saved to {ScenePath}\n" +
            $"{db.words.Length} civic words loaded.\n\n" +
            "Press Play.",
            "OK");
    }

    // =======================================================
    // Word data assets
    // =======================================================

    static WordDatabase BuildWordAssets()
    {
        if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);

        var infos = new List<WordData>();
        foreach (var def in WORDS)
        {
            string assetPath = $"{DataDir}/{def.Word}.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var w = ScriptableObject.CreateInstance<WordData>();
            w.word = def.Word;
            w.definition = def.Definition;
            AssetDatabase.CreateAsset(w, assetPath);

            var so = new SerializedObject(w);
            so.FindProperty("word").stringValue = def.Word;
            so.FindProperty("definition").stringValue = def.Definition;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(w);

            infos.Add(w);
        }

        var db = AssetDatabase.LoadAssetAtPath<WordDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<WordDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        db.words = infos.ToArray();
        EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return db;
    }

    // =======================================================
    // Scene
    // =======================================================

    static void BuildScene(WordDatabase db)
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
        var bgVignette = MakeImage("BackgroundVignette", canvasGO.transform,
            new Color(NavyBgDark.r, NavyBgDark.g, NavyBgDark.b, 0.4f));
        Stretch(bgVignette);
        var bgvRT = bgVignette.GetComponent<RectTransform>();
        bgvRT.anchorMin = new Vector2(0, 0);
        bgvRT.anchorMax = new Vector2(1, 0.5f);

        // Title
        var title = MakeText("Title", canvasGO.transform, "WORD SEARCH",
            64, FontStyles.Bold, White);
        SetAnchor(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -55);
        title.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 70);

        // Score (top-left)
        var scoreText = MakeText("ScoreText", canvasGO.transform, "Score: 0",
            36, FontStyles.Bold, White);
        var scoreRT = scoreText.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0, 1);
        scoreRT.anchorMax = new Vector2(0, 1);
        scoreRT.pivot = new Vector2(0, 0.5f);
        scoreRT.anchoredPosition = new Vector2(60, -55);
        scoreRT.sizeDelta = new Vector2(360, 50);
        scoreText.alignment = TextAlignmentOptions.Left;

        // Found count (top-right)
        var foundCountText = MakeText("FoundCountText", canvasGO.transform, "0 / 7",
            36, FontStyles.Bold, GreenAccent);
        var fcRT = foundCountText.GetComponent<RectTransform>();
        fcRT.anchorMin = new Vector2(1, 1);
        fcRT.anchorMax = new Vector2(1, 1);
        fcRT.pivot = new Vector2(1, 0.5f);
        fcRT.anchoredPosition = new Vector2(-60, -55);
        fcRT.sizeDelta = new Vector2(280, 50);
        foundCountText.alignment = TextAlignmentOptions.Right;

        // Timer slider
        var timerSlider = MakeSlider("TimerSlider", canvasGO.transform, GreenAccent);
        var timerRT = timerSlider.GetComponent<RectTransform>();
        SetAnchor(timerRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        timerRT.anchoredPosition = new Vector2(0, -130);
        timerRT.sizeDelta = new Vector2(1400, 20);
        var timerFill = timerSlider.fillRect.GetComponent<Image>();

        // ---- Grid ----
        var gridGO = new GameObject("WordSearchGrid",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(WordSearchGrid));
        gridGO.transform.SetParent(canvasGO.transform, false);
        gridGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.18f);
        var gridRT = gridGO.GetComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0.5f, 0.5f);
        gridRT.anchorMax = new Vector2(0.5f, 0.5f);
        gridRT.pivot = new Vector2(0.5f, 0.5f);
        gridRT.anchoredPosition = new Vector2(-360, -50);
        gridRT.sizeDelta = new Vector2(740, 740);

        // Cells parent (inside grid GO; GridLayoutGroup arranges children)
        var cellsParentGO = new GameObject("Cells",
            typeof(RectTransform), typeof(GridLayoutGroup));
        cellsParentGO.transform.SetParent(gridGO.transform, false);
        var cellsRT = cellsParentGO.GetComponent<RectTransform>();
        cellsRT.anchorMin = Vector2.zero;
        cellsRT.anchorMax = Vector2.one;
        cellsRT.offsetMin = new Vector2(10, 10);
        cellsRT.offsetMax = new Vector2(-10, -10);
        var glg = cellsParentGO.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(68, 68);
        glg.spacing = new Vector2(4, 4);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 10;

        // Cell template (one disabled child of cellsParent, cloned at runtime)
        var cellTemplate = BuildCellTemplate(cellsParentGO.transform);
        cellTemplate.SetActive(false);

        // ---- Word list panel (right) ----
        var listPanel = MakeImage("WordListPanel", canvasGO.transform,
            new Color(0, 0, 0, 0.22f));
        var listRT = listPanel.GetComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0.5f, 0.5f);
        listRT.anchorMax = new Vector2(0.5f, 0.5f);
        listRT.pivot = new Vector2(0.5f, 0.5f);
        listRT.anchoredPosition = new Vector2(560, -50);
        listRT.sizeDelta = new Vector2(440, 740);

        var listTitle = MakeText("WordListTitle", listPanel.transform, "FIND THESE WORDS",
            32, FontStyles.Bold, GreenAccent);
        var ltRT = listTitle.GetComponent<RectTransform>();
        ltRT.anchorMin = new Vector2(0, 1);
        ltRT.anchorMax = new Vector2(1, 1);
        ltRT.pivot = new Vector2(0.5f, 1f);
        ltRT.anchoredPosition = new Vector2(0, -24);
        ltRT.sizeDelta = new Vector2(0, 50);

        var wordListContent = new GameObject("WordListContent",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        wordListContent.transform.SetParent(listPanel.transform, false);
        var wlcRT = wordListContent.GetComponent<RectTransform>();
        wlcRT.anchorMin = new Vector2(0, 0);
        wlcRT.anchorMax = new Vector2(1, 1);
        wlcRT.offsetMin = new Vector2(30, 24);
        wlcRT.offsetMax = new Vector2(-30, -90);
        var wlcVlg = wordListContent.GetComponent<VerticalLayoutGroup>();
        wlcVlg.spacing = 14;
        wlcVlg.padding = new RectOffset(0, 0, 8, 8);
        wlcVlg.childAlignment = TextAnchor.UpperCenter;
        wlcVlg.childForceExpandWidth = true;
        wlcVlg.childForceExpandHeight = false;
        wlcVlg.childControlWidth = true;
        wlcVlg.childControlHeight = true;
        var wlcFit = wordListContent.GetComponent<ContentSizeFitter>();
        wlcFit.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Word entry template (single child, disabled, cloned at runtime by UIManager)
        var wordEntryTemplate = MakeText("WordEntryTemplate", wordListContent.transform,
            "WORD", 34, FontStyles.Bold, White);
        wordEntryTemplate.alignment = TextAlignmentOptions.Center;
        var weLE = wordEntryTemplate.gameObject.AddComponent<LayoutElement>();
        weLE.minHeight = 44; weLE.preferredHeight = 44;
        wordEntryTemplate.gameObject.SetActive(false);

        // ---- Toast ----
        var toastPanel = MakeImage("ToastPanel", canvasGO.transform, ToastBg);
        var toastRT = toastPanel.GetComponent<RectTransform>();
        toastRT.anchorMin = new Vector2(0.5f, 0f);
        toastRT.anchorMax = new Vector2(0.5f, 0f);
        toastRT.pivot = new Vector2(0.5f, 0f);
        toastRT.anchoredPosition = new Vector2(0, 50);
        toastRT.sizeDelta = new Vector2(700, 90);

        var toastText = MakeText("ToastText", toastPanel.transform, "FOUND: WORD",
            44, FontStyles.Bold, White);
        var tttRT = toastText.GetComponent<RectTransform>();
        tttRT.anchorMin = Vector2.zero;
        tttRT.anchorMax = Vector2.one;
        tttRT.offsetMin = Vector2.zero;
        tttRT.offsetMax = Vector2.zero;
        toastPanel.gameObject.SetActive(false);

        // ---- End screen ----
        var endPanel = MakeImage("EndScreenPanel", canvasGO.transform, NavyBg);
        Stretch(endPanel);

        var endTitle = MakeText("EndTitle", endPanel.transform, "ROUND COMPLETE",
            72, FontStyles.Bold, White);
        SetAnchor(endTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        endTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
        endTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 100);

        var finalScore = MakeText("FinalScoreText", endPanel.transform, "0",
            144, FontStyles.Bold, GreenAccent);
        SetAnchor(finalScore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        finalScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);
        finalScore.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 200);

        var endMessage = MakeText("EndMessageText", endPanel.transform, "",
            38, FontStyles.Normal, White);
        SetAnchor(endMessage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        endMessage.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        endMessage.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 160);
        endMessage.textWrappingMode = TextWrappingModes.Normal;

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

        // ---- Wire up grid component ----
        var gridComp = gridGO.GetComponent<WordSearchGrid>();
        gridComp.cellsParent = cellsRT;
        gridComp.cellTemplate = cellTemplate;
        gridComp.cellNormalBg = CellBg;
        gridComp.cellNormalText = TextPrimary;
        gridComp.cellSelectedBg = CellSelectedBg;
        gridComp.cellSelectedText = White;
        gridComp.cellFoundBg = CellFoundBg;
        gridComp.cellFoundText = White;
        EditorUtility.SetDirty(gridComp);

        // ---- Managers ----
        var gameManagerGO = new GameObject("GameManager");
        var gameManager = gameManagerGO.AddComponent<WordSearchGameManager>();

        var dbLive = AssetDatabase.LoadAssetAtPath<WordDatabase>(DatabasePath);
        if (dbLive == null)
        {
            Debug.LogError($"Failed to reload {DatabasePath}; using stale reference.");
            dbLive = db;
        }
        gameManager.wordDatabase = dbLive;
        var so = new SerializedObject(gameManager);
        so.FindProperty("wordDatabase").objectReferenceValue = dbLive;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);

        var uiManagerGO = new GameObject("UIManager");
        var uiManager = uiManagerGO.AddComponent<WordSearchUIManager>();
        uiManager.grid = gridComp;
        uiManager.scoreText = scoreText;
        uiManager.foundCountText = foundCountText;
        uiManager.timerSlider = timerSlider;
        uiManager.timerFill = timerFill;
        uiManager.timerFullColor = GreenAccent;
        uiManager.timerLowColor = RedAccent;
        uiManager.wordListParent = wordListContent.transform;
        uiManager.wordEntryTemplate = wordEntryTemplate;
        uiManager.toastPanel = toastPanel.gameObject;
        uiManager.toastText = toastText;
        uiManager.endScreenPanel = endPanel.gameObject;
        uiManager.finalScoreText = finalScore;
        uiManager.endMessageText = endMessage;
        uiManager.restartButton = restartBtn;
        uiManager.hubButton = hubBtn;
        uiManager.foundWordColor = GreenAccent;
        uiManager.unfoundWordColor = White;
        EditorUtility.SetDirty(uiManager);

        UnityEventTools.AddPersistentListener(restartBtn.onClick, gameManager.RestartGame);
        UnityEventTools.AddPersistentListener(hubBtn.onClick, gameManager.ReturnToHub);

        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static GameObject BuildCellTemplate(Transform parent)
    {
        var cell = new GameObject("CellTemplate",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cell.transform.SetParent(parent, false);
        cell.GetComponent<Image>().color = CellBg;
        cell.GetComponent<Image>().raycastTarget = false;

        var letter = new GameObject("Letter", typeof(RectTransform), typeof(CanvasRenderer));
        letter.transform.SetParent(cell.transform, false);
        var tmp = letter.AddComponent<TextMeshProUGUI>();
        tmp.text = "A";
        tmp.fontSize = 38;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = TextPrimary;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var lRT = letter.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = Vector2.zero;
        lRT.offsetMax = Vector2.zero;

        return cell;
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
