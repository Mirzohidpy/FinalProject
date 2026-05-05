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
/// One-click builder for Emotion Identifier (Game 8).
/// Procedurally draws 12 emotion face PNGs, imports them as Sprites, creates
/// EmotionData assets, builds the scene, unlocks the hub card.
/// Run via menu: BrainCitizen > Build EmotionID Game.
/// </summary>
public static class EmotionSceneBuilder
{
    const string ScenePath = "Assets/Scenes/EmotionID.unity";
    const string DataDir = "Assets/Data/Emotions";
    const string TexturesDir = DataDir + "/Textures";
    const string DatabasePath = DataDir + "/EmotionDatabase.asset";
    const string HubGameInfoPath = "Assets/Data/Hub/Game08_EmotionID.asset";

    const int FaceSize = 256;

    static readonly Color NavyBg     = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color White      = Color.white;
    static readonly Color GreenReal  = new Color(0.153f, 0.682f, 0.376f);
    static readonly Color RedFake    = new Color(0.906f, 0.298f, 0.235f);
    static readonly Color TimerFull  = new Color(0.180f, 0.800f, 0.443f);
    static readonly Color TimerLow   = new Color(0.906f, 0.298f, 0.235f);
    static readonly Color TextPrimary = new Color(0.118f, 0.153f, 0.380f);
    static readonly Color PanelDim   = new Color(0f, 0f, 0f, 0.78f);
    static readonly Color EasyTag    = new Color(0.18f, 0.80f, 0.44f);
    static readonly Color MediumTag  = new Color(0.95f, 0.70f, 0.10f);
    static readonly Color HardTag    = new Color(0.91f, 0.30f, 0.24f);

    enum EyeStyle { Normal, Wide, Closed, Squint, Heart }
    enum MouthStyle { Smile, Frown, Flat, OpenO, Small, Wavy }
    enum BrowStyle { None, Angry, Concerned, Flat }

    struct EmotionDef
    {
        public string Slug;
        public string Name;
        public EmotionDifficulty Difficulty;
        public string Tip;
        public EyeStyle LeftEye;
        public EyeStyle RightEye;
        public MouthStyle Mouth;
        public BrowStyle Brows;
        public bool Tear;
        public bool Blush;
    }

    static readonly EmotionDef[] EMOTIONS = new[]
    {
        new EmotionDef { Slug="01_Happy",       Name="Happy",       Difficulty=EmotionDifficulty.Easy,   Tip="Sharing what makes you happy lifts others' mood too.",
            LeftEye=EyeStyle.Normal, RightEye=EyeStyle.Normal, Mouth=MouthStyle.Smile, Brows=BrowStyle.None },
        new EmotionDef { Slug="02_Sad",         Name="Sad",         Difficulty=EmotionDifficulty.Easy,   Tip="It is okay to feel sad. Talking to someone helps.",
            LeftEye=EyeStyle.Normal, RightEye=EyeStyle.Normal, Mouth=MouthStyle.Frown, Brows=BrowStyle.Concerned, Tear=true },
        new EmotionDef { Slug="03_Angry",       Name="Angry",       Difficulty=EmotionDifficulty.Easy,   Tip="Take a slow breath. Anger fades faster than we expect.",
            LeftEye=EyeStyle.Squint, RightEye=EyeStyle.Squint, Mouth=MouthStyle.Flat, Brows=BrowStyle.Angry },
        new EmotionDef { Slug="04_Surprised",   Name="Surprised",   Difficulty=EmotionDifficulty.Easy,   Tip="Sudden change is normal. Pause before reacting.",
            LeftEye=EyeStyle.Wide, RightEye=EyeStyle.Wide, Mouth=MouthStyle.OpenO, Brows=BrowStyle.None },
        new EmotionDef { Slug="05_Neutral",     Name="Neutral",     Difficulty=EmotionDifficulty.Easy,   Tip="Calm is a feeling too - protect your peace.",
            LeftEye=EyeStyle.Normal, RightEye=EyeStyle.Normal, Mouth=MouthStyle.Flat, Brows=BrowStyle.None },
        new EmotionDef { Slug="06_Tired",       Name="Tired",       Difficulty=EmotionDifficulty.Easy,   Tip="Rest is part of progress, not a setback.",
            LeftEye=EyeStyle.Closed, RightEye=EyeStyle.Closed, Mouth=MouthStyle.Small, Brows=BrowStyle.None },
        new EmotionDef { Slug="07_Confused",    Name="Confused",    Difficulty=EmotionDifficulty.Medium, Tip="Asking why is a sign of growth, not weakness.",
            LeftEye=EyeStyle.Normal, RightEye=EyeStyle.Squint, Mouth=MouthStyle.Wavy, Brows=BrowStyle.Concerned },
        new EmotionDef { Slug="08_Afraid",      Name="Afraid",      Difficulty=EmotionDifficulty.Medium, Tip="Naming a fear out loud often makes it smaller.",
            LeftEye=EyeStyle.Wide, RightEye=EyeStyle.Wide, Mouth=MouthStyle.Frown, Brows=BrowStyle.Concerned },
        new EmotionDef { Slug="09_Proud",       Name="Proud",       Difficulty=EmotionDifficulty.Medium, Tip="Celebrate small wins - they build into big ones.",
            LeftEye=EyeStyle.Closed, RightEye=EyeStyle.Closed, Mouth=MouthStyle.Smile, Brows=BrowStyle.Flat },
        new EmotionDef { Slug="10_InLove",      Name="In Love",     Difficulty=EmotionDifficulty.Hard,   Tip="Healthy love includes respect and clear words.",
            LeftEye=EyeStyle.Heart, RightEye=EyeStyle.Heart, Mouth=MouthStyle.Smile, Brows=BrowStyle.None },
        new EmotionDef { Slug="11_Disgusted",   Name="Disgusted",   Difficulty=EmotionDifficulty.Hard,   Tip="Discomfort points to your values. Listen to it.",
            LeftEye=EyeStyle.Squint, RightEye=EyeStyle.Normal, Mouth=MouthStyle.Frown, Brows=BrowStyle.Angry },
        new EmotionDef { Slug="12_Embarrassed", Name="Embarrassed", Difficulty=EmotionDifficulty.Hard,   Tip="Everyone has been there. It passes faster than you think.",
            LeftEye=EyeStyle.Normal, RightEye=EyeStyle.Normal, Mouth=MouthStyle.Small, Brows=BrowStyle.None, Blush=true },
    };

    [MenuItem("BrainCitizen/Build EmotionID Game")]
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

        if (!Directory.Exists(TexturesDir)) Directory.CreateDirectory(TexturesDir);

        GenerateAllFaces();
        var db = BuildEmotionAssets();
        if (db == null) return;
        BuildScene(db);
        UnlockInHub();
        AddToBuildSettings();

        EditorUtility.DisplayDialog(
            "Emotion Identifier built",
            $"Scene saved to {ScenePath}\n" +
            $"{db.emotions.Length} emotions in {DataDir}.\n\n" +
            "Press Play.",
            "OK");
    }

    // =======================================================
    // Procedural face generation
    // =======================================================

    static void GenerateAllFaces()
    {
        foreach (var def in EMOTIONS)
        {
            string path = $"{TexturesDir}/{def.Slug}.png";
            var pixels = DrawFace(def);
            var tex = new Texture2D(FaceSize, FaceSize, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100f;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.filterMode = FilterMode.Bilinear;
                imp.SaveAndReimport();
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static Color[] DrawFace(EmotionDef def)
    {
        var px = new Color[FaceSize * FaceSize];
        Color transparent = new Color(0, 0, 0, 0);
        for (int i = 0; i < px.Length; i++) px[i] = transparent;

        int cx = FaceSize / 2;
        int cy = FaceSize / 2;
        int r = FaceSize / 2 - 8;

        Color faceFill   = new Color(1.00f, 0.84f, 0.18f);
        Color faceShade  = new Color(0.95f, 0.72f, 0.10f);
        Color faceOutline = new Color(0.50f, 0.34f, 0.05f);

        FillCircle(px, cx, cy, r, faceFill);
        FillEllipse(px, cx, cy - 38, r - 22, 22, faceShade);
        StrokeCircle(px, cx, cy, r, 3, faceOutline);

        int eyeY = cy + 22;
        int eyeXOff = 38;
        int mouthY = cy - 38;
        int browY = eyeY + 32;

        if (def.Brows != BrowStyle.None)
            DrawBrows(px, cx, browY, eyeXOff, def.Brows);

        DrawEye(px, cx - eyeXOff, eyeY, def.LeftEye);
        DrawEye(px, cx + eyeXOff, eyeY, def.RightEye);
        DrawMouth(px, cx, mouthY, def.Mouth);

        if (def.Tear)
            DrawTear(px, cx - eyeXOff - 4, eyeY - 16);
        if (def.Blush)
            DrawBlush(px, cx, eyeY - 24, eyeXOff);

        return px;
    }

    static void DrawEye(Color[] px, int cx, int cy, EyeStyle style)
    {
        Color black = new Color(0.10f, 0.10f, 0.12f);
        Color white = Color.white;
        Color heart = new Color(0.95f, 0.30f, 0.40f);

        switch (style)
        {
            case EyeStyle.Normal:
                FillEllipse(px, cx, cy, 8, 11, black);
                FillEllipse(px, cx + 2, cy + 3, 3, 4, white);
                break;
            case EyeStyle.Wide:
                FillCircle(px, cx, cy, 14, white);
                StrokeCircle(px, cx, cy, 14, 2, black);
                FillCircle(px, cx, cy, 7, black);
                FillCircle(px, cx + 3, cy + 3, 2, white);
                break;
            case EyeStyle.Closed:
                DrawArcStroke(px, cx, cy + 2, 13, Mathf.PI * 1.05f, Mathf.PI * 1.95f, 3, black);
                break;
            case EyeStyle.Squint:
                DrawLine(px, cx - 13, cy, cx + 13, cy, 3, black);
                break;
            case EyeStyle.Heart:
                FillCircle(px, cx - 6, cy + 3, 8, heart);
                FillCircle(px, cx + 6, cy + 3, 8, heart);
                FillTriangle(px, cx - 13, cy + 3, cx + 13, cy + 3, cx, cy - 14, heart);
                break;
        }
    }

    static void DrawMouth(Color[] px, int cx, int cy, MouthStyle style)
    {
        Color black = new Color(0.10f, 0.10f, 0.12f);
        Color tongue = new Color(0.85f, 0.30f, 0.40f);

        switch (style)
        {
            case MouthStyle.Smile:
                DrawArcStroke(px, cx, cy + 14, 32, Mathf.PI * 1.05f, Mathf.PI * 1.95f, 4, black);
                break;
            case MouthStyle.Frown:
                DrawArcStroke(px, cx, cy - 18, 32, Mathf.PI * 0.05f, Mathf.PI * 0.95f, 4, black);
                break;
            case MouthStyle.Flat:
                DrawLine(px, cx - 22, cy, cx + 22, cy, 4, black);
                break;
            case MouthStyle.OpenO:
                FillEllipse(px, cx, cy - 4, 14, 18, black);
                FillEllipse(px, cx, cy - 6, 8, 12, tongue);
                break;
            case MouthStyle.Small:
                DrawLine(px, cx - 8, cy, cx + 8, cy, 3, black);
                break;
            case MouthStyle.Wavy:
                for (int i = -22; i <= 22; i++)
                {
                    int x = cx + i;
                    int y = cy + Mathf.RoundToInt(3 * Mathf.Sin(i * 0.4f));
                    FillCircle(px, x, y, 2, black);
                }
                break;
        }
    }

    static void DrawBrows(Color[] px, int cx, int browY, int eyeXOff, BrowStyle style)
    {
        Color black = new Color(0.10f, 0.10f, 0.12f);
        int len = 18;
        int slope = 7;
        switch (style)
        {
            case BrowStyle.Angry:
                DrawLine(px, cx - eyeXOff - len, browY + slope, cx - eyeXOff + len, browY - slope, 4, black);
                DrawLine(px, cx + eyeXOff - len, browY - slope, cx + eyeXOff + len, browY + slope, 4, black);
                break;
            case BrowStyle.Concerned:
                DrawLine(px, cx - eyeXOff - len, browY - slope, cx - eyeXOff + len, browY + slope, 4, black);
                DrawLine(px, cx + eyeXOff - len, browY + slope, cx + eyeXOff + len, browY - slope, 4, black);
                break;
            case BrowStyle.Flat:
                DrawLine(px, cx - eyeXOff - len, browY, cx - eyeXOff + len, browY, 4, black);
                DrawLine(px, cx + eyeXOff - len, browY, cx + eyeXOff + len, browY, 4, black);
                break;
        }
    }

    static void DrawTear(Color[] px, int x, int y)
    {
        Color blue = new Color(0.30f, 0.65f, 0.95f);
        Color shine = new Color(0.75f, 0.90f, 1.00f);
        FillEllipse(px, x, y - 6, 5, 9, blue);
        FillCircle(px, x + 1, y - 4, 2, shine);
    }

    static void DrawBlush(Color[] px, int cx, int y, int eyeXOff)
    {
        Color pink = new Color(0.95f, 0.55f, 0.65f, 0.85f);
        FillEllipse(px, cx - eyeXOff - 18, y, 13, 7, pink);
        FillEllipse(px, cx + eyeXOff + 18, y, 13, 7, pink);
    }

    // ---------- pixel primitives ----------

    static void FillCircle(Color[] px, int cx, int cy, int r, Color c)
    {
        if (r <= 0) return;
        int rsq = r * r;
        for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(FaceSize - 1, cy + r); y++)
        {
            int dy = y - cy;
            int dxMax = Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(0, rsq - dy * dy)));
            for (int x = Mathf.Max(0, cx - dxMax); x <= Mathf.Min(FaceSize - 1, cx + dxMax); x++)
                px[y * FaceSize + x] = c;
        }
    }

    static void StrokeCircle(Color[] px, int cx, int cy, int r, int thickness, Color c)
    {
        int rIn = r - thickness;
        int rOutSq = r * r;
        int rInSq = rIn * rIn;
        for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(FaceSize - 1, cy + r); y++)
        {
            int dy = y - cy;
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(FaceSize - 1, cx + r); x++)
            {
                int dx = x - cx;
                int d = dx * dx + dy * dy;
                if (d <= rOutSq && d >= rInSq) px[y * FaceSize + x] = c;
            }
        }
    }

    static void FillEllipse(Color[] px, int cx, int cy, int rx, int ry, Color c)
    {
        if (rx <= 0 || ry <= 0) return;
        for (int y = Mathf.Max(0, cy - ry); y <= Mathf.Min(FaceSize - 1, cy + ry); y++)
        {
            for (int x = Mathf.Max(0, cx - rx); x <= Mathf.Min(FaceSize - 1, cx + rx); x++)
            {
                float u = (x - cx) / (float)rx;
                float v = (y - cy) / (float)ry;
                if (u * u + v * v <= 1f) px[y * FaceSize + x] = c;
            }
        }
    }

    static void DrawArcStroke(Color[] px, int cx, int cy, int r, float a0, float a1, int thickness, Color c)
    {
        int steps = Mathf.Max(48, Mathf.RoundToInt(r * Mathf.Abs(a1 - a0)));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float a = a0 + (a1 - a0) * t;
            int x = cx + Mathf.RoundToInt(Mathf.Cos(a) * r);
            int y = cy + Mathf.RoundToInt(Mathf.Sin(a) * r);
            FillCircle(px, x, y, thickness, c);
        }
    }

    static void DrawLine(Color[] px, int x0, int y0, int x1, int y1, int thickness, Color c)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) + 1;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = x0 + Mathf.RoundToInt((x1 - x0) * t);
            int y = y0 + Mathf.RoundToInt((y1 - y0) * t);
            FillCircle(px, x, y, thickness, c);
        }
    }

    static void FillTriangle(Color[] px, int x0, int y0, int x1, int y1, int x2, int y2, Color c)
    {
        if (y1 < y0) { (x0, y0, x1, y1) = (x1, y1, x0, y0); }
        if (y2 < y0) { (x0, y0, x2, y2) = (x2, y2, x0, y0); }
        if (y2 < y1) { (x1, y1, x2, y2) = (x2, y2, x1, y1); }
        for (int y = y0; y <= y2; y++)
        {
            if (y < 0 || y >= FaceSize) continue;
            int xa, xb;
            float t1 = y2 == y0 ? 0 : (y - y0) / (float)(y2 - y0);
            xa = Mathf.RoundToInt(Mathf.Lerp(x0, x2, t1));
            if (y < y1)
            {
                float t2 = y1 == y0 ? 0 : (y - y0) / (float)(y1 - y0);
                xb = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t2));
            }
            else
            {
                float t2 = y2 == y1 ? 0 : (y - y1) / (float)(y2 - y1);
                xb = Mathf.RoundToInt(Mathf.Lerp(x1, x2, t2));
            }
            if (xa > xb) (xa, xb) = (xb, xa);
            for (int x = Mathf.Max(0, xa); x <= Mathf.Min(FaceSize - 1, xb); x++)
                px[y * FaceSize + x] = c;
        }
    }

    // =======================================================
    // EmotionData assets
    // =======================================================

    static EmotionDatabase BuildEmotionAssets()
    {
        if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);

        var assets = new List<EmotionData>();
        foreach (var def in EMOTIONS)
        {
            string pngPath = $"{TexturesDir}/{def.Slug}.png";
            string assetPath = $"{DataDir}/{def.Slug}.asset";

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (sprite == null)
            {
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(pngPath))
                    if (sub is Sprite s) { sprite = s; break; }
            }
            if (sprite == null)
                Debug.LogWarning($"Sprite at {pngPath} returned null - face will render blank.");

            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var emo = ScriptableObject.CreateInstance<EmotionData>();
            emo.emotionName = def.Name;
            emo.faceSprite = sprite;
            emo.difficulty = def.Difficulty;
            emo.mentalHealthTip = def.Tip;
            AssetDatabase.CreateAsset(emo, assetPath);

            var so = new SerializedObject(emo);
            so.FindProperty("emotionName").stringValue = def.Name;
            so.FindProperty("faceSprite").objectReferenceValue = sprite;
            so.FindProperty("difficulty").enumValueIndex = (int)def.Difficulty;
            so.FindProperty("mentalHealthTip").stringValue = def.Tip;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(emo);
            assets.Add(emo);
        }

        var db = AssetDatabase.LoadAssetAtPath<EmotionDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<EmotionDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }
        db.emotions = assets.ToArray();
        EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return db;
    }

    // =======================================================
    // Scene
    // =======================================================

    static void BuildScene(EmotionDatabase db)
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

        // HUD
        var scoreText = MakeText("ScoreText", canvasGO.transform, "Score: 0",
            40, FontStyles.Bold, White);
        var sRT = scoreText.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(0, 1);
        sRT.pivot = new Vector2(0, 0.5f);
        sRT.anchoredPosition = new Vector2(60, -55);
        sRT.sizeDelta = new Vector2(420, 60);
        scoreText.alignment = TextAlignmentOptions.Left;

        var streakHUDText = MakeText("StreakHUDText", canvasGO.transform, "",
            36, FontStyles.Bold, GreenReal);
        var stRT = streakHUDText.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(1, 1); stRT.anchorMax = new Vector2(1, 1);
        stRT.pivot = new Vector2(1, 0.5f);
        stRT.anchoredPosition = new Vector2(-60, -55);
        stRT.sizeDelta = new Vector2(400, 60);
        streakHUDText.alignment = TextAlignmentOptions.Right;

        // Question header
        var questionCounterText = MakeText("QuestionCounterText", canvasGO.transform,
            "Question 1 / 10", 36, FontStyles.Bold, White);
        var qcRT = questionCounterText.GetComponent<RectTransform>();
        qcRT.anchorMin = new Vector2(0.5f, 1); qcRT.anchorMax = new Vector2(0.5f, 1);
        qcRT.pivot = new Vector2(1, 0.5f);
        qcRT.anchoredPosition = new Vector2(-30, -130);
        qcRT.sizeDelta = new Vector2(500, 50);
        questionCounterText.alignment = TextAlignmentOptions.Right;

        var difficultyText = MakeText("DifficultyText", canvasGO.transform, "EASY",
            36, FontStyles.Bold, EasyTag);
        var diffRT = difficultyText.GetComponent<RectTransform>();
        diffRT.anchorMin = new Vector2(0.5f, 1); diffRT.anchorMax = new Vector2(0.5f, 1);
        diffRT.pivot = new Vector2(0, 0.5f);
        diffRT.anchoredPosition = new Vector2(30, -130);
        diffRT.sizeDelta = new Vector2(300, 50);
        difficultyText.alignment = TextAlignmentOptions.Left;

        // Face panel
        var facePanel = MakeImage("FacePanel", canvasGO.transform, new Color(0.18f, 0.22f, 0.45f));
        SetAnchor(facePanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        facePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -460);
        facePanel.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 560);

        var faceImage = MakeImage("FaceImage", facePanel.transform, White);
        var fiRT = faceImage.GetComponent<RectTransform>();
        fiRT.anchorMin = Vector2.zero; fiRT.anchorMax = Vector2.one;
        fiRT.offsetMin = new Vector2(20, 20); fiRT.offsetMax = new Vector2(-20, -20);
        faceImage.preserveAspect = true;

        // Timer slider
        var timerSlider = MakeSlider("TimerSlider", canvasGO.transform, TimerFull);
        var timerRT = timerSlider.GetComponent<RectTransform>();
        SetAnchor(timerRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        timerRT.anchoredPosition = new Vector2(0, -780);
        timerRT.sizeDelta = new Vector2(720, 24);
        var timerFill = timerSlider.fillRect.GetComponent<Image>();

        // Answer grid (2x2)
        var answerGridGO = new GameObject("AnswerGrid",
            typeof(RectTransform), typeof(GridLayoutGroup));
        answerGridGO.transform.SetParent(canvasGO.transform, false);
        var agRT = answerGridGO.GetComponent<RectTransform>();
        agRT.anchorMin = new Vector2(0.5f, 0f); agRT.anchorMax = new Vector2(0.5f, 0f);
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
            answerButtons[i] = MakeButton($"AnswerButton{i + 1}", answerGridGO.transform,
                "Emotion", new Color(1f, 1f, 1f, 0.92f), TextPrimary, 36);

        // Feedback panel
        var feedbackPanel = MakeImage("FeedbackPanel", canvasGO.transform, PanelDim);
        Stretch(feedbackPanel);
        var feedbackResult = MakeText("FeedbackResultText", feedbackPanel.transform,
            "CORRECT!", 96, FontStyles.Bold, GreenReal);
        SetAnchor(feedbackResult, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        feedbackResult.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);
        feedbackResult.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 130);

        var correctAnswer = MakeText("CorrectAnswerText", feedbackPanel.transform, "",
            48, FontStyles.Normal, White);
        SetAnchor(correctAnswer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        correctAnswer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 90);
        correctAnswer.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 80);

        var bonusText = MakeText("BonusText", feedbackPanel.transform, "",
            36, FontStyles.Bold, GreenReal);
        SetAnchor(bonusText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        bonusText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        bonusText.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 60);

        var mentalTip = MakeText("MentalTipText", feedbackPanel.transform, "",
            34, FontStyles.Italic, new Color(1f, 1f, 1f, 0.92f));
        SetAnchor(mentalTip, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        mentalTip.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -140);
        mentalTip.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 200);
        mentalTip.textWrappingMode = TextWrappingModes.Normal;

        feedbackPanel.gameObject.SetActive(false);

        // End screen
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
        rRT.anchorMin = new Vector2(0.5f, 0); rRT.anchorMax = new Vector2(0.5f, 0);
        rRT.pivot = new Vector2(0.5f, 0);
        rRT.anchoredPosition = new Vector2(-260, 250);
        rRT.sizeDelta = new Vector2(440, 130);

        var hubBtn = MakeButton("HubButton", endPanel.transform, "BACK TO HUB",
            new Color(0.3f, 0.3f, 0.5f), White, 44);
        var hbRT = hubBtn.GetComponent<RectTransform>();
        hbRT.anchorMin = new Vector2(0.5f, 0); hbRT.anchorMax = new Vector2(0.5f, 0);
        hbRT.pivot = new Vector2(0.5f, 0);
        hbRT.anchoredPosition = new Vector2(260, 250);
        hbRT.sizeDelta = new Vector2(440, 130);

        endPanel.gameObject.SetActive(false);

        // Managers
        var gameManagerGO = new GameObject("GameManager");
        var gameManager = gameManagerGO.AddComponent<EmotionGameManager>();

        var dbLive = AssetDatabase.LoadAssetAtPath<EmotionDatabase>(DatabasePath);
        if (dbLive == null)
        {
            Debug.LogError($"Failed to reload {DatabasePath}.");
            dbLive = db;
        }
        gameManager.emotionDatabase = dbLive;
        var so = new SerializedObject(gameManager);
        so.FindProperty("emotionDatabase").objectReferenceValue = dbLive;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);

        var uiManagerGO = new GameObject("UIManager");
        var uiManager = uiManagerGO.AddComponent<EmotionUIManager>();
        uiManager.faceImage = faceImage;
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
        uiManager.mentalTipText = mentalTip;
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
