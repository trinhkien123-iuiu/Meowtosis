// Giao diện "Tàu mèo": dựng bàn, diễn hoạt, booster, popup, âm thanh. Luật nằm ở rail-core.js.
(() => {
  const $ = (id) => document.getElementById(id);
  const levels = window.RAIL_LEVELS;
  const CATS = '../prototype/shikacat/cats/';
  const AUDIO = '../prototype/shikacat/audio/';
  const { OPPOSITE, ANGLE } = Rail;
  const SKINS = [
    { cat: 'Red', c: '#f0574d', c2: '#ffa190', d: '#b33a33' },
    { cat: 'Blue', c: '#4b8ff0', c2: '#a3c8ff', d: '#2f62b5' },
    { cat: 'Yellow', c: '#f5bb2c', c2: '#ffe68a', d: '#c4870c' },
    { cat: 'Green', c: '#4fbf5e', c2: '#acefa6', d: '#2f8a3e' },
  ];
  const BLOCKS = {
    rail: { name: 'Đường ray', tiles: [['-'], ['-']], text: 'Chạm toa: toa chạy theo mũi tên tới khi gặp toa khác hoặc cản cuối ray. 3 toa cùng màu nối đuôi trên một ray thì móc lại và rời bến.' },
    cross: { name: 'Giao lộ', tiles: [['+']], text: 'Giao lộ: toa đi thẳng qua, không rẽ. Đây là chỗ duy nhất để toa sang được tuyến khác.' },
    curve: { name: 'Ray cong', tiles: [['r'], ['J']], text: 'Ray cong: toa rẽ theo ray và quay mặt theo hướng mới. Đoàn tàu nối qua chỗ cong vẫn tính.' },
    cargo: { name: 'Toa hàng', tiles: [['-', 'cargo']], text: 'Toa hàng: không chở mèo, không bao giờ rời bến. Nó chắn đường, nhưng cũng là chỗ dừng rất có ích.' },
    switch: { name: 'Bẻ ghi', tiles: [['S', 0], ['S', 1]], text: 'Bẻ ghi: chạm cần gạt vàng để đổi nhánh (tốn 1 chuyến). Toa đi vào nhánh đang đóng sẽ bị chặn.' },
    gate: { name: 'Đèn tín hiệu', tiles: [['G', 0], ['G', 1]], text: 'Đèn tín hiệu: đèn đỏ thì toa không qua được. MỖI chuyến (kể cả bẻ ghi hay bấm Chờ) mọi đèn đổi màu, nên phải canh nhịp.' },
    tunnel: { name: 'Hầm xuyên núi', tiles: [['T', 0], ['T', 1]], text: 'Hầm: toa chạy ra cửa này sẽ chui vào cửa kia và chạy tiếp. Hầm chỉ để chạy xuyên: hai toa hai đầu hầm không nối đoàn với nhau.' },
    mix: { name: 'Tổng hợp', tiles: [['S', 1], ['+'], ['-', 'cargo']], text: 'Mọi thứ đã học gộp lại. Số chuyến chặt hơn: tính kỹ rồi mới chạm!' },
  };
  // Onboarding: ba màn đầu đi theo lời giải ngắn nhất, từng nước có bàn tay chỉ và lời dẫn; `rule` là câu luật hiện sau nước đó,
  // chờ bấm "Đã hiểu!". Số bước phải bằng số nước của lời giải (nếu không khớp thì màn chơi tự do, không onboarding).
  const ONBOARD = {
    0: [
      { text: 'Chạm toa đỏ bên trái: nó chạy theo mũi tên tới khi gặp toa khác.',
        rule: '3 toa cùng màu nối đuôi nhau trên một ray thì móc lại và rời bến! Cho hết toa rời bến là thắng.' },
    ],
    1: [
      { text: 'Toa không vượt qua nhau được. Chạm toa đỏ này: nó chạy tới sát toa đỏ bên cạnh.',
        rule: 'Hai toa đỏ đã nối đuôi nhau, cần thêm một toa đỏ nữa.' },
      { text: 'Chạm toa xanh bên phải: nó chạy sang trái và nối với hai toa xanh.',
        rule: 'Ba toa xanh nối đuôi nên rời bến! Toa xanh đang chắn đường cũng biến mất.' },
      { text: 'Đường đã thông: chạm toa đỏ cuối cùng để nối đoàn toa đỏ.' },
    ],
    2: [
      { text: 'Đây là giao lộ: hai ray cắt nhau. Chạm toa xanh này, nó chạy xuống theo ray dọc.',
        rule: 'Toa chạy thẳng qua giao lộ, không bao giờ rẽ.' },
      { text: 'Chạm toa xanh nằm trên giao lộ: nó chạy xuống sát toa xanh kia.' },
      { text: 'Chạm toa xanh bên phải: nó chạy ngang vào giao lộ và nối thành đoàn xanh dọc ray.',
        rule: 'Giao lộ là chỗ duy nhất để một toa ngang nối với đoàn trên ray dọc.' },
      { text: 'Giờ tới các toa đỏ: chạm toa đỏ giữa.' },
      { text: 'Chạm toa đỏ đầu tiên để nối đoàn đỏ và hoàn thành!' },
    ],
  };
  const BOOST_PRICE = 40;
  const MORE_MOVES_PRICE = 60;
  const STEP_MS = 115;

  // ── Lưu tiến độ (bản thử: chỉ trong trình duyệt này)
  const STORE = 'tau-meo-v2';
  let save;
  try { save = JSON.parse(localStorage.getItem(STORE)); } catch (e) { save = null; }
  save = Object.assign({ coins: 200, stars: {}, seen: {}, boosters: { hint: 3, undo: 3, crane: 2, reverse: 2 }, sound: true, level: 0 }, save || {});
  const persist = () => { try { localStorage.setItem(STORE, JSON.stringify(save)); } catch (e) { /* trình duyệt chặn lưu thì thôi */ } };

  // ── Âm thanh: tiếng Shikacat cho UI, tiếng tàu tự tổng hợp bằng WebAudio
  let ac = null;
  const audio = () => {
    if (!ac) { try { ac = new (window.AudioContext || window.webkitAudioContext)(); } catch (e) { ac = null; } }
    if (ac && ac.state === 'suspended') ac.resume();
    return ac;
  };
  const sfx = {
    tone(freq, dur, { type = 'sine', vol = 0.18, to = null, delay = 0 } = {}) {
      const a = audio(); if (!a || !save.sound) return;
      const t = a.currentTime + delay;
      const o = a.createOscillator(); const g = a.createGain();
      o.type = type; o.frequency.setValueAtTime(freq, t);
      if (to) o.frequency.exponentialRampToValueAtTime(to, t + dur);
      g.gain.setValueAtTime(0.0001, t); g.gain.exponentialRampToValueAtTime(vol, t + 0.012); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
      o.connect(g).connect(a.destination); o.start(t); o.stop(t + dur + 0.03);
    },
    noise(dur, { vol = 0.2, freq = 1200, q = 1, delay = 0 } = {}) {
      const a = audio(); if (!a || !save.sound) return;
      const t = a.currentTime + delay;
      const buffer = a.createBuffer(1, Math.ceil(a.sampleRate * dur), a.sampleRate);
      const data = buffer.getChannelData(0);
      for (let i = 0; i < data.length; i++) data[i] = (Math.random() * 2 - 1) * (1 - i / data.length);
      const src = a.createBufferSource(); src.buffer = buffer;
      const f = a.createBiquadFilter(); f.type = 'bandpass'; f.frequency.value = freq; f.Q.value = q;
      const g = a.createGain(); g.gain.value = vol;
      src.connect(f).connect(g).connect(a.destination); src.start(t);
    },
    clack() { this.noise(0.05, { vol: 0.14, freq: 2600, q: 5 }); this.tone(170, 0.05, { type: 'square', vol: 0.04 }); },
    chuff() { this.noise(0.2, { vol: 0.22, freq: 650, q: 0.7 }); },
    whistle() { this.tone(660, 0.5, { type: 'triangle', vol: 0.13 }); this.tone(830, 0.5, { type: 'triangle', vol: 0.09 }); this.tone(990, 0.3, { vol: 0.05, delay: 0.06 }); },
    thunk() { this.tone(130, 0.2, { vol: 0.28, to: 55 }); this.noise(0.08, { vol: 0.14, freq: 420 }); },
    click() { this.tone(950, 0.05, { type: 'square', vol: 0.07 }); this.tone(520, 0.07, { type: 'square', vol: 0.06, delay: 0.07 }); },
    sweep(up) { this.tone(up ? 220 : 700, 0.4, { type: 'sawtooth', vol: 0.05, to: up ? 760 : 190 }); },
    whoosh() { this.noise(0.35, { vol: 0.18, freq: 1500, q: 0.6 }); },
    signal() { this.tone(880, 0.09, { type: 'triangle', vol: 0.09 }); this.tone(660, 0.13, { type: 'triangle', vol: 0.08, delay: 0.09 }); },
    tunnelIn() { this.tone(320, 0.4, { type: 'sine', vol: 0.13, to: 80 }); this.noise(0.32, { vol: 0.12, freq: 480, q: 0.8 }); },
    tunnelOut() { this.tone(110, 0.3, { type: 'sine', vol: 0.12, to: 420 }); this.noise(0.25, { vol: 0.1, freq: 900, q: 0.7 }); },
    coin(i) { this.tone(1250 + i * 70, 0.08, { type: 'square', vol: 0.045 }); this.tone(1880 + i * 70, 0.12, { type: 'square', vol: 0.035, delay: 0.05 }); },
    file(name, vol = 0.6) { if (!save.sound) return; try { const a = new Audio(AUDIO + name); a.volume = vol; a.play().catch(() => {}); } catch (e) { /* bỏ qua */ } },
  };
  const bgm = new Audio(AUDIO + 'SC_BGM_01.ogg');
  bgm.loop = true; bgm.volume = 0.22;
  const syncMusic = () => { if (save.sound) bgm.play().catch(() => {}); else bgm.pause(); };
  document.addEventListener('pointerdown', () => { audio(); syncMusic(); }, { once: true });

  // ── Trạng thái ván
  let li = 0, level, board, state, history, used, extra, busy = false, mode = null, craneCar = -1, cs = 64;
  let guide = null, ruleOpen = false;   // onboarding: { steps, path, i }
  let carEls = [], leverEls = [], rot = [], fx, hintTimer = 0;

  const wait = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
  const movesLeft = () => level.limit + extra - used;
  const xy = (cell) => ({ x: (cell % board.W) * cs, y: Math.floor(cell / board.W) * cs });
  const center = (cell) => { const p = xy(cell); return { x: p.x + cs / 2, y: p.y + cs / 2 }; };
  const unwrap = (from, to) => { let a = to; while (a - from > 180) a -= 360; while (a - from < -180) a += 360; return a; };

  // ── Vẽ đường ray (SVG 100x100 mỗi ô)
  function straightSvg(horizontal) {
    let s = '';
    for (const p of [12, 32, 52, 72]) s += horizontal ? `<rect x="${p}" y="20" width="10" height="60" rx="2" fill="#7a5233"/>` : `<rect x="20" y="${p}" width="60" height="10" rx="2" fill="#7a5233"/>`;
    for (const q of [33, 61]) {
      s += horizontal ? `<rect x="-1" y="${q}" width="102" height="7" fill="#69727d"/><rect x="-1" y="${q + 1}" width="102" height="3" fill="#e6ecf1"/>`
        : `<rect x="${q}" y="-1" width="7" height="102" fill="#69727d"/><rect x="${q + 1}" y="-1" width="3" height="102" fill="#e6ecf1"/>`;
    }
    return s;
  }
  function curveSvg(p, q) {
    const cx = p === 'R' || q === 'R' ? 100 : 0;
    const cy = p === 'D' || q === 'D' ? 100 : 0;
    const ux = cx === 0 ? 1 : -1;
    const uy = cy === 0 ? 1 : -1;
    let s = '';
    for (const deg of [16, 45, 74]) {
      const t = deg * Math.PI / 180;
      s += `<line x1="${cx + ux * 22 * Math.cos(t)}" y1="${cy + uy * 22 * Math.sin(t)}" x2="${cx + ux * 78 * Math.cos(t)}" y2="${cy + uy * 78 * Math.sin(t)}" stroke="#7a5233" stroke-width="10" stroke-linecap="round"/>`;
    }
    for (const r of [36.5, 64.5]) {
      const sx = cx + ux * r, sy = cy, ex = cx, ey = cy + uy * r;
      const sweep = (sx - cx) * (ey - cy) - (sy - cy) * (ex - cx) > 0 ? 1 : 0;
      const d = `M ${sx} ${sy} A ${r} ${r} 0 0 ${sweep} ${ex} ${ey}`;
      s += `<path d="${d}" fill="none" stroke="#69727d" stroke-width="7"/><path d="${d}" fill="none" stroke="#e6ecf1" stroke-width="3"/>`;
    }
    return s;
  }
  const pairSvg = ([p, q]) => (OPPOSITE[p] === q ? straightSvg(p === 'L' || p === 'R') : curveSvg(p, q));
  function bumperSvg(port) {
    const stripe = (x, y, w, h) => `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="3" fill="#e2433b" stroke="#fff" stroke-width="2.5"/>`;
    if (port === 'R') return stripe(86, 24, 11, 52) + '<rect x="86" y="44" width="11" height="12" fill="#fff"/>';
    if (port === 'L') return stripe(3, 24, 11, 52) + '<rect x="3" y="44" width="11" height="12" fill="#fff"/>';
    if (port === 'U') return stripe(24, 3, 52, 11) + '<rect x="44" y="3" width="12" height="11" fill="#fff"/>';
    return stripe(24, 86, 52, 11) + '<rect x="44" y="86" width="12" height="11" fill="#fff"/>';
  }
  const TUNNEL_COLORS = ['#8a5bff', '#ff8a3d'];
  function mouthSvg(port, id) {
    const color = TUNNEL_COLORS[id % TUNNEL_COLORS.length];
    const angle = { R: 0, D: 90, L: 180, U: 270 }[port];
    return `<g transform="rotate(${angle} 50 50)"><path d="M64 12 h22 a14 14 0 0 1 14 14 v48 a14 14 0 0 1 -14 14 h-22 z" fill="${color}" stroke="#fff" stroke-width="2.5"/>`
      + '<path d="M72 26 h10 a9 9 0 0 1 9 9 v30 a9 9 0 0 1 -9 9 h-10 z" fill="#1a1226"/><path d="M64 12 h5 v76 h-5 z" fill="rgba(0,0,0,.2)"/></g>';
  }
  // Thanh chắn của đèn tín hiệu: đóng thì thanh sọc đỏ trắng chắn ngang ray, mở thì hai trụ nhỏ hai bên; đèn giữa ô đổi đỏ/xanh.
  function gateSvg(open, axis) {
    let g = '';
    if (!open) {
      for (let k = 0; k < 5; k++) g += `<rect x="42" y="${14 + k * 14.4}" width="16" height="14.4" fill="${k % 2 ? '#ffffff' : '#e2433b'}"/>`;
      g += '<rect x="42" y="14" width="16" height="72" fill="none" stroke="#7a2a24" stroke-width="2.5"/>';
    } else {
      g += '<rect x="40" y="6" width="20" height="13" rx="4" fill="#6b727c"/><rect x="40" y="81" width="20" height="13" rx="4" fill="#6b727c"/>';
    }
    const light = open ? '#3ddc6e' : '#ff4a3d';
    g += `<circle cx="50" cy="50" r="12" fill="${light}" opacity=".35"/><circle cx="50" cy="50" r="8" fill="${light}" stroke="#222" stroke-width="3"/><circle cx="47" cy="47" r="2.5" fill="#fff" opacity=".8"/>`;
    return `<g class="barrier" transform="rotate(${axis === 'H' ? 0 : 90} 50 50)">${g}</g>`;
  }
  function railSvg(b, cell, sw, bumpers = true) {
    let s = '<rect x="3" y="3" width="94" height="94" rx="14" fill="#d8c59c"/><rect x="3" y="3" width="94" height="94" rx="14" fill="none" stroke="#b8a173" stroke-width="2.5"/>';
    const active = Rail.pairs(b, cell, sw).map((p) => p.join(''));
    for (const pair of Rail.allPairs(b, cell)) {
      const on = active.includes(pair.join('')) || b.kind[cell] === 'G';
      s += `<g class="route" opacity="${on ? 1 : 0.28}">${pairSvg(pair)}</g>`;
    }
    if (bumpers) {
      const ports = new Set(Rail.allPairs(b, cell).flat());
      for (const port of ports) {
        const tunnel = b.tunnels.get(cell + ':' + port);
        if (tunnel) { s += mouthSvg(port, tunnel.id); continue; }
        const n = Rail.neighbour(b, cell, port);
        const linked = n >= 0 && Rail.allPairs(b, n).flat().includes(OPPOSITE[port]);
        if (!linked) s += bumperSvg(port);
      }
    }
    const gate = b.gateAt.get(cell);
    if (gate !== undefined) s += gateSvg(sw[b.switches.length + gate], b.gates[gate].axis);
    return `<svg viewBox="0 0 100 100">${s}</svg>`;
  }

  // ── Dựng bàn
  function cellSize() {
    const width = Math.min(window.innerWidth, 480) - 48;
    const height = window.innerHeight - 360;
    return Math.max(30, Math.min(88, Math.floor(Math.min(width / board.W, height / board.H))));
  }

  function build() {
    cs = cellSize();
    const root = $('board');
    root.innerHTML = '';
    root.style.width = board.W * cs + 'px';
    root.style.height = board.H * cs + 'px';
    for (let cell = 0; cell < board.W * board.H; cell++) {
      const p = xy(cell);
      const g = document.createElement('div');
      g.className = 'ground g' + ((Math.floor(cell / board.W) + cell % board.W) % 2);
      Object.assign(g.style, { left: p.x + 1 + 'px', top: p.y + 1 + 'px', width: cs - 2 + 'px', height: cs - 2 + 'px' });
      if (!Rail.isRail(board, cell)) {
        const seed = (cell * 9301 + 49297) % 233280 / 233280;
        g.innerHTML = `<div class="tuft" style="left:${20 + seed * 40}%;top:${30 + seed * 30}%"></div>` + (seed > 0.55 ? `<div class="flower" style="left:${60 - seed * 30}%;top:${62 - seed * 20}%"></div>` : '');
      }
      root.appendChild(g);
      if (Rail.isRail(board, cell)) {
        const r = document.createElement('div');
        r.className = 'rail';
        r.dataset.cell = cell;
        Object.assign(r.style, { left: p.x + 'px', top: p.y + 'px', width: cs + 'px', height: cs + 'px' });
        r.innerHTML = railSvg(board, cell, state.sw);
        root.appendChild(r);
      }
    }
    leverEls = board.switches.map((s, index) => {
      const used4 = new Set([s.stem, s.a, s.b]);
      const free = ['U', 'R', 'D', 'L'].find((d) => !used4.has(d)) || 'U';
      const c = center(s.cell);
      const el = document.createElement('div');
      el.className = 'lever';
      const size = cs * 0.44;
      const off = { U: [0, -0.5], D: [0, 0.5], L: [-0.5, 0], R: [0.5, 0] }[free];
      Object.assign(el.style, { width: size + 'px', height: size + 'px', left: c.x + off[0] * cs - size / 2 + 'px', top: c.y + off[1] * cs - size / 2 + 'px' });
      el.innerHTML = '<svg viewBox="0 0 20 20"><path d="M3 10 H14 M10 5 L15 10 L10 15" stroke="#7a3c00" stroke-width="3.2" fill="none" stroke-linecap="round" stroke-linejoin="round"/></svg>';
      el.addEventListener('pointerdown', (e) => { e.stopPropagation(); tapSwitch(index); });
      root.appendChild(el);
      return el;
    });
    carEls = level.cars.map((car, index) => {
      const el = document.createElement('div');
      const cargo = board.cars[index].cargo;
      el.className = 'car' + (cargo ? ' cargo' : '');
      el.style.width = el.style.height = cs + 'px';
      if (!cargo) {
        const skin = SKINS[car.color];
        el.style.setProperty('--c', skin.c); el.style.setProperty('--c2', skin.c2); el.style.setProperty('--cd', skin.d);
      }
      el.innerHTML = '<div class="shadow"></div><div class="wagon"><div class="wh a"></div><div class="wh b"></div><div class="wh c"></div><div class="wh d"></div>'
        + '<div class="hitch back"></div><div class="hitch front"></div><div class="body"><div class="floor"></div><div class="nose"></div><div class="lamp"></div></div><div class="arrow"></div></div>'
        + (cargo ? '<div class="crate"></div>' : `<img class="cat" src="${CATS}Image_Cat_${SKINS[car.color].cat}.png" alt="">`);
      el.addEventListener('pointerdown', (e) => { e.stopPropagation(); tapCar(index); });
      root.appendChild(el);
      return el;
    });
    fx = document.createElement('div');
    fx.id = 'fx';
    root.appendChild(fx);
    rot = state.dir.map((d) => ANGLE[d]);
    renderAll();
  }

  function setRot(i, deg) { rot[i] = deg; carEls[i].style.setProperty('--rot', deg + 'deg'); }

  function renderCars() {
    const occupied = Rail.occupancy(board, state);
    state.pos.forEach((cell, i) => {
      const el = carEls[i];
      el.getAnimations({ subtree: true }).forEach((a) => a.cancel());
      el.style.display = cell < 0 ? 'none' : '';
      el.style.opacity = '';
      el.classList.remove('linked', 'hop', 'hint', 'pick', 'lifted', 'rolling', 'squash');
      if (cell < 0) return;
      const p = xy(cell);
      el.style.transform = `translate(${p.x}px, ${p.y}px)`;
      setRot(i, unwrap(rot[i] || 0, ANGLE[state.dir[i]]));
      el.classList.toggle('stuck', !Rail.slide(board, state, i, occupied));
      const img = el.querySelector('.cat');
      if (img) img.src = `${CATS}Image_Cat_${SKINS[board.cars[i].color].cat}.png`;
    });
  }

  function renderRails() {
    document.querySelectorAll('#board .rail').forEach((el) => {
      const cell = Number(el.dataset.cell);
      if (board.kind[cell] === 'S' || board.kind[cell] === 'G') el.innerHTML = railSvg(board, cell, state.sw);
    });
    const occupied = Rail.occupancy(board, state);
    board.switches.forEach((s, index) => {
      const s0 = state.sw[index] ? s.b : s.a;
      leverEls[index].querySelector('svg').style.transform = `rotate(${ANGLE[s0]}deg)`;
      leverEls[index].classList.toggle('locked', occupied[s.cell] >= 0);
    });
  }

  function renderHud() {
    $('lvTitle').textContent = `Màn ${li + 1}`;
    $('lvBlock').textContent = BLOCKS[level.block].name;
    $('coins').textContent = save.coins;
    $('movesLeft').textContent = movesLeft();
    $('ticket').classList.toggle('low', movesLeft() <= 2);
    const chips = $('chips');
    const colors = [...new Set(board.cars.filter((c) => !c.cargo).map((c) => c.color))].sort();
    chips.innerHTML = '';
    colors.forEach((color) => {
      const left = state.pos.filter((cell, i) => cell >= 0 && board.cars[i].color === color && !board.cars[i].cargo).length;
      const chip = document.createElement('div');
      chip.className = 'chip' + (left === 0 ? ' done' : '');
      chip.innerHTML = `<img src="${CATS}Image_Cat_${SKINS[color].cat}.png" alt="">${left === 0 ? '✓' : left}`;
      chips.appendChild(chip);
    });
    for (const [key, id] of [['hint', 'nHint'], ['undo', 'nUndo'], ['crane', 'nCrane'], ['reverse', 'nReverse']]) {
      const n = save.boosters[key];
      $(id).textContent = n > 0 ? n : '+';
      $(id).classList.toggle('buy', n <= 0);
    }
    $('bCrane').classList.toggle('active', mode === 'crane-pick' || mode === 'crane-drop');
    $('bReverse').classList.toggle('active', mode === 'reverse');
    $('soundBtn').textContent = save.sound ? '🔊' : '🔇';
    $('waitBtn').style.display = board.gates.length ? '' : 'none';
  }

  function renderAll() { renderCars(); renderRails(); renderHud(); }

  function say(text, kind = '') {
    $('bubbleText').textContent = text;
    const bubble = $('bubble');
    bubble.className = 'bubble' + (kind ? ' ' + kind : '');
    if (kind === 'warn') { void bubble.offsetWidth; }
  }
  const defaultTip = () => (used === 0 && level.tip) ? level.tip : `Còn ${Rail.catsLeft(board, state)} toa mèo. Mỗi chuyến đều quý!`;

  // ── Hiệu ứng
  function puff(x, y, n = 3, delay = 0) {
    for (let i = 0; i < n; i++) {
      const el = document.createElement('div');
      el.className = 'puff';
      el.style.left = x + (Math.random() - 0.5) * cs * 0.3 + 'px';
      el.style.top = y + (Math.random() - 0.5) * cs * 0.2 + 'px';
      el.style.animationDelay = delay + i * 80 + 'ms';
      fx.appendChild(el);
      setTimeout(() => el.remove(), 1000 + delay + i * 80);
    }
  }
  function sparks(x, y, n = 10) {
    for (let i = 0; i < n; i++) {
      const el = document.createElement('div');
      el.className = 'spark';
      const a = Math.random() * Math.PI * 2;
      const d = 14 + Math.random() * 26;
      el.style.left = x + 'px'; el.style.top = y + 'px';
      el.style.setProperty('--dx', Math.cos(a) * d + 'px'); el.style.setProperty('--dy', Math.sin(a) * d + 'px');
      fx.appendChild(el);
      setTimeout(() => el.remove(), 500);
    }
  }
  function confetti(x, y, n = 22) {
    const colors = ['#ffd34a', '#ff6d8a', '#6ad1ff', '#7dff7a', '#c79bff', '#fff'];
    for (let i = 0; i < n; i++) {
      const el = document.createElement('div');
      el.className = 'confetti';
      const a = Math.random() * Math.PI * 2;
      const d = 30 + Math.random() * 70;
      el.style.left = x + 'px'; el.style.top = y + 'px';
      el.style.background = colors[i % colors.length];
      el.style.setProperty('--dx', Math.cos(a) * d + 'px'); el.style.setProperty('--dy', Math.sin(a) * d - 30 + 'px');
      fx.appendChild(el);
      setTimeout(() => el.remove(), 1100);
    }
  }
  function floatText(x, y, text, cls = 'float') {
    const el = document.createElement('div');
    el.className = cls; el.textContent = text;
    el.style.left = x + 'px'; el.style.top = y + 'px';
    fx.appendChild(el);
    setTimeout(() => el.remove(), 1200);
  }
  function rain() {
    const colors = ['#ffd34a', '#ff6d8a', '#6ad1ff', '#7dff7a', '#c79bff'];
    for (let i = 0; i < 70; i++) {
      const el = document.createElement('div');
      el.className = 'rain';
      el.style.left = Math.random() * 100 + 'vw';
      el.style.background = colors[i % colors.length];
      el.style.animationDelay = Math.random() * 0.8 + 's';
      el.style.setProperty('--dx', (Math.random() - 0.5) * 120 + 'px');
      document.body.appendChild(el);
      setTimeout(() => el.remove(), 3200);
    }
  }
  const frontOf = (i, cell) => { const c = center(cell); const a = (rot[i] * Math.PI) / 180; return { x: c.x + Math.cos(a) * cs * 0.45, y: c.y + Math.sin(a) * cs * 0.45 }; };

  // ── Bàn tay chỉ nước (hướng dẫn và Gợi ý)
  function showHand(action) {
    clearHand();
    if (!action) return;
    if (action.type === 'wait') { $('waitBtn').classList.add('hintpulse'); return; }
    let p;
    if (action.type === 'car') { p = center(state.pos[action.index]); carEls[action.index].classList.add('hint'); }
    else { const el = leverEls[action.index]; p = { x: el.offsetLeft + el.offsetWidth / 2, y: el.offsetTop + el.offsetHeight / 2 }; }
    const hand = document.createElement('div');
    hand.className = 'hand'; hand.textContent = '👆'; hand.id = 'hand';
    hand.style.left = p.x + 'px'; hand.style.top = p.y + 'px';
    fx.appendChild(hand);
  }
  function clearHand() {
    const hand = document.getElementById('hand');
    if (hand) hand.remove();
    $('waitBtn').classList.remove('hintpulse');
    carEls.forEach((el) => el.classList.remove('hint'));
  }

  // ── Diễn hoạt toa chạy theo đường ray: đi qua từng tâm ô, xoay ở chỗ cong.
  function pathAngles(i, points, endAngle) {
    const angles = [rot[i]];
    let prev = rot[i];
    const seg = (a, b) => { const dx = b.x - a.x, dy = b.y - a.y; return Math.abs(dx) > Math.abs(dy) ? (dx > 0 ? 0 : 180) : (dy > 0 ? 90 : 270); };
    for (let k = 1; k < points.length; k++) {
      const into = unwrap(prev, seg(points[k - 1], points[k]));
      let target = into;
      if (k < points.length - 1) {
        const out = unwrap(into, seg(points[k], points[k + 1]));
        target = (into + out) / 2;
      } else if (endAngle !== undefined) {
        // Dừng ở chỗ cong thì quay mặt theo hướng ra (mũi tên chỉ đúng chỗ nó sẽ chạy tiếp).
        target = unwrap(into, endAngle);
      }
      angles.push(target);
      prev = target;
    }
    return angles;
  }

  function animateAlong(i, points, { endAngle, fadeOut = false, stepMs = STEP_MS, sound = true } = {}) {
    const el = carEls[i];
    const angles = pathAngles(i, points, endAngle);
    const duration = 120 + stepMs * (points.length - 1);
    const frames = points.map((p, k) => ({ transform: `translate(${p.x}px, ${p.y}px)`, opacity: fadeOut && k === points.length - 1 ? 0 : 1 }));
    const turns = angles.map((a) => ({ transform: `rotate(${a}deg)` }));
    const shadowTurns = angles.map((a) => ({ transform: `rotate(${a}deg) translateY(6%)` }));
    el.classList.add('rolling');
    const options = { duration, easing: fadeOut ? 'ease-in' : 'ease-in-out', fill: 'forwards' };
    const move = el.animate(frames, options);
    el.querySelector('.wagon').animate(turns, options);
    el.querySelector('.shadow').animate(shadowTurns, options);
    if (sound) for (let k = 1; k < points.length; k++) setTimeout(() => sfx.clack(), (duration * k) / points.length);
    return move.finished.then(() => {
      el.classList.remove('rolling');
      const last = points[points.length - 1];
      el.getAnimations({ subtree: true }).forEach((a) => a.cancel());
      el.style.transform = `translate(${last.x}px, ${last.y}px)`;
      setRot(i, angles[angles.length - 1]);
      if (fadeOut) el.style.opacity = '0';
    });
  }

  // Các khung hình của một chuyến chạy: toa đi qua tâm từng ô, xoay ở chỗ cong; qua hầm thì mờ đi ở cửa này và hiện ra ở cửa kia.
  function runFrames(i, run) {
    const frames = [];
    const last = run.cells.length - 1;
    let prevAngle = rot[i];
    run.cells.forEach((cell, k) => {
      const p = xy(cell);
      let a = prevAngle;
      if (k > 0) {
        const into = unwrap(prevAngle, ANGLE[run.dirs[k - 1]]);
        const out = unwrap(into, ANGLE[run.dirs[k]]);
        a = k === last ? out : (into + out) / 2;
      }
      if (run.hops[k]) {
        const from = xy(run.cells[k - 1]);
        frames.push({ x: from.x, y: from.y, a: prevAngle, op: 0, hop: 'in' });
        frames.push({ x: p.x, y: p.y, a, op: 0 });
      }
      frames.push({ x: p.x, y: p.y, a, op: 1, hop: run.hops[k] ? 'out' : '' });
      prevAngle = a;
    });
    return frames;
  }

  function animateFrames(i, frames, duration) {
    const el = carEls[i];
    el.classList.add('rolling');
    const options = { duration, easing: 'ease-in-out', fill: 'forwards' };
    const move = el.animate(frames.map((f) => ({ transform: `translate(${f.x}px, ${f.y}px)`, opacity: f.op })), options);
    el.querySelector('.wagon').animate(frames.map((f) => ({ transform: `rotate(${f.a}deg)` })), options);
    el.querySelector('.shadow').animate(frames.map((f) => ({ transform: `rotate(${f.a}deg) translateY(6%)` })), options);
    return move.finished.then(() => {
      el.classList.remove('rolling');
      const end = frames[frames.length - 1];
      el.getAnimations({ subtree: true }).forEach((anim) => anim.cancel());
      el.style.transform = `translate(${end.x}px, ${end.y}px)`;
      el.style.opacity = '';
      setRot(i, end.a);
    });
  }

  async function runCar(i, run) {
    const frames = runFrames(i, run);
    const hopCount = run.hops.filter(Boolean).length;
    const duration = 120 + STEP_MS * (run.cells.length - 1) + hopCount * 130;
    sfx.chuff();
    const start = frontOf(i, run.cells[0]);
    puff(start.x, start.y, 3);
    const trail = setInterval(() => {
      const m = carEls[i].getBoundingClientRect(); const b = $('board').getBoundingClientRect();
      puff(m.left - b.left + cs / 2, m.top - b.top + cs * 0.3, 1);
    }, 220);
    // Âm thanh và tia sáng theo từng khung: lạch cạch qua ô, vút vào cửa hầm, vút ra cửa kia.
    frames.forEach((f, index) => {
      const at = duration * index / Math.max(1, frames.length - 1);
      if (f.hop === 'in') setTimeout(() => { sfx.tunnelIn(); sparks(f.x + cs / 2, f.y + cs / 2, 10); puff(f.x + cs / 2, f.y + cs / 2, 3); }, at);
      else if (f.hop === 'out') setTimeout(() => { sfx.tunnelOut(); sparks(f.x + cs / 2, f.y + cs / 2, 10); puff(f.x + cs / 2, f.y + cs / 2, 3); }, at);
      else if (index > 0 && f.op === 1) setTimeout(() => sfx.clack(), at);
    });
    await animateFrames(i, frames, duration);
    clearInterval(trail);
    const last = run.cells[run.cells.length - 1];
    const front = frontOf(i, last);
    sfx.thunk();
    carEls[i].classList.add('squash');
    if (run.stop === 'car') {
      const other = Rail.occupancy(board, state)[Rail.neighbour(board, last, run.dir)];
      if (other >= 0) { carEls[other].classList.remove('squash'); void carEls[other].offsetWidth; carEls[other].classList.add('squash'); }
      sparks(front.x, front.y, 6);
    } else {
      sparks(front.x, front.y, 12);
    }
    await wait(140);
  }

  // Đường cho một đoàn rời bến: nối dài từ đầu đoàn theo ray (tránh toa khác), chọn phía dài hơn / ra được mép bàn.
  function departurePath(train, pre) {
    const occupied = Rail.occupancy(board, pre);
    const mine = new Set(train.cells);
    const extend = (end, prev) => {
      const toward = ['U', 'D', 'L', 'R'].find((d) => Rail.neighbour(board, end, d) === prev);
      let out = Rail.exitPort(board, end, toward, pre.sw);
      const list = [];
      let cell = end;
      for (let k = 0; k < 8 && out; k++) {
        const n = Rail.neighbour(board, cell, out);
        if (n < 0) { const c = xy(cell); const [dr, dc] = Rail.DIRS[out]; list.push({ x: c.x + dc * cs * 1.3, y: c.y + dr * cs * 1.3, off: true }); break; }
        const o2 = Rail.exitPort(board, n, OPPOSITE[out], pre.sw);
        if (o2 === null || (occupied[n] >= 0 && !mine.has(n))) break;
        list.push(xy(n));
        cell = n; out = o2;
      }
      return list;
    };
    const cells = train.cells;
    const front = extend(cells[cells.length - 1], cells[cells.length - 2]);
    const back = extend(cells[0], cells[1]);
    const score = (list) => list.length + (list.length && list[list.length - 1].off ? 3 : 0);
    if (score(back) > score(front)) return { order: cells.slice().reverse(), ext: back };
    return { order: cells.slice(), ext: front };
  }

  async function departTrains(trains, pre) {
    sfx.file('SC_SFX_Gameplay_Box_Created_01.ogg', 0.7);
    sfx.whistle();
    const occupied = Rail.occupancy(board, pre);
    trains.forEach((train) => {
      train.cells.forEach((cell) => {
        const i = occupied[cell];
        carEls[i].classList.add('linked', 'hop');
        const img = carEls[i].querySelector('.cat');
        if (img) img.src = `${CATS}Image_Cat_${SKINS[board.cars[i].color].cat}EndGame.png`;
      });
      const mid = center(train.cells[Math.floor(train.cells.length / 2)]);
      confetti(mid.x, mid.y, 26);
      floatText(mid.x, mid.y - cs * 0.4, `Xuất bến! +${train.cells.length} 🐱`);
    });
    await wait(620);
    const runs = [];
    trains.forEach((train) => {
      const { order, ext } = departurePath(train, pre);
      const seq = order.map(xy).concat(ext);
      const contiguous = train.cells.every((cell, k) => k === 0 || Math.abs(Math.floor(cell / board.W) - Math.floor(train.cells[k - 1] / board.W)) + Math.abs((cell % board.W) - (train.cells[k - 1] % board.W)) === 1);
      const m = contiguous ? ext.length : 0;
      order.forEach((cell, j) => {
        const i = occupied[cell];
        if (m === 0) {
          const c = center(cell);
          puff(c.x, c.y, 4);
          runs.push(carEls[i].animate([{ opacity: 1, transform: carEls[i].style.transform + ' scale(1)' }, { opacity: 0, transform: carEls[i].style.transform + ' scale(1.3)' }], { duration: 420, fill: 'forwards' }).finished);
          return;
        }
        // Mỗi toa đi theo dấu toa phía trước: từ chỗ của mình tới m ô phía trước trên cùng đường.
        const path = seq.slice(j, j + m + 1);
        while (path.length < m + 1) path.push(seq[seq.length - 1]);
        runs.push(animateAlong(i, path, { fadeOut: true, stepMs: 150, sound: j === 0 }));
        if (j === order.length - 1) { const c = center(cell); puff(c.x, c.y, 4); }
      });
      sfx.chuff();
      setTimeout(() => sfx.chuff(), 260);
    });
    await Promise.all(runs);
  }

  // ── Một nước đi
  async function commit(result, action) {
    busy = true;
    clearHand();
    history.push({ state: Rail.cloneState(state), used, extra });
    used++;
    renderHud();
    if (action.type === 'car') {
      await runCar(action.index, result.slide);
    } else if (action.type === 'wait') {
      sfx.signal();
      floatText(center(0).x, -cs * 0.2, '🚦 Chờ...');
      await wait(260);
    } else {
      sfx.click();
      const lever = leverEls[action.index];
      lever.classList.remove('flip'); void lever.offsetWidth; lever.classList.add('flip');
      state = Rail.cloneState(state);
      state.sw[action.index] ^= 1;
      renderRails();
      const c = center(board.switches[action.index].cell);
      sparks(c.x, c.y, 8);
      await wait(260);
    }
    if (result.trains.length) {
      // Trạng thái ngay sau nước đi, trước khi đoàn rời bến.
      const pre = Rail.cloneState(result.next);
      result.trains.forEach((train) => train.cells.forEach((cell, k) => { pre.pos[train.cars[k]] = cell; }));
      state = pre;
      await departTrains(result.trains, pre);
      document.querySelectorAll('.chip').forEach((chip) => chip.classList.add('pop'));
    }
    state = result.next;
    renderAll();
    if (board.gates.length) pulseGates();
    busy = false;
    afterMove();
  }

  // Mọi đèn vừa đổi màu: thanh chắn nảy một cái, có tia sáng và tiếng "ting-tong".
  function pulseGates() {
    sfx.signal();
    document.querySelectorAll('#board .rail').forEach((el) => {
      const cell = Number(el.dataset.cell);
      if (board.kind[cell] !== 'G') return;
      el.classList.remove('gflip');
      void el.offsetWidth;
      el.classList.add('gflip');
      const c = center(cell);
      sparks(c.x, c.y, 7);
    });
  }

  function afterMove() {
    if (Rail.isWon(board, state)) { win(); return; }
    if (guide) { guideNext(); return; }
    if (movesLeft() <= 0) { lose('Hết chuyến rồi mà bãi vẫn còn toa mèo!'); return; }
    const anyMove = Rail.actions(board, state).some((a) => Rail.apply(board, state, a));
    if (!anyMove) { lose('Không toa nào chạy được nữa!'); return; }
    say(defaultTip());
    // Kiểm tra ngõ cụt sau một nhịp để không làm khựng diễn hoạt.
    clearTimeout(hintTimer);
    hintTimer = setTimeout(() => {
      if (busy) return;
      const next = Rail.hint(board, state, 60000);
      if (next === null) say('Bãi tàu kẹt rồi! Hoàn tác, hoặc dùng Cần cẩu / Quay đầu.', 'warn');
      else if (level.guide && used < level.guide) showHand(next);
    }, 120);
  }

  // ── Onboarding: làm theo bàn tay, mỗi nước một lời dẫn, có câu luật chờ "Đã hiểu!"
  function showStep() {
    ruleOpen = false;
    $('ruleBtn').style.display = 'none';
    const step = guide.steps[guide.i];
    if (!step) return;
    say(step.text);
    showHand(guide.path[guide.i]);
  }

  function guideNext() {
    guide.i++;
    const done = guide.steps[guide.i - 1];
    if (done && done.rule) {
      ruleOpen = true;
      clearHand();
      say(done.rule, 'mode');
      $('ruleBtn').style.display = 'block';
      sfx.file('SC_SFX_UI_Popup_Open_01.ogg', 0.5);
      return;
    }
    showStep();
  }

  // Trong onboarding chỉ nhận đúng nước bàn tay chỉ; sai thì rung nhẹ và nhắc.
  function guideAllows(action, el) {
    if (!guide) return true;
    if (ruleOpen) return false;
    const need = guide.path[guide.i];
    if (need && need.type === action.type && (action.type === 'wait' || need.index === action.index)) return true;
    if (el) deny(el);
    say('Làm theo bàn tay chỉ nhé!', 'warn');
    setTimeout(() => { if (guide && !ruleOpen && !busy) showStep(); }, 1200);
    return false;
  }

  function deny(el, front) {
    sfx.file('SC_Gameplay_Mistake_01.ogg', 0.5);
    el.classList.remove('deny'); void el.offsetWidth; el.classList.add('deny');
    if (front) floatText(front.x, front.y, '✖', 'cross');
  }

  function tapCar(i) {
    if (busy || state.pos[i] < 0) return;
    if (mode === 'crane-pick') return cranePick(i);
    if (mode === 'reverse') return reverseCar(i);
    if (mode === 'crane-drop') return cancelMode();
    if (movesLeft() <= 0) return;
    if (!guideAllows({ type: 'car', index: i }, carEls[i])) return;
    const result = Rail.apply(board, state, { type: 'car', index: i });
    if (!result) { deny(carEls[i], frontOf(i, state.pos[i])); say('Toa này đang bị chắn, chạy không được!', 'warn'); return; }
    commit(result, { type: 'car', index: i });
  }

  function tapWait() {
    if (busy) return;
    if (mode) { cancelMode(); return; }
    if (movesLeft() <= 0) return;
    if (!guideAllows({ type: 'wait' })) return;
    const result = Rail.apply(board, state, { type: 'wait' });
    if (!result) return;
    commit(result, { type: 'wait' });
  }

  function tapSwitch(s) {
    if (busy) return;
    if (mode) { cancelMode(); return; }
    if (movesLeft() <= 0) return;
    if (!guideAllows({ type: 'switch', index: s }, leverEls[s])) return;
    const result = Rail.apply(board, state, { type: 'switch', index: s });
    if (!result) { deny(leverEls[s]); say('Có toa đứng trên ghi, chưa gạt được!', 'warn'); return; }
    commit(result, { type: 'switch', index: s });
  }

  // ── Booster
  function spend(key) {
    if (save.boosters[key] > 0) { save.boosters[key]--; persist(); return true; }
    if (save.coins >= BOOST_PRICE) { save.coins -= BOOST_PRICE; persist(); sfx.coin(0); return true; }
    say(`Không đủ xu (cần ${BOOST_PRICE}).`, 'warn');
    sfx.file('SC_Gameplay_Mistake_01.ogg', 0.5);
    return false;
  }
  const refund = (key) => { save.boosters[key]++; persist(); };

  function useHint() {
    if (busy) return;
    cancelMode();
    const next = Rail.hint(board, state);
    if (next === null) { say('Không còn đường thắng từ đây. Hoàn tác thử nhé!', 'warn'); return; }
    if (!spend('hint')) return;
    sfx.file('SC_SFX_UI_Button_01.ogg', 0.5);
    showHand(next);
    say(next.type === 'car' ? 'Thử chạm toa này xem!' : next.type === 'switch' ? 'Thử gạt ghi này xem!' : 'Thử bấm Chờ để đổi đèn!');
    renderHud();
  }

  function useUndo() {
    if (busy) return;
    cancelMode();
    if (!history.length) { say('Chưa có chuyến nào để hoàn tác.', 'warn'); return; }
    if (!spend('undo')) return;
    sfx.whoosh();
    const last = history.pop();
    state = last.state; used = last.used; extra = last.extra;
    renderAll();
    say(defaultTip());
  }

  function useCrane() {
    if (busy) return;
    if (mode === 'crane-pick' || mode === 'crane-drop') { cancelMode(); return; }
    cancelMode();
    if (save.boosters.crane <= 0 && save.coins < BOOST_PRICE) { spend('crane'); return; }
    mode = 'crane-pick';
    carEls.forEach((el, i) => { if (state.pos[i] >= 0) el.classList.add('pick'); });
    say('Cần cẩu: chọn toa muốn nhấc (không tốn chuyến).', 'mode');
    renderHud();
  }

  function cranePick(i) {
    craneCar = i;
    mode = 'crane-drop';
    carEls.forEach((el) => el.classList.remove('pick'));
    carEls[i].classList.add('lifted');
    sfx.sweep(true);
    const occupied = Rail.occupancy(board, state);
    for (let cell = 0; cell < board.W * board.H; cell++) {
      if (!Rail.isRail(board, cell) || board.kind[cell] === 'S' || board.kind[cell] === 'G' || occupied[cell] >= 0) continue;
      const p = xy(cell);
      const t = document.createElement('div');
      t.className = 'target';
      Object.assign(t.style, { left: p.x + 4 + 'px', top: p.y + 4 + 'px', width: cs - 8 + 'px', height: cs - 8 + 'px' });
      t.addEventListener('pointerdown', (e) => { e.stopPropagation(); craneDrop(cell); });
      $('board').appendChild(t);
    }
    say('Chọn ô ray trống để đặt toa xuống.', 'mode');
  }

  async function craneDrop(cell) {
    const i = craneCar;
    const dir = Rail.craneDir(board, state, i, cell);
    if (!dir || !spend('crane')) { cancelMode(); return; }
    busy = true;
    clearTargets();
    history.push({ state: Rail.cloneState(state), used, extra });
    const from = xy(state.pos[i]);
    const to = xy(cell);
    // Móc cẩu thả xuống, nhấc toa bay sang ô mới.
    const hook = document.createElement('div');
    hook.className = 'hook';
    Object.assign(hook.style, { left: from.x + cs / 2 - 3 + 'px', top: -cs + 'px', height: from.y + cs * 0.9 + 'px' });
    fx.appendChild(hook);
    hook.animate([{ transform: 'scaleY(0)' }, { transform: 'scaleY(1)' }], { duration: 260, easing: 'ease-out' });
    await wait(280);
    sfx.sweep(true);
    const lift = -cs * 0.35;
    const el = carEls[i];
    const fly = el.animate([
      { transform: `translate(${from.x}px, ${from.y}px)` },
      { transform: `translate(${from.x}px, ${from.y + lift}px)`, offset: 0.25 },
      { transform: `translate(${to.x}px, ${to.y + lift}px)`, offset: 0.8 },
      { transform: `translate(${to.x}px, ${to.y}px)` },
    ], { duration: 720, easing: 'ease-in-out', fill: 'forwards' });
    hook.animate([
      { left: from.x + cs / 2 - 3 + 'px', height: from.y + cs * 0.9 + 'px' },
      { left: from.x + cs / 2 - 3 + 'px', height: from.y + lift + cs * 0.9 + 'px', offset: 0.25 },
      { left: to.x + cs / 2 - 3 + 'px', height: to.y + lift + cs * 0.9 + 'px', offset: 0.8 },
      { left: to.x + cs / 2 - 3 + 'px', height: to.y + cs * 0.9 + 'px' },
    ], { duration: 720, easing: 'ease-in-out', fill: 'forwards' });
    await fly.finished;
    hook.remove();
    sfx.thunk();
    const c = center(cell);
    puff(c.x, c.y + cs * 0.3, 4);
    const next = Rail.cloneState(state);
    next.pos[i] = cell;
    next.dir[i] = dir;
    state = next;
    mode = null; craneCar = -1;
    renderAll();
    await settleTrains();
    busy = false;
    afterMove();
  }

  // Sau khi dùng booster mà tạo thành đoàn tàu thì cho đoàn rời bến luôn.
  async function settleTrains() {
    const occupied = Rail.occupancy(board, state);
    const trains = Rail.trains(board, state, occupied).map((cells) => ({ cells, cars: cells.map((cell) => occupied[cell]) }));
    if (!trains.length) return;
    const pre = Rail.cloneState(state);
    await departTrains(trains, pre);
    trains.forEach((train) => train.cars.forEach((car) => { state.pos[car] = -1; }));
    renderAll();
  }

  async function reverseCar(i) {
    const dir = Rail.reversed(board, state, i);
    if (!dir) { cancelMode(); return; }
    if (!spend('reverse')) { cancelMode(); return; }
    busy = true;
    history.push({ state: Rail.cloneState(state), used, extra });
    carEls.forEach((el) => el.classList.remove('pick'));
    sfx.whoosh();
    const target = rot[i] + 180;
    const options = { duration: 420, easing: 'cubic-bezier(.3,1.4,.5,1)', fill: 'forwards' };
    const spin = carEls[i].querySelector('.wagon').animate([{ transform: `rotate(${rot[i]}deg)` }, { transform: `rotate(${target}deg)` }], options);
    carEls[i].querySelector('.shadow').animate([{ transform: `rotate(${rot[i]}deg) translateY(6%)` }, { transform: `rotate(${target}deg) translateY(6%)` }], options);
    const c = center(state.pos[i]);
    sparks(c.x, c.y, 10);
    await spin.finished;
    setRot(i, target);
    state = Rail.cloneState(state);
    state.dir[i] = dir;
    mode = null;
    renderAll();
    busy = false;
    afterMove();
  }

  function useReverse() {
    if (busy) return;
    if (mode === 'reverse') { cancelMode(); return; }
    cancelMode();
    if (save.boosters.reverse <= 0 && save.coins < BOOST_PRICE) { spend('reverse'); return; }
    mode = 'reverse';
    carEls.forEach((el, i) => { if (state.pos[i] >= 0) el.classList.add('pick'); });
    say('Quay đầu: chọn toa muốn đổi chiều chạy.', 'mode');
    renderHud();
  }

  function clearTargets() { document.querySelectorAll('.target').forEach((t) => t.remove()); }
  function cancelMode() {
    if (!mode) return;
    mode = null; craneCar = -1;
    clearTargets();
    carEls.forEach((el) => el.classList.remove('pick', 'lifted'));
    say(defaultTip());
    renderHud();
  }

  // ── Thắng / thua
  function win() {
    const stars = used <= level.par ? 3 : used <= level.par + 1 ? 2 : 1;
    const coins = 5 + stars * 10;
    save.coins += coins;
    save.stars[li] = Math.max(save.stars[li] || 0, stars);
    save.level = Math.min(li + 1, levels.length - 1);
    persist();
    sfx.file('SC_SFX_Gameplay_AllBoxesCreated_01.ogg', 0.7);
    setTimeout(() => {
      sfx.file('SC_SFX_UI_Win_01.ogg', 0.7);
      rain();
      $('stars').innerHTML = [0, 1, 2].map((k) => `<span class="${k < stars ? 'on' : 'off'}" style="animation-delay:${0.25 + k * 0.25}s">⭐</span>`).join('');
      [0, 1, 2].forEach((k) => { if (k < stars) setTimeout(() => sfx.tone(700 + k * 200, 0.2, { type: 'triangle', vol: 0.12 }), 250 + k * 250); });
      $('winCoins').textContent = '+' + coins;
      $('winText').textContent = `${used} chuyến · ít nhất ${level.par}`;
      $('winPop').classList.add('show');
      for (let k = 0; k < 6; k++) setTimeout(() => sfx.coin(k), 1100 + k * 70);
      renderHud();
    }, 450);
  }

  function lose(text) {
    sfx.file('SC_SFX_UI_Lose_01.ogg', 0.7);
    $('stage').classList.remove('shake'); void $('stage').offsetWidth; $('stage').classList.add('shake');
    setTimeout(() => {
      $('loseText').textContent = text;
      $('loseMore').disabled = movesLeft() > 0 && text.startsWith('Không');
      $('losePop').classList.add('show');
    }, 500);
  }

  // ── Màn chơi
  function load(index) {
    li = (index + levels.length) % levels.length;
    level = levels[li];
    board = Rail.prepare(level);
    state = Rail.startState(level);
    history = []; used = 0; extra = 0; mode = null; craneCar = -1; busy = false;
    ['winPop', 'losePop', 'mapPop'].forEach((id) => $(id).classList.remove('show'));
    save.level = li; persist();
    build();
    guide = null; ruleOpen = false;
    $('ruleBtn').style.display = 'none';
    const steps = ONBOARD[li];
    if (steps) {
      const solved = Rail.solve(board, state, null, 100000, false);
      if (solved && solved.path.length === steps.length) guide = { steps, path: solved.path, i: 0 };
    }
    $('app').classList.toggle('onboard', !!guide);
    if (guide) {
      showStep();
      return;
    }
    say(defaultTip());
    if (level.tip && !save.seen[level.block]) showCard(level.block);
    if (level.guide) setTimeout(() => { if (!busy && used === 0) showHand(Rail.hint(board, state)); }, 300);
  }

  function showCard(block) {
    const info = BLOCKS[block];
    save.seen[block] = true; persist();
    $('cardTitle').textContent = info.name;
    $('cardText').textContent = info.text;
    const tiles = $('cardTiles');
    tiles.innerHTML = '';
    info.tiles.forEach(([ch, extraInfo]) => {
      const sample = { rails: [ch === 'T' ? '-' : ch], switches: ch === 'S' ? [{ r: 0, c: 0, stem: 'L', a: 'R', b: 'D', state: extraInfo }] : [], gates: ch === 'G' ? [{ r: 0, c: 0, axis: 'H', open: extraInfo }] : [], cars: [] };
      const b = Rail.prepare(sample);
      const tile = document.createElement('div');
      tile.className = 'tile';
      tile.innerHTML = railSvg(b, 0, sample.switches.map((s) => s.state).concat(sample.gates.map((g) => g.open)), false);
      if (ch === 'T') tile.innerHTML += `<svg viewBox="0 0 100 100" style="position:absolute;inset:4px;width:calc(100% - 8px);height:calc(100% - 8px)">${mouthSvg(extraInfo ? 'L' : 'R', 0)}</svg>`;
      if (extraInfo === 'cargo') tile.innerHTML += '<div class="car cargo" style="position:absolute;inset:4px;width:auto;height:auto;--rot:0deg"><div class="wagon"><div class="body"></div></div><div class="crate"></div></div>';
      tiles.appendChild(tile);
    });
    setTimeout(() => sfx.file('SC_SFX_UI_Popup_Open_01.ogg', 0.6), 100);
    $('cardPop').classList.add('show');
  }

  function showMap() {
    const list = $('levels');
    list.innerHTML = '';
    let lastBlock = '';
    levels.forEach((lv, index) => {
      if (lv.block !== lastBlock) {
        lastBlock = lv.block;
        const name = document.createElement('div');
        name.className = 'blockname';
        name.textContent = BLOCKS[lv.block].name;
        list.appendChild(name);
      }
      const b = document.createElement('button');
      b.className = 'lv' + (index === li ? ' cur' : '');
      const stars = save.stars[index] || 0;
      b.innerHTML = `${index + 1}<small>${'★'.repeat(stars)}${'☆'.repeat(3 - stars)}</small>`;
      b.addEventListener('click', () => { sfx.file('SC_SFX_UI_Button_01.ogg', 0.5); load(index); });
      list.appendChild(b);
    });
    sfx.file('SC_SFX_UI_Popup_Open_01.ogg', 0.6);
    $('mapPop').classList.add('show');
  }

  // ── Nút
  $('waitBtn').addEventListener('click', tapWait);
  $('ruleBtn').addEventListener('click', () => { if (!guide || !ruleOpen) return; sfx.file('SC_SFX_UI_Button_01.ogg', 0.5); showStep(); });
  $('bHint').addEventListener('click', useHint);
  $('bUndo').addEventListener('click', useUndo);
  $('bCrane').addEventListener('click', useCrane);
  $('bReverse').addEventListener('click', useReverse);
  $('board').addEventListener('pointerdown', () => { if (mode) cancelMode(); });
  $('mapBtn').addEventListener('click', showMap);
  $('mapClose').addEventListener('click', () => $('mapPop').classList.remove('show'));
  $('soundBtn').addEventListener('click', () => { save.sound = !save.sound; persist(); syncMusic(); renderHud(); });
  $('cardOk').addEventListener('click', () => { sfx.file('SC_SFX_UI_Popup_Close_01.ogg', 0.6); $('cardPop').classList.remove('show'); });
  $('winNext').addEventListener('click', () => load(li + 1));
  $('winRetry').addEventListener('click', () => load(li));
  $('loseRetry').addEventListener('click', () => load(li));
  $('loseMore').addEventListener('click', () => {
    if (save.coins < MORE_MOVES_PRICE) { $('loseText').textContent = `Không đủ xu (cần ${MORE_MOVES_PRICE}).`; return; }
    save.coins -= MORE_MOVES_PRICE; persist();
    extra += 3;
    sfx.coin(0); sfx.coin(2);
    $('losePop').classList.remove('show');
    renderHud();
    say('Thêm 3 chuyến! Tính kỹ nhé.');
  });
  window.addEventListener('resize', () => { if (!busy && level) { const s = Rail.cloneState(state); build(); state = s; renderAll(); } });

  // Cho phép chơi tự động khi kiểm tra (gọi từ console).
  window.game = {
    tapCar, tapSwitch, tapWait, load, useHint, useUndo,
    get state() { return state; }, get board() { return board; }, get busy() { return busy; }, get used() { return used; },
    get level() { return level; }, get won() { return Rail.isWon(board, state); },
  };
  load(Math.min(save.level || 0, levels.length - 1));
})();
