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
/// One-click builder for Civic Quiz Showdown (Game 10).
/// Creates 25 question assets across 5 difficulty tiers, builds a scene with
/// 4-option multiple-choice UI, three lifelines (50:50, Hint, Pause), end-game
/// leaderboard with name input + PlayerPrefs persistence. Unlocks the hub.
/// Run via menu: BrainCitizen > Build QuizShowdown Game.
/// </summary>
public static class CivicQuizSceneBuilder
{
    const string ScenePath = "Assets/Scenes/QuizShowdown.unity";
    const string DataDir = "Assets/Data/QuizShowdown";
    const string DatabasePath = DataDir + "/CivicQuizDatabase.asset";
    const string HubGameInfoPath = "Assets/Data/Hub/Game10_QuizShowdown.asset";

    static readonly Color NavyBg     = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color NavyDarker = new Color(0.063f, 0.082f, 0.220f);
    static readonly Color White      = Color.white;
    static readonly Color Green      = new Color(0.180f, 0.800f, 0.443f);
    static readonly Color Red        = new Color(0.906f, 0.298f, 0.235f);
    static readonly Color Gold       = new Color(0.95f, 0.78f, 0.20f);
    static readonly Color CardBg     = new Color(0.96f, 0.97f, 1f, 0.96f);
    static readonly Color CardHidden = new Color(0.5f, 0.5f, 0.6f, 0.45f);
    static readonly Color TextPrim   = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color PanelDim   = new Color(0f, 0f, 0f, 0.78f);

    struct QDef
    {
        public string Slug;
        public string Question;
        public string[] Options;
        public int CorrectIndex;
        public int Difficulty;
        public string Hint;
        public QDef(string s, string q, string[] o, int c, int d, string h)
        { Slug = s; Question = q; Options = o; CorrectIndex = c; Difficulty = d; Hint = h; }
    }

    static readonly QDef[] QUESTIONS = new[]
    {
        // -------- Tier 1 (Easy) --------
        new QDef("T1_Q01_EUMembers",  "How many countries are in the European Union today?",
            new[] { "25", "27", "30", "32" }, 1, 1,
            "Two more than 25, after the UK left in 2020."),
        new QDef("T1_Q02_UNHQ",       "Where is the United Nations headquarters located?",
            new[] { "Geneva", "Brussels", "New York", "Vienna" }, 2, 1,
            "On the East River, in the Americas."),
        new QDef("T1_Q03_DemoOrigin", "Which place is considered the birthplace of democracy?",
            new[] { "Rome", "Egypt", "Athens", "Paris" }, 2, 1,
            "Around 500 BCE, in Greece."),
        new QDef("T1_Q04_UNFounded",  "When was the United Nations founded?",
            new[] { "1919", "1945", "1955", "1972" }, 1, 1,
            "Just after World War II ended."),
        new QDef("T1_Q05_VotingAge",  "What is the most common minimum voting age worldwide?",
            new[] { "16", "18", "21", "25" }, 1, 1,
            "It usually matches the age of legal adulthood."),

        // -------- Tier 2 (Easy-Medium) --------
        new QDef("T2_Q06_UDHRArticles","How many articles are in the Universal Declaration of Human Rights?",
            new[] { "10", "20", "30", "50" }, 2, 2,
            "Adopted by the UN General Assembly in 1948."),
        new QDef("T2_Q07_BerlinWall", "When did the Berlin Wall fall?",
            new[] { "1979", "1985", "1989", "1991" }, 2, 2,
            "Late 1980s, ending 28 years of separation."),
        new QDef("T2_Q08_NATOFounded","When was NATO founded?",
            new[] { "1939", "1945", "1949", "1955" }, 2, 2,
            "Late 1940s, with 12 founding members."),
        new QDef("T2_Q09_NobelPeace", "Where is the Nobel Peace Prize awarded?",
            new[] { "Stockholm", "Oslo", "Geneva", "Paris" }, 1, 2,
            "All other Nobel prizes are awarded in Sweden."),
        new QDef("T2_Q10_Compulsory", "Which country has compulsory federal voting?",
            new[] { "Australia", "Canada", "Germany", "United Kingdom" }, 0, 2,
            "Down under - failure to vote results in a small fine."),

        // -------- Tier 3 (Medium) --------
        new QDef("T3_Q11_MagnaCarta", "When was the Magna Carta signed?",
            new[] { "1188", "1215", "1252", "1320" }, 1, 3,
            "Early 13th century, in England."),
        new QDef("T3_Q12_Geneva",     "What does the Geneva Convention regulate?",
            new[] { "Trade tariffs", "Wartime conduct", "Climate change", "Internet privacy" }, 1, 3,
            "Treatment of soldiers and civilians during war."),
        new QDef("T3_Q13_FirstUNSG",  "Who was the first UN Secretary-General?",
            new[] { "Dag Hammarskjold", "Trygve Lie", "Kofi Annan", "Ban Ki-moon" }, 1, 3,
            "A Norwegian who served 1946-1952."),
        new QDef("T3_Q14_WWITreaty",  "Which treaty formally ended World War I?",
            new[] { "Versailles", "Vienna", "Trianon", "Brest-Litovsk" }, 0, 3,
            "Signed at a famous French palace in 1919."),
        new QDef("T3_Q15_Amnesty",    "In which decade was Amnesty International founded?",
            new[] { "1940s", "1950s", "1960s", "1970s" }, 2, 3,
            "Started in London in 1961."),

        // -------- Tier 4 (Hard) --------
        new QDef("T4_Q16_Westphalia", "When was the Treaty of Westphalia signed?",
            new[] { "1488", "1648", "1763", "1815" }, 1, 4,
            "Ended the Thirty Years War."),
        new QDef("T4_Q17_EUTreaty",   "Which treaty established the European Union?",
            new[] { "Treaty of Rome", "Maastricht Treaty", "Lisbon Treaty", "Schengen Agreement" }, 1, 4,
            "Named after a Dutch city, signed in 1992."),
        new QDef("T4_Q18_AU",         "When was the African Union founded?",
            new[] { "1963", "1985", "2002", "2011" }, 2, 4,
            "Replaced the OAU in the early 21st century."),
        new QDef("T4_Q19_LeagueEnd",  "When was the League of Nations officially dissolved?",
            new[] { "1939", "1945", "1946", "1948" }, 2, 4,
            "The year after WWII ended."),
        new QDef("T4_Q20_Ozone",      "Which treaty protects the ozone layer?",
            new[] { "Kyoto Protocol", "Paris Accords", "Montreal Protocol", "Geneva Convention" }, 2, 4,
            "Signed in 1987 in a Canadian city."),

        // -------- Tier 5 (Extreme) --------
        new QDef("T5_Q21_UNMembers",  "How many member states does the United Nations have?",
            new[] { "145", "178", "193", "207" }, 2, 5,
            "Most recent additions were Montenegro and South Sudan."),
        new QDef("T5_Q22_FirstWomenVote", "Which country first granted women full national voting rights?",
            new[] { "Norway", "Finland", "Australia", "New Zealand" }, 3, 5,
            "1893, in the South Pacific."),
        new QDef("T5_Q23_NATOFounders","How many founding members did NATO have in 1949?",
            new[] { "8", "12", "16", "24" }, 1, 5,
            "Twelve western democracies."),
        new QDef("T5_Q24_WTO",        "When was the World Trade Organization founded?",
            new[] { "1947", "1971", "1995", "2001" }, 2, 5,
            "It replaced GATT in the mid-1990s."),
        new QDef("T5_Q25_Helsinki",   "When were the Helsinki Accords signed?",
            new[] { "1955", "1975", "1987", "1991" }, 1, 5,
            "A Cold War-era agreement on European security."),
    };

    [MenuItem("BrainCitizen/Build QuizShowdown Game")]
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

        var db = BuildQuestionAssets();
        if (db == null) return;
        BuildScene(db);
        UnlockInHub();
        AddToBuildSettings();

        EditorUtility.DisplayDialog(
            "Civic Quiz Showdown built",
            $"Scene saved to {ScenePath}\n" +
            $"{db.questions.Length} questions in {DataDir}.\n\n" +
            "15 questions per game. Good luck!",
            "OK");
    }

    static CivicQuizDatabase BuildQuestionAssets()
    {
        if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);

        var assets = new List<CivicQuizQuestion>();
        foreach (var def in QUESTIONS)
        {
            string path = $"{DataDir}/{def.Slug}.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var q = ScriptableObject.CreateInstance<CivicQuizQuestion>();
            q.question = def.Question;
            q.options = def.Options;
            q.correctIndex = def.CorrectIndex;
            q.difficulty = def.Difficulty;
            q.hint = def.Hint;
            AssetDatabase.CreateAsset(q, path);

            var so = new SerializedObject(q);
            so.FindProperty("question").stringValue = def.Question;
            var optsProp = so.FindProperty("options");
            optsProp.arraySize = def.Options.Length;
            for (int i = 0; i < def.Options.Length; i++)
                optsProp.GetArrayElementAtIndex(i).stringValue = def.Options[i];
            so.FindProperty("correctIndex").intValue = def.CorrectIndex;
            so.FindProperty("difficulty").intValue = def.Difficulty;
            so.FindProperty("hint").stringValue = def.Hint;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(q);
            assets.Add(q);
        }

        var db = AssetDatabase.LoadAssetAtPath<CivicQuizDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<CivicQuizDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        db.questions = assets.ToArray();
        EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return db;
    }

    // =======================================================
    // Scene
    // =======================================================

    static void BuildScene(CivicQuizDatabase db)
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
        var bgVignette = MakeImage("BackgroundVignette", canvasGO.transform, NavyDarker);
        bgVignette.color = new Color(NavyDarker.r, NavyDarker.g, NavyDarker.b, 0.5f);
        var vRT = bgVignette.GetComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0, 0); vRT.anchorMax = new Vector2(1, 0.5f);
        vRT.offsetMin = Vector2.zero; vRT.offsetMax = Vector2.zero;

        // HUD
        var scoreText = MakeText("ScoreText", canvasGO.transform, "Score: 0",
            42, FontStyles.Bold, White);
        var sRT = scoreText.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(0, 1);
        sRT.pivot = new Vector2(0, 0.5f);
        sRT.anchoredPosition = new Vector2(60, -55);
        sRT.sizeDelta = new Vector2(500, 60);
        scoreText.alignment = TextAlignmentOptions.Left;

        var streakText = MakeText("StreakText", canvasGO.transform, "",
            36, FontStyles.Bold, Gold);
        var stRT = streakText.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(1, 1); stRT.anchorMax = new Vector2(1, 1);
        stRT.pivot = new Vector2(1, 0.5f);
        stRT.anchoredPosition = new Vector2(-60, -55);
        stRT.sizeDelta = new Vector2(420, 60);
        streakText.alignment = TextAlignmentOptions.Right;

        var questionCounterText = MakeText("QuestionCounterText", canvasGO.transform,
            "Question 1 / 15", 36, FontStyles.Bold, White);
        var qcRT = questionCounterText.GetComponent<RectTransform>();
        qcRT.anchorMin = new Vector2(0.5f, 1); qcRT.anchorMax = new Vector2(0.5f, 1);
        qcRT.pivot = new Vector2(0.5f, 0.5f);
        qcRT.anchoredPosition = new Vector2(0, -55);
        qcRT.sizeDelta = new Vector2(500, 60);

        var basePointsText = MakeText("BasePointsText", canvasGO.transform, "Worth: 100",
            32, FontStyles.Bold, Gold);
        var bpRT = basePointsText.GetComponent<RectTransform>();
        bpRT.anchorMin = new Vector2(0.5f, 1); bpRT.anchorMax = new Vector2(0.5f, 1);
        bpRT.pivot = new Vector2(0.5f, 0.5f);
        bpRT.anchoredPosition = new Vector2(0, -110);
        bpRT.sizeDelta = new Vector2(500, 50);

        // Question card
        var qCard = MakeImage("QuestionCard", canvasGO.transform, new Color(1f, 1f, 1f, 0.06f));
        var qcRT2 = qCard.GetComponent<RectTransform>();
        qcRT2.anchorMin = new Vector2(0.5f, 1); qcRT2.anchorMax = new Vector2(0.5f, 1);
        qcRT2.pivot = new Vector2(0.5f, 1);
        qcRT2.anchoredPosition = new Vector2(0, -180);
        qcRT2.sizeDelta = new Vector2(1500, 230);

        var questionText = MakeText("QuestionText", qCard.transform, "Question?",
            42, FontStyles.Bold, White);
        var qtRT = questionText.GetComponent<RectTransform>();
        qtRT.anchorMin = Vector2.zero; qtRT.anchorMax = Vector2.one;
        qtRT.offsetMin = new Vector2(40, 30); qtRT.offsetMax = new Vector2(-40, -30);
        questionText.alignment = TextAlignmentOptions.Center;
        questionText.textWrappingMode = TextWrappingModes.Normal;

        // Timer
        var timerSlider = MakeSlider("TimerSlider", canvasGO.transform, Green);
        var trRT = timerSlider.GetComponent<RectTransform>();
        trRT.anchorMin = new Vector2(0.5f, 1); trRT.anchorMax = new Vector2(0.5f, 1);
        trRT.pivot = new Vector2(0.5f, 1);
        trRT.anchoredPosition = new Vector2(0, -440);
        trRT.sizeDelta = new Vector2(1500, 22);
        var timerFill = timerSlider.fillRect.GetComponent<Image>();

        // Answer grid (2x2)
        var answerGridGO = new GameObject("AnswerGrid",
            typeof(RectTransform), typeof(GridLayoutGroup));
        answerGridGO.transform.SetParent(canvasGO.transform, false);
        var agRT = answerGridGO.GetComponent<RectTransform>();
        agRT.anchorMin = new Vector2(0.5f, 0.5f); agRT.anchorMax = new Vector2(0.5f, 0.5f);
        agRT.pivot = new Vector2(0.5f, 0.5f);
        agRT.anchoredPosition = new Vector2(0, -110);
        agRT.sizeDelta = new Vector2(1520, 280);
        var glg = answerGridGO.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(740, 130);
        glg.spacing = new Vector2(20, 20);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.childAlignment = TextAnchor.MiddleCenter;

        var answerButtons = new Button[4];
        for (int i = 0; i < 4; i++)
            answerButtons[i] = MakeButton($"AnswerButton{i + 1}", answerGridGO.transform,
                "Option", CardBg, TextPrim, 30);

        // Lifelines row
        var lifelinesGO = new GameObject("LifelinesRow",
            typeof(RectTransform), typeof(HorizontalLayoutGroup));
        lifelinesGO.transform.SetParent(canvasGO.transform, false);
        var llRT = lifelinesGO.GetComponent<RectTransform>();
        llRT.anchorMin = new Vector2(0.5f, 0); llRT.anchorMax = new Vector2(0.5f, 0);
        llRT.pivot = new Vector2(0.5f, 0);
        llRT.anchoredPosition = new Vector2(0, 80);
        llRT.sizeDelta = new Vector2(900, 110);
        var hlg = lifelinesGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        var fiftyFiftyButton = MakeLifelineButton(lifelinesGO.transform, "50:50", "Hide two wrong answers");
        var hintButton = MakeLifelineButton(lifelinesGO.transform, "Hint", "Reveal a clue");
        var pauseButton = MakeLifelineButton(lifelinesGO.transform, "+30s", "Add 30 seconds");

        // Hint modal
        var hintPanel = MakeImage("HintPanel", canvasGO.transform, PanelDim);
        Stretch(hintPanel);
        var hintCard = MakeImage("HintCard", hintPanel.transform, CardBg);
        var hcRT = hintCard.GetComponent<RectTransform>();
        hcRT.anchorMin = new Vector2(0.5f, 0.5f); hcRT.anchorMax = new Vector2(0.5f, 0.5f);
        hcRT.pivot = new Vector2(0.5f, 0.5f);
        hcRT.anchoredPosition = Vector2.zero;
        hcRT.sizeDelta = new Vector2(1100, 480);

        var hintTitle = MakeText("HintTitle", hintCard.transform, "HINT", 56, FontStyles.Bold, Gold);
        SetAnchor(hintTitle, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        hintTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        hintTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 70);

        var hintText = MakeText("HintText", hintCard.transform, "...", 36, FontStyles.Normal, TextPrim);
        var htRT = hintText.GetComponent<RectTransform>();
        htRT.anchorMin = new Vector2(0, 0); htRT.anchorMax = new Vector2(1, 1);
        htRT.offsetMin = new Vector2(60, 130); htRT.offsetMax = new Vector2(-60, -120);
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.textWrappingMode = TextWrappingModes.Normal;

        var hintCloseBtn = MakeButton("HintCloseButton", hintCard.transform, "GOT IT",
            Green, White, 36);
        var hcbRT = hintCloseBtn.GetComponent<RectTransform>();
        hcbRT.anchorMin = new Vector2(0.5f, 0); hcbRT.anchorMax = new Vector2(0.5f, 0);
        hcbRT.pivot = new Vector2(0.5f, 0);
        hcbRT.anchoredPosition = new Vector2(0, 30);
        hcbRT.sizeDelta = new Vector2(280, 80);

        hintPanel.gameObject.SetActive(false);

        // End screen
        var endPanel = MakeImage("EndScreenPanel", canvasGO.transform, NavyDarker);
        Stretch(endPanel);

        var endTitle = MakeText("EndTitle", endPanel.transform, "GAME OVER",
            72, FontStyles.Bold, White);
        SetAnchor(endTitle, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        endTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -90);
        endTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

        var finalScore = MakeText("FinalScoreText", endPanel.transform, "0",
            120, FontStyles.Bold, Green);
        SetAnchor(finalScore, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        finalScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -210);
        finalScore.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 160);

        // Name input + save row
        var nameInput = MakeInputField(endPanel.transform, "Enter your name");
        var niRT = nameInput.GetComponent<RectTransform>();
        niRT.anchorMin = new Vector2(0.5f, 1); niRT.anchorMax = new Vector2(0.5f, 1);
        niRT.pivot = new Vector2(0.5f, 1);
        niRT.anchoredPosition = new Vector2(-180, -390);
        niRT.sizeDelta = new Vector2(560, 80);

        var saveBtn = MakeButton("SaveScoreButton", endPanel.transform, "SAVE",
            Gold, NavyDarker, 36);
        var sbRT = saveBtn.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0.5f, 1); sbRT.anchorMax = new Vector2(0.5f, 1);
        sbRT.pivot = new Vector2(0.5f, 1);
        sbRT.anchoredPosition = new Vector2(220, -390);
        sbRT.sizeDelta = new Vector2(220, 80);

        // Leaderboard list
        var lbHeading = MakeText("LeaderboardHeading", endPanel.transform, "TOP SCORES",
            36, FontStyles.Bold, Gold);
        SetAnchor(lbHeading, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        lbHeading.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -510);
        lbHeading.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 50);

        var lbContainer = new GameObject("LeaderboardContainer",
            typeof(RectTransform), typeof(VerticalLayoutGroup));
        lbContainer.transform.SetParent(endPanel.transform, false);
        var lbcRT = lbContainer.GetComponent<RectTransform>();
        lbcRT.anchorMin = new Vector2(0.5f, 1); lbcRT.anchorMax = new Vector2(0.5f, 1);
        lbcRT.pivot = new Vector2(0.5f, 1);
        lbcRT.anchoredPosition = new Vector2(0, -570);
        lbcRT.sizeDelta = new Vector2(900, 350);
        var lbVlg = lbContainer.GetComponent<VerticalLayoutGroup>();
        lbVlg.spacing = 8;
        lbVlg.padding = new RectOffset(10, 10, 10, 10);
        lbVlg.childAlignment = TextAnchor.UpperCenter;
        lbVlg.childForceExpandWidth = true;
        lbVlg.childForceExpandHeight = false;
        lbVlg.childControlWidth = true;
        lbVlg.childControlHeight = false;

        var lbEntryTemplate = BuildLeaderboardEntryTemplate(lbContainer.transform);
        lbEntryTemplate.SetActive(false);

        var restartBtn = MakeButton("RestartButton", endPanel.transform, "PLAY AGAIN",
            Green, White, 40);
        var rbRT = restartBtn.GetComponent<RectTransform>();
        rbRT.anchorMin = new Vector2(0.5f, 0); rbRT.anchorMax = new Vector2(0.5f, 0);
        rbRT.pivot = new Vector2(0.5f, 0);
        rbRT.anchoredPosition = new Vector2(-260, 60);
        rbRT.sizeDelta = new Vector2(440, 110);

        var hubBtn = MakeButton("HubButton", endPanel.transform, "BACK TO HUB",
            new Color(0.3f, 0.3f, 0.5f), White, 40);
        var hbRT = hubBtn.GetComponent<RectTransform>();
        hbRT.anchorMin = new Vector2(0.5f, 0); hbRT.anchorMax = new Vector2(0.5f, 0);
        hbRT.pivot = new Vector2(0.5f, 0);
        hbRT.anchoredPosition = new Vector2(260, 60);
        hbRT.sizeDelta = new Vector2(440, 110);

        endPanel.gameObject.SetActive(false);

        // Managers
        var gmGO = new GameObject("GameManager");
        var gameManager = gmGO.AddComponent<CivicQuizGameManager>();

        var dbLive = AssetDatabase.LoadAssetAtPath<CivicQuizDatabase>(DatabasePath);
        if (dbLive == null) { Debug.LogError($"Failed to reload {DatabasePath}."); dbLive = db; }
        gameManager.questionDatabase = dbLive;
        var gmSO = new SerializedObject(gameManager);
        gmSO.FindProperty("questionDatabase").objectReferenceValue = dbLive;
        gmSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);

        var uiGO = new GameObject("UIManager");
        var ui = uiGO.AddComponent<CivicQuizUIManager>();
        ui.questionCounterText = questionCounterText;
        ui.scoreText = scoreText;
        ui.streakText = streakText;
        ui.basePointsText = basePointsText;
        ui.questionText = questionText;
        ui.timerSlider = timerSlider;
        ui.timerFill = timerFill;
        ui.timerFullColor = Green;
        ui.timerLowColor = Red;
        ui.answerGrid = answerGridGO.transform;
        ui.answerNormalColor = CardBg;
        ui.answerCorrectColor = Green;
        ui.answerWrongColor = Red;
        ui.answerHiddenColor = CardHidden;
        ui.fiftyFiftyButton = fiftyFiftyButton;
        ui.hintButton = hintButton;
        ui.pauseButton = pauseButton;
        ui.lifelineActive = Gold;
        ui.lifelineUsed = new Color(0.4f, 0.4f, 0.45f);
        ui.hintPanel = hintPanel.gameObject;
        ui.hintText = hintText;
        ui.hintCloseButton = hintCloseBtn;
        ui.endScreenPanel = endPanel.gameObject;
        ui.endTitleText = endTitle;
        ui.finalScoreText = finalScore;
        ui.nameInputField = nameInput;
        ui.saveScoreButton = saveBtn;
        ui.leaderboardContainer = lbContainer.transform;
        ui.leaderboardEntryTemplate = lbEntryTemplate;
        ui.restartButton = restartBtn;
        ui.hubButton = hubBtn;
        ui.winColor = Green;
        ui.loseColor = Red;
        EditorUtility.SetDirty(ui);

        UnityEngine.Events.UnityAction<int> onAnswer = gameManager.OnPlayerAnswer;
        for (int i = 0; i < 4; i++)
            UnityEventTools.AddIntPersistentListener(answerButtons[i].onClick, onAnswer, i);
        UnityEventTools.AddPersistentListener(restartBtn.onClick, gameManager.RestartGame);
        UnityEventTools.AddPersistentListener(hubBtn.onClick, gameManager.ReturnToHub);

        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static GameObject BuildLeaderboardEntryTemplate(Transform parent)
    {
        var entry = new GameObject("LeaderboardEntryTemplate",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        entry.transform.SetParent(parent, false);
        entry.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
        var le = entry.GetComponent<LayoutElement>();
        le.minHeight = 60; le.preferredHeight = 60;

        var rank = MakeText("RankText", entry.transform, "1.", 30, FontStyles.Bold, Gold);
        var rRT = rank.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0, 0); rRT.anchorMax = new Vector2(0, 1);
        rRT.pivot = new Vector2(0, 0.5f);
        rRT.anchoredPosition = new Vector2(40, 0);
        rRT.sizeDelta = new Vector2(80, 0);
        rank.alignment = TextAlignmentOptions.Center;

        var nameT = MakeText("NameText", entry.transform, "Player", 28, FontStyles.Bold, White);
        var nRT = nameT.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0, 0); nRT.anchorMax = new Vector2(0.7f, 1);
        nRT.pivot = new Vector2(0, 0.5f);
        nRT.offsetMin = new Vector2(140, 0); nRT.offsetMax = new Vector2(0, 0);
        nameT.alignment = TextAlignmentOptions.MidlineLeft;

        var scoreT = MakeText("ScoreText", entry.transform, "0", 30, FontStyles.Bold, Green);
        var sRT = scoreT.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0.7f, 0); sRT.anchorMax = new Vector2(1, 1);
        sRT.pivot = new Vector2(1, 0.5f);
        sRT.offsetMin = new Vector2(0, 0); sRT.offsetMax = new Vector2(-40, 0);
        scoreT.alignment = TextAlignmentOptions.MidlineRight;

        return entry;
    }

    static Button MakeLifelineButton(Transform parent, string label, string subLabel)
    {
        var go = new GameObject(label.Replace(":", "") + "Button",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = Gold;
        var le = go.GetComponent<LayoutElement>();
        le.minWidth = 260; le.preferredWidth = 260;
        le.minHeight = 100; le.preferredHeight = 100;

        var topText = MakeText("Label", go.transform, label, 38, FontStyles.Bold, NavyDarker);
        var ltRT = topText.GetComponent<RectTransform>();
        ltRT.anchorMin = new Vector2(0, 0.45f); ltRT.anchorMax = new Vector2(1, 1);
        ltRT.offsetMin = Vector2.zero; ltRT.offsetMax = Vector2.zero;

        var subText = MakeText("SubLabel", go.transform, subLabel, 18, FontStyles.Italic, NavyDarker);
        var stRT = subText.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0, 0); stRT.anchorMax = new Vector2(1, 0.45f);
        stRT.offsetMin = Vector2.zero; stRT.offsetMax = Vector2.zero;

        return go.GetComponent<Button>();
    }

    static TMP_InputField MakeInputField(Transform parent, string placeholder)
    {
        var go = new GameObject("InputField",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = White;

        var input = go.GetComponent<TMP_InputField>();

        var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(go.transform, false);
        var taRT = textArea.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(20, 10); taRT.offsetMax = new Vector2(-20, -10);

        var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer));
        phGO.transform.SetParent(textArea.transform, false);
        var phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text = placeholder;
        phTMP.color = new Color(0.45f, 0.45f, 0.5f);
        phTMP.fontStyle = FontStyles.Italic;
        phTMP.fontSize = 32;
        phTMP.alignment = TextAlignmentOptions.MidlineLeft;
        var phRT = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero; phRT.offsetMax = Vector2.zero;

        var txtGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtGO.transform.SetParent(textArea.transform, false);
        var txtTMP = txtGO.AddComponent<TextMeshProUGUI>();
        txtTMP.color = NavyDarker;
        txtTMP.fontSize = 32;
        txtTMP.alignment = TextAlignmentOptions.MidlineLeft;
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;

        input.textViewport = taRT;
        input.textComponent = txtTMP;
        input.placeholder = phTMP;
        input.characterLimit = 12;

        return input;
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
        faRT.anchorMin = new Vector2(0, 0); faRT.anchorMax = new Vector2(1, 1);
        faRT.offsetMin = new Vector2(4, 4); faRT.offsetMax = new Vector2(-4, -4);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = fillColor;
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0); fillRT.anchorMax = new Vector2(1, 1);
        fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
        slider.fillRect = fillRT;
        return slider;
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
