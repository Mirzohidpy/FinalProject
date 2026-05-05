# Brain Citizen Academy

A Unity 2D hub-and-spoke educational game with **10 short mini-games** covering civics, geography, math, language, logic, history, and memory.

> Group project for **SE203 Game Development**, New Uzbekistan University, 2026.

## The games

| # | Game | Category | What you do |
|---|------|----------|-------------|
| 1 | True or False News | Civics | Decide if a headline is real or fabricated; sources shown after each answer. |
| 2 | Flag Quiz | Geography | Identify 30 flags across three difficulty tiers. |
| 3 | Word Search | Language | Find hidden words on a letter grid. |
| 4 | Math Sprint | Math | Quick arithmetic against a timer; difficulty ramps with streak. |
| 5 | Memory Match | Memory | Flip cards two at a time; six pairs, time-attack scoring. |
| 6 | Doppi Facts | Civics | Spot the fake claim among three real ones. |
| 7 | Maze Runner | Logic | Navigate a maze; answer a question at every checkpoint. |
| 8 | Emotion ID | Social | Match facial expressions to emotion labels. |
| 9 | Timeline Sort | History | Drag historical events into chronological order. |
| 10 | Quiz Showdown | Civics | Multiple-choice civic-knowledge questions across four categories. |

The Hub scene is the entry point; cards stay locked until each game is implemented.

## Tech stack

- **Unity 6** (`6000.4.5f1`) — see `ProjectSettings/ProjectVersion.txt`
- **uGUI 2.0** with TextMeshPro
- C# / .NET (Unity built-in)
- WebGL build target for the web portal
- No external runtime dependencies

## First-time setup

1. Install Unity `6000.4.5f1` via Unity Hub (with the **WebGL Build Support** module).
2. Clone the repo and open the project root in Unity Hub.
3. Wait for the first import (1–2 min). If prompted, **Import TMP Essentials**.
4. Press **Play** — you should land on the Hub scene with 10 game cards.

The play-mode start scene is already set to `Assets/Scenes/HubScene.unity`, so Play always starts there.

## Repo layout

```
FinalProject/
├── Assets/
│   ├── Data/                # ScriptableObject data (flags, headlines, hub registry)
│   ├── Editor/              # scene builders + WebGLBuilder
│   ├── Scenes/              # HubScene.unity + one .unity per game
│   ├── Scripts/
│   │   ├── Core/            # cross-game code (HubManager, GameInfo, GameRegistry)
│   │   └── Games/<Name>/    # one folder per game (GameManager + UIManager)
│   └── Resources/           # runtime-loaded assets
├── ProjectSettings/         # Unity config (committed)
├── Packages/                # Unity package manifest (committed)
├── web/                     # web launcher portal — see web/README.md
└── CLAUDE.md                # detailed contributor guide
```

## Architecture pattern (every game follows this)

Each mini-game is a **separate scene** loaded from the Hub. Within a game scene:

- **Data** — ScriptableObjects for game content
- **GameManager** — game logic only; broadcasts via static `System.Action` events
- **UIManager** — subscribes to events and drives all UI; knows nothing about logic

This decoupling is deliberate. Reference implementation: `Assets/Scripts/Games/TrueFalseNews/`.

## Editor menu commands

| Menu item | What it does |
|-----------|--------------|
| `BrainCitizen → Build Hub Scene` | (Re)creates `HubScene.unity` and the game registry |
| `BrainCitizen → Build TrueFalseNews Scene` | (Re)creates Game 1 scene with full UI hierarchy |
| `BrainCitizen → Build FlagQuiz Game` | (Re)creates Game 2 scene + 30 `FlagData.asset` files |
| `BrainCitizen → Build WebGL` | Builds the project to `web/public/build/braincitizen/` for the portal |

Builders are **idempotent** — they prompt before overwriting. Re-running is safe.

See [`CLAUDE.md`](./CLAUDE.md) for the full per-game playbook and Unity 6 gotchas.

## Web portal

`web/` contains a launcher portal that hosts the Unity WebGL build in an iframe. To run it:

```bash
# 1. In Unity: BrainCitizen → Build WebGL
# 2. Then:
cd web
python3 server.py            # or: npm install && npm start
# 3. Open http://localhost:8080
```

See [`web/README.md`](./web/README.md) for the API table and details.

## License & credits

- Flag images © [flagcdn.com](https://flagcdn.com/).
- Headline data references public sources (UN, WHO, NASA, BBC) for the True/False game.
- Built by the SE203 team — see GitHub contributors.
