// So các phương án luật khử 3 trên toàn bộ level, bằng mô hình người chơi casual.
//   node tools/study-clear.js            -> bảng theo khối + từng level khối 1–2
//   node tools/study-clear.js --json f   -> ghi thêm kết quả chi tiết ra file f
//
// Phương án (đổi findRuns trên bản sao luật, không sửa game):
//   hiện tại     : 3 cùng màu thẳng hàng là khử
//   khử an toàn  : dãy nào khử xong làm màu đó tuyệt chủng thì không khử
//   khử từ 4     : chỉ dãy từ 4 trở lên mới khử
//   không khử    : bỏ hẳn luật khử
//
// Người chơi casual (không nhìn trước, chọn ngẫu nhiên trong các nước "lấp thêm ô"):
//   quên luật    : chọn bất kỳ nước nào làm thêm một ô có mèo (trước khi khử), không để ý khử 3 hay tuyệt chủng
//   cẩn thận     : như trên nhưng bỏ các nước thua ngay
// Hết nước lấp thì đi nước bất kỳ (thay thế...). Quá 3×par nước hoặc hết nước đi được thì tính là "kẹt".
const fs = require('fs');
const { loadLogic, loadChapters, asDef, rng, census } = require('./lib');

const RUNS = 200;
const ctx = loadLogic();
const CH = loadChapters(ctx);
const originalFindRuns = ctx.findRuns;

// Đếm mèo theo màu (cả mèo ngủ), để biết khử dãy nào thì tuyệt chủng
const catColors = cells => {
  const n = {};
  for (const c of cells) if (c && /[A-F]/.test(c.color)) n[c.color] = (n[c.color] || 0) + 1;
  return n;
};

function runsMin(level, cells, min) {
  const out = new Set();
  const colorAt = i => cells[i] && cells[i].color !== 'V' ? cells[i].color : null;
  const scan = (start, stride, count) => {
    let runStart = 0;
    for (let k = 1; k <= count; k++) {
      const prev = colorAt(start + (k - 1) * stride);
      const cur = k < count ? colorAt(start + k * stride) : null;
      if (cur && cur === prev) continue;
      if (prev && k - runStart >= min) for (let j = runStart; j < k; j++) out.add(start + j * stride);
      runStart = k;
    }
  };
  for (let r = 0; r < level.h; r++) scan(r * level.w, 1, level.w);
  for (let c = 0; c < level.w; c++) scan(c, level.w, level.h);
  return [...out];
}

const VARIANTS = {
  'hiện tại': originalFindRuns,
  'khử an toàn': (level, cells) => {
    const runs = originalFindRuns(level, cells);
    if (!runs.length) return runs;
    const have = catColors(cells), gone = {};
    for (const i of runs) if (/[A-F]/.test(cells[i].color)) gone[cells[i].color] = (gone[cells[i].color] || 0) + 1;
    return runs.filter(i => !(/[A-F]/.test(cells[i].color) && gone[cells[i].color] >= have[cells[i].color]));
  },
  'khử từ 4': (level, cells) => runsMin(level, cells, 4),
  'không khử': () => [],
};

const filled = cells => cells.filter(c => c && c.color !== 'V').length;

function casualGame(lv, rnd, careful, maxMoves) {
  let c = lv.init, id = lv.nextId, clears = 0;
  for (let t = 0; t < maxMoves; t++) {
    if (ctx.isLost(lv, c)) return { result: 'thua', clears };
    if (ctx.isWon(lv, c)) return { result: 'thắng', clears };
    const options = [];
    for (const [f, d] of ctx.allMoves(lv, c)) {
      const r = ctx.swipe(lv, c, f, d, id);
      if (r) options.push(r);
    }
    if (!options.length) return { result: 'kẹt', clears };
    const before = filled(c);
    const safe = careful ? options.filter(r => !ctx.isLost(lv, r.cells)) : options;
    let pool = safe.filter(r => filled(r.moved) > before);
    if (!pool.length) pool = safe;
    if (!pool.length) return { result: careful ? 'kẹt' : 'thua', clears };
    const r = rnd.pick(pool);
    if (r.removed.length >= 3) clears++;
    c = r.cells;
    id = r.nextId;
  }
  return { result: 'kẹt', clears };
}

function measure(def) {
  const lv = ctx.parseLevel(asDef(def));
  const path = ctx.solve(lv, lv.init, 80000, 1500);
  const out = { solvable: !!path, par: path ? path.length : null, extra: path ? path.length - census(def).fill : null };
  const maxMoves = Math.max(12, 3 * (out.par || census(def).fill || 4));
  for (const careful of [false, true]) {
    const rnd = rng(7);
    let win = 0, lose = 0, clears = 0;
    for (let k = 0; k < RUNS; k++) {
      const g = casualGame(lv, rnd, careful, maxMoves);
      if (g.result === 'thắng') win++;
      if (g.result === 'thua') lose++;
      clears += g.clears;
    }
    out[careful ? 'careful' : 'forgetful'] = { win: win / RUNS, lose: lose / RUNS, clears: clears / RUNS };
  }
  return out;
}

const results = {};
for (const [name, findRuns] of Object.entries(VARIANTS)) {
  ctx.findRuns = findRuns;
  results[name] = ctx.LEVELS.map((def, i) => ({ level: i + 1, ...measure(def) }));
  process.stderr.write(`xong phương án "${name}"\n`);
}
ctx.findRuns = originalFindRuns;

const pct = x => (100 * x).toFixed(0).padStart(3) + '%';
const avg = (list, f) => list.length ? list.reduce((s, x) => s + f(x), 0) / list.length : 0;
console.log('Tỷ lệ thắng của người chơi casual (quên luật / cẩn thận), số level giải được, nước thừa trung bình\n');
for (const [b, ch] of CH.entries()) {
  console.log(`Khối ${b + 1} · ${ch.name} (level ${ch.from + 1}–${ch.to + 1})`);
  for (const name of Object.keys(VARIANTS)) {
    const list = results[name].slice(ch.from, ch.to + 1);
    const ok = list.filter(x => x.solvable);
    console.log(`  ${name.padEnd(12)} giải được ${String(ok.length).padStart(2)}/${list.length}` +
      `  thắng ${pct(avg(ok, x => x.forgetful.win))} / ${pct(avg(ok, x => x.careful.win))}` +
      `  thua ${pct(avg(ok, x => x.forgetful.lose))}  khử/ván ${avg(ok, x => x.forgetful.clears).toFixed(2)}` +
      `  thừa ${avg(ok, x => x.extra).toFixed(2)}`);
  }
}
console.log('\nTừng level khối 1–2 (thắng quên luật / cẩn thận · par · thừa):');
const head = Object.keys(VARIANTS);
console.log('level  ' + head.map(h => h.padEnd(22)).join(''));
for (let i = 0; i <= CH[1].to; i++) {
  console.log(String(i + 1).padStart(5) + '  ' + head.map(h => {
    const x = results[h][i];
    return (x.solvable ? `${pct(x.forgetful.win)}/${pct(x.careful.win)} p${x.par} t${x.extra}` : 'không giải được').padEnd(22);
  }).join(''));
}
const jsonAt = process.argv.indexOf('--json');
if (jsonAt > 0) fs.writeFileSync(process.argv[jsonAt + 1], JSON.stringify(results, null, 1));
