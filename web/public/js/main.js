// ── Utility helpers ───────────────────────────────────────────────────────
const $ = id => document.getElementById(id);
const view = () => $('view');

// Load game data from embedded constants (works via file:// and any server)
async function fetchData(endpoint) {
  const data = GAME_DATA[endpoint];
  if (!data) throw new Error(`No data found for "${endpoint}"`);
  return data;
}

function shuffle(arr) {
  const a = [...arr];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

function pick(arr, n) { return shuffle(arr).slice(0, n); }

let toastTimer;
function toast(msg, type = '', dur = 2200) {
  const el = $('toast');
  el.textContent = msg;
  el.className = `show ${type}`;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.className = '', dur);
}

// ── Leaderboard — backend API (SQLite via Express) ────────────────────────
async function postScore(game, name, score) {
  try {
    const r = await fetch(`/api/leaderboard/${game}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, score })
    });
    if (!r.ok) {
      const err = await r.text();
      toast(`Save failed: ${err}`, 'error', 3000);
      return [];
    }
    return r.json();
  } catch (e) {
    toast(`Cannot reach server — open via http://localhost:3000`, 'error', 4000);
    return [];
  }
}

async function getLeaderboard(game) {
  try {
    const r = await fetch(`/api/leaderboard/${game}`);
    return r.ok ? r.json() : [];
  } catch {
    return [];
  }
}

// ── Router / navigation ───────────────────────────────────────────────────
const GAMES = {
  flagquiz:      { name: '🚩 Flag Quiz',       class: FlagQuizGame,      endpoint: 'flags'          },
  truefalsegame: { name: '📰 True/False News',  class: TrueFalseGame,     endpoint: 'headlines'      },
  wordsearch:    { name: '🔤 Word Search',       class: WordSearchGame,    endpoint: 'words'          },
  emotionid:     { name: '😊 Emotion ID',        class: EmotionIDGame,     endpoint: 'emotions'       },
  mathsprint:    { name: '🔢 Math Sprint',       class: MathSprintGame,    endpoint: 'math-levels'    },
  quizshowdown:  { name: '🏆 Quiz Showdown',     class: QuizShowdownGame,  endpoint: 'quiz-questions' },
  mazerunner:    { name: '🌀 Maze Runner',       class: MazeRunnerGame,    endpoint: 'maze-questions' },
  timelinesort:  { name: '⏳ Timeline Sort',     class: TimelineSortGame,  endpoint: 'timeline-events'},
  doppifacts:    { name: '🔨 DoppiFacts',        class: DoppiFactsGame,    endpoint: 'doppifacts'     },
  memorymatch:   { name: '🃏 Memory Match',      class: MemoryMatchGame,   endpoint: 'memory-cards'   }
};

let currentGame = null;

function showHub() {
  if (currentGame?.destroy) currentGame.destroy();
  currentGame = null;

  $('nav-bar').classList.add('hidden');

  const hub = document.createElement('div');
  hub.id = 'hub';

  const civicGames  = ['flagquiz','truefalsegame','wordsearch','quizshowdown','mazerunner','timelinesort','doppifacts'];
  const mentalGames = ['emotionid','mathsprint','memorymatch'];

  hub.innerHTML = `
    <div class="hub-hero">
      <h1>🎓 Brain Citizen Academy</h1>
      <p>10 mini-games to sharpen your civic knowledge and mental skills</p>
    </div>
    <div class="hub-category">
      <h2>🗳 Civic Awareness</h2>
      <div class="game-grid" id="civic-grid"></div>
    </div>
    <div class="hub-category">
      <h2>🧠 Mental Skills</h2>
      <div class="game-grid" id="mental-grid"></div>
    </div>
  `;
  view().innerHTML = '';
  view().appendChild(hub);

  const icons = { flagquiz:'🚩', truefalsegame:'📰', wordsearch:'🔤', quizshowdown:'🏆', mazerunner:'🌀', timelinesort:'⏳', doppifacts:'🔨', emotionid:'😊', mathsprint:'🔢', memorymatch:'🃏' };
  const names = { flagquiz:'Flag Quiz', truefalsegame:'True/False News', wordsearch:'Word Search', quizshowdown:'Quiz Showdown', mazerunner:'Maze Runner', timelinesort:'Timeline Sort', doppifacts:'DoppiFacts', emotionid:'Emotion ID', mathsprint:'Math Sprint', memorymatch:'Memory Match' };

  function addCards(ids, gridId) {
    const grid = document.getElementById(gridId);
    ids.forEach(id => {
      const card = document.createElement('div');
      card.className = 'game-card';
      card.innerHTML = `<span class="icon">${icons[id]}</span><div class="name">${names[id]}</div>`;
      card.addEventListener('click', () => loadGame(id));
      grid.appendChild(card);
    });
  }
  addCards(civicGames, 'civic-grid');
  addCards(mentalGames, 'mental-grid');
}

async function loadGame(id) {
  const def = GAMES[id];
  if (!def) return;

  $('nav-bar').classList.remove('hidden');
  $('nav-title').textContent = def.name;
  $('nav-score').textContent = '';
  view().innerHTML = '<div class="game-screen text-center" style="padding-top:60px"><p style="color:var(--muted);font-size:1.2rem">Loading…</p></div>';

  try {
    const data = await fetchData(def.endpoint);
    if (currentGame?.destroy) currentGame.destroy();
    view().innerHTML = '';
    const screen = document.createElement('div');
    screen.className = 'game-screen';
    view().appendChild(screen);
    currentGame = new def.class(screen, data);
    currentGame.gameId = id;
    currentGame.start();
  } catch (e) {
    view().innerHTML = `<div class="game-screen text-center" style="padding-top:60px"><p class="text-red">Error: ${e.message}</p><br><button class="btn btn-secondary" onclick="showHub()">← Hub</button></div>`;
  }
}

// ── Back button ───────────────────────────────────────────────────────────
$('back-btn').addEventListener('click', showHub);

// ── Init ──────────────────────────────────────────────────────────────────
showHub();

// ── Expose globals needed by game classes ─────────────────────────────────
window.showHub = showHub;
window.postScore = postScore;
window.getLeaderboard = getLeaderboard;
window.shuffle = shuffle;
window.pick = pick;
window.toast = toast;
window.setNavScore = (s) => { $('nav-score').textContent = `Score: ${s}`; };
