# web/

Launcher portal for **Brain Citizen Academy**. Loads the Unity WebGL build of the project's hub scene and lets the player pick one of the ten games.

The frontend is plain static HTML/CSS/JS. The backend is a small dev server (Python or Node) that exposes a JSON API for the leaderboard, scores, and profiles.

> Source of truth for game logic is the Unity project at the repo root — **never reimplement games in JavaScript**. The `play.html` page just iframes the Unity WebGL build.

## Quick start

```bash
# 1. Build the Unity project to WebGL.
#    Open the repo root in Unity Hub (6000.4.5f1), then:
#    Menu: BrainCitizen → Build WebGL
#    (Output goes to web/public/build/braincitizen/.)

# 2. Start the dev backend.
cd web

# Option A — Python (no installs required, uses stdlib):
python3 server.py

# Option B — Node.js (if you have node installed):
npm install
npm start

# 3. Open http://localhost:8080 in a browser.
```

Both servers expose the same API and serve the same static files. Pick whichever you have available.

## Layout

```
web/
├── public/                  # everything the server serves to the browser
│   ├── index.html           # landing page (10 game cards, leaderboard, about)
│   ├── play.html            # full-screen Unity WebGL host (iframes /build/braincitizen/index.html)
│   ├── css/styles.css       # shared styles
│   ├── js/main.js           # leaderboard fetch, light glue
│   └── build/               # Unity WebGL output — generated, gitignored
├── server.py                # Python stdlib dev backend (no installs; primary)
├── server.js                # Node/Express dev backend (alternative)
├── package.json             # Node/Express deps
└── README.md
```

## Backend API

All in-memory. Resets on restart. Replace with a real backend (Firebase, Cloud Run, etc.) for production.

| Method | Path                | Body / Query                          | Returns                                   |
|--------|---------------------|---------------------------------------|-------------------------------------------|
| GET    | `/api/health`       |                                       | `{ ok: true, ts }`                        |
| GET    | `/api/games`        |                                       | `{ games: [...] }`                        |
| GET    | `/api/leaderboard`  | optional `?gameId=flag-quiz`          | `{ scope, leaderboard: [{name,points}] }` |
| POST   | `/api/score`        | `{ gameId, score, playerId, name }`   | `{ ok: true, profile }`                   |
| GET    | `/api/profile`      | `?playerId=...`                       | `{ profile }`                             |

Registered game IDs: `true-false-news`, `flag-quiz`, `word-search`, `emotion-id`, `math-sprint`, `civic-quiz`, `maze-runner`, `timeline-sort`, `doppi-facts`, `memory-match`.

## Headers note (WebGL)

Unity WebGL ships compressed `.data.gz`, `.wasm.gz`, `.js.gz` files. Both servers set `Content-Encoding: gzip` and the right `Content-Type` so the browser decompresses inline. If you put this behind nginx or another server in production, replicate those headers — otherwise the build will refuse to load.

## Why this layout?

Earlier the `web/` folder contained JavaScript reimplementations of all 10 games. That meant game logic had to be maintained in two places (Unity + JS) and they drifted apart. The portal now hosts the actual Unity WebGL build, so there's one source of truth.
