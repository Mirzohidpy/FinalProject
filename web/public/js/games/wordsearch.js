class WordSearchGame {
  constructor(el, words) {
    this.el = el;
    this.allWords = words;
    this.gridSize = 10;
    this.grid = [];
    this.wordsToFind = [];
    this.foundWords = new Set();
    this.selectedCells = [];
    this.isSelecting = false;
    this.startCell = null;
    this.score = 0;
    this.timerInterval = null;
    this.timeLeft = 120;
    this.destroyed = false;
  }

  start() {
    this.wordsToFind = pick(this.allWords, 7);
    this.foundWords = new Set();
    this.score = 0;
    this.buildGrid();
    this.render();
    this.startTimer();
  }

  buildGrid() {
    const size = this.gridSize;
    this.grid = Array.from({ length: size }, () => Array(size).fill(''));
    const dirs = [[0,1],[1,0],[1,1],[0,-1],[-1,0],[-1,-1],[1,-1],[-1,1]];
    this.wordPositions = {};

    for (const wd of this.wordsToFind) {
      const word = wd.word;
      let placed = false;
      for (let attempt = 0; attempt < 200 && !placed; attempt++) {
        const [dr, dc] = dirs[Math.floor(Math.random() * dirs.length)];
        const row = Math.floor(Math.random() * size);
        const col = Math.floor(Math.random() * size);
        if (this.canPlace(word, row, col, dr, dc)) {
          const cells = [];
          for (let i = 0; i < word.length; i++) {
            this.grid[row + dr*i][col + dc*i] = word[i];
            cells.push([row + dr*i, col + dc*i]);
          }
          this.wordPositions[word] = cells;
          placed = true;
        }
      }
    }

    const alpha = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    for (let r = 0; r < size; r++)
      for (let c = 0; c < size; c++)
        if (!this.grid[r][c]) this.grid[r][c] = alpha[Math.floor(Math.random() * 26)];
  }

  canPlace(word, row, col, dr, dc) {
    for (let i = 0; i < word.length; i++) {
      const r = row + dr*i, c = col + dc*i;
      if (r < 0 || r >= this.gridSize || c < 0 || c >= this.gridSize) return false;
      if (this.grid[r][c] && this.grid[r][c] !== word[i]) return false;
    }
    return true;
  }

  render() {
    this.el.innerHTML = `
      <div class="score-row">
        <div class="score-chip">Found <span id="ws-found">0/${this.wordsToFind.length}</span></div>
        <div class="score-chip">Score <span id="ws-score">0</span></div>
        <div class="score-chip">Time <span id="ws-time">2:00</span></div>
      </div>
      <div class="timer-bar"><div class="timer-fill" id="ws-timer" style="width:100%"></div></div>
      <div class="ws-layout">
        <div class="ws-grid-wrapper">
          <div class="ws-grid" id="ws-grid"></div>
        </div>
        <div class="ws-word-list">
          <h3>Find these words:</h3>
          <div id="ws-words"></div>
        </div>
      </div>
      <div class="feedback-box" id="ws-fb"></div>
    `;

    this.renderGrid();
    this.renderWordList();
  }

  renderGrid() {
    const grid = this.el.querySelector('#ws-grid');
    grid.innerHTML = '';
    for (let r = 0; r < this.gridSize; r++) {
      for (let c = 0; c < this.gridSize; c++) {
        const cell = document.createElement('div');
        cell.className = 'ws-cell';
        cell.dataset.r = r;
        cell.dataset.c = c;
        cell.textContent = this.grid[r][c];

        // Check if already found
        const cellKey = `${r},${c}`;
        let isFound = false;
        for (const w of this.foundWords) {
          if (this.wordPositions[w]?.some(([pr,pc]) => pr===r && pc===c)) { isFound = true; break; }
        }
        if (isFound) cell.classList.add('found');

        cell.addEventListener('mousedown', e => this.startSelect(e, r, c));
        cell.addEventListener('mouseover', e => this.continueSelect(e, r, c));
        cell.addEventListener('mouseup',   e => this.endSelect(e));
        cell.addEventListener('touchstart', e => { e.preventDefault(); this.startSelect(e, r, c); }, { passive: false });
        cell.addEventListener('touchmove',  e => {
          e.preventDefault();
          const t = e.touches[0];
          const el = document.elementFromPoint(t.clientX, t.clientY);
          if (el?.classList.contains('ws-cell')) this.continueSelect(e, +el.dataset.r, +el.dataset.c);
        }, { passive: false });
        cell.addEventListener('touchend', e => this.endSelect(e));
        grid.appendChild(cell);
      }
    }
  }

  renderWordList() {
    const el = this.el.querySelector('#ws-words');
    el.innerHTML = this.wordsToFind.map(wd =>
      `<div class="ws-word-item ${this.foundWords.has(wd.word) ? 'found' : ''}" id="wsw-${wd.word}">${wd.word}</div>`
    ).join('');
  }

  startSelect(e, r, c) {
    this.isSelecting = true;
    this.startCell = [r, c];
    this.selectedCells = [[r, c]];
    this.highlightSelected();
  }

  continueSelect(e, r, c) {
    if (!this.isSelecting) return;
    const [sr, sc] = this.startCell;
    const dr = r - sr, dc = c - sc;
    const len = Math.max(Math.abs(dr), Math.abs(dc));
    if (len === 0) { this.selectedCells = [[sr, sc]]; this.highlightSelected(); return; }
    const normR = dr === 0 ? 0 : dr / Math.abs(dr);
    const normC = dc === 0 ? 0 : dc / Math.abs(dc);
    if (Math.abs(dr) !== Math.abs(dc) && dr !== 0 && dc !== 0) return; // non-diagonal multi-dir
    const cells = [];
    for (let i = 0; i <= len; i++) cells.push([sr + normR*i, sc + normC*i]);
    this.selectedCells = cells;
    this.highlightSelected();
  }

  endSelect(e) {
    if (!this.isSelecting) return;
    this.isSelecting = false;
    this.checkWord();
    this.selectedCells = [];
    this.highlightSelected();
  }

  highlightSelected() {
    this.el.querySelectorAll('.ws-cell').forEach(cell => {
      const r = +cell.dataset.r, c = +cell.dataset.c;
      const isFoundCell = [...this.foundWords].some(w => this.wordPositions[w]?.some(([pr,pc]) => pr===r && pc===c));
      if (isFoundCell) { cell.className = 'ws-cell found'; return; }
      const isSel = this.selectedCells.some(([sr,sc]) => sr===r && sc===c);
      cell.className = 'ws-cell' + (isSel ? ' selected' : '');
    });
  }

  checkWord() {
    const letters = this.selectedCells.map(([r,c]) => this.grid[r][c]).join('');
    const reversed = letters.split('').reverse().join('');

    for (const wd of this.wordsToFind) {
      if (this.foundWords.has(wd.word)) continue;
      if (letters === wd.word || reversed === wd.word) {
        this.foundWords.add(wd.word);
        const timeBonus = Math.floor(this.timeLeft / 10) * 5;
        this.score += 100 + timeBonus;
        setNavScore(this.score);

        const fb = this.el.querySelector('#ws-fb');
        if (fb) {
          fb.textContent = `Found "${wd.word}"! ${wd.definition}`;
          fb.className = 'feedback-box show correct-fb';
          setTimeout(() => { if (!this.destroyed) fb.className = 'feedback-box'; }, 2000);
        }
        toast(`✓ ${wd.word}`, 'success', 1200);
        this.updateFoundUI();

        if (this.foundWords.size === this.wordsToFind.length) {
          clearInterval(this.timerInterval);
          this.score += 200; // all-found bonus
          setTimeout(() => { if (!this.destroyed) this.showEnd(true); }, 1500);
        }
        return;
      }
    }
  }

  updateFoundUI() {
    const fc = this.el.querySelector('#ws-found');
    if (fc) fc.textContent = `${this.foundWords.size}/${this.wordsToFind.length}`;
    const sc = this.el.querySelector('#ws-score');
    if (sc) sc.textContent = this.score;
    this.renderGrid();
    this.renderWordList();
  }

  startTimer() {
    clearInterval(this.timerInterval);
    this.timeLeft = 120;
    this.timerInterval = setInterval(() => {
      if (this.destroyed) { clearInterval(this.timerInterval); return; }
      this.timeLeft--;
      const m = Math.floor(this.timeLeft / 60), s = this.timeLeft % 60;
      const t = this.el.querySelector('#ws-time');
      if (t) t.textContent = `${m}:${String(s).padStart(2,'0')}`;
      const bar = this.el.querySelector('#ws-timer');
      if (bar) {
        const pct = (this.timeLeft / 120) * 100;
        bar.style.width = pct + '%';
        bar.style.background = pct > 40 ? 'var(--green)' : pct > 20 ? 'var(--yellow)' : 'var(--red)';
      }
      if (this.timeLeft <= 0) {
        clearInterval(this.timerInterval);
        this.showEnd(false);
      }
    }, 1000);
  }

  showEnd(allFound) {
    clearInterval(this.timerInterval);
    this.el.innerHTML = `
      <div class="end-screen">
        <h2>${allFound ? '🎉 All Words Found!' : '⏰ Time Up!'}</h2>
        <div class="final-score">${this.score}</div>
        <p>${this.foundWords.size} of ${this.wordsToFind.length} words found</p>
        <div class="end-actions">
          <button class="btn btn-primary" id="ws-again">Play Again</button>
          <button class="btn btn-secondary" onclick="showHub()">Hub</button>
        </div>
        <div class="leaderboard">
          <h3>🏆 Leaderboard</h3>
          <div class="name-row">
            <input class="name-input" id="ws-name" placeholder="Your name" maxlength="14" />
            <button class="btn btn-warning" id="ws-save">Save</button>
          </div>
          <div id="ws-entries"></div>
        </div>
      </div>
    `;
    this.el.querySelector('#ws-again').addEventListener('click', () => this.start());
    this.el.querySelector('#ws-save').addEventListener('click', () => this.saveScore());
    this.loadLB();
  }

  async saveScore() {
    const name = this.el.querySelector('#ws-name').value.trim() || 'Player';
    const entries = await postScore('wordsearch', name, this.score);
    this.renderLB(entries);
    toast('Score saved!', 'success');
  }

  async loadLB() {
    const entries = await getLeaderboard('wordsearch');
    this.renderLB(entries);
  }

  renderLB(entries) {
    const el = this.el.querySelector('#ws-entries');
    if (!el) return;
    el.innerHTML = entries.map((e, i) =>
      `<div class="lb-entry"><span class="lb-rank">${i+1}</span><span class="lb-name">${e.name}</span><span class="lb-score">${e.score}</span></div>`
    ).join('') || '<p class="text-muted text-center">No scores yet</p>';
  }

  destroy() { this.destroyed = true; clearInterval(this.timerInterval); }
}
