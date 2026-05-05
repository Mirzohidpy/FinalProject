class TrueFalseGame {
  constructor(el, headlines) {
    this.el = el;
    this.headlines = headlines;
    this.score = 0;
    this.streak = 0;
    this.currentQ = 0;
    this.questions = [];
    this.timerInterval = null;
    this.timeLeft = 0;
    this.inputEnabled = false;
    this.destroyed = false;
  }

  start() {
    this.questions = shuffle(this.headlines).slice(0, 10);
    this.score = 0; this.streak = 0; this.currentQ = 0;
    this.showQuestion();
  }

  showQuestion() {
    if (this.currentQ >= this.questions.length) { this.showEnd(); return; }
    const h = this.questions[this.currentQ];

    this.el.innerHTML = `
      <div class="score-row">
        <div class="score-chip">Q <span>${this.currentQ+1}/10</span></div>
        <div class="score-chip">Score <span id="tfn-score">${this.score}</span></div>
        <div class="score-chip">Streak <span>${this.streak}🔥</span></div>
      </div>
      <div class="timer-bar"><div class="timer-fill" id="tfn-timer" style="width:100%"></div></div>
      <div class="text-center">
        <span class="category-badge">${h.category}</span>
      </div>
      <div class="headline-card">"${h.headline}"</div>
      <div class="tfn-buttons">
        <button class="btn btn-success" id="tfn-real">✅ Real</button>
        <button class="btn btn-danger"  id="tfn-fake">❌ Fake</button>
      </div>
      <div class="feedback-box" id="tfn-fb"></div>
    `;

    this.el.querySelector('#tfn-real').addEventListener('click', () => this.onAnswer(true, h));
    this.el.querySelector('#tfn-fake').addEventListener('click', () => this.onAnswer(false, h));
    this.startTimer(20);
  }

  startTimer(secs) {
    clearInterval(this.timerInterval);
    this.timeLeft = secs;
    this.inputEnabled = true;
    const bar = this.el.querySelector('#tfn-timer');
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
        if (this.inputEnabled) this.onAnswer(null, this.questions[this.currentQ]);
      }
    }, 1000);
  }

  onAnswer(playerSaysReal, h) {
    if (!this.inputEnabled) return;
    this.inputEnabled = false;
    clearInterval(this.timerInterval);

    const correct = playerSaysReal === h.isReal;
    const timeBonus = correct ? this.timeLeft * 5 : 0;

    if (correct) {
      this.streak++;
      const streakBonus = (this.streak - 1) * 30;
      this.score += 100 + timeBonus + streakBonus;
    } else {
      this.streak = 0;
    }

    const fb = this.el.querySelector('#tfn-fb');
    if (fb) {
      fb.classList.add('show', correct ? 'correct-fb' : 'wrong-fb');
      const verdict = h.isReal ? '✅ REAL' : '❌ FAKE';
      const srcLink = h.sourceUrl
        ? `<a href="${h.sourceUrl}" target="_blank" rel="noopener" style="color:var(--accent2);text-decoration:underline">${h.sourceHint}</a>`
        : `<em>${h.sourceHint}</em>`;
      fb.innerHTML = `<strong>${verdict}</strong> — ${h.explanation}<br><span style="color:var(--muted);font-size:.85rem">Source: ${srcLink}</span>`;
    }
    const sc = this.el.querySelector('#tfn-score');
    if (sc) sc.textContent = this.score;
    setNavScore(this.score);

    this.el.querySelectorAll('.tfn-buttons .btn').forEach(b => b.disabled = true);
    setTimeout(() => { if (!this.destroyed) { this.currentQ++; this.showQuestion(); } }, 2400);
  }

  showEnd() {
    this.el.innerHTML = `
      <div class="end-screen">
        <h2>📰 Round Complete!</h2>
        <div class="final-score">${this.score}</div>
        <p>You rated 10 headlines</p>
        <div class="end-actions">
          <button class="btn btn-primary" id="tfn-again">Play Again</button>
          <button class="btn btn-secondary" onclick="showHub()">Hub</button>
        </div>
        <div class="leaderboard">
          <h3>🏆 Leaderboard</h3>
          <div class="name-row">
            <input class="name-input" id="tfn-name" placeholder="Your name" maxlength="14" />
            <button class="btn btn-warning" id="tfn-save">Save</button>
          </div>
          <div id="tfn-entries"></div>
        </div>
      </div>
    `;
    this.el.querySelector('#tfn-again').addEventListener('click', () => this.start());
    this.el.querySelector('#tfn-save').addEventListener('click', () => this.saveScore());
    this.loadLB();
  }

  async saveScore() {
    const name = this.el.querySelector('#tfn-name').value.trim() || 'Player';
    const entries = await postScore('truefalsegame', name, this.score);
    this.renderLB(entries);
    toast('Score saved!', 'success');
  }

  async loadLB() {
    const entries = await getLeaderboard('truefalsegame');
    this.renderLB(entries);
  }

  renderLB(entries) {
    const el = this.el.querySelector('#tfn-entries');
    if (!el) return;
    el.innerHTML = entries.map((e, i) =>
      `<div class="lb-entry"><span class="lb-rank">${i+1}</span><span class="lb-name">${e.name}</span><span class="lb-score">${e.score}</span></div>`
    ).join('') || '<p class="text-muted text-center">No scores yet</p>';
  }

  destroy() { this.destroyed = true; clearInterval(this.timerInterval); }
}
