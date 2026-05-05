// MazeRunner — exact Unity values:
// Timer counts UP (elapsed seconds). penaltyPerWrongAnswer=50 (gate stays locked, retry by touching again).
// timeBonusBase=1500, decaySeconds=90: finalBonus = max(0, round(1500*(1-elapsed/90)))
// pointsPerGate=200 (no time component per gate)
class MazeRunnerGame {
  constructor(el, questions) {
    this.el = el;
    this.allQuestions = questions;
    this.score = 0;
    this.gatesCleared = 0;
    this.destroyed = false;
    this.gameWon = false;
    this.gameLost = false;
    this.timerInterval = null;
    this.startTime = 0;
    this.elapsed = 0;
    this.inputEnabled = false;
    this.animFrame = null;
    this.keyState = {};
    this.gateModal = null;
  }

  // Maze: 0=path, 1=wall, 2=gate, 3=start, 4=goal
  buildMaze() {
    return [
      [1,1,1,1,1,1,1,1,1,1,1,1,1,1,1],
      [1,3,0,0,0,0,0,1,0,0,0,0,0,0,1],
      [1,1,1,0,1,1,0,1,0,1,1,1,0,1,1],
      [1,0,0,0,1,0,0,2,0,0,1,0,0,0,1],
      [1,0,1,1,1,0,1,1,1,0,1,1,1,0,1],
      [1,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
      [1,1,1,0,1,1,1,2,1,1,1,0,1,1,1],
      [1,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
      [1,0,1,1,1,0,1,1,1,0,1,1,1,0,1],
      [1,0,0,0,1,0,0,2,0,0,1,0,0,0,1],
      [1,1,1,0,1,1,0,1,0,1,1,1,0,1,1],
      [1,0,0,0,0,0,0,1,0,0,0,0,0,0,1],
      [1,0,1,1,1,1,1,2,1,1,1,1,1,0,1],
      [1,0,0,0,0,0,0,0,0,0,0,0,0,2,1],
      [1,1,1,1,1,1,1,1,1,1,1,1,1,4,1],
    ];
  }

  start() {
    this.maze = this.buildMaze();
    this.cellSize = 36;
    this.cols = this.maze[0].length;
    this.rows = this.maze.length;

    outer: for (let r = 0; r < this.rows; r++)
      for (let c = 0; c < this.cols; c++)
        if (this.maze[r][c] === 3) { this.playerRow = r; this.playerCol = c; break outer; }

    this.gates = [];
    for (let r = 0; r < this.rows; r++)
      for (let c = 0; c < this.cols; c++)
        if (this.maze[r][c] === 2) this.gates.push({ r, c, cleared: false });

    this.questions = shuffle(this.allQuestions).slice(0, this.gates.length);
    this.gatesCleared = 0;
    this.score = 0;
    this.elapsed = 0;
    this.startTime = Date.now();
    this.gameWon = false;
    this.gameLost = false;
    this.keyState = {};
    this.moveAccum = 0;

    this.el.innerHTML = `
      <div class="score-row">
        <div class="score-chip">Gates <span id="mr-gates">0/${this.gates.length}</span></div>
        <div class="score-chip">Score <span id="mr-score">0</span></div>
        <div class="score-chip">Time <span id="mr-time">0:00</span></div>
      </div>
      <div class="maze-wrapper">
        <canvas id="maze-canvas"></canvas>
        <p class="maze-controls-hint">Arrow keys or WASD to move · Reach 🏁 after clearing all gates</p>
      </div>
    `;

    this.canvas = this.el.querySelector('#maze-canvas');
    this.canvas.width  = this.cols * this.cellSize;
    this.canvas.height = this.rows * this.cellSize;
    this.ctx = this.canvas.getContext('2d');

    this.keydownHandler = e => {
      this.keyState[e.key] = true;
      if (['ArrowUp','ArrowDown','ArrowLeft','ArrowRight'].includes(e.key)) e.preventDefault();
    };
    this.keyupHandler = e => { this.keyState[e.key] = false; };
    window.addEventListener('keydown', this.keydownHandler);
    window.addEventListener('keyup',   this.keyupHandler);

    this.startTimer();
    this.gameLoop();
  }

  startTimer() {
    clearInterval(this.timerInterval);
    // Timer counts UP — display elapsed seconds
    this.timerInterval = setInterval(() => {
      if (this.destroyed || this.gameWon || this.gameLost) return;
      this.elapsed = Math.floor((Date.now() - this.startTime) / 1000);
      const m = Math.floor(this.elapsed / 60), s = this.elapsed % 60;
      const t = this.el.querySelector('#mr-time');
      if (t) t.textContent = `${m}:${String(s).padStart(2,'0')}`;
    }, 1000);
  }

  gameLoop() {
    if (this.destroyed || this.gameWon || this.gameLost) return;
    if (!this.gateModal) this.handleInput();
    this.draw();
    this.animFrame = requestAnimationFrame(() => this.gameLoop());
  }

  handleInput() {
    this.moveAccum = (this.moveAccum || 0) + 1;
    if (this.moveAccum < 8) return;
    this.moveAccum = 0;

    const up    = this.keyState['ArrowUp']    || this.keyState['w'] || this.keyState['W'];
    const down  = this.keyState['ArrowDown']  || this.keyState['s'] || this.keyState['S'];
    const left  = this.keyState['ArrowLeft']  || this.keyState['a'] || this.keyState['A'];
    const right = this.keyState['ArrowRight'] || this.keyState['d'] || this.keyState['D'];

    let dr = 0, dc = 0;
    if (up)    dr = -1;
    if (down)  dr =  1;
    if (left)  dc = -1;
    if (right) dc =  1;
    if (!dr && !dc) return;

    const nr = this.playerRow + dr;
    const nc = this.playerCol + dc;
    if (nr < 0 || nr >= this.rows || nc < 0 || nc >= this.cols) return;
    const cell = this.maze[nr][nc];
    if (cell === 1) return;

    if (cell === 2) {
      const gate = this.gates.find(g => g.r === nr && g.c === nc && !g.cleared);
      if (gate) { this.triggerGate(gate, nr, nc); return; }
    }

    if (cell === 4) {
      if (this.gatesCleared >= this.gates.length) {
        this.playerRow = nr; this.playerCol = nc;
        this.gameWon = true;
        clearInterval(this.timerInterval);
        this.elapsed = Math.floor((Date.now() - this.startTime) / 1000);
        cancelAnimationFrame(this.animFrame);
        this.draw();
        setTimeout(() => { if (!this.destroyed) this.showEnd(true); }, 600);
        return;
      }
      toast('Clear all gates first!', '', 1200);
      return;
    }

    this.playerRow = nr;
    this.playerCol = nc;
  }

  triggerGate(gate, nr, nc) {
    const q = this.questions[this.gates.indexOf(gate)];
    if (!q) return;
    this.gateModal = { gate, nr, nc, q };

    const modal = document.createElement('div');
    modal.className = 'maze-gate-modal';
    const choicesHtml = q.options.map((opt, i) =>
      `<button class="choice-btn" data-idx="${i}" style="margin-bottom:8px">${['A','B','C','D'][i]}.  ${opt}</button>`
    ).join('');

    modal.innerHTML = `
      <div class="maze-gate-inner">
        <h3>🚧 Gate ${this.gatesCleared + 1} — Answer to pass!</h3>
        <div class="question-box" style="font-size:1rem;margin-bottom:16px">${q.question}</div>
        <div>${choicesHtml}</div>
        <div class="feedback-box" id="mr-modal-fb" style="margin-top:12px"></div>
      </div>
    `;

    document.body.appendChild(modal);

    modal.querySelectorAll('.choice-btn').forEach(btn => {
      btn.addEventListener('click', () => this.onGateAnswer(btn, +btn.dataset.idx, q, gate, nr, nc, modal));
    });
  }

  onGateAnswer(btn, idx, q, gate, nr, nc, modal) {
    modal.querySelectorAll('.choice-btn').forEach(b => b.disabled = true);
    const correct = idx === q.correctIndex;
    btn.classList.add(correct ? 'correct' : 'wrong');
    if (!correct) modal.querySelectorAll('.choice-btn')[q.correctIndex].classList.add('correct');

    const fb = modal.querySelector('#mr-modal-fb');
    if (fb) {
      fb.classList.add('show', correct ? 'correct-fb' : 'wrong-fb');
      fb.textContent = correct ? `✓ Correct! ${q.explanation}` : `✗ ${q.explanation}`;
    }

    setTimeout(() => {
      document.body.removeChild(modal);
      this.gateModal = null;

      if (correct) {
        gate.cleared = true;
        this.maze[gate.r][gate.c] = 0;
        this.gatesCleared++;
        // Unity: pointsPerGate = 200 (no per-gate time bonus)
        this.score += 200;
        setNavScore(this.score);
        const gc = this.el.querySelector('#mr-gates');
        if (gc) gc.textContent = `${this.gatesCleared}/${this.gates.length}`;
        const sc = this.el.querySelector('#mr-score');
        if (sc) sc.textContent = this.score;
        this.playerRow = nr; this.playerCol = nc;
        toast('Gate cleared! +200 pts', 'success', 1200);
      } else {
        // Unity: penaltyPerWrongAnswer = 50, gate stays locked (player can retry by touching again)
        this.score = Math.max(0, this.score - 50);
        setNavScore(this.score);
        const sc = this.el.querySelector('#mr-score');
        if (sc) sc.textContent = this.score;
        toast('Wrong! -50 pts · touch gate again to retry', 'error', 1800);
      }
    }, 1800);
  }

  draw() {
    const ctx = this.ctx;
    const cs = this.cellSize;
    ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

    const colors = { 1:'#1a1d2e', 0:'#252840', 2:'#eab308', 3:'#22c55e', 4:'#6c63ff' };

    for (let r = 0; r < this.rows; r++) {
      for (let c = 0; c < this.cols; c++) {
        const cell = this.maze[r][c];
        ctx.fillStyle = colors[cell] || '#252840';
        ctx.fillRect(c*cs, r*cs, cs, cs);

        if (cell === 2) {
          const gate = this.gates.find(g => g.r === r && g.c === c);
          if (gate && !gate.cleared) {
            ctx.fillStyle = '#eab308';
            ctx.fillRect(c*cs+2, r*cs+2, cs-4, cs-4);
            ctx.fillStyle = '#1a1d2e';
            ctx.font = `${cs*0.6}px serif`;
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText('🚧', c*cs + cs/2, r*cs + cs/2);
          }
        } else if (cell === 4) {
          ctx.font = `${cs*0.65}px serif`;
          ctx.textAlign = 'center';
          ctx.textBaseline = 'middle';
          ctx.fillText('🏁', c*cs + cs/2, r*cs + cs/2);
        }

        ctx.strokeStyle = '#0f1117';
        ctx.lineWidth = 1;
        ctx.strokeRect(c*cs, r*cs, cs, cs);
      }
    }

    ctx.font = `${this.cellSize*0.7}px serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('🔵', this.playerCol*this.cellSize + this.cellSize/2, this.playerRow*this.cellSize + this.cellSize/2);
  }

  showEnd(won) {
    clearInterval(this.timerInterval);
    cancelAnimationFrame(this.animFrame);

    // Unity: timeBonusBase=1500, decaySeconds=90, bonus = max(0, round(1500*(1-elapsed/90)))
    const timeBonus = won ? Math.max(0, Math.round(1500 * (1 - this.elapsed / 90))) : 0;
    if (won) this.score += timeBonus;
    setNavScore(this.score);

    const m = Math.floor(this.elapsed / 60), s = this.elapsed % 60;
    const timeStr = `${m}:${String(s).padStart(2,'0')}`;

    this.el.innerHTML = `
      <div class="end-screen">
        <h2 style="color:${won ? 'var(--green)' : 'var(--red)'}">${won ? '🏆 Maze Escaped!' : '💀 Game Over!'}</h2>
        <div class="final-score">${this.score}</div>
        <p>${this.gatesCleared}/${this.gates.length} gates · time: ${timeStr}${won ? ` · +${timeBonus} time bonus` : ''}</p>
        <div class="end-actions">
          <button class="btn btn-primary" id="mr-again">Play Again</button>
          <button class="btn btn-secondary" onclick="showHub()">Hub</button>
        </div>
        <div class="leaderboard">
          <h3>🏆 Leaderboard</h3>
          <div class="name-row">
            <input class="name-input" id="mr-name" placeholder="Your name" maxlength="14" />
            <button class="btn btn-warning" id="mr-save">Save</button>
          </div>
          <div id="mr-entries"></div>
        </div>
      </div>
    `;
    this.el.querySelector('#mr-again').addEventListener('click', () => this.start());
    this.el.querySelector('#mr-save').addEventListener('click', () => this.saveScore());
    this.loadLB();
  }

  async saveScore() {
    const name = this.el.querySelector('#mr-name').value.trim() || 'Player';
    const entries = await postScore('mazerunner', name, this.score);
    this.renderLB(entries);
    toast('Score saved!', 'success');
  }

  async loadLB() {
    const entries = await getLeaderboard('mazerunner');
    this.renderLB(entries);
  }

  renderLB(entries) {
    const el = this.el.querySelector('#mr-entries');
    if (!el) return;
    el.innerHTML = entries.map((e, i) =>
      `<div class="lb-entry"><span class="lb-rank">${i+1}</span><span class="lb-name">${e.name}</span><span class="lb-score">${e.score}</span></div>`
    ).join('') || '<p class="text-muted text-center">No scores yet</p>';
  }

  destroy() {
    this.destroyed = true;
    clearInterval(this.timerInterval);
    cancelAnimationFrame(this.animFrame);
    window.removeEventListener('keydown', this.keydownHandler);
    window.removeEventListener('keyup',   this.keyupHandler);
    const modal = document.querySelector('.maze-gate-modal');
    if (modal) modal.remove();
  }
}
