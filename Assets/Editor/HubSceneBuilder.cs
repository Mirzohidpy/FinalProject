#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-click builder for the Hub scene + GameInfo/GameRegistry assets.
/// Run via menu: BrainCitizen > Build Hub Scene.
///
/// Produces a 2-column grid of cards grouped by category, with hover/click
/// feedback and a clear locked state for unimplemented games.
/// </summary>
public static class HubSceneBuilder
{
    const string ScenePath = "Assets/Scenes/HubScene.unity";
    const string DataDir = "Assets/Data/Hub";
    const string RegistryPath = DataDir + "/GameRegistry.asset";
    const string TrueFalseScenePath = "Assets/Scenes/TrueFalseNews.unity";

    static readonly Color NavyBg        = new Color(0.118f, 0.153f, 0.380f); // #1E2761
    static readonly Color NavyBgDark    = new Color(0.063f, 0.082f, 0.220f); // deeper for vignette
    static readonly Color White         = Color.white;
    static readonly Color WhiteSoft     = new Color(1f, 1f, 1f, 0.85f);
    static readonly Color CardBg        = new Color(1f, 1f, 1f, 0.96f);
    static readonly Color CardLockedBg  = new Color(0.85f, 0.85f, 0.9f, 0.45f);
    static readonly Color GreenReady    = new Color(0.153f, 0.682f, 0.376f);
    static readonly Color GreyLocked    = new Color(0.5f, 0.5f, 0.6f);
    static readonly Color TextPrimary   = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color TextLocked    = new Color(0.45f, 0.45f, 0.5f);
    static readonly Color SectionAccent = new Color(0.180f, 0.800f, 0.443f); // #2ECC71

    struct GameSpec
    {
        public int Number;
        public string DisplayName;
        public string SceneName;
        public string Tagline;
        public bool IsImplemented;
        public GameCategory Category;

        public GameSpec(int n, string d, string s, string t, bool i, GameCategory c)
        {
            Number = n; DisplayName = d; SceneName = s; Tagline = t;
            IsImplemented = i; Category = c;
        }
    }

    static readonly GameSpec[] GAMES = new[]
    {
        new GameSpec( 1, "True or False News",   "TrueFalseNews", "Can you spot the fake headline?",                    true,  GameCategory.CivicAwareness),
        new GameSpec( 2, "Flag Quiz",             "FlagQuiz",      "How well do you know the world's flags?",            true,  GameCategory.CivicAwareness),
        new GameSpec( 3, "Word Search",           "WordSearch",    "Find the hidden civic vocabulary.",                  false, GameCategory.CivicAwareness),
        new GameSpec( 4, "Math Sprint",           "MathSprint",    "Think fast, calculate faster.",                      false, GameCategory.MentalSkills),
        new GameSpec( 5, "Memory Match",          "MemoryMatch",   "Train your working memory.",                         false, GameCategory.MentalSkills),
        new GameSpec( 6, "Doppi Facts",           "DoppiFacts",    "Smash the civic facts - miss the myths.",            false, GameCategory.CivicAwareness),
        new GameSpec( 7, "Maze Runner",           "MazeRunner",    "Navigate to the truth - answer to open gates.",      false, GameCategory.MentalSkills),
        new GameSpec( 8, "Emotion Identifier",    "EmotionID",     "Understand how people feel.",                        false, GameCategory.MentalSkills),
        new GameSpec( 9, "Timeline Sort",         "TimelineSort",  "Put history in the right order.",                    false, GameCategory.CivicAwareness),
        new GameSpec(10, "Civic Quiz Showdown",   "QuizShowdown",  "The ultimate test - 15 questions, 3 lifelines.",     false, GameCategory.CivicAwareness),
    };

    [MenuItem("BrainCitizen/Build Hub Scene")]
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

        var registry = BuildRegistry();
        BuildScene(registry);
        AddToBuildSettings();
        SetPlayModeStartScene();

        EditorUtility.DisplayDialog(
            "Hub built",
            $"HubScene saved to {ScenePath}\n\n" +
            "Press Play - it will always start at the hub.",
            "OK");
    }

    // -------------------------------------------------------
    // Data assets
    // -------------------------------------------------------

    static GameRegistry BuildRegistry()
    {
        if (!Directory.Exists(DataDir))
            Directory.CreateDirectory(DataDir);

        var infos = new List<GameInfo>();
        foreach (var spec in GAMES)
        {
            string assetPath = $"{DataDir}/Game{spec.Number:D2}_{spec.SceneName}.asset";
            var info = AssetDatabase.LoadAssetAtPath<GameInfo>(assetPath);
            if (info == null)
            {
                info = ScriptableObject.CreateInstance<GameInfo>();
                AssetDatabase.CreateAsset(info, assetPath);
            }
            info.gameNumber    = spec.Number;
            info.displayName   = spec.DisplayName;
            info.sceneName     = spec.SceneName;
            info.tagline       = spec.Tagline;
            info.isImplemented = spec.IsImplemented;
            info.category      = spec.Category;
            EditorUtility.SetDirty(info);
            infos.Add(info);
        }

        var registry = AssetDatabase.LoadAssetAtPath<GameRegistry>(RegistryPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<GameRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
        }
        registry.games = infos.ToArray();
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return registry;
    }

    // -------------------------------------------------------
    // Scene
    // -------------------------------------------------------

    static void BuildScene(GameRegistry registry)
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

        // Background (top-down vignette using two stacked images)
        var bg = MakeImage("Background", canvasGO.transform, NavyBg);
        Stretch(bg);
        var bgVignette = MakeImage("BackgroundVignette", canvasGO.transform, NavyBgDark);
        Stretch(bgVignette);
        bgVignette.color = new Color(NavyBgDark.r, NavyBgDark.g, NavyBgDark.b, 0.4f);
        // Anchored to bottom for a subtle gradient feel
        var bgvRT = bgVignette.GetComponent<RectTransform>();
        bgvRT.anchorMin = new Vector2(0, 0);
        bgvRT.anchorMax = new Vector2(1, 0.5f);
        bgvRT.offsetMin = Vector2.zero;
        bgvRT.offsetMax = Vector2.zero;

        // Title
        var title = MakeText("Title", canvasGO.transform, "BRAIN CITIZEN ACADEMY",
            96, FontStyles.Bold, White);
        SetAnchor(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -90);
        title.GetComponent<RectTransform>().sizeDelta = new Vector2(1800, 110);
        title.alignment = TextAlignmentOptions.Center;

        // Subtitle
        var subtitle = MakeText("Subtitle", canvasGO.transform, "Choose a Mini-Game",
            44, FontStyles.Italic, WhiteSoft);
        SetAnchor(subtitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        subtitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -180);
        subtitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1800, 60);
        subtitle.alignment = TextAlignmentOptions.Center;

        // ScrollView root
        var scrollGO = new GameObject("ScrollView",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(canvasGO.transform, false);
        scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.18f);
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRT.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRT.pivot = new Vector2(0.5f, 0.5f);
        scrollRT.anchoredPosition = new Vector2(0, -100);
        scrollRT.sizeDelta = new Vector2(1620, 720);
        var scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        // Viewport
        var viewport = new GameObject("Viewport",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGO.transform, false);
        viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(10, 10);
        vpRT.offsetMax = new Vector2(-10, -10);
        scrollRect.viewport = vpRT;

        // Content (vertical layout: stacks sections)
        var content = new GameObject("Content",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var cRT = content.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = Vector2.zero;
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 32;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        var csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = cRT;

        // Section template (header + grid stacked vertically inside)
        var sectionTemplate = BuildSectionTemplate(content.transform);
        sectionTemplate.SetActive(false);

        // Card template
        var cardTemplate = BuildCardTemplate(content.transform);
        cardTemplate.SetActive(false);

        // HubManager
        var hubGO = new GameObject("HubManager");
        var hub = hubGO.AddComponent<HubManager>();
        hub.registry = registry;
        hub.sectionsContainer = content.transform;
        hub.sectionTemplate = sectionTemplate;
        hub.cardTemplate = cardTemplate;
        hub.readyAccent  = GreenReady;
        hub.lockedAccent = GreyLocked;
        hub.cardReadyBg  = CardBg;
        hub.cardLockedBg = CardLockedBg;
        hub.textPrimary  = TextPrimary;
        hub.textLocked   = TextLocked;

        if (!Directory.Exists("Assets/Scenes"))
            Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static GameObject BuildSectionTemplate(Transform parent)
    {
        var section = new GameObject("SectionTemplate",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        section.transform.SetParent(parent, false);
        var svlg = section.GetComponent<VerticalLayoutGroup>();
        svlg.spacing = 16;
        svlg.padding = new RectOffset(0, 0, 0, 8);
        svlg.childForceExpandWidth = true;
        svlg.childForceExpandHeight = false;
        svlg.childControlWidth = true;
        svlg.childControlHeight = true;
        var sfit = section.GetComponent<ContentSizeFitter>();
        sfit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Header row: accent bar + heading text
        var header = new GameObject("HeaderRow",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        header.transform.SetParent(section.transform, false);
        var hlg = header.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.padding = new RectOffset(4, 0, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        var hLE = header.GetComponent<LayoutElement>();
        hLE.minHeight = 60;
        hLE.preferredHeight = 60;

        // Accent bar
        var bar = MakeImage("AccentBar", header.transform, SectionAccent);
        var barRT = bar.GetComponent<RectTransform>();
        barRT.sizeDelta = new Vector2(8, 48);
        var barLE = bar.gameObject.AddComponent<LayoutElement>();
        barLE.minWidth = 8; barLE.preferredWidth = 8;
        barLE.minHeight = 48; barLE.preferredHeight = 48;

        // Heading text (must be named "HeaderText" — HubManager looks it up)
        var headerText = MakeText("HeaderText", header.transform, "SECTION",
            44, FontStyles.Bold, White);
        var htRT = headerText.GetComponent<RectTransform>();
        htRT.sizeDelta = new Vector2(1500, 60);
        var htLE = headerText.gameObject.AddComponent<LayoutElement>();
        htLE.minWidth = 1400; htLE.preferredWidth = 1400;
        htLE.minHeight = 60;  htLE.preferredHeight = 60;
        headerText.alignment = TextAlignmentOptions.Left;

        // Grid: 2-column GridLayoutGroup (must be named "Grid")
        var grid = new GameObject("Grid",
            typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        grid.transform.SetParent(section.transform, false);
        var glg = grid.GetComponent<GridLayoutGroup>();
        glg.padding = new RectOffset(0, 0, 0, 0);
        glg.spacing = new Vector2(20, 20);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperCenter;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.cellSize = new Vector2(770, 150);
        var gfit = grid.GetComponent<ContentSizeFitter>();
        gfit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        gfit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        return section;
    }

    static GameObject BuildCardTemplate(Transform parent)
    {
        var card = new GameObject("CardTemplate",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button), typeof(LayoutElement), typeof(HubCardEffect));
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = CardBg;
        var le = card.GetComponent<LayoutElement>();
        le.minHeight = 150;
        le.preferredHeight = 150;

        // Number badge (left)
        var numberText = MakeText("NumberText", card.transform, "01",
            88, FontStyles.Bold, GreenReady);
        var nRT = numberText.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0, 0);
        nRT.anchorMax = new Vector2(0, 1);
        nRT.pivot = new Vector2(0, 0.5f);
        nRT.anchoredPosition = new Vector2(30, 0);
        nRT.sizeDelta = new Vector2(140, 0);
        numberText.alignment = TextAlignmentOptions.Center;

        // Vertical separator line
        var sep = MakeImage("Separator", card.transform,
            new Color(TextPrimary.r, TextPrimary.g, TextPrimary.b, 0.15f));
        var sepRT = sep.GetComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0, 0.15f);
        sepRT.anchorMax = new Vector2(0, 0.85f);
        sepRT.pivot = new Vector2(0, 0.5f);
        sepRT.anchoredPosition = new Vector2(170, 0);
        sepRT.sizeDelta = new Vector2(2, 0);

        // Name (top)
        var nameText = MakeText("NameText", card.transform, "Game Name",
            38, FontStyles.Bold, TextPrimary);
        var nmRT = nameText.GetComponent<RectTransform>();
        nmRT.anchorMin = new Vector2(0, 0.5f);
        nmRT.anchorMax = new Vector2(1, 1);
        nmRT.offsetMin = new Vector2(190, 0);
        nmRT.offsetMax = new Vector2(-180, -16);
        nameText.alignment = TextAlignmentOptions.Left;

        // Tagline (below name)
        var taglineText = MakeText("TaglineText", card.transform, "Tagline",
            24, FontStyles.Italic, TextPrimary);
        var tRT = taglineText.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0, 0);
        tRT.anchorMax = new Vector2(1, 0.5f);
        tRT.offsetMin = new Vector2(190, 16);
        tRT.offsetMax = new Vector2(-180, 0);
        taglineText.alignment = TextAlignmentOptions.Left;

        // Status badge (right)
        var statusBg = MakeImage("StatusBadge", card.transform,
            new Color(GreenReady.r, GreenReady.g, GreenReady.b, 0.12f));
        var sbRT = statusBg.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0.5f);
        sbRT.anchorMax = new Vector2(1, 0.5f);
        sbRT.pivot = new Vector2(1, 0.5f);
        sbRT.anchoredPosition = new Vector2(-25, 0);
        sbRT.sizeDelta = new Vector2(170, 64);

        var statusText = MakeText("StatusText", statusBg.transform, "PLAY",
            28, FontStyles.Bold, GreenReady);
        var sRT = statusText.GetComponent<RectTransform>();
        sRT.anchorMin = Vector2.zero;
        sRT.anchorMax = Vector2.one;
        sRT.offsetMin = Vector2.zero;
        sRT.offsetMax = Vector2.zero;
        statusText.alignment = TextAlignmentOptions.Center;

        return card;
    }

    // -------------------------------------------------------
    // Build Settings
    // -------------------------------------------------------

    static void AddToBuildSettings()
    {
        var existing = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        existing.RemoveAll(s => s.path == ScenePath);
        existing.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        if (File.Exists(TrueFalseScenePath) && !existing.Exists(s => s.path == TrueFalseScenePath))
            existing.Add(new EditorBuildSettingsScene(TrueFalseScenePath, true));
        EditorBuildSettings.scenes = existing.ToArray();
    }

    static void SetPlayModeStartScene()
    {
        var hubAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (hubAsset != null)
            EditorSceneManager.playModeStartScene = hubAsset;
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

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
