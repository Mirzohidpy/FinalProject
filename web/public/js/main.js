// Brain Citizen Academy — landing page logic.
// Currently fetches the leaderboard from the dev backend.

const LEADERBOARD_ENDPOINT = '/api/leaderboard';

document.addEventListener('DOMContentLoaded', () => {
  loadLeaderboard().catch((err) => {
    console.warn('[BrainCitizen] Leaderboard load failed:', err);
    renderLeaderboardError();
  });
});

async function loadLeaderboard() {
  const res = await fetch(LEADERBOARD_ENDPOINT, { headers: { 'Accept': 'application/json' } });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  const data = await res.json();
  if (!Array.isArray(data.leaderboard)) throw new Error('Malformed payload');
  renderLeaderboard(data.leaderboard);
}

function renderLeaderboard(rows) {
  const list = document.getElementById('leaderboard-list');
  if (!list) return;
  list.innerHTML = '';
  if (!rows.length) {
    const li = document.createElement('li');
    li.className = 'leaderboard-item placeholder';
    li.textContent = 'No scores yet — be the first.';
    list.appendChild(li);
    return;
  }
  rows.forEach((row, idx) => {
    const li = document.createElement('li');
    li.className = 'leaderboard-item' + (idx === 0 ? ' top' : '');
    li.innerHTML = `
      <span class="rank">${idx + 1}</span>
      <span class="name">${escapeHtml(row.name || 'Anonymous')}</span>
      <span class="pts">${Number(row.points || 0).toLocaleString()} pts</span>
    `;
    list.appendChild(li);
  });
}

function renderLeaderboardError() {
  const list = document.getElementById('leaderboard-list');
  if (!list) return;
  list.innerHTML = '';
  const li = document.createElement('li');
  li.className = 'leaderboard-item placeholder';
  li.textContent = 'Backend not running. Start it with: cd web && python3 server.py';
  list.appendChild(li);
}

function escapeHtml(s) {
  return String(s).replace(/[&<>"']/g, (c) => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;',
  }[c]));
}
