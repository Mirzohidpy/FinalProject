// DoppiFacts — exact Unity values:
// spawnIntervalPerWave = [1.4, 1.1, 0.85, 0.65] seconds (FIXED, not random!)
// lingerPerWave = [2600, 2100, 1700, 1300] ms
// interWaveDelay = 1400 ms
// moles per wave = [6, 8, 10, 12]
// hit TRUE fact: +100 + streakBonus (streakBonusMultiplier=25)
// click FALSE fact: -75
// miss TRUE fact: -25
// ignore FALSE fact: +10
class DoppiFactsGame {
  constructor(el, facts) {
    this.el = el;
    this.allFacts = facts;
    this.score = 0;
    this.streak = 0;
    this.currentWave = 0;
    this.molesShown = 0;
    this.destroyed = false;
    // Unity: spawnIntervalPerWave seconds, lingerMs, molesPerWave
    this.waveConfig = [
      { moles: 6,  lingerMs: 2600, spawnMs: 1400 },
      { moles: 8,  lingerMs: 2100, spawnMs: 1100 },
      { moles: 10, lingerMs: 1700, spawnMs:  850 },
      { moles: 12, lingerMs: 1300, spawnMs:  650 },
    ];
    this.activeMoles = new Array(9).fill(null);
    this.usedFacts = new Set();
    this.waveTimer = null;
  }

  start() {
    this.score = 0; this.streak = 0; this.currentWave = 0;
    this.usedFacts = new Set();
    this.render();
    this.startWave();
  }

  render() {
    this.el.innerHTML = `
      <div class="doppi-info">
        <div class="chip">Wave <span id="dp-wave">1/${this.waveConfig.length}</span></div>
        <div class="chip">Score <span id="dp-score">0</span></div>
        <div class="chip">Streak <span id="dp-streak">0🔥</span></div>
      </div>
      <div class="wave-bar">${this.waveConfig.map((_,i) =>
        `<div class="wave-dot ${i === 0 ? 'active' : ''}" id="dp-wd-${i}"></div>`
      ).join('')}</div>
      <p class="text-center text-muted mb-16" style="font-size:.85rem">
        Click TRUE facts · Ignore FALSE facts
      </p>
      <div class="mole-grid" id="dp-grid"></div>
      <div class="feedback-box" id="dp-fb"></div>
    `;
    this.renderHoles();
  }

  renderHoles() {
    const grid = this.el.querySelector('#dp-grid');
    grid.innerHTML = '';
    for (let i = 0; i < 9; i++) {
      const hole = document.createElement('div');
      hole.className = 'mole-hole';
      hole.id = `dp-hole-${i}`;
      hole.innerHTML = `<div class="mole-content" id="dp-mole-${i}"></div>`;
      grid.appendChild(hole);
    }
  }

  startWave() {
    if (this.currentWave >= this.waveConfig.length) { this.showEnd(); return; }
    const cfg = this.waveConfig[this.currentWave];
    this.molesShown = 0;
    this.activeMoles = new Array(9).fill(null);

    this.waveConfig.forEach((_, i) => {
      const dot = this.el.querySelector(`#dp-wd-${i}`);
      if (!dot) return;
      dot.className = 'wave-dot' + (i < this.currentWave ? ' done' : i === this.currentWave ? ' active' : '');
    });
    const waveEl = this.el.querySelector('#dp-wave');
    if (waveEl) waveEl.textContent = `${this.currentWave+1}/${this.waveConfig.length}`;

    this.scheduleNextMole(cfg);
  }

  scheduleNextMole(cfg) {
    if (this.destroyed) return;
    if (this.molesShown >= cfg.moles) {
      // Unity: interWaveDelay = 1400ms after last mole's linger
      this.waveTimer = setTimeout(() => {
        if (this.destroyed) return;
        this.currentWave++;
        this.startWave();
      }, cfg.lingerMs + 1400);
      return;
    }

    // Unity: fixed spawnInterval per wave (NOT random)
    this.waveTimer = setTimeout(() => {
      if (this.destroyed) return;
      this.showMole(cfg);
      this.molesShown++;
      this.scheduleNextMole(cfg);
    }, cfg.spawnMs);
  }

  getAvailableHole() {
    const empty = [];
    for (let i = 0; i < 9; i++) if (!this.activeMoles[i]) empty.push(i);
    if (!empty.length) return -1;
    return empty[Math.floor(Math.random() * empty.length)];
  }

  getRandomFact() {
    const pool = this.allFacts.filter(f => !this.usedFacts.has(f.statement));
    if (!pool.length) { this.usedFacts.clear(); return this.allFacts[Math.floor(Math.random() * this.allFacts.length)]; }
    return pool[Math.floor(Math.random() * pool.length)];
  }

  showMole(cfg) {
    const holeIdx = this.getAvailableHole();
    if (holeIdx === -1) return;

    const fact = this.getRandomFact();
    this.usedFacts.add(fact.statement);
    this.activeMoles[holeIdx] = fact;

    const mole = this.el.querySelector(`#dp-mole-${holeIdx}`);
    if (!mole) return;

    mole.textContent = fact.statement;
    mole.className = 'mole-content visible';
    mole.style.cursor = 'pointer';

    let clicked = false;
    const clickHandler = () => {
      if (clicked) return;
      clicked = true;
      clearTimeout(mole._hideTimer);
      this.onMoleClick(holeIdx, fact, mole);
    };
    mole._clickHandler = clickHandler;
    mole.addEventListener('click', clickHandler);

    mole._hideTimer = setTimeout(() => {
      if (clicked) return;
      mole.removeEventListener('click', clickHandler);
      if (fact.isTrue) {
        // Missed a true fact
        this.score = Math.max(0, this.score - 25);
        this.streak = 0;
        this.showFeedback(`Missed TRUE: "${fact.statement.slice(0,40)}..." (-25)`, false);
        mole.className = 'mole-content missed';
      } else {
        // Correctly ignored a false fact
        this.score += 10;
        mole.className = 'mole-content hit';
      }
      this.updateScore();
      setTimeout(() => { if (!this.destroyed) { mole.className = 'mole-content'; this.activeMoles[holeIdx] = null; } }, 400);
    }, cfg.lingerMs);
  }

  onMoleClick(holeIdx, fact, mole) {
    mole.removeEventListener('click', mole._clickHandler);
    clearTimeout(mole._hideTimer);

    if (fact.isTrue) {
      // Unity: streakBonusMultiplier = 25
      const streakBonus = this.streak * 25;
      this.streak++;
      this.score += 100 + streakBonus;
      mole.className = 'mole-content hit';
      this.showFeedback(`TRUE! ${fact.explanation} (+${100 + streakBonus - 25})`, true);
    } else {
      this.score = Math.max(0, this.score - 75);
      this.streak = 0;
      mole.className = 'mole-content missed';
      this.showFeedback(`FALSE: ${fact.explanation} (-75)`, false);
    }

    this.updateScore();
    setTimeout(() => { if (!this.destroyed) { mole.className = 'mole-content'; this.activeMoles[holeIdx] = null; } }, 500);
  }

  updateScore() {
    const sc = this.el.querySelector('#dp-score');
    if (sc) sc.textContent = this.score;
    const st = this.el.querySelector('#dp-streak');
    if (st) st.textContent = `${this.streak}🔥`;
    setNavScore(this.score);
  }

  showFeedback(msg, good) {
    const fb = this.el.querySelector('#dp-fb');
    if (!fb) return;
    fb.className = `feedback-box show ${good ? 'correct-fb' : 'wrong-fb'}`;
    fb.textContent = msg;
    clearTimeout(fb._timer);
    fb._timer = setTimeout(() => { if (!this.destroyed) fb.className = 'feedback-box'; }, 1400);
  }

  showEnd() {
    clearTimeout(this.waveTimer);
    this.el.innerHTML = `
      <div class="end-screen">
        <h2>🔨 Round Complete!</h2>
        <div class="final-score">${this.score}</div>
        <p>${this.waveConfig.length} waves · best streak: ${this.streak}</p>
        <div class="end-actions">
          <button class="btn btn-primary" id="dp-again">Play Again</button>
          <button class="btn btn-secondary" onclick="showHub()">Hub</button>
        </div>
        <div class="leaderboard">
          <h3>🏆 Leaderboard</h3>
          <div class="name-row">
            <input class="name-input" id="dp-name" placeholder="Your name" maxlength="14" />
            <button class="btn btn-warning" id="dp-save">Save</button>
          </div>
          <div id="dp-entries"></div>
        </div>
      </div>
    `;
    this.el.querySelector('#dp-again').addEventListener('click', () => this.start());
    this.el.querySelector('#dp-save').addEventListener('click', () => this.saveScore());
    this.loadLB();
  }

  async saveScore() {
    const name = this.el.querySelector('#dp-name').value.trim() || 'Player';
    const entries = await postScore('doppifacts', name, this.score);
    this.renderLB(entries);
    toast('Score saved!', 'success');
  }

  async loadLB() {
    const entries = await getLeaderboard('doppifacts');
    this.renderLB(entries);
  }

  renderLB(entries) {
    const el = this.el.querySelector('#dp-entries');
    if (!el) return;
    el.innerHTML = entries.map((e, i) =>
      `<div class="lb-entry"><span class="lb-rank">${i+1}</span><span class="lb-name">${e.name}</span><span class="lb-score">${e.score}</span></div>`
    ).join('') || '<p class="text-muted text-center">No scores yet</p>';
  }

  destroy() {
    this.destroyed = true;
    clearTimeout(this.waveTimer);
    for (let i = 0; i < 9; i++) {
      const mole = document.querySelector(`#dp-mole-${i}`);
      if (mole?._hideTimer) clearTimeout(mole._hideTimer);
    }
  }
}
