// Dùng chung cho các công cụ level: nạp luật từ game, khung chuẩn hoá, số ngẫu nhiên có seed, phân tích level.
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const GAME = path.join(__dirname, '..', 'prototype', 'meowtosis.html');
const LEVELS_JSON = path.join(__dirname, '..', 'data', 'levels.json');

// Rút khối // ==LOGIC== ... // ==/LOGIC== của game và chạy trong vm: công cụ dùng đúng luật game đang chạy.
function loadLogic(htmlPath = GAME) {
  const html = fs.readFileSync(htmlPath, 'utf8');
  const m = html.match(/\/\/ ==LOGIC==([\s\S]*?)\/\/ ==\/LOGIC==/);
  if (!m) throw new Error('Không thấy khối LOGIC trong ' + htmlPath);
  const ctx = { console };
  vm.createContext(ctx);
  // const/let ở top-level không gắn vào context, nên xuất các tên cần dùng qua globalThis.
  vm.runInContext(m[1] + `
Object.assign(globalThis, { LEVELS, DIRS, parseLevel, step, swipe, resolve, findRuns, allMoves, solve,
  isFull, isComplete, isWon, isLost, keyOf, targetsDone, mutate, transplant, extinct, wake });`, ctx, { filename: htmlPath });
  return ctx;
}

const asDef = d => Array.isArray(d) ? { rows: d } : d;

// Mảng CHAPTERS nằm ở phần giao diện (ngoài khối LOGIC): đọc riêng ra.
function loadChapters(ctx, htmlPath = GAME) {
  const src = fs.readFileSync(htmlPath, 'utf8').match(/const CHAPTERS = (\[[\s\S]*?\n\]);/);
  if (!src) throw new Error('Không thấy CHAPTERS trong ' + htmlPath);
  return new Function('LEVELS', `return ${src[1]};`)(ctx.LEVELS);
}

// Dữ liệu level để chuyển sang Unity: định nghĩa gốc + chương + số nước tối ưu + một lời giải [hàng, cột, hướng].
function exportLevels(ctx) {
  const chapters = loadChapters(ctx);
  return ctx.LEVELS.map((d, i) => {
    const def = asDef(d), lv = ctx.parseLevel(def), path = ctx.solve(lv, lv.init);
    const k = chapters.findIndex(c => i >= c.from && i <= c.to);
    return { index: i + 1, chapter: k + 1, chapterName: chapters[k].name, ...JSON.parse(JSON.stringify(def)),
      par: def.par !== undefined ? def.par : (path ? path.length : null),
      solution: def.solution ? def.solution : (path ? path.map(([from, dir]) => [Math.floor(from / lv.w), from % lv.w, dir]) : null) };
  });
}

// Dạng chuẩn của một khung (bỏ qua xoay và lật) để phát hiện hai level trùng khung.
function canon(rows) {
  const cells = [];
  rows.forEach((row, r) => [...row].forEach((ch, c) => { if (ch !== '#') cells.push([r, c]); }));
  const forms = [];
  for (let k = 0; k < 8; k++) {
    const t = cells.map(([r, c]) => { let [a, b] = k & 4 ? [c, r] : [r, c]; if (k & 1) a = -a; if (k & 2) b = -b; return [a, b]; });
    const mr = Math.min(...t.map(p => p[0])), mc = Math.min(...t.map(p => p[1]));
    forms.push(t.map(([a, b]) => `${a - mr},${b - mc}`).sort().join(' '));
  }
  return forms.sort()[0];
}

function rng(seed) {
  let s = (seed * 2654435761 % 2147483648 + 12345) % 2147483648;   // trộn seed: seed liền nhau không ra dãy giống nhau
  const rnd = () => (s = (s * 1103515245 + 12345) % 2147483648) / 2147483648;
  rnd.pick = arr => arr[Math.floor(rnd() * arr.length)];
  return rnd;
}

// Chạy một lời giải, đếm các sự kiện. Dùng để biết cơ chế nào thật sự cần và để chấm "bất ngờ".
function events(ctx, lv, path) {
  const ev = { push: 0, longPush: 0, replace: 0, clear: 0, cleared: 0, toxic: 0, wake: 0, chainWake: 0, wakeUse: 0,
    spread: 0, eaten: 0, virusDie: 0, germDie: 0, germClear: 0, slide: 0 };
  const sleepers = new Set(lv.init.filter(x => x && x.sleep).map(x => x.id));
  let c = lv.init, id = lv.nextId;   // cấp id mới như game, để mèo sinh ra và virus lan ra không trùng id cũ
  for (const [f, d] of path) {
    if (sleepers.has(c[f].id)) ev.wakeUse++;   // vuốt một con lúc đầu đang ngủ: đánh thức là cần thiết
    const r = ctx.swipe(lv, c, f, d, id);
    if (r.pushed) {
      ev.push++;
      let n = 0;
      for (let k = ctx.step(lv, f, d); k >= 0 && c[k]; k = ctx.step(lv, k, d)) n++;
      if (n >= 2) ev.longPush++;
    }
    if (r.replaced) ev.replace++;
    if (r.slid) ev.slide++;
    const killed = [r.replaced, ...r.removed, ...r.poisoned, ...r.eaten].filter(Boolean);
    ev.virusDie += killed.filter(x => x.color === 'V').length;
    ev.germDie += killed.filter(x => x.color === lv.kill).length;
    const onToxic = r.moved.filter((x, i) => x && lv.toxic[i]).length;
    ev.toxic += onToxic + r.poisoned.length;
    const runs = r.removed.length - onToxic;
    if (runs > 0) { ev.clear++; ev.cleared += runs; }
    ev.germClear += r.removed.filter(x => x.color === lv.kill).length && runs > 0 ? 1 : 0;
    ev.spread += r.spread.length;
    ev.eaten += r.eaten.filter(x => x.color !== 'V').length;
    const woke = r.cells.filter(x => x && !x.sleep && c.some(o => o && o.sleep && o.id === x.id)).length;
    ev.wake += woke;
    if (woke >= 2) ev.chainWake++;
    c = r.cells;
    id = r.nextId;
  }
  return ev;
}
const featString = ev => Object.entries(ev).filter(([, v]) => v).map(([k, v]) => k + v).join(' ');

// Duyệt toàn bộ không gian trạng thái (tới `cap`), tính khoảng cách tới thắng của từng trạng thái.
// Trả: số trạng thái, tỷ lệ ngõ cụt, số nước đầu đi được / còn thắng được / vẫn tối ưu.
function explore(ctx, lv, cap = 150000) {
  const startKey = ctx.keyOf(lv.init);
  const succ = new Map(), state = new Map([[startKey, lv.init]]);
  const queue = [startKey];
  let capped = false;
  for (let q = 0; q < queue.length; q++) {
    const key = queue[q], cells = state.get(key), out = [];
    succ.set(key, out);
    if (ctx.isLost(lv, cells) || ctx.isComplete(lv, cells)) continue;
    for (const [f, d] of ctx.allMoves(lv, cells)) {
      const r = ctx.swipe(lv, cells, f, d);
      if (!r) continue;
      const k2 = ctx.keyOf(r.cells);
      out.push(k2);
      if (!state.has(k2)) {
        if (state.size >= cap) { capped = true; continue; }
        state.set(k2, r.cells);
        queue.push(k2);
      }
    }
  }
  // Khoảng cách tới thắng: BFS ngược từ các trạng thái thắng.
  const pred = new Map();
  for (const [k, out] of succ) for (const k2 of out) { if (!pred.has(k2)) pred.set(k2, []); pred.get(k2).push(k); }
  const dist = new Map(), back = [];
  for (const [k, cells] of state) if (ctx.isWon(lv, cells)) { dist.set(k, 0); back.push(k); }
  for (let q = 0; q < back.length; q++) {
    const d = dist.get(back[q]);
    for (const k of pred.get(back[q]) || []) if (!dist.has(k)) { dist.set(k, d + 1); back.push(k); }
  }
  let open = 0, dead = 0;
  for (const [k, cells] of state) {
    if (ctx.isLost(lv, cells) || ctx.isComplete(lv, cells)) continue;
    open++;
    if (!dist.has(k)) dead++;
  }
  const par = dist.get(startKey);
  const first = succ.get(startKey) || [];
  const firstOk = first.filter(k => dist.has(k)).length;
  const firstOpt = first.filter(k => dist.get(k) === par - 1).length;
  // Bị chặn ở `cap` thì chưa thấy hết các trạng thái thắng: tỷ lệ ngõ cụt không có nghĩa.
  return { states: state.size, capped, par, deadFrac: capped ? null : open ? dead / open : 0, first: first.length, firstOk, firstOpt };
}

// Chơi ngẫu nhiên (bỏ qua nước không đi được), đếm tỷ lệ thắng / thua.
function randomPlay(ctx, lv, runs = 300, seed = 5) {
  const rnd = rng(seed);
  let wins = 0, losses = 0;
  for (let k = 0; k < runs; k++) {
    let c = lv.init;
    for (let t = 0; t < 80; t++) {
      if (ctx.isLost(lv, c)) { losses++; break; }
      if (ctx.isWon(lv, c)) { wins++; break; }
      const m = ctx.allMoves(lv, c);
      if (!m.length) break;
      const [f, d] = rnd.pick(m);
      const r = ctx.swipe(lv, c, f, d);
      if (r) c = r.cells;
    }
  }
  return { win: wins / runs, lose: losses / runs };
}

// Đếm thông tin cơ bản của level: số ô cần lấp, số mèo, số màu, cơ chế có mặt.
function census(def) {
  const d = asDef(def), flat = d.rows.join('');
  return {
    w: d.rows[0].length, h: d.rows.length,
    slots: [...flat].filter(ch => ch !== '#' && ch !== '~').length,
    cats: [...flat].filter(ch => /[A-Fa-f]/.test(ch)).length,
    // Số mèo còn phải sinh thêm để kín ổ = số nước tối thiểu (mỗi nước sinh đúng một mèo)
    fill: [...flat].filter(ch => ch !== '#' && ch !== '~' && !/[A-Fa-f]/.test(ch)).length,
    colors: new Set([...flat].filter(ch => /[A-Fa-f]/.test(ch)).map(ch => ch.toUpperCase())).size,
    mech: [d.goal && 'hộp', d.oneway && d.oneway.length && 'màng', flat.includes('~') && 'độc', /[a-f]/.test(flat) && 'ngủ',
      /[<>^v]/.test(flat) && 'virus', d.kill && 'khuẩn', flat.includes('=') && 'băng'].filter(Boolean),
  };
}

// Nước đầu: bao nhiêu nước đi được, bao nhiêu còn thắng được, bao nhiêu vẫn tối ưu (giải lại từ từng nước bằng A*).
function firstMoves(ctx, lv, par, nodes = 30000, maxMs = 1000) {
  const seen = new Set();
  let first = 0, firstOk = 0, firstOpt = 0;
  for (const [f, d] of ctx.allMoves(lv, lv.init)) {
    const r = ctx.swipe(lv, lv.init, f, d);
    if (!r) continue;
    const k = ctx.keyOf(r.cells);
    if (seen.has(k)) continue;
    seen.add(k);
    first++;
    if (ctx.isLost(lv, r.cells)) continue;
    const p = ctx.solve(lv, r.cells, nodes, maxMs);
    if (p) { firstOk++; if (p.length === par - 1) firstOpt++; }
  }
  return { first, firstOk, firstOpt };
}

// Độ khó theo quyết định: đi dọc một lời giải tối ưu, ở mỗi bước đếm số nước đi được (n), số nước còn thắng được (ok)
// và số nước vẫn tối ưu (opt). bitsWin = Σ log2(n/ok): rủi ro sa vào ngõ cụt; bitsOpt = Σ log2(n/opt): phải nghĩ bao
// nhiêu để giữ 3 sao. Level dài nhưng nước nào cũng đúng thì điểm thấp; level ngắn mà toàn bẫy thì điểm cao.
// Giải không kịp maxMs thì coi như còn thắng được (không phạt oan).
function decisionBits(ctx, lv, path, maxMs = 250) {
  let c = lv.init, left = path.length, bitsWin = 0, bitsOpt = 0, id = lv.nextId;
  for (const [f, d] of path) {
    const seen = new Set();
    let n = 0, ok = 0, opt = 0;
    for (const [f2, d2] of ctx.allMoves(lv, c)) {
      const r = ctx.swipe(lv, c, f2, d2);
      if (!r) continue;
      const k = ctx.keyOf(r.cells);
      if (seen.has(k)) continue;
      seen.add(k);
      n++;
      if (ctx.isLost(lv, r.cells)) continue;
      const p = ctx.solve(lv, r.cells, 80000, maxMs);
      if (p || ctx.solve.cut) { ok++; if (p && p.length === left - 1) opt++; }
    }
    bitsWin += Math.log2(n / Math.max(ok, 1));
    bitsOpt += Math.log2(n / Math.max(opt, 1));
    const r = ctx.swipe(lv, c, f, d, id);
    c = r.cells;
    id = r.nextId;
    left--;
  }
  return { bitsWin: +bitsWin.toFixed(2), bitsOpt: +bitsOpt.toFixed(2) };
}

// Điểm độ khó (để sắp thứ tự trong chương) và điểm bất ngờ (nước đầu "hiển nhiên" thường sai, lời giải cần mẹo).
// Mẹo: thay thế, khử 3, ô độc, đánh thức dây chuyền, virus ăn mèo, diệt virus, khử 3 vi khuẩn.
// extra: số nước phải tiêu thêm ngoài số mèo còn thiếu (nước "lùi một bước để tiến hai bước").
function scores(info) {
  const { par, deadFrac, first, firstOk, firstOpt, win, ev } = info;
  const trap = first ? 1 - firstOk / first : 0;           // nước đầu dẫn vào ngõ cụt
  const narrow = first ? 1 - firstOpt / first : 0;        // nước đầu không tối ưu
  const tricks = (ev.replace ? 1 : 0) + (ev.clear ? 1 : 0) + (ev.toxic ? 1 : 0) + (ev.chainWake ? 1 : 0)
    + (ev.eaten ? 1 : 0) + (ev.virusDie ? 1 : 0) + (ev.germClear ? 1 : 0);
  const extra = info.fill === undefined ? 0 : par - info.fill;
  const difficulty = par * 0.6 + (1 - win) * 3 + (deadFrac || 0) * 3 + narrow * 2;
  const surprise = trap * 3 + narrow * 2 + tricks + Math.min(extra, 3) * 0.5;
  return { trap, narrow, tricks, extra, difficulty: +difficulty.toFixed(2), surprise: +surprise.toFixed(2) };
}

function analyze(ctx, def, opts = {}) {
  const lv = ctx.parseLevel(asDef(def));
  const t0 = Date.now();
  const path = ctx.solve(lv, lv.init, opts.nodes || 80000);
  const ms = Date.now() - t0;
  if (!path) return { def, solved: false, ms };
  let ex = explore(ctx, lv, opts.cap || 20000);
  if (ex.capped) ex = { ...ex, ...firstMoves(ctx, lv, path.length) };
  const rp = randomPlay(ctx, lv, opts.runs || 300);
  const ev = events(ctx, lv, path);
  const info = { def, solved: true, ms, ...census(def), ...ex, par: path.length, ...rp, ev, path };
  return { ...info, ...scores(info) };
}

module.exports = { GAME, LEVELS_JSON, loadLogic, loadChapters, exportLevels, asDef, canon, rng, events, featString,
  explore, firstMoves, decisionBits, randomPlay, census, scores, analyze };
