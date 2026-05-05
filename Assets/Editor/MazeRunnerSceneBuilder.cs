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
/// One-click builder for Maze Runner (Game 7).
/// Procedurally generates a 9x9 perfect maze (recursive backtracker),
/// places 5 gates along the start->end path, builds the scene with player
/// + camera + UI, creates 15 question assets, unlocks the hub card.
/// Run via menu: BrainCitizen > Build MazeRunner Game.
/// </summary>
public static class MazeRunnerSceneBuilder
{
    const string ScenePath = "Assets/Scenes/MazeRunner.unity";
    const string DataDir = "Assets/Data/MazeRunner";
    const string DatabasePath = DataDir + "/MazeQuestionDatabase.asset";
    const string SpritePath = DataDir + "/_white.png";
    const string HubGameInfoPath = "Assets/Data/Hub/Game07_MazeRunner.asset";

    const int W = 9, H = 9;
    const int GateCount = 5;
    const float WallThickness = 0.14f;

    static readonly Color NavyBg     = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color WallColor  = new Color(0.92f, 0.94f, 1f);
    static readonly Color GateColor  = new Color(0.95f, 0.55f, 0.18f);
    static readonly Color GoalColor  = new Color(0.97f, 0.85f, 0.20f);
    static readonly Color PlayerColor = new Color(0.20f, 0.85f, 0.50f);
    static readonly Color White      = Color.white;
    static readonly Color Green      = new Color(0.180f, 0.800f, 0.443f);
    static readonly Color Red        = new Color(0.906f, 0.298f, 0.235f);
    static readonly Color PanelDim   = new Color(0f, 0f, 0f, 0.78f);
    static readonly Color CardBg     = new Color(1f, 1f, 1f, 0.97f);
    static readonly Color TextOnCard = new Color(0.118f, 0.153f, 0.380f);

    struct QDef
    {
        public string Slug;
        public string Question;
        public string[] Options;
        public int CorrectIndex;
        public string Explanation;
        public QDef(string s, string q, string[] o, int c, string e)
        { Slug = s; Question = q; Options = o; CorrectIndex = c; Explanation = e; }
    }

    static readonly QDef[] QUESTIONS = new[]
    {
        new QDef("Q01_UNFounded",   "When was the United Nations founded?",
            new[] { "1919", "1945", "1955", "1972" }, 1,
            "The UN Charter took effect in October 1945."),
        new QDef("Q02_EUMembers",   "How many countries are in the European Union today?",
            new[] { "20", "23", "27", "31" }, 2,
            "After the UK left in 2020, 27 members remain."),
        new QDef("Q03_DemocracyOrigin", "Which place is considered the birthplace of democracy?",
            new[] { "Rome", "Egypt", "Athens", "Paris" }, 2,
            "Athens developed democracy around 500 BCE."),
        new QDef("Q04_UDHRArticles","How many articles are in the Universal Declaration of Human Rights?",
            new[] { "10", "20", "30", "50" }, 2,
            "The UDHR has 30 articles, adopted in 1948."),
        new QDef("Q05_BerlinWall",  "When did the Berlin Wall fall?",
            new[] { "1979", "1985", "1989", "1991" }, 2,
            "The Wall opened on 9 November 1989."),
        new QDef("Q06_VotingAge",   "What is the most common minimum voting age worldwide?",
            new[] { "16", "18", "21", "25" }, 1,
            "Most democracies set voting age at 18."),
        new QDef("Q07_UNHQ",        "Where is the UN headquarters located?",
            new[] { "Geneva", "Brussels", "New York", "Vienna" }, 2,
            "On the East River in New York City."),
        new QDef("Q08_NATOFounded", "When was NATO founded?",
            new[] { "1939", "1945", "1949", "1955" }, 2,
            "Originally with 12 founding members in 1949."),
        new QDef("Q09_NobelPeace",  "Where is the Nobel Peace Prize awarded?",
            new[] { "Stockholm", "Oslo", "Geneva", "Paris" }, 1,
            "All other Nobels are awarded in Stockholm."),
        new QDef("Q10_AmnestyFounded","Amnesty International was founded in which decade?",
            new[] { "1940s", "1950s", "1960s", "1970s" }, 2,
            "Founded in London in 1961."),
        new QDef("Q11_OzoneTreaty", "Which treaty protects the ozone layer?",
            new[] { "Kyoto Protocol", "Paris Accords", "Montreal Protocol", "Geneva Convention" }, 2,
            "The Montreal Protocol was signed in 1987."),
        new QDef("Q12_GenevaConvention", "What does the Geneva Convention regulate?",
            new[] { "Trade tariffs", "Wartime conduct", "Climate change", "Internet privacy" }, 1,
            "It governs the treatment of soldiers and civilians in war."),
        new QDef("Q13_HumanRightsDay","When is Human Rights Day observed each year?",
            new[] { "January 1", "March 8", "October 24", "December 10" }, 3,
            "Anniversary of the UDHR adoption in 1948."),
        new QDef("Q14_LeagueOfNations","Which organization preceded the United Nations?",
            new[] { "League of Nations", "Hague Tribunal", "World Court", "Allied Council" }, 0,
            "Founded after WWI; dissolved in 1946."),
        new QDef("Q15_CompulsoryVote","Which country has compulsory federal voting?",
            new[] { "Australia", "Canada", "Germany", "United Kingdom" }, 0,
            "Failure to vote results in a small fine in Australia."),
    };

    [MenuItem("BrainCitizen/Build MazeRunner Game")]
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

        var sprite = EnsureWhiteSprite();
        if (sprite == null)
        {
            EditorUtility.DisplayDialog("Sprite generation failed",
                "Could not create the white sprite at " + SpritePath,
                "OK");
            return;
        }
        var db = BuildQuestionAssets();
        if (db == null) return;
        BuildScene(db, sprite);
        UnlockInHub();
        AddToBuildSettings();

        EditorUtility.DisplayDialog(
            "Maze Runner built",
            $"Scene saved to {ScenePath}\n" +
            $"{db.questions.Length} questions in {DataDir}.\n\n" +
            "WASD or arrow keys to move. Reach the yellow goal.",
            "OK");
    }

    // =======================================================
    // White sprite asset
    // =======================================================

    static Sprite EnsureWhiteSprite()
    {
        if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);

        if (!File.Exists(SpritePath))
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (int i = 0; i < 64; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(SpritePath);
        }

        var imp = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (imp == null) return null;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 8;
        imp.filterMode = FilterMode.Point;
        imp.alphaIsTransparency = false;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    // =======================================================
    // Question assets
    // =======================================================

    static MazeQuestionDatabase BuildQuestionAssets()
    {
        if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);

        var assets = new List<MazeQuestionData>();
        foreach (var def in QUESTIONS)
        {
            string path = $"{DataDir}/{def.Slug}.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var q = ScriptableObject.CreateInstance<MazeQuestionData>();
            q.question = def.Question;
            q.options = def.Options;
            q.correctIndex = def.CorrectIndex;
            q.explanation = def.Explanation;
            AssetDatabase.CreateAsset(q, path);

            var so = new SerializedObject(q);
            so.FindProperty("question").stringValue = def.Question;
            var optsProp = so.FindProperty("options");
            optsProp.arraySize = def.Options.Length;
            for (int i = 0; i < def.Options.Length; i++)
                optsProp.GetArrayElementAtIndex(i).stringValue = def.Options[i];
            so.FindProperty("correctIndex").intValue = def.CorrectIndex;
            so.FindProperty("explanation").stringValue = def.Explanation;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(q);
            assets.Add(q);
        }

        var db = AssetDatabase.LoadAssetAtPath<MazeQuestionDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<MazeQuestionDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        db.questions = assets.ToArray();
        EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return db;
    }

    // =======================================================
    // Maze generation (recursive backtracker, iterative)
    // =======================================================

    static (bool[,] hWall, bool[,] vWall) GenerateMaze()
    {
        var hWall = new bool[W, H + 1];
        var vWall = new bool[W + 1, H];
        for (int x = 0; x < W; x++)
            for (int y = 0; y <= H; y++) hWall[x, y] = true;
        for (int x = 0; x <= W; x++)
            for (int y = 0; y < H; y++) vWall[x, y] = true;

        var visited = new bool[W, H];
        var stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(0, 0));
        visited[0, 0] = true;

        while (stack.Count > 0)
        {
            var c = stack.Peek();
            var nbrs = new List<(Vector2Int cell, int dir)>();
            if (c.y < H - 1 && !visited[c.x, c.y + 1]) nbrs.Add((new Vector2Int(c.x, c.y + 1), 0));
            if (c.x < W - 1 && !visited[c.x + 1, c.y]) nbrs.Add((new Vector2Int(c.x + 1, c.y), 1));
            if (c.y > 0 && !visited[c.x, c.y - 1]) nbrs.Add((new Vector2Int(c.x, c.y - 1), 2));
            if (c.x > 0 && !visited[c.x - 1, c.y]) nbrs.Add((new Vector2Int(c.x - 1, c.y), 3));

            if (nbrs.Count == 0) { stack.Pop(); continue; }
            var pick = nbrs[Random.Range(0, nbrs.Count)];
            switch (pick.dir)
            {
                case 0: hWall[c.x, c.y + 1] = false; break;
                case 1: vWall[c.x + 1, c.y] = false; break;
                case 2: hWall[c.x, c.y] = false; break;
                case 3: vWall[c.x, c.y] = false; break;
            }
            visited[pick.cell.x, pick.cell.y] = true;
            stack.Push(pick.cell);
        }
        return (hWall, vWall);
    }

    static List<Vector2Int> BfsPath(bool[,] hWall, bool[,] vWall, Vector2Int start, Vector2Int end)
    {
        var parent = new Vector2Int[W, H];
        var reached = new bool[W, H];
        for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) parent[x, y] = new Vector2Int(-1, -1);
        var q = new Queue<Vector2Int>();
        q.Enqueue(start);
        reached[start.x, start.y] = true;

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            if (c == end) break;

            if (c.y < H - 1 && !hWall[c.x, c.y + 1] && !reached[c.x, c.y + 1])
            { reached[c.x, c.y + 1] = true; parent[c.x, c.y + 1] = c; q.Enqueue(new Vector2Int(c.x, c.y + 1)); }
            if (c.x < W - 1 && !vWall[c.x + 1, c.y] && !reached[c.x + 1, c.y])
            { reached[c.x + 1, c.y] = true; parent[c.x + 1, c.y] = c; q.Enqueue(new Vector2Int(c.x + 1, c.y)); }
            if (c.y > 0 && !hWall[c.x, c.y] && !reached[c.x, c.y - 1])
            { reached[c.x, c.y - 1] = true; parent[c.x, c.y - 1] = c; q.Enqueue(new Vector2Int(c.x, c.y - 1)); }
            if (c.x > 0 && !vWall[c.x, c.y] && !reached[c.x - 1, c.y])
            { reached[c.x - 1, c.y] = true; parent[c.x - 1, c.y] = c; q.Enqueue(new Vector2Int(c.x - 1, c.y)); }
        }

        var path = new List<Vector2Int>();
        var cur = end;
        int safety = 0;
        while (cur.x >= 0 && safety < W * H + 5)
        {
            path.Add(cur);
            cur = parent[cur.x, cur.y];
            safety++;
        }
        path.Reverse();
        return path;
    }

    static List<(Vector2Int from, Vector2Int to)> PickGates(List<Vector2Int> path)
    {
        var gates = new List<(Vector2Int, Vector2Int)>();
        int len = path.Count;
        if (len < 2) return gates;
        for (int g = 1; g <= GateCount; g++)
        {
            int idx = (len * g) / (GateCount + 1);
            idx = Mathf.Clamp(idx, 1, len - 2);
            gates.Add((path[idx], path[idx + 1]));
        }
        return gates;
    }

    // =======================================================
    // Scene
    // =======================================================

    static void BuildScene(MazeQuestionDatabase db, Sprite sprite)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Camera (use the default Main Camera that comes with NewSceneSetup.DefaultGameObjects)
        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera", typeof(Camera));
            camGO.tag = "MainCamera";
            cam = camGO.GetComponent<Camera>();
        }
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(W, H) / 2f + 2f;
        cam.transform.position = new Vector3(W / 2f, H / 2f, -10f);
        cam.backgroundColor = NavyBg;
        cam.clearFlags = CameraClearFlags.SolidColor;

        // Maze data
        var (hWall, vWall) = GenerateMaze();
        var path = BfsPath(hWall, vWall, new Vector2Int(0, 0), new Vector2Int(W - 1, H - 1));
        var gateDoors = PickGates(path);

        // Maze parent
        var mazeRoot = new GameObject("Maze").transform;

        // Walls
        for (int x = 0; x < W; x++)
            for (int y = 0; y <= H; y++)
                if (hWall[x, y])
                    CreateBoxSprite(sprite, mazeRoot, $"HW_{x}_{y}",
                        new Vector2(x + 0.5f, y),
                        new Vector2(1f + WallThickness, WallThickness),
                        WallColor, sortingOrder: 1, isTrigger: false);

        for (int x = 0; x <= W; x++)
            for (int y = 0; y < H; y++)
                if (vWall[x, y])
                    CreateBoxSprite(sprite, mazeRoot, $"VW_{x}_{y}",
                        new Vector2(x, y + 0.5f),
                        new Vector2(WallThickness, 1f + WallThickness),
                        WallColor, sortingOrder: 1, isTrigger: false);

        // Gates
        for (int i = 0; i < gateDoors.Count; i++)
        {
            var (from, to) = gateDoors[i];
            Vector2 pos;
            Vector2 size;
            if (from.x == to.x)
            {
                int wallY = Mathf.Max(from.y, to.y);
                pos = new Vector2(from.x + 0.5f, wallY);
                size = new Vector2(1f - WallThickness, WallThickness * 1.8f);
            }
            else
            {
                int wallX = Mathf.Max(from.x, to.x);
                pos = new Vector2(wallX, from.y + 0.5f);
                size = new Vector2(WallThickness * 1.8f, 1f - WallThickness);
            }
            var gateGO = CreateBoxSprite(sprite, mazeRoot, $"Gate_{i}",
                pos, size, GateColor, sortingOrder: 2, isTrigger: true);
            var gate = gateGO.AddComponent<MazeRunnerGate>();
            gate.gateIndex = i;
        }

        // Goal at end cell
        var goalGO = CreateBoxSprite(sprite, mazeRoot, "Goal",
            new Vector2(W - 0.5f, H - 0.5f),
            new Vector2(0.55f, 0.55f),
            GoalColor, sortingOrder: 5, isTrigger: true);
        goalGO.AddComponent<MazeRunnerGoal>();

        // Player at start cell
        var playerGO = new GameObject("Player");
        playerGO.transform.position = new Vector3(0.5f, 0.5f, 0f);
        playerGO.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        var psr = playerGO.AddComponent<SpriteRenderer>();
        psr.sprite = sprite;
        psr.color = PlayerColor;
        psr.sortingOrder = 10;
        var prb = playerGO.AddComponent<Rigidbody2D>();
        prb.gravityScale = 0f;
        prb.constraints = RigidbodyConstraints2D.FreezeRotation;
        prb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var pcc = playerGO.AddComponent<CircleCollider2D>();
        pcc.radius = 0.5f;
        var pmover = playerGO.AddComponent<MazeRunnerPlayer>();

        // Canvas + UI
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGO = new GameObject("Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // HUD (top bar)
        var hud = MakeImage("HudBar", canvasGO.transform, new Color(0f, 0f, 0f, 0.45f));
        var hudRT = hud.GetComponent<RectTransform>();
        hudRT.anchorMin = new Vector2(0, 1); hudRT.anchorMax = new Vector2(1, 1);
        hudRT.pivot = new Vector2(0.5f, 1); hudRT.anchoredPosition = Vector2.zero;
        hudRT.sizeDelta = new Vector2(0, 90);

        var timerText = MakeText("TimerText", hud.transform, "00:00",
            48, FontStyles.Bold, White);
        var tRT = timerText.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0.5f, 0.5f); tRT.anchorMax = new Vector2(0.5f, 0.5f);
        tRT.sizeDelta = new Vector2(300, 70);
        timerText.alignment = TextAlignmentOptions.Center;

        var scoreText = MakeText("ScoreText", hud.transform, "Score: 0",
            38, FontStyles.Bold, White);
        var sRT = scoreText.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0, 0.5f); sRT.anchorMax = new Vector2(0, 0.5f);
        sRT.pivot = new Vector2(0, 0.5f);
        sRT.anchoredPosition = new Vector2(60, 0);
        sRT.sizeDelta = new Vector2(420, 70);
        scoreText.alignment = TextAlignmentOptions.Left;

        var gatesText = MakeText("GatesText", hud.transform, $"Gates: 0 / {GateCount}",
            38, FontStyles.Bold, White);
        var gRT = gatesText.GetComponent<RectTransform>();
        gRT.anchorMin = new Vector2(1, 0.5f); gRT.anchorMax = new Vector2(1, 0.5f);
        gRT.pivot = new Vector2(1, 0.5f);
        gRT.anchoredPosition = new Vector2(-60, 0);
        gRT.sizeDelta = new Vector2(420, 70);
        gatesText.alignment = TextAlignmentOptions.Right;

        // Hint text bottom-left
        var hint = MakeText("HintText", canvasGO.transform,
            "Move with WASD or arrow keys. Answer at gates.",
            26, FontStyles.Italic, new Color(1f, 1f, 1f, 0.75f));
        var hRT = hint.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0, 0); hRT.anchorMax = new Vector2(0, 0);
        hRT.pivot = new Vector2(0, 0);
        hRT.anchoredPosition = new Vector2(40, 30);
        hRT.sizeDelta = new Vector2(900, 40);
        hint.alignment = TextAlignmentOptions.Left;

        // Question popup
        var qPanel = MakeImage("QuestionPanel", canvasGO.transform, PanelDim);
        Stretch(qPanel);

        var qCard = MakeImage("QuestionCard", qPanel.transform, CardBg);
        var qcRT = qCard.GetComponent<RectTransform>();
        qcRT.anchorMin = new Vector2(0.5f, 0.5f); qcRT.anchorMax = new Vector2(0.5f, 0.5f);
        qcRT.pivot = new Vector2(0.5f, 0.5f);
        qcRT.anchoredPosition = Vector2.zero;
        qcRT.sizeDelta = new Vector2(1320, 760);

        var qCounter = MakeText("QuestionCounterText", qCard.transform, "Gate 1 / 5",
            32, FontStyles.Bold, Green);
        var qcoRT = qCounter.GetComponent<RectTransform>();
        qcoRT.anchorMin = new Vector2(0.5f, 1); qcoRT.anchorMax = new Vector2(0.5f, 1);
        qcoRT.pivot = new Vector2(0.5f, 1);
        qcoRT.anchoredPosition = new Vector2(0, -36);
        qcoRT.sizeDelta = new Vector2(800, 50);

        var qText = MakeText("QuestionText", qCard.transform, "Question?",
            42, FontStyles.Bold, TextOnCard);
        var qtRT = qText.GetComponent<RectTransform>();
        qtRT.anchorMin = new Vector2(0.5f, 1); qtRT.anchorMax = new Vector2(0.5f, 1);
        qtRT.pivot = new Vector2(0.5f, 1);
        qtRT.anchoredPosition = new Vector2(0, -120);
        qtRT.sizeDelta = new Vector2(1180, 240);
        qText.alignment = TextAlignmentOptions.Center;
        qText.textWrappingMode = TextWrappingModes.Normal;

        var answerGridGO = new GameObject("AnswerGrid",
            typeof(RectTransform), typeof(GridLayoutGroup));
        answerGridGO.transform.SetParent(qCard.transform, false);
        var agRT = answerGridGO.GetComponent<RectTransform>();
        agRT.anchorMin = new Vector2(0.5f, 0); agRT.anchorMax = new Vector2(0.5f, 0);
        agRT.pivot = new Vector2(0.5f, 0);
        agRT.anchoredPosition = new Vector2(0, 110);
        agRT.sizeDelta = new Vector2(1180, 240);
        var glg = answerGridGO.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(580, 110);
        glg.spacing = new Vector2(20, 20);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.childAlignment = TextAnchor.MiddleCenter;
        for (int i = 0; i < 4; i++)
            MakeButton($"AnswerButton{i + 1}", answerGridGO.transform,
                "Option", new Color(0.94f, 0.95f, 1f), TextOnCard, 30);

        var feedbackText = MakeText("FeedbackText", qCard.transform, "",
            32, FontStyles.Bold, Green);
        var fRT = feedbackText.GetComponent<RectTransform>();
        fRT.anchorMin = new Vector2(0.5f, 0); fRT.anchorMax = new Vector2(0.5f, 0);
        fRT.pivot = new Vector2(0.5f, 0);
        fRT.anchoredPosition = new Vector2(0, 40);
        fRT.sizeDelta = new Vector2(1180, 60);

        qPanel.gameObject.SetActive(false);

        // Win screen
        var winPanel = MakeImage("WinPanel", canvasGO.transform, NavyBg);
        Stretch(winPanel);
        var winTitle = MakeText("WinTitle", winPanel.transform, "MAZE COMPLETE",
            80, FontStyles.Bold, White);
        SetAnchor(winTitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        winTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
        winTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 110);

        var finalScore = MakeText("FinalScoreText", winPanel.transform, "0",
            160, FontStyles.Bold, Green);
        SetAnchor(finalScore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        finalScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 110);
        finalScore.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 220);

        var finalTime = MakeText("FinalTimeText", winPanel.transform, "Time: 00:00",
            44, FontStyles.Normal, White);
        SetAnchor(finalTime, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        finalTime.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -90);
        finalTime.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 70);

        var restartBtn = MakeButton("RestartButton", winPanel.transform, "PLAY AGAIN",
            Green, White, 44);
        var rRT = restartBtn.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0.5f, 0); rRT.anchorMax = new Vector2(0.5f, 0);
        rRT.pivot = new Vector2(0.5f, 0);
        rRT.anchoredPosition = new Vector2(-260, 250);
        rRT.sizeDelta = new Vector2(440, 130);

        var hubBtn = MakeButton("HubButton", winPanel.transform, "BACK TO HUB",
            new Color(0.3f, 0.3f, 0.5f), White, 44);
        var hbRT = hubBtn.GetComponent<RectTransform>();
        hbRT.anchorMin = new Vector2(0.5f, 0); hbRT.anchorMax = new Vector2(0.5f, 0);
        hbRT.pivot = new Vector2(0.5f, 0);
        hbRT.anchoredPosition = new Vector2(260, 250);
        hbRT.sizeDelta = new Vector2(440, 130);

        winPanel.gameObject.SetActive(false);

        // Managers
        var gmGO = new GameObject("GameManager");
        var gameManager = gmGO.AddComponent<MazeRunnerGameManager>();

        var dbLive = AssetDatabase.LoadAssetAtPath<MazeQuestionDatabase>(DatabasePath);
        if (dbLive == null)
        {
            Debug.LogError($"Failed to reload {DatabasePath}.");
            dbLive = db;
        }
        gameManager.questionDatabase = dbLive;
        gameManager.player = pmover;
        gameManager.totalGates = GateCount;
        var gmSO = new SerializedObject(gameManager);
        gmSO.FindProperty("questionDatabase").objectReferenceValue = dbLive;
        gmSO.FindProperty("player").objectReferenceValue = pmover;
        gmSO.FindProperty("totalGates").intValue = GateCount;
        gmSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);

        var uiGO = new GameObject("UIManager");
        var uiManager = uiGO.AddComponent<MazeRunnerUIManager>();
        uiManager.timerText = timerText;
        uiManager.scoreText = scoreText;
        uiManager.gatesText = gatesText;
        uiManager.questionPanel = qPanel.gameObject;
        uiManager.questionText = qText;
        uiManager.questionCounterText = qCounter;
        uiManager.answerGrid = answerGridGO.transform;
        uiManager.feedbackText = feedbackText;
        uiManager.correctColor = Green;
        uiManager.wrongColor = Red;
        uiManager.winPanel = winPanel.gameObject;
        uiManager.finalScoreText = finalScore;
        uiManager.finalTimeText = finalTime;
        uiManager.restartButton = restartBtn;
        uiManager.hubButton = hubBtn;
        EditorUtility.SetDirty(uiManager);

        UnityEventTools.AddPersistentListener(restartBtn.onClick, gameManager.RestartGame);
        UnityEventTools.AddPersistentListener(hubBtn.onClick, gameManager.ReturnToHub);

        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static GameObject CreateBoxSprite(Sprite sprite, Transform parent, string name,
        Vector2 position, Vector2 size, Color color, int sortingOrder, bool isTrigger)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = isTrigger;
        return go;
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
