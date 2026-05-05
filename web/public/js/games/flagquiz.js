// FlagQuiz — exact Unity values:
// timePerQuestion=8f, pointsPerCorrect=100, timeBonusMultiplier=10, streakBonusMultiplier=50
// Round: 3 easy + 4 medium + 3 hard = 10 questions
// Distractors picked from same difficulty tier
class FlagQuizGame {
  constructor(el, flags) {
    this.el = el;
    this.allFlags = flags;
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
    const easy   = this.allFlags.filter(f => f.difficulty === 0);
    const medium = this.allFlags.filter(f => f.difficulty === 1);
    const hard   = this.allFlags.filter(f => f.difficulty === 2);
    // Unity: 3 easy + 4 medium + 3 hard
    this.questions = [
      ...shuffle(easy).slice(0, 3),
      ...shuffle(medium).slice(0, 4),
      ...shuffle(hard).slice(0, 3)
    ];
    this.score = 0; this.streak = 0; this.currentQ = 0;
    this.showQuestion();
  }

  showQuestion() {
    if (this.currentQ >= this.questions.length) { this.showEnd(); return; }
    const flag = this.questions[this.currentQ];
    const diffLabel = ['easy','medium','hard'][flag.difficulty] || 'easy';

    // Unity: distractors from same difficulty tier only
    const sameTier = this.allFlags.filter(f => f.difficulty === flag.difficulty && f.countryName !== flag.countryName);
    const distractors = shuffle(sameTier).slice(0, 3);
    const choices = shuffle([flag, ...distractors]);

    this.el.innerHTML = `
      <div class="score-row">
        <div class="score-chip">Q <span>${this.currentQ+1}/${this.questions.length}</span></div>
        <div class="score-chip">Score <span id="fq-score">${this.score}</span></div>
        <div class="score-chip">Streak <span id="fq-streak">${this.streak}</span></div>
      </div>
      <div class="timer-bar"><div class="timer-fill" id="fq-timer" style="width:100%"></div></div>
      <div class="text-center">
        <span class="difficulty-badge ${diffLabel}">${diffLabel}</span>
      </div>
      <div class="flag-display" id="fq-flag">${flag.emoji}</div>
      <div class="choices" id="fq-choices"></div>
      <div class="feedback-box" id="fq-feedback"></div>
    `;

    const grid = this.el.querySelector('#fq-choices');
    choices.forEach((f, i) => {
      const btn = document.createElement('button');
      btn.className = 'choice-btn';
      btn.textContent = `${['A','B','C','D'][i]}.  ${f.countryName}`;
      btn.addEventListener('click', () => this.onAnswer(btn, f.countryName === flag.countryName, flag.countryName));
      grid.appendChild(btn);
    });

    this.startTimer(8); // Unity: timePerQuestion = 8f
  }

  startTimer(secs) {
    clearInterval(this.timerInterval);
    this.timeLeft = secs;
    this.inputEnabled = true;
    const bar = this.el.querySelector('#fq-timer');
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
        if (this.inputEnabled) this.onAnswer(null, false, '(time up)');
      }
    }, 1000);
  }

  onAnswer(btn, correct, correctName) {
    if (!this.inputEnabled) return;
    this.inputEnabled = false;
    clearInterval(this.timerInterval);

    // Unity: score += pointsPerCorrect + (timeLeft * timeBonusMultiplier) + (streak * streakBonusMultiplier)
    if (correct) {
      const timeBonus = this.timeLeft * 10;
      const streakBonus = this.streak * 50; // streak before increment
      this.streak++;
      this.score += 100 + timeBonus + streakBonus;
      if (btn) btn.classList.add('correct');
    } else {
      this.streak = 0;
      if (btn) btn.classList.add('wrong');
      this.el.querySelectorAll('.choice-btn').forEach(b => {
        if (b.textContent.includes(correctName)) b.classList.add('correct');
      });
    }

    this.el.querySelectorAll('.choice-btn').forEach(b => b.disabled = true);
    const fb = this.el.querySelector('#fq-feedback');
    if (fb) {
      fb.classList.add('show', correct ? 'correct-fb' : 'wrong-fb');
      fb.textContent = correct
        ? `✓ Correct! +${100 + this.timeLeft * 10}pts${this.streak > 1 ? ` (streak x${this.streak})` : ''}`
        : `✗ The correct answer was ${correctName}`;
    }
    const sc = this.el.querySelector('#fq-score');
    if (sc) sc.textContent = this.score;
    setNavScore(this.score);

    setTimeout(() => { if (!this.destroyed) { this.currentQ++; this.showQuestion(); } }, 1800);
  }

  showEnd() {
    clearInterval(this.timerInterval);
    this.el.innerHTML = `
      <div class="end-screen">
        <h2>🚩 Round Complete!</h2>
        <div class="final-score">${this.score}</div>
        <p>${this.questions.length} flags · best streak ${this.streak}</p>
        <div class="end-actions">
          <button class="btn btn-primary" id="fq-again">Play Again</button>
          <button class="btn btn-secondary" onclick="showHub()">Hub</button>
        </div>
        <div class="leaderboard" id="fq-lb">
          <h3>🏆 Leaderboard</h3>
          <div class="name-row">
            <input class="name-input" id="fq-name" placeholder="Your name" maxlength="14" />
            <button class="btn btn-warning" id="fq-save">Save</button>
          </div>
          <div id="fq-entries"></div>
        </div>
      </div>
    `;
    this.el.querySelector('#fq-again').addEventListener('click', () => this.start());
    this.el.querySelector('#fq-save').addEventListener('click', () => this.saveScore());
    this.loadLB();
  }

  async saveScore() {
    const name = this.el.querySelector('#fq-name').value.trim() || 'Player';
    const entries = await postScore('flagquiz', name, this.score);
    this.renderLB(entries);
    toast('Score saved!', 'success');
  }

  async loadLB() {
    const entries = await getLeaderboard('flagquiz');
    this.renderLB(entries);
  }

  renderLB(entries) {
    const el = this.el.querySelector('#fq-entries');
    if (!el) return;
    el.innerHTML = entries.map((e, i) =>
      `<div class="lb-entry"><span class="lb-rank">${i+1}</span><span class="lb-name">${e.name}</span><span class="lb-score">${e.score}</span></div>`
    ).join('') || '<p class="text-muted text-center">No scores yet</p>';
  }

  destroy() { this.destroyed = true; clearInterval(this.timerInterval); }
}
