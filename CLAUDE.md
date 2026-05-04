# Brain Citizen Academy — Project Guide for Claude Code

A Unity 2D hub game with 10 educational mini-games. SE203 Game Development, Group Project, 2026.

This file is loaded automatically by Claude Code. Read it before suggesting changes.

## Tech stack

- **Unity 6** (`6000.4.5f1`) — see `ProjectSettings/ProjectVersion.txt`
- **uGUI 2.0** (`com.unity.ugui`) — bundles TextMeshPro
- C# / .NET — Unity's built-in
- No external runtime dependencies beyond Unity itself

## First-time setup after cloning

1. Open project in **Unity Hub** with version `6000.4.5f1` (install via Unity Hub if missing)
2. Wait for first import (1–2 min)
3. If prompted, **Import TMP Essentials** — required for any text to render
4. Press **Play** — you should land on the Hub scene with 10 game cards (some locked)

If the Hub scene isn't open, the play-mode start scene is set to `Assets/Scenes/HubScene.unity` so pressing Play always starts there.

## Folder structure

```
Assets/
  Data/                          ← all ScriptableObject data assets
    Flags/Textures/              ← flag PNGs from flagcdn.com
    Flags/                       ← FlagData.asset + FlagDatabase.asset
    Headlines/                   ← HeadlineData.asset + HeadlineDatabase.asset
    Hub/                         ← GameInfo.asset (one per game) + GameRegistry.asset
  Editor/                        ← editor-only scripts (scene builders, menu commands)
  Scenes/                        ← .unity files
    HubScene.unity               ← always entry point
    TrueFalseNews.unity          ← Game 1
    FlagQuiz.unity               ← Game 2
    (more added as games are built)
  Scripts/
    Core/                        ← cross-game code (HubManager, GameInfo, GameRegistry, HubCardEffect)
    Games/<GameName>/            ← one folder per game
ProjectSettings/                 ← Unity config — commit this
Packages/                        ← Unity package manifest — commit this
.gitignore                       ← Library/, Temp/, *.csproj etc — already configured
```

**Don't commit:** `Library/`, `Temp/`, `Logs/`, `UserSettings/`, generated `.csproj`/`.sln` files. The `.gitignore` handles this.

## Architecture pattern (every game follows this)

Each mini-game is a **separate scene** loaded from the Hub. Within a game scene:

- **Data** — ScriptableObjects for game content (questions, flags, headlines, etc.)
- **GameManager** — game logic only. Broadcasts via static `System.Action` events.
- **UIManager** — subscribes to GameManager events, drives all UI. Knows nothing about logic.

This decoupling is deliberate. Don't merge them, don't have UI poll the manager.

Reference implementation: `Assets/Scripts/Games/TrueFalseNews/`

## How to add a new game (the playbook)

Say you're adding **Game 3 (Word Search)**:

1. **Create the data types** in `Assets/Scripts/Games/WordSearch/`:
   - `WordData.cs` — ScriptableObject with `[CreateAssetMenu(menuName = "BrainCitizen/Word")]`
   - `WordDatabase.cs` — ScriptableObject holding the word pool
2. **Create the runtime scripts** in the same folder:
   - `WordSearchGameManager.cs` — game flow, scoring, events
   - `WordSearchUIManager.cs` — subscribes to events, updates UI
3. **Create an editor builder** in `Assets/Editor/WordSearchSceneBuilder.cs` with `[MenuItem("BrainCitizen/Build WordSearch Game")]`. Builder must:
   - Create data assets (or load existing)
   - Build the scene programmatically (no manual setup)
   - Set `gameInfo.isImplemented = true` on `Assets/Data/Hub/Game03_WordSearch.asset`
   - Add the new scene to `EditorBuildSettings.scenes`
4. **Update `HubSceneBuilder.cs`** — flip the matching `GameSpec` row to `true` so re-runs of "Build Hub Scene" stay consistent
5. **Run the menu command** in Unity → done. Hub auto-unlocks the card.

The hub registry pattern is the contract. Don't bypass it.

## Editor builder commands (Unity menu)

| Menu item | What it does |
|-----------|-------------|
| `BrainCitizen → Build Hub Scene` | (Re)creates `HubScene.unity`, all `GameInfo.asset` files, `GameRegistry.asset`, sets play-mode start scene |
| `BrainCitizen → Build TrueFalseNews Scene` | (Re)creates Game 1 scene with full UI hierarchy and component wiring |
| `BrainCitizen → Build FlagQuiz Game` | (Re)creates Game 2 scene + 30 `FlagData.asset` files using PNGs in `Assets/Data/Flags/Textures/` |

Builders are **idempotent** — they prompt before overwriting. Re-running is safe.

## Unity 6 gotchas we already hit (don't repeat them)

These cost real time during Game 1 and Game 2. Watch for them in future games:

1. **Asset references go null after `AssetDatabase.Refresh()`.** If a builder creates an asset, calls `AssetDatabase.Refresh()`, then assigns the in-memory reference to a scene component, the scene saves it as `fileID: 0`. Fix: re-load the asset via `AssetDatabase.LoadAssetAtPath<T>(path)` *after* the refresh and assign that.

2. **Direct field assignment on `ScriptableObject` assets sometimes doesn't persist.** Pattern that works:
   ```csharp
   var asset = ScriptableObject.CreateInstance<T>();
   asset.field = value;            // assign BEFORE CreateAsset
   AssetDatabase.CreateAsset(asset, path);
   var so = new SerializedObject(asset);
   so.FindProperty("field").SetValue(...);   // belt-and-braces
   so.ApplyModifiedPropertiesWithoutUndo();
   EditorUtility.SetDirty(asset);
   ```

3. **`Button[]` / `TMP_Text[]` arrays don't reliably serialize when assigned at edit-time from a script.** Workaround: use a single `Transform parent` field on the UIManager and resolve children at `Awake()`:
   ```csharp
   public Transform answerGrid;
   void Awake() {
       for (int i = 0; i < answerGrid.childCount; i++) { /* find Button + TMP_Text */ }
   }
   ```
   See `FlagQuizUIManager.cs` for the canonical pattern.

4. **LiberationSans SDF (TMP default font) doesn't have emoji or unusual unicode.** Symbols like ✓ ✗ 🔥 🔒 render as boxes. Stick to ASCII or import a font with full coverage.

5. **`enableWordWrapping` is obsolete in Unity 6 TMP.** Use `textWrappingMode = TextWrappingModes.Normal`.

6. **Don't generate flag/asset images procedurally** if you can download real ones. We tried; it wasted hours. For flags, `https://flagcdn.com/w640/{ISO_2}.png` is free and reliable.

## Game-specific notes

- **Game 1 (True or False News)** — 30 headlines (15 real / 15 fake) across 4 categories. Rounds pick 10 random.
- **Game 2 (Flag Quiz)** — 30 flags (10 easy / 10 medium / 10 hard). Round = 3 easy + 4 medium + 3 hard with same-tier distractors.
- **Game 6 (Whack-a-Fact in the deck)** — internally renamed to **"Doppi Facts"** per team decision. Scene name: `DoppiFacts`. Display name: `Doppi Facts`.

## Code conventions

- **Public fields on MonoBehaviour/ScriptableObject** are fine — Unity Inspector wants them.
- **Events**: `public static event System.Action<...>` on the GameManager. Static so UI can subscribe without an Inspector reference.
- **Subscribe in `OnEnable`, unsubscribe in `OnDisable`** — never in `Start`/`Awake`. Otherwise re-loading the scene leaks subscriptions.
- **No comments unless the *why* is non-obvious.** Don't narrate what the code already says.
- **Scene navigation** uses `SceneManager.LoadScene(name)`. Hub scene name is `"HubScene"`; guard with `Application.CanStreamedLevelBeLoaded` if calling before the hub exists.

## Working with Claude Code on this repo

- Ask Claude to add a game by name; it should follow the playbook above.
- For UI tweaks, prefer modifying the editor builder (`*SceneBuilder.cs`) and re-running the menu, rather than hand-editing the `.unity` scene.
- If something's broken at runtime, **check the Console first** and paste the error to Claude — runtime exceptions are the fastest signal.
- Never commit `Library/`, `Temp/`, generated `.csproj`. The `.gitignore` should catch them; if `git status` shows them, something's off.
