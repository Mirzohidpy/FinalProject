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
/// One-click builder for Memory Match.
/// Reuses 12 flag PNGs from Assets/Data/Flags/Textures/ as the card faces.
/// Creates MemoryCardData + MemoryCardDatabase assets, builds the scene
/// with one card template, populates the GameManager's levels array, and
/// unlocks Game 5 in the hub.
///
/// Run via: BrainCitizen > Build MemoryMatch Game
/// </summary>
public static class MemoryMatchSceneBuilder
{
    const string ScenePath = "Assets/Scenes/MemoryMatch.unity";
    const string DataDir = "Assets/Data/Memory";
    const string DatabasePath = DataDir + "/MemoryCardDatabase.asset";
    const string FlagsTexturesDir = "Assets/Data/Flags/Textures";
    const string HubGameInfoPath = "Assets/Data/Hub/Game05_MemoryMatch.asset";

    static readonly Color NavyBg        = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color White         = Color.white;
    static readonly Color GreenAccent   = new Color(0.180f, 0.800f, 0.443f);
    static readonly Color GreenSolid    = new Color(0.153f, 0.682f, 0.376f);
    static readonly Color RedAccent     = new Color(0.906f, 0.298f, 0.235f);
    static readonly Color CardFaceUp    = Color.white;
    static readonly Color ToastBg       = new Color(0.118f, 0.153f, 0.380f, 0.92f);
    static readonly Color BoardBg       = new Color(0f, 0f, 0f, 0.18f);

    // 12 flags reused as card faces. Source: Assets/Data/Flags/Textures/{name}.png
    static readonly string[] CARD_FLAGS = new[]
    {
        "Japan", "France", "Germany", "Italy", "Russia", "Sweden",
        "Switzerland", "Indonesia", "Poland", "Netherlands", "Belgium", "Ireland",
    };

    struct LevelDef
    {
        public string Name;
        public int Cols;
        public int Rows;
        public float Seconds;
    }

    static readonly LevelDef[] LEVELS = new[]
    {
        new LevelDef { Name = "Warm Up",   Cols = 4, Rows = 4, Seconds = 90f  },
        new LevelDef { Name = "Steady",    Cols = 5, Rows = 4, Seconds = 90f  },
        new LevelDef { Name = "Challenge", Cols = 6, Rows = 4, Seconds = 100f },
    };

    [MenuItem("BrainCitizen/Build MemoryMatch Game")]
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

        var db = BuildCardAssets();
        if (db == null) return;
        BuildScene(db);
        UnlockInHub();
        AddToBuildSettings();

        EditorUtility.DisplayDialog(
            "Memory Match built",
            $"Scene saved to {ScenePath}\n" +
            $"{db.cards.Length} cards loaded from {FlagsTexturesDir}.\n\n" +
            "Press Play.",
            "OK");
    }

    // =======================================================
    // Card data assets (sourced from flag PNGs)
    // =======================================================

    static MemoryCardDatabase BuildCardAssets()
    {
        if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);

        var missing = new List<string>();
        foreach (var name in CARD_FLAGS)
        {
            string pngPath = $"{FlagsTexturesDir}/{name}.png";
            if (!File.Exists(pngPath)) missing.Add(pngPath);
        }
        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Missing flag PNGs",
                "The following PNG files are missing from " + FlagsTexturesDir + ":\n\n" +
                string.Join("\n", missing) +
                "\n\nThe builder cannot continue without them.",
                "OK");
            return null;
        }

        // Pass 1: ensure each PNG is configured as a Sprite
        foreach (var name in CARD_FLAGS)
        {
            string pngPath = $"{FlagsTexturesDir}/{name}.png";
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            }
            if (importer == null) continue;
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Pass 2: create MemoryCardData assets pointing at the sprites
        var infos = new List<MemoryCardData>();
        int nullSpriteCount = 0;
        foreach (var name in CARD_FLAGS)
        {
            string pngPath  = $"{FlagsTexturesDir}/{name}.png";
            string assetPath = $"{DataDir}/{name}.asset";

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (sprite == null)
            {
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(pngPath))
                    if (sub is Sprite s) { sprite = s; break; }
            }
            if (sprite == null)
            {
                nullSpriteCount++;
                Debug.LogWarning($"Sprite at {pngPath} returned null - card will render blank.");
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var card = ScriptableObject.CreateInstance<MemoryCardData>();
            card.cardName = name;
            card.cardSprite = sprite;
            AssetDatabase.CreateAsset(card, assetPath);

            var so = new SerializedObject(card);
            so.FindProperty("cardName").stringValue = name;
            so.FindProperty("cardSprite").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(card);

            infos.Add(card);
        }
        if (nullSpriteCount > 0)
            Debug.LogError($"MemoryMatchSceneBuilder: {nullSpriteCount} of {CARD_FLAGS.Length} sprites failed to load.");

        var db = AssetDatabase.LoadAssetAtPath<MemoryCardDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<MemoryCardDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        db.cards = infos.ToArray();
        EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return db;
    }

    // =======================================================
    // Scene
    // =======================================================

    static void BuildScene(MemoryCardDatabase db)
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

        // Combo (top-right)
        var comboText = MakeText("ComboText", canvasGO.transform, "",
            36, FontStyles.Bold, GreenAccent);
        var comboRT = comboText.GetComponent<RectTransform>();
        comboRT.anchorMin = new Vector2(1, 1);
        comboRT.anchorMax = new Vector2(1, 1);
        comboRT.pivot = new Vector2(1, 0.5f);
        comboRT.anchoredPosition = new Vector2(-60, -55);
        comboRT.sizeDelta = new Vector2(400, 60);
        comboText.alignment = TextAlignmentOptions.Right;

        // Match count (top-center, left)
        var matchCountText = MakeText("MatchCountText", canvasGO.transform, "0 / 8",
            36, FontStyles.Bold, White);
        var mcRT = matchCountText.GetComponent<RectTransform>();
        mcRT.anchorMin = new Vector2(0.5f, 1);
        mcRT.anchorMax = new Vector2(0.5f, 1);
        mcRT.pivot = new Vector2(1, 0.5f);
        mcRT.anchoredPosition = new Vector2(-30, -55);
        mcRT.sizeDelta = new Vector2(360, 60);
        matchCountText.alignment = TextAlignmentOptions.Right;

        // Level name (top-center, right)
        var levelText = MakeText("LevelText", canvasGO.transform, "LEVEL 1: WARM UP",
            36, FontStyles.Bold, GreenAccent);
        var lvRT = levelText.GetComponent<RectTransform>();
        lvRT.anchorMin = new Vector2(0.5f, 1);
        lvRT.anchorMax = new Vector2(0.5f, 1);
        lvRT.pivot = new Vector2(0, 0.5f);
        lvRT.anchoredPosition = new Vector2(30, -55);
        lvRT.sizeDelta = new Vector2(560, 60);
        levelText.alignment = TextAlignmentOptions.Left;

        // Timer slider
        var timerSlider = MakeSlider("TimerSlider", canvasGO.transform, GreenAccent);
        var timerRT = timerSlider.GetComponent<RectTransform>();
        SetAnchor(timerRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        timerRT.anchoredPosition = new Vector2(0, -120);
        timerRT.sizeDelta = new Vector2(1400, 18);
        var timerFill = timerSlider.fillRect.GetComponent<Image>();

        // Cards parent (centered, holds GridLayoutGroup)
        var cardsParentGO = new GameObject("CardsParent",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(GridLayoutGroup));
        cardsParentGO.transform.SetParent(canvasGO.transform, false);
        cardsParentGO.GetComponent<Image>().color = BoardBg;
        var cpRT = cardsParentGO.GetComponent<RectTransform>();
        cpRT.anchorMin = new Vector2(0.5f, 0.5f);
        cpRT.anchorMax = new Vector2(0.5f, 0.5f);
        cpRT.pivot = new Vector2(0.5f, 0.5f);
        cpRT.anchoredPosition = new Vector2(0, -50);
        cpRT.sizeDelta = new Vector2(1240, 840);

        var glg = cardsParentGO.GetComponent<GridLayoutGroup>();
        glg.padding = new RectOffset(20, 20, 20, 20);
        glg.spacing = new Vector2(14, 14);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;
        glg.cellSize = new Vector2(285, 195);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.MiddleCenter;

        // Card template (single inactive child, cloned at runtime)
        var cardTemplate = BuildCardTemplate(cardsParentGO.transform);
        cardTemplate.SetActive(false);

        // Toast
        var toastPanel = MakeImage("ToastPanel", canvasGO.transform, ToastBg);
        var toastRT = toastPanel.GetComponent<RectTransform>();
        toastRT.anchorMin = new Vector2(0.5f, 0f);
        toastRT.anchorMax = new Vector2(0.5f, 0f);
        toastRT.pivot = new Vector2(0.5f, 0f);
        toastRT.anchoredPosition = new Vector2(0, 30);
        toastRT.sizeDelta = new Vector2(820, 80);
        var toastText = MakeText("ToastText", toastPanel.transform, "",
            38, FontStyles.Bold, White);
        var ttRT = toastText.GetComponent<RectTransform>();
        ttRT.anchorMin = Vector2.zero;
        ttRT.anchorMax = Vector2.one;
        ttRT.offsetMin = Vector2.zero;
        ttRT.offsetMax = Vector2.zero;
        toastPanel.gameObject.SetActive(false);

        // End screen
        var endPanel = MakeImage("EndScreenPanel", canvasGO.transform, NavyBg);
        Stretch(endPanel);

        var endTitle = MakeText("EndTitle", endPanel.transform, "ALL LEVELS COMPLETE",
            72, FontStyles.Bold, White);
        SetAnchor(endTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        endTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
        endTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1600, 100);

        var finalScore = MakeText("FinalScoreText", endPanel.transform, "0",
            144, FontStyles.Bold, GreenAccent);
        SetAnchor(finalScore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        finalScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 130);
        finalScore.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 200);

        var bestComboText = MakeText("BestComboText", endPanel.transform, "Best Combo: x0",
            44, FontStyles.Bold, GreenAccent);
        SetAnchor(bestComboText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        bestComboText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
        bestComboText.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 70);

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
        var gameManager = gameManagerGO.AddComponent<MemoryMatchGameManager>();

        var dbLive = AssetDatabase.LoadAssetAtPath<MemoryCardDatabase>(DatabasePath);
        if (dbLive == null)
        {
            Debug.LogError($"Failed to reload {DatabasePath}; using stale reference.");
            dbLive = db;
        }
        gameManager.cardDatabase = dbLive;

        // Populate levels array via SerializedObject (reliable struct-array writeback)
        var so = new SerializedObject(gameManager);
        so.FindProperty("cardDatabase").objectReferenceValue = dbLive;
        var levelsProp = so.FindProperty("levels");
        levelsProp.arraySize = LEVELS.Length;
        for (int i = 0; i < LEVELS.Length; i++)
        {
            var elem = levelsProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("levelName").stringValue   = LEVELS[i].Name;
            elem.FindPropertyRelative("columns").intValue        = LEVELS[i].Cols;
            elem.FindPropertyRelative("rows").intValue           = LEVELS[i].Rows;
            elem.FindPropertyRelative("roundSeconds").floatValue = LEVELS[i].Seconds;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);

        var uiManagerGO = new GameObject("UIManager");
        var uiManager = uiManagerGO.AddComponent<MemoryMatchUIManager>();
        uiManager.cardsLayout = glg;
        uiManager.cardTemplate = cardTemplate;
        uiManager.boardSize = new Vector2(1200, 800);
        uiManager.cellSpacing = new Vector2(14, 14);
        uiManager.scoreText = scoreText;
        uiManager.comboText = comboText;
        uiManager.levelText = levelText;
        uiManager.matchCountText = matchCountText;
        uiManager.timerSlider = timerSlider;
        uiManager.timerFill = timerFill;
        uiManager.timerFullColor = GreenAccent;
        uiManager.timerLowColor = RedAccent;
        uiManager.toastPanel = toastPanel.gameObject;
        uiManager.toastText = toastText;
        uiManager.endScreenPanel = endPanel.gameObject;
        uiManager.endTitleText = endTitle;
        uiManager.finalScoreText = finalScore;
        uiManager.endMessageText = endMessage;
        uiManager.bestComboText = bestComboText;
        uiManager.restartButton = restartBtn;
        uiManager.hubButton = hubBtn;
        uiManager.cardFaceDownColor = NavyBg;
        uiManager.cardFaceUpColor = CardFaceUp;
        uiManager.cardMatchedColor = GreenAccent;
        EditorUtility.SetDirty(uiManager);

        UnityEventTools.AddPersistentListener(restartBtn.onClick, gameManager.RestartGame);
        UnityEventTools.AddPersistentListener(hubBtn.onClick, gameManager.ReturnToHub);

        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static GameObject BuildCardTemplate(Transform parent)
    {
        var card = new GameObject("CardTemplate",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button), typeof(MemoryCard));
        card.transform.SetParent(parent, false);

        var bg = card.GetComponent<Image>();
        bg.color = NavyBg;

        var face = MakeImage("FaceImage", card.transform, Color.white);
        var fRT = face.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero;
        fRT.anchorMax = Vector2.one;
        fRT.offsetMin = new Vector2(8, 8);
        fRT.offsetMax = new Vector2(-8, -8);
        face.preserveAspect = true;
        face.raycastTarget = false;

        var back = MakeText("BackLabel", card.transform, "?",
            96, FontStyles.Bold, White);
        var bRT = back.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero;
        bRT.anchorMax = Vector2.one;
        bRT.offsetMin = Vector2.zero;
        bRT.offsetMax = Vector2.zero;
        back.alignment = TextAlignmentOptions.Center;
        back.raycastTarget = false;

        var memCard = card.GetComponent<MemoryCard>();
        memCard.background = bg;
        memCard.faceImage = face;
        memCard.backLabel = back;
        memCard.button = card.GetComponent<Button>();
        memCard.backgroundFaceDownColor = NavyBg;
        memCard.backgroundFaceUpColor = CardFaceUp;
        memCard.backgroundMatchedColor = GreenAccent;
        EditorUtility.SetDirty(memCard);

        return card;
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
