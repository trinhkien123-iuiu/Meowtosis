// Phân tích và tìm level cho chuỗi dạy luật đầu game (15 level, mỗi giai đoạn dạy một luật):
//   A · lấp kín là thắng (chỉ cần nhân đôi vào ô trống)        B · 3 con cùng màu thẳng hàng thì biến mất
//   C · còn ô trống phía sau thì cả dãy bị đẩy                  D · hết chỗ đẩy thì bản sao đè lên con khác màu
// Mỗi nước được xếp loại theo việc nó làm: fill (bản sao vào ô trống), push (đẩy dãy), replace (đè con khác màu);
// kèm theo sau nước đó: clear (khử 3 an toàn) hoặc lose (thua: hết một màu).
//   node tools/onboarding-design.js check            -> bảng phân tích các level dạy luật đang có trong game (level 1–15)
//   node tools/onboarding-design.js check 'A.' '..'  -> phân tích một bàn
//   node tools/onboarding-design.js search B 40      -> tìm 40 ứng viên cho giai đoạn B (in ra, chọn bằng tay)
const { loadLogic, asDef, rng } = require('./lib');

const ctx = loadLogic();

// Loại của một nước (đi được) từ trạng thái `cells`.
function classify(lv, cells, f, d) {
  const r = ctx.swipe(lv, cells, f, d);
  if (!r) return null;
  const onToxic = r.moved ? r.moved.filter((x, i) => x && lv.toxic[i]).length : 0;
  const cleared = r.removed.length - onToxic > 0;
  return {
    r,
    kind: r.pushed ? 'push' : r.replaced ? 'replace' : 'fill',
    clear: cleared,
    lose: ctx.isLost(lv, r.cells),
    win: ctx.isWon(lv, r.cells),
  };
}

// Toàn bộ không gian trạng thái (bàn nhỏ nên duyệt hết được), mỗi cạnh có loại nước.
function graph(lv, cap = 30000) {
  const start = ctx.keyOf(lv.init);
  const states = new Map([[start, lv.init]]);
  const edges = new Map();
  const queue = [start];
  let capped = false;
  for (let q = 0; q < queue.length; q++) {
    const key = queue[q], cells = states.get(key), out = [];
    edges.set(key, out);
    if (ctx.isLost(lv, cells) || ctx.isComplete(lv, cells)) continue;
    for (const [f, d] of ctx.allMoves(lv, cells)) {
      const m = classify(lv, cells, f, d);
      if (!m) continue;
      const to = ctx.keyOf(m.r.cells);
      out.push({ to, f, d, kind: m.kind, clear: m.clear, lose: m.lose, win: m.win });
      if (!states.has(to)) {
        if (states.size >= cap) { capped = true; continue; }
        states.set(to, m.r.cells);
        queue.push(to);
      }
    }
  }
  return { start, states, edges, capped };
}

// Số nước ít nhất để thắng khi chỉ được đi các nước thoả `allowed`; Infinity nếu không thắng được.
function bestWin(lv, g, allowed = () => true) {
  const dist = new Map([[g.start, 0]]);
  const queue = [g.start];
  for (let q = 0; q < queue.length; q++) {
    const key = queue[q];
    for (const e of g.edges.get(key) || []) {
      if (!allowed(e) || dist.has(e.to)) continue;
      dist.set(e.to, dist.get(key) + 1);
      if (e.win) return dist.get(e.to);
      queue.push(e.to);
    }
  }
  return Infinity;
}

function analyze(def) {
  def = asDef(def);
  const lv = ctx.parseLevel(def);
  const g = graph(lv);
  const count = { fill: 0, clear: 0, push: 0, replace: 0, lose: 0 };
  for (const out of g.edges.values()) for (const e of out) {
    if (e.lose) count.lose++;
    else if (e.clear) count.clear++;
    count[e.kind] += e.kind === 'fill' ? 0 : 1;
    if (e.kind === 'fill' && !e.clear && !e.lose) count.fill++;
  }
  // Trạng thái còn sống mà không còn đường thắng: người chơi bị kẹt, chỉ còn cách chơi lại.
  const canWin = new Set();
  const pred = new Map();
  for (const [k, out] of g.edges) for (const e of out) { if (!pred.has(e.to)) pred.set(e.to, []); pred.get(e.to).push(k); }
  const back = [];
  for (const [k, cells] of g.states) if (ctx.isWon(lv, cells)) { canWin.add(k); back.push(k); }
  for (let q = 0; q < back.length; q++) for (const k of pred.get(back[q]) || []) if (!canWin.has(k)) { canWin.add(k); back.push(k); }
  let alive = 0, stuck = 0;
  for (const [k, cells] of g.states) {
    if (ctx.isLost(lv, cells) || ctx.isComplete(lv, cells)) continue;
    alive++;
    if (!canWin.has(k)) stuck++;
  }
  const par = bestWin(lv, g);
  const first = g.edges.get(g.start) || [];
  return {
    rows: def.rows,
    states: g.states.size,
    capped: g.capped,
    par,
    // thắng được mà không cần loại nước đó (Infinity: loại nước đó là bắt buộc)
    noClear: bestWin(lv, g, e => !e.clear),
    noPush: bestWin(lv, g, e => e.kind !== 'push'),
    noReplace: bestWin(lv, g, e => e.kind !== 'replace'),
    fillOnly: bestWin(lv, g, e => e.kind === 'fill' && !e.clear),
    // chỉ lấp ô trống (khử 3 tự xảy ra thì vẫn được) / không đè (lấp + đẩy)
    fillWin: bestWin(lv, g, e => e.kind === 'fill'),
    pushWin: bestWin(lv, g, e => e.kind !== 'replace'),
    count,
    stuck: alive ? stuck / alive : 0,
    firstMoves: first.length,
    firstGood: first.filter(e => canWin.has(e.to)).length,
    firstLose: first.filter(e => e.lose).length,
  };
}

const show = a => {
  const inf = v => v === Infinity ? '—' : v;
  const c = a.count;
  return `${a.rows.join('/').padEnd(26)} par ${inf(a.par)}  chỉ-lấp ${inf(a.fillOnly)}  lấp+khử ${inf(a.fillWin)}  lấp+đẩy ${inf(a.pushWin)}  ` +
    `| nước: lấp ${c.fill} khử ${c.clear} đẩy ${c.push} đè ${c.replace} thua ${c.lose} | kẹt ${(a.stuck * 100).toFixed(0)}% ` +
    `| nước đầu ${a.firstGood}/${a.firstMoves} tốt, ${a.firstLose} thua | ${a.states} trạng thái${a.capped ? '+' : ''}`;
};

// Yêu cầu của từng giai đoạn. Trả về điểm (cao là tốt) hoặc null nếu không đạt.
const PHASES = {
  // A: mọi nước đều là lấp ô trống; không thể khử, đẩy, đè hay thua.
  A: a => (a.count.clear || a.count.push || a.count.replace || a.count.lose) ? null : a.par,
  // B: thắng được chỉ bằng lấp ô (không cần đẩy, đè); phải có khử 3 trong mọi lời giải hoặc có bẫy khử 3 làm thua ngay nước đầu.
  // B: lời giải ngắn nhất chỉ cần lấp ô (đẩy, đè không giúp đi tắt), và chỉ lấp thì bắt buộc phải khử 3.
  // (đẩy/đè có thể đi tắt: người chơi chưa học chúng, nên số nước chuẩn của level đặt bằng lời giải chỉ lấp `fillWin`.)
  B: a => {
    if (a.fillWin === Infinity || a.fillOnly !== Infinity || a.stuck > 0.35) return null;
    return a.fillWin + Math.min(a.firstLose, 3) - (a.par < a.fillWin ? 1 : 0);
  },
  // B0: như B nhưng không bắt buộc khử; có bẫy: nước đầu dễ thấy làm khử hết một màu (thua).
  B0: a => (a.fillWin === Infinity || a.firstLose === 0 || a.stuck > 0.35) ? null : a.fillWin + a.firstLose,
  // C: đẩy là bắt buộc, đè không giúp đi tắt.
  C: a => (a.fillWin !== Infinity || a.pushWin !== a.par || a.stuck > 0.4) ? null : a.par + Math.min(a.count.clear, 4) * 0.3,
  // D: đè là bắt buộc.
  D: a => (a.noReplace !== Infinity || a.stuck > 0.45) ? null : a.par + (a.noPush === Infinity ? 2 : 0),
};

// Sinh bàn ngẫu nhiên: khung liền khối trong w×h, vài con mèo.
function randomBoard(rand, w, h, size, colors, cats) {
  const cells = Array(w * h).fill('#');
  const start = Math.floor(rand() * w * h);
  cells[start] = '.';
  const shape = [start];
  while (shape.length < size) {
    const c = shape[Math.floor(rand() * shape.length)];
    const nb = [c - w, c + w, c % w ? c - 1 : -1, (c + 1) % w ? c + 1 : -1].filter(x => x >= 0 && x < w * h && cells[x] === '#');
    if (!nb.length) continue;
    const n = nb[Math.floor(rand() * nb.length)];
    cells[n] = '.';
    shape.push(n);
  }
  const pool = shape.slice().sort(() => rand() - 0.5).slice(0, cats);
  pool.forEach((c, i) => { cells[c] = 'ABCDE'[i < colors ? i : Math.floor(rand() * colors)]; });
  const rows = [];
  for (let r = 0; r < h; r++) rows.push(cells.slice(r * w, r * w + w).join(''));
  // bỏ hàng, cột rỗng ở mép
  while (rows.length && /^#+$/.test(rows[0])) rows.shift();
  while (rows.length && /^#+$/.test(rows[rows.length - 1])) rows.pop();
  let cols = rows.map(r => r.split(''));
  const emptyCol = j => cols.every(r => r[j] === '#');
  while (cols[0].length && emptyCol(0)) cols = cols.map(r => r.slice(1));
  while (cols[0].length && emptyCol(cols[0].length - 1)) cols = cols.map(r => r.slice(0, -1));
  return cols.map(r => r.join(''));
}

function search(phase, wanted, seed = 1) {
  const rand = rng(seed);
  const specs = {
    B0: { sizes: [[3, 2, 5], [3, 3, 6], [4, 2, 7]], colors: [2], cats: [2, 3, 4] },
    B: { sizes: [[3, 2, 6], [3, 3, 6], [3, 3, 7], [4, 2, 7], [4, 2, 8], [4, 3, 8]], colors: [2], cats: [3, 4, 5] },
    C: { sizes: [[3, 2, 5], [4, 2, 6], [3, 3, 7], [4, 3, 9], [4, 4, 11]], colors: [2, 3], cats: [3, 4, 5] },
    D: { sizes: [[3, 2, 5], [3, 3, 6], [4, 3, 8], [4, 3, 10], [4, 4, 12]], colors: [2, 3], cats: [3, 4, 5, 6] },
    A: { sizes: [[3, 3, 6], [4, 3, 8], [4, 4, 10]], colors: [1, 2], cats: [1, 2, 3] },
  }[phase];
  const seen = new Set(), found = [];
  const maxTries = +(process.env.TRIES || 6000);
  for (let tries = 0; tries < maxTries && found.length < wanted; tries++) {
    const [w, h, size] = specs.sizes[Math.floor(rand() * specs.sizes.length)];
    const colors = specs.colors[Math.floor(rand() * specs.colors.length)];
    const cats = Math.max(colors, specs.cats[Math.floor(rand() * specs.cats.length)]);
    const rows = randomBoard(rand, w, h, size, colors, cats);
    const key = rows.join('/');
    if (seen.has(key)) continue;
    seen.add(key);
    const lv = ctx.parseLevel({ rows });
    if (ctx.findRuns(lv, lv.init).length || ctx.isLost(lv, lv.init) || ctx.isComplete(lv, lv.init)) continue;
    const a = analyze({ rows });
    if (a.capped || a.par === Infinity) continue;
    const score = PHASES[phase](a);
    if (score === null) continue;
    found.push({ a, score });
  }
  found.sort((x, y) => y.score - x.score);
  return found;
}

if (require.main === module) {
  const [cmd, ...rest] = process.argv.slice(2);
  if (cmd === 'search') {
    const [phase, n = '30', seed = '1'] = rest;
    for (const { a, score } of search(phase, +n, +seed)) console.log(score.toFixed(1).padStart(5), show(a));
  } else if (cmd === 'check' && rest.length) {
    console.log(show(analyze({ rows: rest })));
  } else {
    ctx.LEVELS.slice(0, 15).forEach((d, i) => console.log(String(i + 1).padStart(2), show(analyze(d))));
  }
}

module.exports = { analyze, show, search, PHASES };
