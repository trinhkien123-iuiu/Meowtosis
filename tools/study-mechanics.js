// Mỗi cơ chế có thật sự đổi cách giải không? So từng level có cơ chế với bản "bỏ cơ chế" (phản chứng):
//   khuẩn  : vi khuẩn G thành ô trống, bỏ điều kiện diệt khuẩn
//   ngủ    : mèo ngủ thành mèo thức cùng màu
//   độc    : ô độc thành lỗ (#)
//   màng   : bỏ màng một chiều
//   hộp    : bỏ hộp mục tiêu
//   virus  : virus thành ô trống
//   băng   : ô băng thành ô thường
// Đo: par, nước thừa (par − số ô phải lấp), độ khó theo quyết định (bitsOpt, bitsWin như tools/order.js),
// tỷ lệ thắng của người chơi casual cẩn thận (như study-clear.js), và lời giải tối ưu có dùng tới cơ chế không.
// Cơ chế "có nghĩa" khi bỏ nó đi thì cách giải đổi hẳn; chỉ làm dài thêm vài nước mà quyết định như cũ là cơ chế "trang trí".
//   node tools/study-mechanics.js [tên cơ chế] [--json file]
const fs = require('fs');
const { loadLogic, asDef, census, events, decisionBits, rng } = require('./lib');

const ctx = loadLogic();
const RUNS = 150;

const mapRows = (d, f) => ({ ...d, rows: d.rows.map(row => [...row].map(f).join('')) });
const MECHANICS = {
  'khuẩn': { has: d => !!d.kill, strip: d => { const x = mapRows(d, ch => ch === 'G' ? '.' : ch); delete x.kill; return x; },
    used: ev => `diệt ${ev.germDie}${ev.germClear ? `, khử 3 khuẩn ${ev.germClear}` : ''}` },
  'ngủ': { has: d => /[a-f]/.test(d.rows.join('')), strip: d => mapRows(d, ch => /[a-f]/.test(ch) ? ch.toUpperCase() : ch),
    used: ev => `đánh thức ${ev.wake}${ev.wakeUse ? `, vuốt mèo vừa thức ${ev.wakeUse}` : ''}` },
  'độc': { has: d => d.rows.join('').includes('~'), strip: d => mapRows(d, ch => ch === '~' ? '#' : ch),
    used: ev => `vào ô độc ${ev.toxic}` },
  'màng': { has: d => !!(d.oneway && d.oneway.length), strip: d => { const x = { ...d }; delete x.oneway; return x; },
    used: () => '' },
  'hộp': { has: d => !!d.goal, strip: d => { const x = { ...d }; delete x.goal; return x; }, used: () => '' },
  'băng': { has: d => d.rows.join('').includes('='), strip: d => mapRows(d, ch => ch === '=' ? '.' : ch),
    used: ev => `trượt ${ev.slide}` },
  'virus': { has: d => /[<>^v]/.test(d.rows.join('')), strip: d => mapRows(d, ch => /[<>^v]/.test(ch) ? '.' : ch),
    used: ev => `lan ${ev.spread}, ăn mèo ${ev.eaten}, diệt ${ev.virusDie}` },
};

const filled = cells => cells.filter(c => c && c.color !== 'V').length;
function carefulWin(lv, maxMoves) {
  const rnd = rng(7);
  let win = 0;
  for (let k = 0; k < RUNS; k++) {
    let c = lv.init, id = lv.nextId;
    for (let t = 0; t < maxMoves; t++) {
      if (ctx.isLost(lv, c)) break;
      if (ctx.isWon(lv, c)) { win++; break; }
      const options = [];
      for (const [f, d] of ctx.allMoves(lv, c)) {
        const r = ctx.swipe(lv, c, f, d, id);
        if (r && !ctx.isLost(lv, r.cells)) options.push(r);
      }
      if (!options.length) break;
      let pool = options.filter(r => filled(r.moved) > filled(c));
      if (!pool.length) pool = options;
      const r = rnd.pick(pool);
      c = r.cells;
      id = r.nextId;
    }
  }
  return win / RUNS;
}

function measure(def) {
  const lv = ctx.parseLevel(def);
  const path = ctx.solve(lv, lv.init, 80000, 2000);
  if (!path) return { solvable: false };
  const bits = decisionBits(ctx, lv, path, 80);
  return { solvable: true, par: path.length, extra: path.length - census(def).fill, ...bits,
    win: carefulWin(lv, Math.max(12, 3 * path.length)), ev: events(ctx, lv, path) };
}

const only = process.argv[2] && !process.argv[2].startsWith('--') ? [process.argv[2]] : Object.keys(MECHANICS);
const results = {};
for (const name of only) {
  const m = MECHANICS[name];
  results[name] = [];
  ctx.LEVELS.forEach((raw, i) => {
    const def = asDef(raw);
    if (!m.has(def)) return;
    const withIt = measure(def), without = measure(m.strip(def));
    results[name].push({ level: i + 1, rows: def.rows.join('/'), withIt, without, used: withIt.solvable ? m.used(withIt.ev) : '' });
  });
  process.stderr.write(`xong cơ chế "${name}" (${results[name].length} level)\n`);
}

const f = (x, k, digits = 1) => x.solvable ? (+x[k]).toFixed(digits) : '–';
for (const [name, list] of Object.entries(results)) {
  console.log(`\n== ${name}: ${list.length} level ==`);
  console.log('level  par có/bỏ  thừa có/bỏ  bitsOpt có/bỏ  bitsWin có/bỏ  casual thắng có/bỏ  lời giải dùng');
  for (const x of list) {
    const a = x.withIt, b = x.without;
    console.log(`${String(x.level).padStart(5)}  ${f(a, 'par', 0).padStart(3)}/${f(b, 'par', 0).padEnd(4)}   ${f(a, 'extra', 0).padStart(3)}/${f(b, 'extra', 0).padEnd(4)}` +
      `   ${f(a, 'bitsOpt').padStart(5)}/${f(b, 'bitsOpt').padEnd(6)}  ${f(a, 'bitsWin').padStart(5)}/${f(b, 'bitsWin').padEnd(6)}` +
      `  ${a.solvable ? (100 * a.win).toFixed(0).padStart(4) + '%' : '   –'}/${b.solvable ? (100 * b.win).toFixed(0) + '%' : '–'}`.padEnd(20) + `  ${x.used}`);
  }
  const both = list.filter(x => x.withIt.solvable && x.without.solvable);
  const mean = g => (both.reduce((s, x) => s + g(x), 0) / Math.max(both.length, 1)).toFixed(2);
  console.log(`trung bình (${both.length} level giải được cả hai): Δpar ${mean(x => x.withIt.par - x.without.par)}` +
    `  ΔbitsOpt ${mean(x => x.withIt.bitsOpt - x.without.bitsOpt)}  ΔbitsWin ${mean(x => x.withIt.bitsWin - x.without.bitsWin)}` +
    `  Δcasual thắng ${mean(x => 100 * (x.withIt.win - x.without.win))} điểm %`);
}
const jsonAt = process.argv.indexOf('--json');
if (jsonAt > 0) fs.writeFileSync(process.argv[jsonAt + 1], JSON.stringify(results, null, 1));
