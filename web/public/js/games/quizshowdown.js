class QuizShowdownGame {
  constructor(el, questions) {
    this.el = el;
    this.allQuestions = questions;
    this.basePoints = [100,200,300,500,1000,2000,4000,8000,16000,32000,64000,125000,250000,500000,1000000];
    this.score = 0;
    this.streak = 0;
    this.currentQ = 0;
    this.roundQuestions = [];
    this.timerInterval = null;
    this.timeLeft = 0;
    this.inputEnabled = false;
    this.destroyed = false;
    this.fiftyFiftyUsed = false;
    this.hintUsed = false;
    this.pauseUsed = false;
    this.hiddenChoices = new Set();
  }

  start() {
    this.score = 0; this.streak = 0; this.currentQ = 0;
    this.fiftyFiftyUsed = false; this.hintUsed = false; this.pauseUsed = false;
    this.roundQuestions = this.buildRound();
    this.showQuestion();
  }

  buildRound() {
    const result = [];
    for (let tier = 1; tier <= 5; tier++) {
      const pool = shuffle(this.allQuestions.filter(q => q.difficulty === tier));
      result.push(...pool.slice(0, 3));
    }
    return result;
  }

  showQuestion() {
    if (this.currentQ >= this.roundQuestions.length) { this.showEnd(true); return; }
    const q = this.roundQuestions[this.currentQ];
    this.hiddenChoices = new Set();
    const basePts = this.basePoints[Math.min(this.currentQ, this.basePoints.length - 1)];
    const tier = Math.floor(this.currentQ / 3) + 1;

    const tierDots = Array.from({length:5}, (_,i) =>
      `<div class="qs-tier ${i+1 < tier ? 'done' : i+1 === tier ? 'current' : ''}">Tier ${i+1}</div>`
    ).join('');

    this.el.innerHTML = `
      <div class="qs-tier-bar">${tierDots}</div>
      <div class="score-row">
        <div class="score-chip">Q <span>${this.currentQ+1}/${this.roundQuestions.length}</span></div>
        <div class="score-chip">Score <span id="qs-score">${this.score.toLocaleString()}</span></div>
        <div class="score-chip">Streak <span>${this.streak}</span></div>
      </div>
      <div class="qs-worth">Worth: <strong>${basePts.toLocaleString()} pts</strong> (×${this.getMultiplier()} streak bonus)</div>
      <div class="timer-bar"><div class="timer-fill" id="qs-timer" style="width:100%"></div></div>
      <div class="lifelines">
        <button class="lifeline-btn" id="qs-5050" ${this.fiftyFiftyUsed ? 'disabled' : ''}>50/50</button>
        <button class="lifeline-btn" id="qs-hint" ${this.hintUsed ? 'disabled' : ''}>💡 Hint</button>
        <button class="lifeline-btn" id="qs-pause" ${this.pauseUsed ? 'disabled' : ''}>⏸ +30s</button>
      </div>
      <div class="hint-box" id="qs-hint-box"></div>
      <div class="question-box">${q.question}</div>
      <div class="choices" id="qs-choices"></div>
      <div class="feedback-box" id="qs-fb"></div>
    `;

    const choicesEl = this.el.querySelector('#qs-choices');
    q.options.forEach((opt, i) => {
      const btn = document.createElement('button');
      btn.className = 'choice-btn';
      btn.textContent = `${['A','B','C','D'][i]}.  ${opt}`;
      btn.dataset.idx = i;
      btn.addEventListener('click', () => this.onAnswer(i, q));
      choicesEl.appendChild(btn);
    });

    this.el.querySelector('#qs-5050').addEventListener('click', () => this.useFiftyFifty(q));
    this.el.querySelector('#qs-hint').addEventListener('click', () => this.useHint(q));
    this.el.querySelector('#qs-pause').addEventListener('click', () => this.usePause());

    this.startTimer(30);
  }

  getMultiplier() {
    const steps = Math.min(this.streak, 4);
    return (100 + steps * 50) / 100 + 'x';
  }

  startTimer(secs) {
    clearInterval(this.timerInterval);
    this.timeLeft = secs;
    this.inputEnabled = true;
    const bar = this.el.querySelector('#qs-timer');
    this.timerInterval = setInterval(() => {
      if (this.destroyed) { clearInterval(this.timerInterval); return; }
      this.timeLeft--;
      if (bar) {
        const pct = (this.timeLeft / secs) * 100;
        bar.style.width = pct + '%';
        bar.style.background = pct > 40 ? 'var(--green)' : pct > 20 ? 'var(--yellow)' : 'var(--red)';
      }
      if (this.timeLeft <= 0) {
        clearInterval(this.timerInterval);
        if (this.inputEnabled) this.onAnswer(-1, this.roundQuestions[this.currentQ]);
      }
    }, 1000);
  }

  onAnswer(idx, q) {
    if (!this.inputEnabled) return;
    if (this.hiddenChoices.has(idx)) return;
    this.inputEnabled = false;
    clearInterval(this.timerInterval);

    const correct = idx === q.correctIndex;
    const basePts = this.basePoints[Math.min(this.currentQ, this.basePoints.length - 1)];

    if (correct) {
      this.streak++;
      const multPct = 100 + Math.min(this.streak - 1, 4) * 50;
      const pts = Math.floor(basePts * multPct / 100);
      this.score += pts;
    } else {
      this.streak = 0;
    }

    // highlight buttons
    this.el.querySelectorAll('.choice-btn').forEach(b => {
      b.disabled = true;
      if (+b.dataset.idx === q.correctIndex) b.classList.add('correct');
      else if (+b.dataset.idx === idx && !correct) b.classList.add('wrong');
    });

    const fb = this.el.querySelector('#qs-fb');
    if (fb) {
      fb.classList.add('show', correct ? 'correct-fb' : 'wrong-fb');
      fb.textContent = correct ? `✓ Correct! +${Math.floor(basePts * (100 + Math.min(this.streak-1,4)*50)/100).toLocaleString()} pts` : `✗ Correct answer: ${q.options[q.correctIndex]}`;
    }
    const sc = this.el.querySelector('#qs-score');
    if (sc) sc.textContent = this.score.toLocaleString();
    setNavScore(this.score);

    setTimeout(() => {
      if (this.destroyed) return;
      if (!correct) { this.showEnd(false); return; }
      this.currentQ++;
      this.showQuestion();
    }, 2200);
  }

  useFiftyFifty(q) {
    if (this.fiftyFiftyUsed || !this.inputEnabled) return;
    this.fiftyFiftyUsed = true;
    const wrongs = shuffle([0,1,2,3].filter(i => i !== q.correctIndex));
    this.hiddenChoices = new Set([wrongs[0], wrongs[1]]);
    this.el.querySelectorAll('.choice-btn').forEach(b => {
      if (this.hiddenChoices.has(+b.dataset.idx)) b.classList.add('hidden');
    });
    this.el.querySelector('#qs-5050').disabled = true;
  }

  useHint(q) {
    if (this.hintUsed || !this.inputEnabled) return;
    this.hintUsed = true;
    const box = this.el.querySelector('#qs-hint-box');
    if (box) { box.textContent = `💡 Hint: ${q.hint}`; box.classList.add('show'); }
    this.el.querySelector('#qs-hint').disabled = true;
  }

  usePause() {
    if (this.pauseUsed || !this.inputEnabled) return;
    this.pauseUsed = true;
    this.timeLeft += 30;
    this.el.querySelector('#qs-pause').disabled = true;
    toast('+30 seconds added!', 'success', 1500);
  }

  showEnd(wonAll) {
    clearInterval(this.timerInterval);
    this.el.innerHTML = `
      <div class="end-screen">
        <h2 style="color:${wonAll ? 'var(--green)' : 'var(--red)'}">${wonAll ? '🏆 PERFECT GAME!' : '💀 GAME OVER'}</h2>
        <div class="final-score">${this.score.toLocaleString()}</div>
        <p>${this.currentQ} questions answered${wonAll ? ' — Full board cleared!' : ''}</p>
        <div class="end-actions">
          <button class="btn btn-primary" id="qs-again">Play Again</button>
          <button class="btn btn-secondary" onclick="showHub()">Hub</button>
        </div>
        <div class="leaderboard">
          <h3>🏆 Leaderboard</h3>
          <div class="name-row">
            <input class="name-input" id="qs-name" placeholder="Your name" maxlength="14" />
            <button class="btn btn-warning" id="qs-save">Save Score</button>
          </div>
          <div id="qs-entries"></div>
        </div>
      </div>
    `;
    this.el.querySelector('#qs-again').addEventListener('click', () => this.start());
    this.el.querySelector('#qs-save').addEventListener('click', () => this.saveScore());
    this.loadLB();
  }

  async saveScore() {
    const name = this.el.querySelector('#qs-name').value.trim() || 'Player';
    const entries = await postScore('quizshowdown', name, this.score);
    this.renderLB(entries);
    toast('Score saved!', 'success');
  }

  async loadLB() {
    const entries = await getLeaderboard('quizshowdown');
    this.renderLB(entries);
  }

  renderLB(entries) {
    const el = this.el.querySelector('#qs-entries');
    if (!el) return;
    el.innerHTML = entries.map((e, i) =>
      `<div class="lb-entry"><span class="lb-rank">${i+1}</span><span class="lb-name">${e.name}</span><span class="lb-score">${e.score.toLocaleString()}</span></div>`
    ).join('') || '<p class="text-muted text-center">No scores yet</p>';
  }

  destroy() { this.destroyed = true; clearInterval(this.timerInterval); }
}
