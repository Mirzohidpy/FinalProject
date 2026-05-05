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
/// One-click builder for Doppi Facts (Game 6).
/// Creates 30 DoppiFact assets, builds a 3x3 hole-grid scene, wires the
/// game/UI managers, unlocks the hub card, and adds the scene to build settings.
/// Run via menu: BrainCitizen > Build DoppiFacts Game.
/// </summary>
public static class DoppiFactsSceneBuilder
{
    const string ScenePath = "Assets/Scenes/DoppiFacts.unity";
    const string DataDir = "Assets/Data/DoppiFacts";
    const string DatabasePath = DataDir + "/DoppiFactDatabase.asset";
    const string HubGameInfoPath = "Assets/Data/Hub/Game06_DoppiFacts.asset";

    static readonly Color NavyBg       = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color NavyBgDark   = new Color(0.063f, 0.082f, 0.220f);
    static readonly Color White        = Color.white;
    static readonly Color Green        = new Color(0.180f, 0.800f, 0.443f);
    static readonly Color Red          = new Color(0.906f, 0.298f, 0.235f);
    static readonly Color Mole         = new Color(0.95f, 0.78f, 0.45f);
    static readonly Color MoleEdge     = new Color(0.78f, 0.55f, 0.25f);
    static readonly Color HoleColor    = new Color(0.05f, 0.06f, 0.10f, 0.85f);
    static readonly Color BannerBg     = new Color(0f, 0f, 0f, 0.55f);
    static readonly Color PanelDim     = new Color(0f, 0f, 0f, 0.75f);
    static readonly Color TextOnMole   = new Color(0.10f, 0.08f, 0.05f);

    struct FactDef
    {
        public string Slug;
        public string Statement;
        public bool IsTrue;
        public string Explanation;
        public FactDef(string s, string t, bool i, string e)
        { Slug = s; Statement = t; IsTrue = i; Explanation = e; }
    }

    static readonly FactDef[] FACTS = new[]
    {
        // 15 TRUE
        new FactDef("True_01_EU27",         "The European Union has 27 member states.",                     true,  "Croatia joined as the 27th member; the UK left in 2020."),
        new FactDef("True_02_UN1945",       "The United Nations was founded in 1945.",                      true,  "The UN Charter took effect on 24 October 1945."),
        new FactDef("True_03_AntarcticaPop","Antarctica has no permanent human population.",                 true,  "Only researchers live there, on rotating tours."),
        new FactDef("True_04_AustraliaVote","Voting is compulsory in Australia.",                            true,  "Citizens face a small fine for not voting."),
        new FactDef("True_05_BerlinWall",   "The Berlin Wall fell in 1989.",                                true,  "It was opened on 9 November 1989, ending 28 years of separation."),
        new FactDef("True_06_GreeceDemo",   "Greece is considered the birthplace of democracy.",            true,  "Athens developed the first known democratic system around 500 BCE."),
        new FactDef("True_07_UDHR30",       "The Universal Declaration of Human Rights has 30 articles.",   true,  "It was adopted by the UN General Assembly in 1948."),
        new FactDef("True_08_SwissReferenda","Switzerland regularly holds national citizen referendums.",    true,  "Federal votes happen up to four times a year."),
        new FactDef("True_09_UNSC5",        "The UN Security Council has five permanent members.",          true,  "China, France, Russia, the UK and the US."),
        new FactDef("True_10_Smallpox",     "Smallpox is the only human disease ever fully eradicated.",    true,  "WHO declared it eradicated worldwide in 1980."),
        new FactDef("True_11_SAfrica1994",  "South Africa held its first multi-racial election in 1994.",   true,  "Nelson Mandela was elected president that year."),
        new FactDef("True_12_InternetDARPA","The Internet has its roots in a US Department of Defense project.", true, "ARPANET, predecessor of the modern Internet, launched in 1969."),
        new FactDef("True_13_SaudiWomen",   "Saudi Arabia first allowed women to vote in 2015.",            true,  "Women voted in municipal elections that December."),
        new FactDef("True_14_MEPVote",      "All EU citizens can vote in European Parliament elections.",   true,  "Direct elections have been held every five years since 1979."),
        new FactDef("True_15_NobelOslo",    "The Nobel Peace Prize is awarded annually in Oslo, Norway.",   true,  "All other Nobel prizes are awarded in Stockholm, Sweden."),

        // 15 FALSE
        new FactDef("False_01_UN250",       "The United Nations has 250 member states.",                    false, "The UN actually has 193 member states."),
        new FactDef("False_02_TenPercent",  "Humans only use 10 percent of their brain.",                   false, "Brain imaging shows nearly all regions are active over a day."),
        new FactDef("False_03_WallSpace",   "The Great Wall of China is visible from the Moon.",            false, "It cannot be seen with the naked eye even from low Earth orbit."),
        new FactDef("False_04_VaxAutism",   "Vaccines cause autism.",                                       false, "Decades of research find no link; the original 1998 paper was retracted."),
        new FactDef("False_05_LightningTwice","Lightning never strikes the same place twice.",              false, "Tall structures like the Empire State Building are hit dozens of times a year."),
        new FactDef("False_06_AntibioticsCold","Antibiotics work against the common cold.",                 false, "Colds are viral; antibiotics only treat bacterial infections."),
        new FactDef("False_07_AllEuro",     "All European countries use the Euro.",                         false, "Sweden, Denmark, Switzerland, Norway and the UK do not, among others."),
        new FactDef("False_08_5GVirus",     "5G networks spread viruses.",                                  false, "Radio waves cannot carry biological viruses."),
        new FactDef("False_09_SaharaAlways","The Sahara has always been a desert.",                         false, "Until about 5,000 years ago much of it was green grassland and lakes."),
        new FactDef("False_10_NoClimate",   "Climate change is not happening.",                             false, "Global temperatures and CO2 levels have been rising for over a century."),
        new FactDef("False_11_Voting21",    "The voting age is 21 in every democracy.",                     false, "Most democracies set it at 18; some allow voting at 16."),
        new FactDef("False_12_EUPresident", "The European Union has a single, directly elected president.", false, "The EU has multiple presidents and none are elected directly by all citizens."),
        new FactDef("False_13_8Glasses",    "Drinking 8 glasses of water a day is required by health law.", false, "There is no such law; needs vary by person and climate."),
        new FactDef("False_14_UN1900",      "The United Nations was founded in 1900.",                      false, "The UN was founded in 1945, after World War II."),
        new FactDef("False_15_AustraliaBig","Australia is larger than Russia.",                             false, "Russia is roughly twice the area of Australia."),
    };

    [MenuItem("BrainCitizen/Build DoppiFacts Game")]
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

        var db = BuildFactAssets();
        if (db == null) return;
        BuildScene(db);
        UnlockInHub();
        AddToBuildSettings();

        EditorUtility.DisplayDialog(
            "Doppi Facts built",
            $"Scene saved to {ScenePath}\n" +
            $"{db.facts.Length} facts created in {DataDir}.\n\n" +
            "Press Play.",
            "OK");
    }

    // =======================================================
    // Data assets
    // =======================================================

    static DoppiFactDatabase BuildFactAssets()
    {
        if (!Directory.Exists(DataDir))
            Directory.CreateDirectory(DataDir);

        var assets = new List<DoppiFactData>();
        foreach (var def in FACTS)
        {
            string path = $"{DataDir}/{def.Slug}.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var fact = ScriptableObject.CreateInstance<DoppiFactData>();
            fact.statement = def.Statement;
            fact.isTrue = def.IsTrue;
            fact.explanation = def.Explanation;
            AssetDatabase.CreateAsset(fact, path);

            var so = new SerializedObject(fact);
            so.FindProperty("statement").stringValue = def.Statement;
            so.FindProperty("isTrue").boolValue = def.IsTrue;
            so.FindProperty("explanation").stringValue = def.Explanation;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fact);

            assets.Add(fact);
        }

        var db = AssetDatabase.LoadAssetAtPath<DoppiFactDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<DoppiFactDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        db.facts = assets.ToArray();
        EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return db;
    }

    // =======================================================
    // Scene
    // =======================================================

    static void BuildScene(DoppiFactDatabase db)
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

        // Background
        var bg = MakeImage("Background", canvasGO.transform, NavyBg);
        Stretch(bg);
        var bgVignette = MakeImage("BackgroundVignette", canvasGO.transform, NavyBgDark);
        Stretch(bgVignette);
        bgVignette.color = new Color(NavyBgDark.r, NavyBgDark.g, NavyBgDark.b, 0.5f);
        var vRT = bgVignette.GetComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0, 0);
        vRT.anchorMax = new Vector2(1, 0.5f);

        // Title
        var title = MakeText("Title", canvasGO.transform, "DOPPI FACTS",
            72, FontStyles.Bold, White);
        SetAnchor(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -70);
        title.GetComponent<RectTransform>().sizeDelta = new Vector2(1600, 90);

        var subtitle = MakeText("Subtitle", canvasGO.transform,
            "Smash the TRUE facts. Avoid the myths.",
            32, FontStyles.Italic, new Color(1f, 1f, 1f, 0.85f));
        SetAnchor(subtitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        subtitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -135);
        subtitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1600, 50);

        // HUD
        var scoreText = MakeText("ScoreText", canvasGO.transform, "Score: 0",
            42, FontStyles.Bold, White);
        var scoreRT = scoreText.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0, 1); scoreRT.anchorMax = new Vector2(0, 1);
        scoreRT.pivot = new Vector2(0, 0.5f);
        scoreRT.anchoredPosition = new Vector2(60, -55);
        scoreRT.sizeDelta = new Vector2(420, 60);
        scoreText.alignment = TextAlignmentOptions.Left;

        var waveText = MakeText("WaveText", canvasGO.transform, "Wave 0 / 4",
            38, FontStyles.Bold, White);
        var waveRT = waveText.GetComponent<RectTransform>();
        waveRT.anchorMin = new Vector2(0.5f, 1); waveRT.anchorMax = new Vector2(0.5f, 1);
        waveRT.pivot = new Vector2(0.5f, 0.5f);
        waveRT.anchoredPosition = new Vector2(0, -210);
        waveRT.sizeDelta = new Vector2(500, 50);

        var streakText = MakeText("StreakText", canvasGO.transform, "",
            36, FontStyles.Bold, Green);
        var sRT = streakText.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(1, 1); sRT.anchorMax = new Vector2(1, 1);
        sRT.pivot = new Vector2(1, 0.5f);
        sRT.anchoredPosition = new Vector2(-60, -55);
        sRT.sizeDelta = new Vector2(420, 60);
        streakText.alignment = TextAlignmentOptions.Right;

        // Hole grid (3x3)
        var holeGridGO = new GameObject("HoleGrid",
            typeof(RectTransform), typeof(GridLayoutGroup));
        holeGridGO.transform.SetParent(canvasGO.transform, false);
        var hgRT = holeGridGO.GetComponent<RectTransform>();
        hgRT.anchorMin = new Vector2(0.5f, 0.5f);
        hgRT.anchorMax = new Vector2(0.5f, 0.5f);
        hgRT.pivot = new Vector2(0.5f, 0.5f);
        hgRT.anchoredPosition = new Vector2(0, -60);
        hgRT.sizeDelta = new Vector2(1500, 810);
        var glg = holeGridGO.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(480, 250);
        glg.spacing = new Vector2(30, 30);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 3;
        glg.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < 9; i++)
            BuildHole(holeGridGO.transform, i);

        // Banner panel
        var bannerPanel = new GameObject("BannerPanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bannerPanel.transform.SetParent(canvasGO.transform, false);
        bannerPanel.GetComponent<Image>().color = BannerBg;
        var bpRT = bannerPanel.GetComponent<RectTransform>();
        bpRT.anchorMin = new Vector2(0, 0.5f);
        bpRT.anchorMax = new Vector2(1, 0.5f);
        bpRT.pivot = new Vector2(0.5f, 0.5f);
        bpRT.anchoredPosition = Vector2.zero;
        bpRT.sizeDelta = new Vector2(0, 220);
        var bannerText = MakeText("BannerText", bannerPanel.transform, "WAVE 1",
            120, FontStyles.Bold, White);
        var btRT = bannerText.GetComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
        btRT.offsetMin = Vector2.zero; btRT.offsetMax = Vector2.zero;
        bannerPanel.SetActive(false);

        // End screen
        var endPanel = MakeImage("EndScreenPanel", canvasGO.transform, PanelDim);
        Stretch(endPanel);
        var endTitle = MakeText("EndTitle", endPanel.transform, "GAME OVER",
            80, FontStyles.Bold, White);
        SetAnchor(endTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        endTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -220);
        endTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 110);

        var finalScore = MakeText("FinalScoreText", endPanel.transform, "0",
            160, FontStyles.Bold, Green);
        SetAnchor(finalScore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        finalScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 90);
        finalScore.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 220);

        var endMessage = MakeText("EndMessageText", endPanel.transform, "",
            42, FontStyles.Normal, White);
        SetAnchor(endMessage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        endMessage.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -90);
        endMessage.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 90);

        var restartBtn = MakeButton("RestartButton", endPanel.transform, "PLAY AGAIN",
            Green, White, 44);
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
        var gameManager = gameManagerGO.AddComponent<DoppiFactsGameManager>();

        var dbLive = AssetDatabase.LoadAssetAtPath<DoppiFactDatabase>(DatabasePath);
        if (dbLive == null)
        {
            Debug.LogError($"Failed to reload {DatabasePath} - GameManager will have null database.");
            dbLive = db;
        }
        gameManager.factDatabase = dbLive;
        var goSO = new SerializedObject(gameManager);
        goSO.FindProperty("factDatabase").objectReferenceValue = dbLive;
        goSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);

        var uiManagerGO = new GameObject("UIManager");
        var uiManager = uiManagerGO.AddComponent<DoppiFactsUIManager>();
        uiManager.holeGrid = holeGridGO.transform;
        uiManager.scoreText = scoreText;
        uiManager.waveText = waveText;
        uiManager.streakText = streakText;
        uiManager.bannerPanel = bannerPanel;
        uiManager.bannerText = bannerText;
        uiManager.endScreenPanel = endPanel.gameObject;
        uiManager.finalScoreText = finalScore;
        uiManager.endMessageText = endMessage;
        uiManager.restartButton = restartBtn;
        uiManager.hubButton = hubBtn;
        uiManager.moleNeutralColor = Mole;
        uiManager.hitCorrectColor = Green;
        uiManager.hitWrongColor = Red;
        EditorUtility.SetDirty(uiManager);

        UnityEventTools.AddPersistentListener(restartBtn.onClick, gameManager.RestartGame);
        UnityEventTools.AddPersistentListener(hubBtn.onClick, gameManager.ReturnToHub);

        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static void BuildHole(Transform parent, int index)
    {
        var hole = new GameObject($"Hole_{index}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        hole.transform.SetParent(parent, false);
        hole.GetComponent<Image>().color = HoleColor;

        // Decorative dark band at the bottom of the hole, suggesting a pit
        var lip = MakeImage("Lip", hole.transform, new Color(0f, 0f, 0f, 0.4f));
        var lipRT = lip.GetComponent<RectTransform>();
        lipRT.anchorMin = new Vector2(0, 0);
        lipRT.anchorMax = new Vector2(1, 0);
        lipRT.pivot = new Vector2(0.5f, 0);
        lipRT.sizeDelta = new Vector2(0, 24);

        // Mole — Image + Button on same GO. TMP_Text child for the statement.
        var mole = new GameObject("Mole",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        mole.transform.SetParent(hole.transform, false);
        mole.GetComponent<Image>().color = Mole;
        var mRT = mole.GetComponent<RectTransform>();
        mRT.anchorMin = new Vector2(0.05f, 0.1f);
        mRT.anchorMax = new Vector2(0.95f, 0.95f);
        mRT.offsetMin = Vector2.zero;
        mRT.offsetMax = Vector2.zero;
        mole.transform.localScale = Vector3.zero;

        // Edge ring for visual distinction
        var edge = MakeImage("Edge", mole.transform, MoleEdge);
        edge.color = new Color(MoleEdge.r, MoleEdge.g, MoleEdge.b, 0.35f);
        var eRT = edge.GetComponent<RectTransform>();
        eRT.anchorMin = Vector2.zero; eRT.anchorMax = Vector2.one;
        eRT.offsetMin = new Vector2(-4, -4); eRT.offsetMax = new Vector2(4, 4);
        edge.transform.SetAsFirstSibling();

        var statement = MakeText("Statement", mole.transform, "",
            22, FontStyles.Bold, TextOnMole);
        var stRT = statement.GetComponent<RectTransform>();
        stRT.anchorMin = Vector2.zero; stRT.anchorMax = Vector2.one;
        stRT.offsetMin = new Vector2(18, 14); stRT.offsetMax = new Vector2(-18, -14);
        statement.alignment = TextAlignmentOptions.Center;
        statement.textWrappingMode = TextWrappingModes.Normal;
        statement.enableAutoSizing = true;
        statement.fontSizeMin = 16;
        statement.fontSizeMax = 26;
        statement.raycastTarget = false;
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
        rt.anchorMin = min;
        rt.anchorMax = max;
    }
}
#endif
