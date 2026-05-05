// MathSprint — exact Unity values:
// 4-button multiple choice (NOT text input)
// pointsPerCorrect=100, streakBonusMultiplier=20, timeBonusMultiplier=10
// feedbackTime=1.4s, distractors: range=max(5, abs(correct)/4)
class MathSprintGame {
  constructor(el, levels) {
    this.el = el;
    this.levels = levels;
    this.score = 0;
    this.currentLevel = 0;
    this.currentQ = 0;
    this.timerInterval = null;
    this.timeLeft = 0;
    this.inputEnabled = false;
    this.destroyed = false;
    this.streak = 0;
    this.bestStreak = 0;
    this.currentAnswer = 0;
  }

  start() {
    this.score = 0; this.currentLevel = 0; this.streak = 0; this.bestStreak = 0;
    this.startLevel();
  }

  startLevel() {
    if (this.currentLevel >= this.levels.length) { this.showEnd(true); return; }
    const lvl = this.levels[this.currentLevel];
    this.currentQ = 0;

    this.el.innerHTML = `
      <div class="math-level-bar">${this.levels.map((l, i) =>
        `<div class="level-dot ${i < this.currentLevel ? 'done' : i === this.currentLevel ? 'active' : ''}"></div>`
      ).join('')}</div>
      <div class="score-row">
        <div class="score-chip">Level <span>${this.currentLevel+1}/${this.levels.length}: ${lvl.levelName || lvl.operation}</span></div>
        <div class="score-chip">Score <span id="ms-score">${this.score}</span></div>
        <div class="score-chip">Streak <span id="ms-streak">${this.streak}</span></div>
      </div>
      ${lvl.suddenDeath ? '<p class="text-center text-red" style="margin-bottom:12px;font-weight:700">SUDDEN DEATH — one wrong answer ends the game!</p>' : ''}
      <div class="timer-bar"><div class="timer-fill" id="ms-timer" style="width:100%"></div></div>
      <div class="math-equation" id="ms-eq">?</div>
      <div class="choices" id="ms-choices"></div>
      <div class="feedback-box" id="ms-fb"></div>
    `;

    this.showNextQuestion();
  }

  generateQuestion(lvl) {
    const r = () => lvl.minOperand + Math.floor(Math.random() * (lvl.maxOperand - lvl.minOperand + 1));
    let op = lvl.operation;
    if (op === 'mixAddSub') op = Math.random() < .5 ? 'add' : 'subtract';
    if (op === 'mixAll') { const o = ['add','subtract','multiply']; op = o[Math.floor(Math.random()*3)]; }

    let a = r(), b = r(), ans, sym;
    if (op === 'subtract') {
      if (a < b) [a, b] = [b, a];
      ans = a - b; sym = '-';
    } else if (op === 'multiply') {
      ans = a * b; sym = 'x';
    } else {
      ans = a + b; sym = '+';
    }
    return { eq: `${a}  ${sym}  ${b}  =  ?`, ans };
  }

  // Unity: range = Max(5, Abs(correct) / 4), distractors = correct +/- random within range
  generateChoices(correct) {
    const range = Math.max(5, Math.floor(Math.abs(correct) / 4));
    const choices = new Set([correct]);
    let attempts = 0;
    while (choices.size < 4 && attempts < 50) {
      const offset = Math.floor(Math.random() * range * 2 + 1) - range;
      if (offset !== 0) choices.add(correct + offset);
      attempts++;
    }
    // fallback if not enough unique values
    let fallback = 1;
    while (choices.size < 4) { choices.add(correct + fallback); fallback++; }
    return shuffle([...choices]);
  }

  showNextQuestion() {
    if (this.currentQ >= this.levels[this.currentLevel].questions) {
      clearInterval(this.timerInterval);
      this.currentLevel++;
      setTimeout(() => { if (!this.destroyed) this.startLevel(); }, 600);
      return;
    }
    const lvl = this.levels[this.currentLevel];
    const q = this.generateQuestion(lvl);
    this.currentAnswer = q.ans;

    const eq = this.el.querySelector('#ms-eq');
    if (eq) eq.textContent = q.eq;
    const fb = this.el.querySelector('#ms-fb');
    if (fb) { fb.className = 'feedback-box'; fb.textContent = ''; }

    // Build 4 multiple-choice buttons
    const choicesEl = this.el.querySelector('#ms-choices');
    if (choicesEl) {
      choicesEl.innerHTML = '';
      const options = this.generateChoices(q.ans);
      options.forEach((val, i) => {
        const btn = document.createElement('button');
        btn.className = 'choice-btn';
        btn.textContent = `${['A','B','C','D'][i]}.  ${val}`;
        btn.addEventListener('click', () => this.processAnswer(val === q.ans, val, q.ans, btn));
        choicesEl.appendChild(btn);
      });
    }

    this.startTimer(lvl.seconds);
  }

  startTimer(secs) {
    clearInterval(this.timerInterval);
    this.timeLeft = secs;
    this.inputEnabled = true;
    const bar = this.el.querySelector('#ms-timer');
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
        if (this.inputEnabled) this.processAnswer(false, null, this.currentAnswer, null);
      }
    }, 1000);
  }

  processAnswer(correct, chosen, correctAns, clickedBtn) {
    if (!this.inputEnabled) return;
    this.inputEnabled = false;
    clearInterval(this.timerInterval);

    const lvl = this.levels[this.currentLevel];

    // Disable all buttons and highlight
    this.el.querySelectorAll('.choice-btn').forEach(b => {
      b.disabled = true;
      if (parseInt(b.textContent.split('.')[1]) === correctAns) b.classList.add('correct');
    });
    if (clickedBtn && !correct) clickedBtn.classList.add('wrong');

    if (correct) {
      // Unity: score += pointsPerCorrect + (timeLeft * timeBonusMultiplier) + (streak * streakBonusMultiplier)
      const timeBonus  = this.timeLeft * 10;
      const streakBonus = this.streak * 20;
      this.streak++;
      this.bestStreak = Math.max(this.bestStreak, this.streak);
      this.score += 100 + timeBonus + streakBonus;
      if (clickedBtn) clickedBtn.classList.add('correct');
    } else {
      this.streak = 0;
      if (lvl.suddenDeath) {
        const fb = this.el.querySelector('#ms-fb');
        if (fb) { fb.classList.add('show','wrong-fb'); fb.textContent = `✗ Answer was ${correctAns} — SUDDEN DEATH!`; }
        setTimeout(() => { if (!this.destroyed) this.showEnd(false); }, 1400);
        return;
      }
    }

    const fb = this.el.querySelector('#ms-fb');
    if (fb) {
      fb.classList.add('show', correct ? 'correct-fb' : 'wrong-fb');
      fb.textContent = correct ? `✓ Correct! Answer: ${correctAns}` : `✗ Answer was ${correctAns}`;
    }
    const sc = this.el.querySelector('#ms-score');
    if (sc) sc.textContent = this.score;
    const st = this.el.querySelector('#ms-streak');
    if (st) st.textContent = this.streak;
    setNavScore(this.score);

    this.currentQ++;
    // Unity: feedbackTime = 1.4f
    setTimeout(() => { if (!this.destroyed) this.showNextQuestion(); }, 1400);
  }

  showEnd(won) {
    clearInterval(this.timerInterval);
    this.el.innerHTML = `
      <div class="end-screen">
        <h2>${won ? '🎉 All Levels Complete!' : '💥 Sudden Death!'}</h2>
        <div class="final-score">${this.score}</div>
        <p>Best streak: ${this.bestStreak} · ${won ? `${this.levels.length} levels done` : `Stopped at level ${this.currentLevel+1}`}</p>
        <div class="end-actions">
          <button class="btn btn-primary" id="ms-again">Play Again</button>
          <button class="btn btn-secondary" onclick="showHub()">Hub</button>
        </div>
        <div class="leaderboard">
          <h3>🏆 Leaderboard</h3>
          <div class="name-row">
            <input class="name-input" id="ms-name" placeholder="Your name" maxlength="14" />
            <button class="btn btn-warning" id="ms-save">Save</button>
          </div>
          <div id="ms-entries"></div>
        </div>
      </div>
    `;
    this.el.querySelector('#ms-again').addEventListener('click', () => this.start());
    this.el.querySelector('#ms-save').addEventListener('click', () => this.saveScore());
    this.loadLB();
  }

  async saveScore() {
    const name = this.el.querySelector('#ms-name').value.trim() || 'Player';
    const entries = await postScore('mathsprint', name, this.score);
    this.renderLB(entries);
    toast('Score saved!', 'success');
  }

  async loadLB() {
    const entries = await getLeaderboard('mathsprint');
    this.renderLB(entries);
  }

  renderLB(entries) {
    const el = this.el.querySelector('#ms-entries');
    if (!el) return;
    el.innerHTML = entries.map((e, i) =>
      `<div class="lb-entry"><span class="lb-rank">${i+1}</span><span class="lb-name">${e.name}</span><span class="lb-score">${e.score}</span></div>`
    ).join('') || '<p class="text-muted text-center">No scores yet</p>';
  }

  destroy() { this.destroyed = true; clearInterval(this.timerInterval); }
}
