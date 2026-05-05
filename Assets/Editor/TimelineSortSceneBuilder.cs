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
/// One-click builder for Timeline Sort (Game 9).
/// Creates 21 historical event assets, builds the scene with a draggable
/// card list + submit flow, unlocks the hub card.
/// Run via menu: BrainCitizen > Build TimelineSort Game.
/// </summary>
public static class TimelineSortSceneBuilder
{
    const string ScenePath = "Assets/Scenes/TimelineSort.unity";
    const string DataDir = "Assets/Data/TimelineEvents";
    const string DatabasePath = DataDir + "/TimelineEventDatabase.asset";
    const string HubGameInfoPath = "Assets/Data/Hub/Game09_TimelineSort.asset";

    static readonly Color NavyBg     = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color White      = Color.white;
    static readonly Color Green      = new Color(0.180f, 0.800f, 0.443f);
    static readonly Color Red        = new Color(0.906f, 0.298f, 0.235f);
    static readonly Color CardBg     = new Color(0.97f, 0.98f, 1f, 0.96f);
    static readonly Color TextPrimary = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color TextMuted  = new Color(0.45f, 0.50f, 0.62f);

    struct EventDef
    {
        public string Slug;
        public string Title;
        public int Year;
        public TimelineEra Era;
        public string Explanation;
        public EventDef(string s, string t, int y, TimelineEra e, string x = "")
        { Slug = s; Title = t; Year = y; Era = e; Explanation = x; }
    }

    static readonly EventDef[] EVENTS = new[]
    {
        new EventDef("01_Rome",         "Founding of Rome",                            -753, TimelineEra.Ancient),
        new EventDef("02_Confucius",    "Birth of Confucius",                          -551, TimelineEra.Ancient),
        new EventDef("03_Marathon",     "Battle of Marathon",                          -490, TimelineEra.Ancient),
        new EventDef("04_Alexander",    "Death of Alexander the Great",                -323, TimelineEra.Ancient),
        new EventDef("05_RomeSplit",    "Roman Empire splits East and West",            395, TimelineEra.Ancient),
        new EventDef("06_MagnaCarta",   "Magna Carta is signed",                       1215, TimelineEra.Ancient),
        new EventDef("07_BlackDeath",   "Black Death plague peaks in Europe",          1347, TimelineEra.Ancient),

        new EventDef("08_Columbus",     "Columbus reaches the Americas",               1492, TimelineEra.EarlyModern),
        new EventDef("09_Reformation",  "Protestant Reformation begins",               1517, TimelineEra.EarlyModern),
        new EventDef("10_Galileo",      "Galileo demonstrates the telescope",          1609, TimelineEra.EarlyModern),
        new EventDef("11_BillOfRights", "English Bill of Rights",                      1689, TimelineEra.EarlyModern),
        new EventDef("12_USIndep",      "US Declaration of Independence",              1776, TimelineEra.EarlyModern),
        new EventDef("13_FrenchRev",    "French Revolution begins",                    1789, TimelineEra.EarlyModern),
        new EventDef("14_CivilWarEnd",  "American Civil War ends",                     1865, TimelineEra.EarlyModern),

        new EventDef("15_WWI",          "World War I begins",                          1914, TimelineEra.Modern),
        new EventDef("16_UN",           "United Nations is founded",                   1945, TimelineEra.Modern),
        new EventDef("17_UDHR",         "Universal Declaration of Human Rights",       1948, TimelineEra.Modern),
        new EventDef("18_BerlinWall",   "Berlin Wall falls",                           1989, TimelineEra.Modern),
        new EventDef("19_USSREnd",      "Soviet Union dissolves",                      1991, TimelineEra.Modern),
        new EventDef("20_WWW",          "World Wide Web becomes public",               1993, TimelineEra.Modern),
        new EventDef("21_Euro",         "EU adopts the Euro currency",                 2002, TimelineEra.Modern),
    };

    [MenuItem("BrainCitizen/Build TimelineSort Game")]
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

        var db = BuildEventAssets();
        if (db == null) return;
        BuildScene(db);
        UnlockInHub();
        AddToBuildSettings();

        EditorUtility.DisplayDialog(
            "Timeline Sort built",
            $"Scene saved to {ScenePath}\n" +
            $"{db.events.Length} events in {DataDir}.\n\n" +
            "Drag cards to reorder, then press Submit.",
            "OK");
    }

    // =======================================================
    // Event assets
    // =======================================================

    static TimelineEventDatabase BuildEventAssets()
    {
        if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);

        var assets = new List<TimelineEventData>();
        foreach (var def in EVENTS)
        {
            string path = $"{DataDir}/{def.Slug}.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var ev = ScriptableObject.CreateInstance<TimelineEventData>();
            ev.title = def.Title;
            ev.year = def.Year;
            ev.era = def.Era;
            ev.explanation = def.Explanation;
            AssetDatabase.CreateAsset(ev, path);

            var so = new SerializedObject(ev);
            so.FindProperty("title").stringValue = def.Title;
            so.FindProperty("year").intValue = def.Year;
            so.FindProperty("era").enumValueIndex = (int)def.Era;
            so.FindProperty("explanation").stringValue = def.Explanation;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ev);
            assets.Add(ev);
        }

        var db = AssetDatabase.LoadAssetAtPath<TimelineEventDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<TimelineEventDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        db.events = assets.ToArray();
        EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return db;
    }

    // =======================================================
    // Scene
    // =======================================================

    static void BuildScene(TimelineEventDatabase db)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGO = new GameObject("Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = MakeImage("Background", canvasGO.transform, NavyBg);
        Stretch(bg);

        // Title
        var title = MakeText("Title", canvasGO.transform, "TIMELINE SORT",
            72, FontStyles.Bold, White);
        SetAnchor(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -70);
        title.GetComponent<RectTransform>().sizeDelta = new Vector2(1600, 80);

        var subtitle = MakeText("Subtitle", canvasGO.transform,
            "Drag the events into chronological order, oldest at top.",
            30, FontStyles.Italic, new Color(1f, 1f, 1f, 0.85f));
        SetAnchor(subtitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        subtitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -135);
        subtitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1600, 50);

        // List container
        var listGO = new GameObject("CardList",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listGO.transform.SetParent(canvasGO.transform, false);
        var listRT = listGO.GetComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0.5f, 0.5f);
        listRT.anchorMax = new Vector2(0.5f, 0.5f);
        listRT.pivot = new Vector2(0.5f, 0.5f);
        listRT.anchoredPosition = new Vector2(0, -20);
        listRT.sizeDelta = new Vector2(1180, 600);
        var vlg = listGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 16;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var fit = listGO.GetComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Card template
        var cardTemplate = BuildCardTemplate(listGO.transform);
        cardTemplate.SetActive(false);

        // Score / result
        var scoreText = MakeText("ScoreText", canvasGO.transform,
            "Sort oldest to newest, then submit.",
            32, FontStyles.Bold, White);
        var scRT = scoreText.GetComponent<RectTransform>();
        scRT.anchorMin = new Vector2(0.5f, 0); scRT.anchorMax = new Vector2(0.5f, 0);
        scRT.pivot = new Vector2(0.5f, 0);
        scRT.anchoredPosition = new Vector2(0, 240);
        scRT.sizeDelta = new Vector2(1500, 50);

        var resultText = MakeText("ResultText", canvasGO.transform, "",
            36, FontStyles.Bold, Green);
        var rsRT = resultText.GetComponent<RectTransform>();
        rsRT.anchorMin = new Vector2(0.5f, 0); rsRT.anchorMax = new Vector2(0.5f, 0);
        rsRT.pivot = new Vector2(0.5f, 0);
        rsRT.anchoredPosition = new Vector2(0, 180);
        rsRT.sizeDelta = new Vector2(1500, 50);

        // Buttons
        var submitBtn = MakeButton("SubmitButton", canvasGO.transform, "SUBMIT",
            Green, White, 44);
        var subRT = submitBtn.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0); subRT.anchorMax = new Vector2(0.5f, 0);
        subRT.pivot = new Vector2(0.5f, 0);
        subRT.anchoredPosition = new Vector2(0, 50);
        subRT.sizeDelta = new Vector2(440, 110);

        var restartBtn = MakeButton("RestartButton", canvasGO.transform, "PLAY AGAIN",
            Green, White, 40);
        var rstRT = restartBtn.GetComponent<RectTransform>();
        rstRT.anchorMin = new Vector2(0.5f, 0); rstRT.anchorMax = new Vector2(0.5f, 0);
        rstRT.pivot = new Vector2(0.5f, 0);
        rstRT.anchoredPosition = new Vector2(-260, 50);
        rstRT.sizeDelta = new Vector2(440, 110);
        restartBtn.gameObject.SetActive(false);

        var hubBtn = MakeButton("HubButton", canvasGO.transform, "BACK TO HUB",
            new Color(0.3f, 0.3f, 0.5f), White, 40);
        var hubRT = hubBtn.GetComponent<RectTransform>();
        hubRT.anchorMin = new Vector2(0.5f, 0); hubRT.anchorMax = new Vector2(0.5f, 0);
        hubRT.pivot = new Vector2(0.5f, 0);
        hubRT.anchoredPosition = new Vector2(260, 50);
        hubRT.sizeDelta = new Vector2(440, 110);
        hubBtn.gameObject.SetActive(false);

        // Managers
        var gmGO = new GameObject("GameManager");
        var gameManager = gmGO.AddComponent<TimelineSortGameManager>();

        var dbLive = AssetDatabase.LoadAssetAtPath<TimelineEventDatabase>(DatabasePath);
        if (dbLive == null) { Debug.LogError($"Failed to reload {DatabasePath}."); dbLive = db; }
        gameManager.eventDatabase = dbLive;
        var gmSO = new SerializedObject(gameManager);
        gmSO.FindProperty("eventDatabase").objectReferenceValue = dbLive;
        gmSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);

        var uiGO = new GameObject("UIManager");
        var uiManager = uiGO.AddComponent<TimelineSortUIManager>();
        uiManager.cardListContainer = listGO.transform;
        uiManager.cardTemplate = cardTemplate;
        uiManager.scoreText = scoreText;
        uiManager.resultText = resultText;
        uiManager.submitButton = submitBtn;
        uiManager.restartButton = restartBtn;
        uiManager.hubButton = hubBtn;
        uiManager.correctColor = Green;
        uiManager.wrongColor = Red;
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
            typeof(LayoutElement), typeof(CanvasGroup), typeof(TimelineCard));
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = CardBg;
        var le = card.GetComponent<LayoutElement>();
        le.minHeight = 100;
        le.preferredHeight = 100;

        // Drag-handle indicator (cosmetic)
        var grip = MakeText("Grip", card.transform, "::", 36, FontStyles.Bold, TextMuted);
        var grRT = grip.GetComponent<RectTransform>();
        grRT.anchorMin = new Vector2(0, 0); grRT.anchorMax = new Vector2(0, 1);
        grRT.pivot = new Vector2(0, 0.5f);
        grRT.anchoredPosition = new Vector2(20, 0);
        grRT.sizeDelta = new Vector2(50, 0);
        grip.alignment = TextAlignmentOptions.Center;
        grip.raycastTarget = false;

        var titleText = MakeText("TitleText", card.transform, "Event title",
            32, FontStyles.Bold, TextPrimary);
        var ttRT = titleText.GetComponent<RectTransform>();
        ttRT.anchorMin = new Vector2(0, 0); ttRT.anchorMax = new Vector2(0.78f, 1);
        ttRT.pivot = new Vector2(0, 0.5f);
        ttRT.offsetMin = new Vector2(80, 8); ttRT.offsetMax = new Vector2(0, -8);
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.raycastTarget = false;

        var yearText = MakeText("YearText", card.transform, "0000",
            34, FontStyles.Bold, Green);
        var yrRT = yearText.GetComponent<RectTransform>();
        yrRT.anchorMin = new Vector2(0.78f, 0); yrRT.anchorMax = new Vector2(1, 1);
        yrRT.pivot = new Vector2(1, 0.5f);
        yrRT.offsetMin = new Vector2(0, 8); yrRT.offsetMax = new Vector2(-25, -8);
        yearText.alignment = TextAlignmentOptions.MidlineRight;
        yearText.raycastTarget = false;
        yearText.gameObject.SetActive(false);

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
            Debug.LogWarning("Couldn't find " + HubGameInfoPath
                + " - run BrainCitizen > Build Hub Scene first.");
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
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return btn;
    }

    static void Stretch(Image img)
    {
        var rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void SetAnchor(Component c, Vector2 min, Vector2 max)
    {
        var rt = c.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
    }
}
#endif
