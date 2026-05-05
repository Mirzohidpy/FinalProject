class MemoryMatchGame {
  constructor(el, cards) {
    this.el = el;
    this.allCards = cards;
    this.score = 0;
    this.combo = 0;
    this.currentLevel = 0;
    this.levels = [
      { cols: 4, rows: 3, seconds: 60 },
      { cols: 4, rows: 4, seconds: 80 },
      { cols: 6, rows: 4, seconds: 100 },
    ];
    this.flipped = [];
    this.matched = new Set();
    this.lockBoard = false;
    this.timerInterval = null;
    this.timeLeft = 0;
    this.destroyed = false;
    this.cards = [];
    this.revealTimeout = null;
  }

  start() {
    this.score = 0; this.combo = 0; this.currentLevel = 0;
    this.startLevel();
  }

  startLevel() {
    if (this.currentLevel >= this.levels.length) { this.showEnd(true); return; }
    const lvl = this.levels[this.currentLevel];
    const needed = (lvl.cols * lvl.rows) / 2;
    const pool = shuffle(this.allCards).slice(0, Math.min(needed, this.allCards.length));

    // If not enough unique cards, repeat from beginning
    let cardPairs = [...pool, ...pool];
    if (cardPairs.length < lvl.cols * lvl.rows) {
      const extra = shuffle(this.allCards).slice(0, (lvl.cols * lvl.rows) / 2 - pool.length);
      cardPairs = [...pool, ...extra, ...pool, ...extra];
    }
    this.cards = shuffle(cardPairs.slice(0, lvl.cols * lvl.rows));
    this.flipped = [];
    this.matched = new Set();
    this.lockBoard = false;
    this.timeLeft = lvl.seconds;

    this.el.innerHTML = `
      <div class="memory-level-info">Level ${this.currentLevel+1}/${this.levels.length} — ${lvl.cols}×${lvl.rows} grid — ${lvl.seconds}s</div>
      <div class="score-row">
        <div class="score-chip">Score <span id="mm-score">${this.score}</span></div>
        <div class="score-chip">Combo <span id="mm-combo">${this.combo}</span></div>
        <div class="score-chip">Time <span id="mm-time">${lvl.seconds}</span></div>
        <div class="score-chip">Pairs <span id="mm-pairs">0/${needed}</span></div>
      </div>
      <div class="timer-bar"><div class="timer-fill" id="mm-timer" style="width:100%"></div></div>
      <div class="memory-grid" id="mm-grid" style="max-width:${lvl.cols*94}px"></div>
    `;

    this.renderGrid(lvl);
    this.revealAll(lvl);
    this.startTimer(lvl);
  }

  renderGrid(lvl) {
    const grid = this.el.querySelector('#mm-grid');
    grid.innerHTML = '';
    this.cards.forEach((card, i) => {
      const cardEl = document.createElement('div');
      cardEl.className = 'mem-card';
      cardEl.innerHTML = `
        <div class="mem-card-inner">
          <div class="mem-card-front">🎴</div>
          <div class="mem-card-back">${card.emoji}</div>
        </div>
      `;
      cardEl.addEventListener('click', () => this.onCardClick(i, cardEl, card));
      grid.appendChild(cardEl);
    });
  }

  revealAll(lvl) {
    this.lockBoard = true;
    this.el.querySelectorAll('.mem-card').forEach(c => c.classList.add('flipped'));
    this.revealTimeout = setTimeout(() => {
      if (this.destroyed) return;
      this.el.querySelectorAll('.mem-card:not(.matched)').forEach(c => c.classList.remove('flipped'));
      this.lockBoard = false;
    }, 3000);
  }

  onCardClick(idx, cardEl, card) {
    if (this.lockBoard) return;
    if (this.matched.has(idx)) return;
    if (this.flipped.includes(idx)) return;
    if (this.flipped.length >= 2) return;

    cardEl.classList.add('flipped');
    this.flipped.push(idx);

    if (this.flipped.length === 2) {
      this.lockBoard = true;
      const [i1, i2] = this.flipped;
      const [c1, c2] = [this.cards[i1], this.cards[i2]];

      if (c1.cardName === c2.cardName) {
        // Match!
        this.combo++;
        const comboBonus = (this.combo - 1) * 50;
        this.score += 100 + comboBonus;
        setNavScore(this.score);
        this.matched.add(i1);
        this.matched.add(i2);

        const lvl = this.levels[this.currentLevel];
        const needed = (lvl.cols * lvl.rows) / 2;

        const cards = this.el.querySelectorAll('.mem-card');
        [i1, i2].forEach(i => cards[i]?.classList.add('matched'));

        this.updateUI(needed);
        this.flipped = [];
        this.lockBoard = false;

        if (this.matched.size === this.cards.length) {
          clearInterval(this.timerInterval);
          const timeBonus = this.timeLeft * 5;
          this.score += 200 + timeBonus; // level clear bonus
          setNavScore(this.score);
          setTimeout(() => {
            if (!this.destroyed) {
              this.currentLevel++;
              this.startLevel();
            }
          }, 900);
        }
      } else {
        // No match
        this.combo = 0;
        setTimeout(() => {
          if (this.destroyed) return;
          const cards = this.el.querySelectorAll('.mem-card');
          [i1, i2].forEach(i => { if (!this.matched.has(i)) cards[i]?.classList.remove('flipped'); });
          this.flipped = [];
          this.lockBoard = false;
        }, 900);
      }
    }
  }

  updateUI(needed) {
    const sc = this.el.querySelector('#mm-score');
    if (sc) sc.textContent = this.score;
    const co = this.el.querySelector('#mm-combo');
    if (co) co.textContent = this.combo;
    const pa = this.el.querySelector('#mm-pairs');
    if (pa) pa.textContent = `${this.matched.size/2}/${needed}`;
  }

  startTimer(lvl) {
    clearInterval(this.timerInterval);
    this.timerInterval = setInterval(() => {
      if (this.destroyed) { clearInterval(this.timerInterval); return; }
      this.timeLeft--;
      const t = this.el.querySelector('#mm-time');
      if (t) t.textContent = this.timeLeft;
      const bar = this.el.querySelector('#mm-timer');
      if (bar) {
        const pct = (this.timeLeft / lvl.seconds) * 100;
        bar.style.width = pct + '%';
        bar.style.background = pct > 40 ? 'var(--green)' : pct > 20 ? 'var(--yellow)' : 'var(--red)';
      }
      if (this.timeLeft <= 0) {
        clearInterval(this.timerInterval);
        this.showEnd(false);
      }
    }, 1000);
  }

  showEnd(won) {
    clearInterval(this.timerInterval);
    this.el.innerHTML = `
      <div class="end-screen">
        <h2>${won ? '🎉 All Levels Complete!' : '⏰ Time Up!'}</h2>
        <div class="final-score">${this.score}</div>
        <p>${won ? `${this.levels.length} levels cleared!` : `Stopped at level ${this.currentLevel+1}`}</p>
        <div class="end-actions">
          <button class="btn btn-primary" id="mm-again">Play Again</button>
          <button class="btn btn-secondary" onclick="showHub()">Hub</button>
        </div>
        <div class="leaderboard">
          <h3>🏆 Leaderboard</h3>
          <div class="name-row">
            <input class="name-input" id="mm-name" placeholder="Your name" maxlength="14" />
            <button class="btn btn-warning" id="mm-save">Save</button>
          </div>
          <div id="mm-entries"></div>
        </div>
      </div>
    `;
    this.el.querySelector('#mm-again').addEventListener('click', () => this.start());
    this.el.querySelector('#mm-save').addEventListener('click', () => this.saveScore());
    this.loadLB();
  }

  async saveScore() {
    const name = this.el.querySelector('#mm-name').value.trim() || 'Player';
    const entries = await postScore('memorymatch', name, this.score);
    this.renderLB(entries);
    toast('Score saved!', 'success');
  }

  async loadLB() {
    const entries = await getLeaderboard('memorymatch');
    this.renderLB(entries);
  }

  renderLB(entries) {
    const el = this.el.querySelector('#mm-entries');
    if (!el) return;
    el.innerHTML = entries.map((e, i) =>
      `<div class="lb-entry"><span class="lb-rank">${i+1}</span><span class="lb-name">${e.name}</span><span class="lb-score">${e.score}</span></div>`
    ).join('') || '<p class="text-muted text-center">No scores yet</p>';
  }

  destroy() {
    this.destroyed = true;
    clearInterval(this.timerInterval);
    clearTimeout(this.revealTimeout);
  }
}
