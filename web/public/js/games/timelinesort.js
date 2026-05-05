class TimelineSortGame {
  constructor(el, events) {
    this.el = el;
    this.allEvents = events;
    this.score = 0;
    this.selectedEvents = [];
    this.dragSrc = null;
    this.destroyed = false;
    this._saveTimeout = null;
    this._explanationTimeout = null;
  }

  start() {
    // Cancel any pending timeouts from the previous round
    clearTimeout(this._saveTimeout);
    clearTimeout(this._explanationTimeout);
    // Forcibly remove any leftover leaderboard panels
    this.el.querySelectorAll('.leaderboard').forEach(n => n.remove());

    const ancient     = shuffle(this.allEvents.filter(e => e.era === 'Ancient')).slice(0, 1);
    const earlyModern = shuffle(this.allEvents.filter(e => e.era === 'EarlyModern')).slice(0, 2);
    const modern      = shuffle(this.allEvents.filter(e => e.era === 'Modern')).slice(0, 2);
    this.selectedEvents = shuffle([...ancient, ...earlyModern, ...modern]);
    this.score = 0;
    this.render();
  }

  render() {
    this.el.innerHTML = `
      <p class="timeline-intro">Drag the events below into <strong>chronological order</strong> (oldest → newest), then submit.</p>
      <div class="score-row">
        <div class="score-chip">Score <span id="tl-score">0</span></div>
      </div>
      <div class="timeline-list" id="tl-list"></div>
      <div style="text-align:center;margin-bottom:16px">
        <button class="btn btn-primary" id="tl-submit">Submit Order</button>
      </div>
      <div class="feedback-box" id="tl-fb"></div>
    `;

    this.renderCards();
    this.el.querySelector('#tl-submit').addEventListener('click', () => this.submit());
  }

  renderCards() {
    const list = this.el.querySelector('#tl-list');
    list.innerHTML = '';
    this.selectedEvents.forEach((ev, i) => {
      const card = document.createElement('div');
      card.className = 'timeline-card';
      card.draggable = true;
      card.dataset.idx = i;
      card.innerHTML = `
        <span style="font-size:1.4rem">⋮⋮</span>
        <div style="flex:1">
          <div class="tl-title">${ev.title}</div>
          <div class="tl-era">${ev.era}</div>
        </div>
        <div class="tl-year" id="tl-year-${i}"></div>
      `;

      card.addEventListener('dragstart', e => { this.dragSrc = card; card.classList.add('dragging'); });
      card.addEventListener('dragend',   e => { card.classList.remove('dragging'); });
      card.addEventListener('dragover',  e => { e.preventDefault(); card.classList.add('drag-over'); });
      card.addEventListener('dragleave', e => { card.classList.remove('drag-over'); });
      card.addEventListener('drop',      e => {
        e.preventDefault();
        card.classList.remove('drag-over');
        if (!this.dragSrc || this.dragSrc === card) return;
        const allCards = [...list.querySelectorAll('.timeline-card')];
        const srcIdx = allCards.indexOf(this.dragSrc);
        const dstIdx = allCards.indexOf(card);
        if (srcIdx < dstIdx) list.insertBefore(this.dragSrc, card.nextSibling);
        else list.insertBefore(this.dragSrc, card);
        this.dragSrc = null;
      });

      // Touch drag support
      card.addEventListener('touchstart', e => {
        this.dragSrc = card;
        card.style.opacity = '0.6';
      }, { passive: true });
      card.addEventListener('touchmove', e => {
        e.preventDefault();
        const t = e.touches[0];
        const el = document.elementFromPoint(t.clientX, t.clientY);
        const target = el?.closest('.timeline-card');
        if (target && target !== card) {
          list.querySelectorAll('.timeline-card').forEach(c => c.classList.remove('drag-over'));
          target.classList.add('drag-over');
        }
      }, { passive: false });
      card.addEventListener('touchend', e => {
        card.style.opacity = '1';
        const t = e.changedTouches[0];
        const el = document.elementFromPoint(t.clientX, t.clientY);
        const target = el?.closest('.timeline-card');
        if (target && target !== card) {
          target.classList.remove('drag-over');
          const allCards = [...list.querySelectorAll('.timeline-card')];
          const srcIdx = allCards.indexOf(card);
          const dstIdx = allCards.indexOf(target);
          if (srcIdx < dstIdx) list.insertBefore(card, target.nextSibling);
          else list.insertBefore(card, target);
        }
        this.dragSrc = null;
      });

      list.appendChild(card);
    });
  }

  submit() {
    const list = this.el.querySelector('#tl-list');
    const cards = [...list.querySelectorAll('.timeline-card')];

    const playerOrder = cards.map(card => this.selectedEvents[+card.dataset.idx]);
    const correctOrder = [...playerOrder].sort((a, b) => a.year - b.year);

    let correctPairs = 0;
    for (let i = 0; i < playerOrder.length - 1; i++) {
      if (playerOrder[i].year <= playerOrder[i+1].year) correctPairs++;
    }
    const fullyCorrect = correctPairs === playerOrder.length - 1;
    this.score = fullyCorrect ? 500 : correctPairs * 100;

    cards.forEach((card, i) => {
      card.draggable = false;
      const evInPos = playerOrder[i];
      const isCorrectPos = evInPos === correctOrder[i];
      card.classList.add(isCorrectPos ? 'correct-pos' : 'wrong-pos');
      const yearEl = card.querySelector('.tl-year');
      if (yearEl) {
        yearEl.textContent = evInPos.year < 0 ? `${-evInPos.year} BCE` : String(evInPos.year);
        yearEl.style.color = isCorrectPos ? 'var(--green)' : 'var(--red)';
      }
    });

    const fb = this.el.querySelector('#tl-fb');
    if (fb) {
      fb.classList.add('show', fullyCorrect ? 'correct-fb' : 'wrong-fb');
      fb.innerHTML = fullyCorrect
        ? `🎉 Perfect order! +500 pts`
        : `${correctPairs} of ${playerOrder.length - 1} pairs correct. +${this.score} pts<br>
           <span style="font-size:.85rem;color:var(--muted)">Correct: ${correctOrder.map(e => e.title).join(' → ')}</span>`;
    }

    const sc = this.el.querySelector('#tl-score');
    if (sc) sc.textContent = this.score;
    setNavScore(this.score);

    // Replace Submit with Play Again
    const submitBtn = this.el.querySelector('#tl-submit');
    if (submitBtn) {
      submitBtn.textContent = 'Play Again';
      submitBtn.onclick = () => this.start();
    }

    // Show explanations on cards (tracked so it can be cancelled)
    this._explanationTimeout = setTimeout(() => {
      if (this.destroyed) return;
      list.querySelectorAll('.timeline-card').forEach((card, i) => {
        const ev = playerOrder[i];
        const tip = document.createElement('div');
        tip.style.cssText = 'font-size:.75rem;color:var(--muted);margin-top:4px;line-height:1.4';
        tip.textContent = ev.explanation;
        card.appendChild(tip);
      });
    }, 200);

    // Show leaderboard save panel (tracked so it can be cancelled)
    if (this.score > 0) {
      this._saveTimeout = setTimeout(() => {
        if (this.destroyed) return;
        // Only append if leaderboard not already present
        if (!this.el.querySelector('.leaderboard')) {
          this.askSaveScore();
        }
      }, 2000);
    }
  }

  askSaveScore() {
    const lb = document.createElement('div');
    lb.className = 'leaderboard';
    lb.innerHTML = `
      <h3>🏆 Leaderboard</h3>
      <div class="name-row">
        <input class="name-input" id="tl-name" placeholder="Your name" maxlength="14" />
        <button class="btn btn-warning" id="tl-save">Save</button>
      </div>
      <div id="tl-entries"></div>
    `;
    this.el.appendChild(lb);
    this.el.querySelector('#tl-save').addEventListener('click', () => this.saveScore());
    this.loadLB();
  }

  async saveScore() {
    const name = this.el.querySelector('#tl-name').value.trim() || 'Player';
    const entries = await postScore('timelinesort', name, this.score);
    this.renderLB(entries);
    toast('Score saved!', 'success');
  }

  async loadLB() {
    const entries = await getLeaderboard('timelinesort');
    this.renderLB(entries);
  }

  renderLB(entries) {
    const el = this.el.querySelector('#tl-entries');
    if (!el) return;
    el.innerHTML = entries.map((e, i) =>
      `<div class="lb-entry"><span class="lb-rank">${i+1}</span><span class="lb-name">${e.name}</span><span class="lb-score">${e.score}</span></div>`
    ).join('') || '<p class="text-muted text-center">No scores yet</p>';
  }

  destroy() {
    this.destroyed = true;
    clearTimeout(this._saveTimeout);
    clearTimeout(this._explanationTimeout);
  }
}
