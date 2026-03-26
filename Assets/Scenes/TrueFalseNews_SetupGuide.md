# Scene Setup Guide — True or False News (Game 1)

## 1. Create the Scene
- File > New Scene > Save as `Assets/Scenes/TrueFalseNews.unity`
- Add this scene to Build Settings

---

## 2. Create the Data Assets
1. Right-click `Assets/Data/Headlines/` > Create > BrainCitizen > **Headline Database** → name it `HeadlineDatabase`
2. For each headline in `SampleHeadlines.md`, Right-click > Create > BrainCitizen > **Headline** and fill in the fields
3. Drag all headline assets into the `HeadlineDatabase.headlines` array

---

## 3. GameObjects Hierarchy

```
Scene
├── GameManager          [TrueFalseGameManager]
├── UIManager            [TrueFalseUIManager]
└── Canvas (Screen Space - Overlay)
    ├── QuestionPanel
    │   ├── CategoryText        (TMP)
    │   ├── HeadlineText        (TMP, large)
    │   ├── SourceHintText      (TMP, small italic)
    │   └── QuestionCounterText (TMP)
    ├── TimerPanel
    │   └── TimerSlider         (Slider, horizontal)
    ├── AnswerPanel
    │   ├── RealButton          (Button + TMP "REAL ✓")
    │   └── FakeButton          (Button + TMP "FAKE ✗")
    ├── FeedbackPanel           (hidden by default)
    │   ├── FeedbackResultText  (TMP, large, bold)
    │   ├── ExplanationText     (TMP)
    │   └── StreakText          (TMP)
    ├── HUD
    │   ├── ScoreText           (TMP)
    │   └── StreakHUDText       (TMP)
    └── EndScreenPanel          (hidden by default)
        ├── FinalScoreText      (TMP)
        ├── EndMessageText      (TMP)
        ├── RestartButton       (Button)
        └── HubButton           (Button)
```

---

## 4. Wire Up Components

### GameManager GameObject
- Component: `TrueFalseGameManager`
- Set `Headline Database` → drag `HeadlineDatabase` asset
- Set `Headlines Per Round` = 10
- Set `Time Per Question` = 5
- Set `Explanation Display Time` = 3

### UIManager GameObject
- Component: `TrueFalseUIManager`
- Drag all UI elements into their matching Inspector slots

### Buttons
- `RealButton.onClick` → call `TrueFalseGameManager.OnPlayerAnswer(true)`
- `FakeButton.onClick` → call `TrueFalseGameManager.OnPlayerAnswer(false)`
- `RestartButton.onClick` → call `TrueFalseGameManager.RestartGame()`
- `HubButton.onClick` → call `TrueFalseGameManager.ReturnToHub()`

---

## 5. TextMeshPro
- Install via Window > Package Manager > TextMeshPro
- Import TMP Essentials when prompted

---

## 6. Suggested Colors (from Brain Citizen Academy theme)
| Element | Color |
|---------|-------|
| Background | `#1E2761` (dark navy) |
| Headline panel | `#FFFFFF` with `#1E2761` text |
| REAL button | `#27AE60` green |
| FAKE button | `#E74C3C` red |
| Timer fill (full) | `#2ECC71` |
| Timer fill (low)  | `#E74C3C` |
| Correct feedback  | `#27AE60` |
| Wrong feedback    | `#E74C3C` |
