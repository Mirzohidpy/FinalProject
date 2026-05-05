// EmotionID — exact Unity values:
// timePerQuestion=8f, pointsPerCorrect=100, timeBonusMultiplier=10, streakBonusMultiplier=50
// Round: 5 easy + 3 medium + 2 hard = 10 questions
// Distractors: first try same tier, fallback to all
class EmotionIDGame {
  constructor(el, emotions) {
    this.el = el;
    this.allEmotions = emotions;
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
    const easy   = this.allEmotions.filter(e => e.difficulty === 0);
    const medium = this.allEmotions.filter(e => e.difficulty === 1);
    const hard   = this.allEmotions.filter(e => e.difficulty === 2);
    // Unity: 5 easy + 3 medium + 2 hard
    this.questions = [
      ...shuffle(easy).slice(0, 5),
      ...shuffle(medium).slice(0, 3),
      ...shuffle(hard).slice(0, 2)
    ];
    this.score = 0; this.streak = 0; this.currentQ = 0;
    this.showQuestion();
  }

  showQuestion() {
    if (this.currentQ >= this.questions.length) { this.showEnd(); return; }
    const emotion = this.questions[this.currentQ];
    const diffLabel = ['Easy','Medium','Hard'][emotion.difficulty];

    // Unity: distractors from same tier first, fallback to all
    let sameTier = this.allEmotions.filter(e => e.difficulty === emotion.difficulty && e.emotionName !== emotion.emotionName);
    let pool = sameTier.length >= 3 ? sameTier : this.allEmotions.filter(e => e.emotionName !== emotion.emotionName);
    const distractors = shuffle(pool).slice(0, 3);
    const choices = shuffle([emotion, ...distractors]);

    this.el.innerHTML = `
      <div class="score-row">
        <div class="score-chip">Q <span>${this.currentQ+1}/${this.questions.length}</span></div>
        <div class="score-chip">Score <span id="emo-score">${this.score}</span></div>
        <div class="score-chip">Streak <span>${this.streak}🔥</span></div>
      </div>
      <div class="timer-bar"><div class="timer-fill" id="emo-timer" style="width:100%"></div></div>
      <div class="text-center">
        <span class="difficulty-badge ${diffLabel.toLowerCase()}">${diffLabel}</span>
      </div>
      <div class="emotion-display">${emotion.emoji}</div>
      <p class="text-center text-muted mb-16" style="font-size:.9rem">What emotion is this?</p>
      <div class="choices" id="emo-choices"></div>
      <div class="feedback-box" id="emo-fb"></div>
    `;

    const grid = this.el.querySelector('#emo-choices');
    choices.forEach((e, i) => {
      const btn = document.createElement('button');
      btn.className = 'choice-btn';
      btn.textContent = `${['A','B','C','D'][i]}.  ${e.emotionName}`;
      btn.addEventListener('click', () => this.onAnswer(btn, e.emotionName === emotion.emotionName, emotion));
      grid.appendChild(btn);
    });

    this.startTimer(8); // Unity: timePerQuestion = 8f
  }

  startTimer(secs) {
    clearInterval(this.timerInterval);
    this.timeLeft = secs;
    this.inputEnabled = true;
    const bar = this.el.querySelector('#emo-timer');
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
        if (this.inputEnabled) this.onAnswer(null, false, this.questions[this.currentQ]);
      }
    }, 1000);
  }

  onAnswer(btn, correct, emotion) {
    if (!this.inputEnabled) return;
    this.inputEnabled = false;
    clearInterval(this.timerInterval);

    // Unity: score += pointsPerCorrect + (timeLeft * timeBonusMultiplier) + (streak * streakBonusMultiplier)
    if (correct) {
      const timeBonus = this.timeLeft * 10;
      const streakBonus = this.streak * 50;
      this.streak++;
      this.score += 100 + timeBonus + streakBonus;
      if (btn) btn.classList.add('correct');
    } else {
      this.streak = 0;
      if (btn) btn.classList.add('wrong');
      this.el.querySelectorAll('.choice-btn').forEach(b => {
        if (b.textContent.includes(emotion.emotionName)) b.classList.add('correct');
      });
    }

    this.el.querySelectorAll('.choice-btn').forEach(b => b.disabled = true);
    const fb = this.el.querySelector('#emo-fb');
    if (fb) {
      fb.classList.add('show', correct ? 'correct-fb' : 'wrong-fb');
      fb.innerHTML = `${correct ? '✓ Correct!' : `✗ It's "${emotion.emotionName}"`}<br>
        <span style="color:var(--accent2);font-size:.85rem">💡 ${emotion.tip}</span>`;
    }
    const sc = this.el.querySelector('#emo-score');
    if (sc) sc.textContent = this.score;
    setNavScore(this.score);

    setTimeout(() => { if (!this.destroyed) { this.currentQ++; this.showQuestion(); } }, 2200);
  }

  showEnd() {
    clearInterval(this.timerInterval);
    this.el.innerHTML = `
      <div class="end-screen">
        <h2>😊 Round Complete!</h2>
        <div class="final-score">${this.score}</div>
        <p>${this.questions.length} emotions identified · streak ${this.streak}</p>
        <div class="end-actions">
          <button class="btn btn-primary" id="emo-again">Play Again</button>
          <button class="btn btn-secondary" onclick="showHub()">Hub</button>
        </div>
        <div class="leaderboard">
          <h3>🏆 Leaderboard</h3>
          <div class="name-row">
            <input class="name-input" id="emo-name" placeholder="Your name" maxlength="14" />
            <button class="btn btn-warning" id="emo-save">Save</button>
          </div>
          <div id="emo-entries"></div>
        </div>
      </div>
    `;
    this.el.querySelector('#emo-again').addEventListener('click', () => this.start());
    this.el.querySelector('#emo-save').addEventListener('click', () => this.saveScore());
    this.loadLB();
  }

  async saveScore() {
    const name = this.el.querySelector('#emo-name').value.trim() || 'Player';
    const entries = await postScore('emotionid', name, this.score);
    this.renderLB(entries);
    toast('Score saved!', 'success');
  }

  async loadLB() {
    const entries = await getLeaderboard('emotionid');
    this.renderLB(entries);
  }

  renderLB(entries) {
    const el = this.el.querySelector('#emo-entries');
    if (!el) return;
    el.innerHTML = entries.map((e, i) =>
      `<div class="lb-entry"><span class="lb-rank">${i+1}</span><span class="lb-name">${e.name}</span><span class="lb-score">${e.score}</span></div>`
    ).join('') || '<p class="text-muted text-center">No scores yet</p>';
  }

  destroy() { this.destroyed = true; clearInterval(this.timerInterval); }
}
