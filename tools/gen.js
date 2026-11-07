// Sinh level ngẫu nhiên theo "job", giữ những level giải được, đúng độ dài, cơ chế thật sự cần, có yếu tố bất ngờ.
//   node tools/gen.js jobs.json [out.json]
// Một job (mọi trường trừ name đều tuỳ chọn):
//   frame: ['..#', '...']          khung cố định, hoặc
//   shape: { w, h, holes: [a, b] } khung ngẫu nhiên w×h khoét a..b ô (vẫn liền một khối, không trùng khung đã có)
//   cells: 'AAABBc'                mèo đặt ngẫu nhiên (chữ thường = đang ngủ, G = vi khuẩn)
//   toxic: n, ice: n (ô băng trơn), virus: n | '<>', membranes: n
//   mode: 'fill' | 'target' | 'kill', targets: n (số hộp)
//   need: ['clear', 'replace', ...]  sự kiện bắt buộc có trong lời giải ngắn nhất (tên như trong lib.events,
//                                    thêm 'ice' = băng làm đổi lời giải,
//                                    thêm 'oneway' = màng làm đổi lời giải)
//   min, max: khoảng số nước tối ưu · maxMs: bỏ level mà bộ giải chạy lâu hơn (mặc định 300)
//   twist: true -> đòi có ít nhất một mẹo trong lời giải và đa số nước đầu không tối ưu
//   strict: true -> mọi cơ chế có trên bàn phải bắt buộc dùng mới thắng được (tools/mechanic-use.js)
//   ms: thời gian chạy job · seed · top: số ứng viên in ra
const fs = require('fs');
const mechanicUse = require('./mechanic-use');
const { loadLogic, asDef, canon, rng, events, featString, explore, firstMoves, randomPlay, census, scores } = require('./lib');

const ctx = loadLogic();
const jobs = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const outFile = process.argv[3];
const used = new Set(ctx.LEVELS.map(d => canon(asDef(d).rows)));
const ARROWS = ['^', 'v', '<', '>'];
const results = [];

// Khung ngẫu nhiên: chữ nhật w×h khoét lỗ, giữ liền khối và giữ đủ hàng/cột.
function randomFrame(rnd, w, h, holes) {
  for (let tries = 0; tries < 200; tries++) {
    const g = Array(w * h).fill('.');
    const n = holes[0] + Math.floor(rnd() * (holes[1] - holes[0] + 1));
    for (let k = 0; k < n; k++) g[Math.floor(rnd() * w * h)] = '#';
    const open = g.map((ch, i) => ch === '.' ? i : -1).filter(i => i >= 0);
    if (!open.length) continue;
    const seen = new Set([open[0]]), stack = [open[0]];
    while (stack.length) {
      const i = stack.pop(), r = Math.floor(i / w), c = i % w;
      for (const [dr, dc] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const rr = r + dr, cc = c + dc, j = rr * w + cc;
        if (rr >= 0 && cc >= 0 && rr < h && cc < w && g[j] === '.' && !seen.has(j)) { seen.add(j); stack.push(j); }
      }
    }
    if (seen.size !== open.length) continue;
    const rows = [];
    for (let r = 0; r < h; r++) rows.push(g.slice(r * w, r * w + w).join(''));
    if (rows.some(row => !row.includes('.'))) continue;
    if ([...Array(w).keys()].some(c => rows.every(row => row[c] === '#'))) continue;
    return rows;
  }
  return null;
}

for (const job of jobs) {
  const { name, frame, shape, cells: put = 'AB', toxic = 0, ice = 0, virus = 0, membranes = 0, mode = 'fill', targets = 2,
    need = [], strict = false, min = 3, max = 99, maxMs = 300, twist = false, ms = 20000, seed = 1, nodes = 80000, top = 4 } = job;
  const rnd = rng(seed);
  const why = {};
  const no = reason => { why[reason] = (why[reason] || 0) + 1; return null; };

  function attempt() {
    const rows0 = frame || randomFrame(rnd, shape.w, shape.h, shape.holes);
    if (!rows0) return null;
    const key = canon(rows0);
    if (used.has(key)) return null;
    const W = rows0[0].length, H = rows0.length;
    const g = rows0.join('').split('');
    const free = () => g.map((ch, j) => ch === '.' ? j : -1).filter(j => j >= 0);
    for (let n = 0; n < toxic; n++) g[rnd.pick(free())] = '~';
    for (let n = 0; n < ice; n++) { const f = free(); if (!f.length) return null; g[rnd.pick(f)] = '='; }
    for (const ch of put) { const f = free(); if (!f.length) return null; g[rnd.pick(f)] = ch; }
    const arrows = typeof virus === 'string' ? [...virus] : Array.from({ length: virus }, () => rnd.pick(ARROWS));
    for (const a of arrows) g[rnd.pick(free())] = a;
    const toRows = arr => { const rows = []; for (let r = 0; r < H; r++) rows.push(arr.slice(r * W, r * W + W).join('')); return rows; };
    const def = { rows: toRows(g) };
    if (membranes) {
      const edges = [];
      for (let i = 0; i < W * H; i++) for (const dir of ['right', 'down']) {
        const r = Math.floor(i / W) + ctx.DIRS[dir][0], c = i % W + ctx.DIRS[dir][1];
        if (r < H && c < W && g[i] !== '#' && g[r * W + c] !== '#') edges.push([i, r * W + c, dir]);
      }
      def.oneway = [];
      for (let n = 0; n < membranes && edges.length; n++) {
        const [i, j, dir] = edges.splice(Math.floor(rnd() * edges.length), 1)[0];
        def.oneway.push(rnd() < 0.5 ? [Math.floor(i / W), i % W, dir] : [Math.floor(j / W), j % W, dir === 'right' ? 'left' : 'up']);
      }
    }
    if (mode === 'kill') def.kill = 'G';
    let lv = ctx.parseLevel(def);
    if (ctx.findRuns(lv, lv.init).length || ctx.isLost(lv, lv.init)) return null;
    const wantSleep = [...put].filter(ch => /[a-z]/.test(ch)).length;
    if (lv.init.filter(c => c && c.sleep).length < wantSleep) return null;   // không cho mèo ngủ thức ngay từ đầu
    if (mode === 'target') {
      // Hộp lấy từ một trạng thái kín ổ đạt được (chơi ngẫu nhiên không thua tới khi kín) -> chắc chắn giải được.
      // Bàn lớn quá rộng để duyệt hết, nên dùng các ván ngẫu nhiên thay cho BFS.
      // Ưu tiên nước lấp thêm ô (đẩy vào chỗ trống) để ván ngẫu nhiên tới được kín ổ trên bàn lớn; thử nước theo
      // thứ tự ngẫu nhiên và lấy nước hợp lệ đầu tiên, khỏi phải tính hết mọi nước ở mỗi bước.
      const count = s => s.filter(x => x && x.color !== 'V' && x.color !== lv.kill).length;
      let end = null;
      for (let walk = 0; walk < 20 && !end; walk++) {
        let c = lv.init;
        for (let t = 0; t < 40 && !ctx.isFull(lv, c); t++) {
          const moves = ctx.allMoves(lv, c).sort(() => rnd() - 0.5), wantGrow = rnd() < 0.85;
          let pick = null, any = null;
          for (const [f, dir] of moves) {
            const r = ctx.swipe(lv, c, f, dir);
            if (!r || ctx.isLost(lv, r.cells)) continue;
            any = any || r;
            if (!wantGrow || count(r.cells) > count(c)) { pick = r; break; }
          }
          if (!(pick || any)) break;
          c = (pick || any).cells;
        }
        if (ctx.isFull(lv, c) && !ctx.isLost(lv, c)) end = c;
      }
      if (!end) return no('không tìm được trạng thái kín');
      // Ưu tiên hộp ở ô lúc đầu trống hoặc khác màu, để hộp thật sự đòi người chơi làm gì đó
      const occ = end.map((c, i) => c && c.color !== 'V' && c.color !== lv.kill && (!lv.init[i] || lv.init[i].color !== c.color) ? i : -1)
        .filter(i => i >= 0);
      if (occ.length < targets) return no('ít ô cho hộp');
      const goal = Array(W * H).fill('.');
      for (let n = 0; n < targets; n++) { const i = occ.splice(Math.floor(rnd() * occ.length), 1)[0]; goal[i] = end[i].color; }
      def.goal = toRows(goal);
      lv = ctx.parseLevel(def);
    }
    if (ctx.isComplete(lv, lv.init)) return null;
    const t0 = Date.now();
    const path = ctx.solve(lv, lv.init, nodes, maxMs);
    const solveMs = Date.now() - t0;
    if (!path) return no(ctx.solve.cut ? 'quá giới hạn giải' : 'không giải được');
    if (path.length < min) return no('quá ngắn');
    if (path.length > max) return no('quá dài');
    if (solveMs > maxMs) return no('giải chậm');
    const ev = events(ctx, lv, path);
    for (const n of need) {
      if (n === 'oneway') {
        const plain = ctx.parseLevel({ ...def, oneway: [] });
        const alt = ctx.solve(plain, plain.init, nodes, maxMs);
        if (alt && alt.length <= path.length) {
          let c = lv.init, ok = true;
          for (const [f, d] of alt) { const r = ctx.swipe(lv, c, f, d); if (!r) { ok = false; break; } c = r.cells; }
          if (ok && ctx.isWon(lv, c)) return no('màng không đổi gì');
        }
      } else if (n === 'ice') {
        // Băng phải đổi cách giải: lời giải của bản không băng đem chơi trên bàn có băng thì không thắng.
        const plain = ctx.parseLevel({ ...def, rows: def.rows.map(row => row.replace(/=/g, '.')) });
        const alt = ctx.solve(plain, plain.init, nodes, maxMs);
        if (alt) {
          let c = lv.init, ok = true;
          for (const [f, d] of alt) { const r = ctx.swipe(lv, c, f, d); if (!r) { ok = false; break; } c = r.cells; }
          if (ok && ctx.isWon(lv, c)) return no('băng không đổi gì');
        }
        if (!ev.slide) return no('không trượt');
      } else if (n === 'virus') { if (!ev.spread || !ev.virusDie) return no('thiếu ' + n); }
      else if (!ev[n]) return no('thiếu ' + n);
    }
    if (strict) {
      const use = mechanicUse.check(ctx, def);
      const weak = Object.keys(use).find(k => use[k] !== 'bắt buộc');
      if (weak) return no(`${weak} ${use[weak]}`);
    }
    // Lọc rẻ trước (nước đầu bằng A*), duyệt toàn bộ không gian sau cùng vì tốn nhất
    const fm = firstMoves(ctx, lv, path.length, nodes, maxMs);
    const pre = scores({ par: path.length, deadFrac: 0, win: 0, ev, ...census(def), ...fm });
    if (twist && pre.tricks < 1) return no('không có mẹo');
    if (twist && fm.firstOpt / fm.first > 0.5) return no('nước đầu dễ đoán');
    const ex = explore(ctx, lv, 3000);
    const info = { def, ...census(def), ...ex, ...fm, par: path.length, ms: solveMs, ...randomPlay(ctx, lv, 150), ev };
    const sc = scores(info);
    if (!frame) used.add(key);   // khung ngẫu nhiên: mỗi khung chỉ giữ một level
    return { ...info, ...sc };
  }

  const out = [];
  const t0 = Date.now();
  while (Date.now() - t0 < ms) { const r = attempt(); if (r) out.push(r); }
  out.sort((x, y) => y.surprise - x.surprise || y.difficulty - x.difficulty);
  console.log(`== ${name} (${out.length} ứng viên) · bị loại: ${JSON.stringify(why)}`);
  for (const r of out.slice(0, top)) {
    console.log('  ' + JSON.stringify(r.def), `par=${r.par} ms=${r.ms} mèo=${r.cats} ô=${r.slots}`,
      `đầu=${r.first}/${r.firstOk}/${r.firstOpt} cụt=${r.deadFrac === null ? "?" : r.deadFrac.toFixed(2)} thừa=${r.extra} khó=${r.difficulty} bấtngờ=${r.surprise}`,
      `[${featString(r.ev)}]`);
    results.push({ job: name, def: r.def, par: r.par, ms: r.ms, cats: r.cats, slots: r.slots, first: r.first,
      firstOk: r.firstOk, firstOpt: r.firstOpt, deadFrac: r.deadFrac, extra: r.extra, difficulty: r.difficulty, surprise: r.surprise,
      feats: featString(r.ev) });
  }
}
if (outFile) fs.writeFileSync(outFile, JSON.stringify(results, null, 1));
console.log('DONE');
