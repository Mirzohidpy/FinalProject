// Brain Citizen Academy dev backend.
// Serves the static web/public/, hosts the Unity WebGL build under
// public/build/, and exposes a small JSON API. State is in-memory — wipes on
// restart. Run with: npm install && npm start.

const path    = require('path');
const fs      = require('fs');
const express = require('express');

const app  = express();
const PORT = Number(process.env.PORT) || 8080;
const ROOT      = __dirname;
const PUBLIC    = path.join(ROOT, 'public');
const BUILD_DIR = path.join(PUBLIC, 'build');

app.disable('x-powered-by');
app.use(express.json({ limit: '32kb' }));

// ----- API routes BEFORE static so they aren't shadowed -----

const games = [
  { gameId: 'true-false-news', title: 'True or False News', category: 'Civics',    scene: 'TrueFalseNews' },
  { gameId: 'flag-quiz',       title: 'Flag Quiz',          category: 'Geography', scene: 'FlagQuiz' },
  { gameId: 'word-search',     title: 'Word Search',        category: 'Language',  scene: 'WordSearch' },
  { gameId: 'emotion-id',      title: 'Emotion ID',         category: 'Social',    scene: 'EmotionID' },
  { gameId: 'math-sprint',     title: 'Math Sprint',        category: 'Math',      scene: 'MathSprint' },
  { gameId: 'civic-quiz',      title: 'Quiz Showdown',      category: 'Civics',    scene: 'CivicQuiz' },
  { gameId: 'maze-runner',     title: 'Maze Runner',        category: 'Logic',     scene: 'MazeRunner' },
  { gameId: 'timeline-sort',   title: 'Timeline Sort',      category: 'History',   scene: 'TimelineSort' },
  { gameId: 'doppi-facts',     title: 'Doppi Facts',        category: 'Civics',    scene: 'DoppiFacts' },
  { gameId: 'memory-match',    title: 'Memory Match',       category: 'Memory',    scene: 'MemoryMatch' },
];

const scoresByGame = new Map(games.map(g => [g.gameId, []]));
const profiles = new Map();

const COINS_PER_POINT = 10;
const DAILY_POINTS_CAP = 200;

function getOrCreateProfile(playerId, name) {
  if (!profiles.has(playerId)) {
    profiles.set(playerId, {
      playerId,
      name: name || 'Anonymous',
      coins: 0,
      points: 0,
      perGame: {},
    });
  }
  const p = profiles.get(playerId);
  if (name && p.name === 'Anonymous') p.name = name;
  return p;
}

app.get('/api/health', (req, res) => res.json({ ok: true, ts: Date.now() }));

app.get('/api/games', (req, res) => res.json({ games }));

app.get('/api/leaderboard', (req, res) => {
  const gameId = String(req.query.gameId || '');
  if (gameId && scoresByGame.has(gameId)) {
    const top = [...scoresByGame.get(gameId)]
      .sort((a, b) => b.score - a.score)
      .slice(0, 50);
    return res.json({ scope: gameId, leaderboard: top.map(s => ({ name: s.name, points: s.score })) });
  }
  const top = [...profiles.values()]
    .sort((a, b) => b.points - a.points)
    .slice(0, 50)
    .map(p => ({ name: p.name, points: p.points }));
  if (top.length === 0) {
    return res.json({
      scope: 'global',
      leaderboard: [
        { name: 'Aziza',     points: 320 },
        { name: 'Bekzod',    points: 285 },
        { name: 'Dilnoza',   points: 240 },
        { name: 'Jasur',     points: 210 },
        { name: 'Madina',    points: 180 },
      ],
    });
  }
  res.json({ scope: 'global', leaderboard: top });
});

app.post('/api/score', (req, res) => {
  const { gameId, score, playerId, name } = req.body || {};
  if (typeof gameId !== 'string' || !scoresByGame.has(gameId)) {
    return res.status(400).json({ error: 'Unknown gameId. Allowed: ' + games.map(g => g.gameId).join(', ') });
  }
  const numScore = Number(score);
  if (!Number.isFinite(numScore) || numScore < 0 || numScore > 1_000_000) {
    return res.status(400).json({ error: 'Score must be a non-negative number under 1,000,000.' });
  }
  const id = String(playerId || `anon-${Date.now()}`).slice(0, 64);
  const profile = getOrCreateProfile(id, name);

  const entry = { playerId: id, name: profile.name, score: numScore, ts: Date.now() };
  scoresByGame.get(gameId).push(entry);

  const coinsEarned = Math.min(numScore, 1000);
  profile.coins += coinsEarned;
  const pointsToAward = Math.floor(profile.coins / COINS_PER_POINT);
  profile.coins -= pointsToAward * COINS_PER_POINT;
  profile.points = Math.min(DAILY_POINTS_CAP * 30, profile.points + pointsToAward);

  const perGame = profile.perGame[gameId] || { best: 0, plays: 0 };
  perGame.best = Math.max(perGame.best, numScore);
  perGame.plays += 1;
  profile.perGame[gameId] = perGame;

  res.json({ ok: true, profile });
});

app.get('/api/profile', (req, res) => {
  const id = String(req.query.playerId || '');
  if (!id) return res.status(400).json({ error: 'playerId is required.' });
  if (!profiles.has(id)) return res.status(404).json({ error: 'No profile yet for that playerId.' });
  res.json({ profile: profiles.get(id) });
});

// ----- WebGL build hosting (Content-Encoding for .gz / .br) -----

app.use('/build', express.static(BUILD_DIR, {
  setHeaders: (res, filePath) => {
    if (filePath.endsWith('.gz')) {
      res.setHeader('Content-Encoding', 'gzip');
      if      (filePath.endsWith('.wasm.gz')) res.setHeader('Content-Type', 'application/wasm');
      else if (filePath.endsWith('.js.gz'))   res.setHeader('Content-Type', 'application/javascript');
      else if (filePath.endsWith('.data.gz')) res.setHeader('Content-Type', 'application/octet-stream');
      else if (filePath.endsWith('.symbols.json.gz')) res.setHeader('Content-Type', 'application/json');
    } else if (filePath.endsWith('.br')) {
      res.setHeader('Content-Encoding', 'br');
    }
  },
}));

app.use(express.static(PUBLIC));

app.listen(PORT, () => {
  const buildExists = fs.existsSync(path.join(BUILD_DIR, 'braincitizen', 'index.html'));
  console.log(`Brain Citizen Academy portal running:  http://localhost:${PORT}`);
  console.log(`  /                   → landing page`);
  console.log(`  /play.html          → Unity WebGL`);
  console.log(`  /api/health         → backend liveness`);
  console.log(`  /api/games          → game catalog`);
  console.log(`  /api/leaderboard    → top players (global, or ?gameId=flag-quiz)`);
  console.log(`  /api/score          → POST { gameId, score, playerId, name }`);
  console.log(`  /api/profile        → GET ?playerId=...`);
  console.log(buildExists
    ? `  /build/braincitizen/index.html → Unity WebGL build (found)`
    : `  ⚠ Unity WebGL build missing. In Unity: BrainCitizen → Build WebGL.`);
});
