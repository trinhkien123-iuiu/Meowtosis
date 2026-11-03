// Sắp lại level từ dễ đến khó TRONG từng khối (CHAPTERS), giữ khối theo cơ chế và giữ nhịp dạy → luyện → trộn.
//   node tools/order.js            -> in thứ tự đề xuất (không sửa gì)
//   node tools/order.js --write    -> ghi thứ tự mới vào prototype/meowtosis.html
// Cần tools/jobs/difficulty.json (node tools/difficulty.js).
//
// Mỗi khối chia thành các giai đoạn theo cơ chế (bảng PHASES). Trong một giai đoạn: level có hướng dẫn (tut) đứng đầu,
// rồi level có mẹo dạy màu mới, còn lại xếp theo độ khó tăng dần. Mẹo "lần đầu gặp màu" và mẹo "bàn đông mèo"
// được chuyển sang đúng level xuất hiện đầu tiên sau khi sắp.
const fs = require('fs');
const path = require('path');
const { GAME, loadLogic, loadChapters, asDef } = require('./lib');

const ctx = loadLogic();
const CH = loadChapters(ctx);
const DIFF = JSON.parse(fs.readFileSync(path.join(__dirname, 'jobs', 'difficulty.json'), 'utf8'));
const keyOfDef = d => JSON.stringify(asDef(d));

// Thông tin từng level để xếp
const info = ctx.LEVELS.map((raw, i) => {
  const def = asDef(raw), m = DIFF[keyOfDef(raw)];
  if (!m) throw new Error(`Level ${i + 1} chưa có số đo độ khó: chạy node tools/difficulty.js`);
  const lv = ctx.parseLevel(def);
  // Level hướng dẫn cơ bản dạy gì: chạy các bước tut, xem có thay thế / khử / đẩy
  let teach = null;
  if (def.tut) {
    let c = lv.init, ev = { push: 0, replace: 0, clear: 0 };
    for (const [r, col, dir] of def.tut) {
      const res = ctx.swipe(lv, c, r * lv.w + col, dir);
      if (res.pushed) ev.push++;
      if (res.replaced) ev.replace++;
      if (res.removed.length) ev.clear++;
      c = res.cells;
    }
    teach = ev.clear ? 'clear' : ev.replace ? 'replace' : ev.push ? 'push' : 'divide';
  }
  // Độ khó: số "bit" phải đoán đúng để giữ tối ưu, rủi ro sa ngõ cụt tính nặng gấp đôi, cộng chút theo độ dài
  const score = m.bitsOpt + 2 * m.bitsWin + 0.3 * m.par;
  return { i, def, lv, m, teach, score, colors: lv.colors.length };
});

const has = (x, name) => x.m.mech.includes(name);
const needs = (x, name) => x.m.need.includes(name);
// Giai đoạn của từng khối, theo thứ tự xuất hiện. Một level thuộc giai đoạn đầu tiên mà nó khớp.
const PHASES = [
  [ // Phòng nuôi cấy: chia → đẩy → thay thế → khử 3 (theo kỹ thuật mà lời giải ngắn nhất cần).
    // Giai đoạn "đẩy" chỉ nhận level dễ: level chỉ cần đẩy nhưng dài thì để luyện sau khi đã học thay thế.
    { name: 'chia', match: x => x.teach === 'divide' },
    { name: 'đẩy', match: x => x.teach === 'push' || (!x.teach && !needs(x, 'replace') && !needs(x, 'clear') && x.score < 15) },
    { name: 'thay thế', match: x => x.teach === 'replace' || (!x.teach && !needs(x, 'clear')) },
    { name: 'khử 3', match: () => true },
  ],
  [{ name: 'hộp', match: () => true }],
  [
    { name: 'màng', match: x => !has(x, 'độc') },
    { name: 'độc', match: x => !has(x, 'màng') },
    { name: 'màng + độc', match: () => true },
  ],
  [
    { name: 'ngủ', match: x => !has(x, 'virus') },
    { name: 'virus', match: x => !has(x, 'ngủ') },
    { name: 'ngủ + virus', match: () => true },
  ],
  [
    { name: 'khuẩn', match: x => !has(x, 'virus') && !has(x, 'độc') },
    { name: 'khuẩn + cơ chế khác', match: () => true },
  ],
  [{ name: 'trộn mọi cơ chế', match: () => true }],
  [
    { name: '4 màu', match: x => x.colors <= 4 },
    { name: '5 màu', match: () => true },
  ],
];
if (PHASES.length !== CH.length) throw new Error('Số khối trong PHASES khác CHAPTERS');

// Sắp xen kẽ trong một khối:
// - mở mỗi giai đoạn bằng level "nhập môn" của nó (có tut, hoặc mẹo "… màu cùng lúc");
// - sau đó luôn lấy level dễ nhất đang chơi được (giai đoạn của nó đã mở);
// - sang giai đoạn kế khi: hết level chơi được; hoặc đã luyện đủ K_MIN level và level dễ nhất còn lại khó hơn
//   level nhập môn kế tiếp quá DELTA (level khó còn lại sẽ thành bài ôn trộn cơ chế ở sau);
//   giai đoạn kế không có nhập môn (giai đoạn trộn) thì mở ngay khi đã luyện đủ K_MIN, hoặc (đã luyện ít nhất 1) khi level dễ nhất
//   còn lại đã khó hơn level dễ nhất của giai đoạn trộn.
const K_MIN = 3, DELTA = 8;
const order = [], report = [];
CH.forEach((ch, b) => {
  const phases = PHASES[b];
  const members = info.slice(ch.from, ch.to + 1);
  for (const x of members) {
    x.phase = phases.findIndex(ph => ph.match(x));
    if (x.phase < 0) throw new Error(`Level ${x.i + 1} không thuộc giai đoạn nào của khối ${b + 1}`);
  }
  const intro = phases.map((_, p) => members.find(x => x.phase === p && x.def.tut)
    || members.find(x => x.phase === p && /màu cùng lúc/.test(x.def.tip || '')) || null);
  const placed = new Set(), seq = [];
  const put = x => { placed.add(x); seq.push(x); };
  const easiest = list => list.reduce((a, z) => (z.score < a.score ? z : a));
  let c = 0, k = 0;
  if (intro[0]) put(intro[0]);
  for (;;) {
    const avail = members.filter(x => !placed.has(x) && x.phase <= c);
    const nx = c + 1 < phases.length ? c + 1 : null;
    const nextOwn = nx === null ? [] : members.filter(x => x.phase === nx);
    const low = avail.length ? easiest(avail).score : Infinity;
    const advance = nx !== null && (!avail.length
      || (intro[nx] && k >= K_MIN && low > intro[nx].score + DELTA)
      || (!intro[nx] && (k >= K_MIN || (k >= 1 && nextOwn.length && low > easiest(nextOwn).score)))
      || (!intro[nx] && !nextOwn.length));
    if (advance) {
      c = nx;
      k = 0;
      if (intro[c]) put(intro[c]);
      report.push(`    → mở giai đoạn "${phases[c].name}"`);
      continue;
    }
    if (!avail.length) break;
    put(easiest(avail));
    k++;
  }
  if (seq.length !== members.length) throw new Error(`Khối ${b + 1}: xếp được ${seq.length}/${members.length}`);
  order.push(...seq);
  report.splice(report.length - phases.length + 1, 0, `== Khối ${b + 1} · ${ch.name}: ` +
    seq.map(x => `L${x.i + 1}${x === intro[x.phase] ? '*' : ''}(${x.score.toFixed(0)})`).join(' '));
});

// Chỉnh tay theo yêu cầu, giữ nguyên khi chạy lại: level có rows thứ nhất đứng ngay sau level có rows thứ hai
// (cùng khối). Nhận diện theo nội dung nên không lệch khi số thứ tự đổi.
const AFTER = [
  [['#AA#', 'C.BB', 'CAA.', '.CB#'], ['..A', '...', 'B..']],   // bàn đông mèo đầu tiên sau vuông 3×3 (2026-11-03)
];
const findRows = rows => order.findIndex(x => JSON.stringify(x.def.rows) === JSON.stringify(rows));
for (const [a, b] of AFTER) {
  if (findRows(a) < 0 || findRows(b) < 0) throw new Error(`Chỉnh tay: không thấy level ${JSON.stringify(findRows(a) < 0 ? a : b)}`);
  const [x] = order.splice(findRows(a), 1);
  order.splice(findRows(b) + 1, 0, x);
  report.push(`Chỉnh tay: ${JSON.stringify(a)} đặt sau ${JSON.stringify(b)}`);
}

// Mẹo đi theo "lần đầu": màu hồng, màu tím, bàn đông mèo đầu tiên (≥ 7 mèo)
const defs = order.map(x => ({ ...x.def }));
function moveTip(re, isFirst) {
  const from = defs.findIndex(d => re.test(d.tip || ''));
  const to = defs.findIndex(isFirst);
  if (from < 0 || to < 0 || from === to) return;
  if (defs[to].tip || defs[to].tut) throw new Error(`Không chuyển được mẹo sang level ${to + 1}: đã có mẹo/hướng dẫn`);
  defs[to].tip = defs[from].tip;
  delete defs[from].tip;
  report.push(`Chuyển mẹo "${defs[to].tip.slice(0, 30)}…" từ vị trí ${from + 1} sang ${to + 1}`);
}
const colorsOf = d => ctx.parseLevel(d).colors;
moveTip(/Mèo hồng đến chơi/, d => colorsOf(d).includes('D'));
moveTip(/Mèo tím đến chơi/, d => colorsOf(d).includes('E'));
moveTip(/Ổ to hơn, đông mèo hơn/, d => d.rows.join('').replace(/[^A-Ea-e]/g, '').length >= 7);

const mapping = order.map((x, k) => [x.i + 1, k + 1]).filter(([a, b]) => a !== b);
report.push(`Đổi chỗ ${mapping.length} level. Cũ→mới: ` + mapping.map(([a, b]) => `${a}→${b}`).join(' '));
console.log(report.join('\n'));

if (!process.argv.includes('--write')) return;

// --- Ghi lại mảng LEVELS: mỗi level giữ nguyên dòng chú thích hình khung, đầu mỗi khối một dòng tiêu đề ---
const html = fs.readFileSync(GAME, 'utf8');
const start = html.indexOf('const LEVELS = [\n'), end = html.indexOf('\n];\n', start);
const body = html.slice(start + 'const LEVELS = [\n'.length, end + 1);
// Tách từng level (đếm ngoặc, bỏ qua chuỗi), lấy chú thích cuối dòng cuối của level đó
const comments = [];
let depth = 0, inStr = false, cur = '';
for (const line of body.split('\n')) {
  if (!depth && /^\s*\/\//.test(line)) continue;           // dòng tiêu đề cũ: bỏ, sẽ viết lại
  if (!line.trim()) continue;
  cur += line + '\n';
  for (let k = 0; k < line.length; k++) {
    const ch = line[k];
    if (inStr) { if (ch === '\\') k++; else if (ch === "'") inStr = false; continue; }
    if (ch === "'") inStr = true;
    else if (ch === '[' || ch === '{') depth++;
    else if (ch === ']' || ch === '}') depth--;
    else if (ch === '/' && line[k + 1] === '/' && !depth) break;
  }
  if (!depth) { const m = cur.trimEnd().match(/\/\/\s*(.*)$/); comments.push(m ? m[1] : ''); cur = ''; }
}
if (comments.length !== ctx.LEVELS.length) throw new Error(`Tách được ${comments.length} level, game có ${ctx.LEVELS.length}`);

const q = s => `'${s.replace(/\\/g, '\\\\').replace(/'/g, "\\'")}'`;
const arr = a => `[${a.map(q).join(', ')}]`;
function fmt(d) {
  const keys = Object.keys(d).filter(k => k !== 'tut');
  if (keys.length === 1 && !d.tut) return arr(d.rows);
  const parts = keys.map(k => k === 'rows' || k === 'goal' ? `${k}: ${arr(d[k])}`
    : k === 'oneway' ? `oneway: [${d.oneway.map(([r, c, dir]) => `[${r}, ${c}, ${q(dir)}]`).join(', ')}]` : `${k}: ${q(d[k])}`);
  if (!d.tut) return `{ ${parts.join(', ')} }`;
  return `{ ${parts.join(', ')}, tut: [\n${d.tut.map(([r, c, dir, text]) => `    [${r}, ${c}, ${q(dir)}, ${q(text)}]`).join(',\n')}] }`;
}
let out = '';
CH.forEach((ch, b) => {
  out += `  // ── Khối ${b + 1} · ${ch.name}: ${ch.note} (level ${ch.from + 1}–${ch.to === ctx.LEVELS.length - 1 ? 'cuối' : ch.to + 1})\n`;
  for (let k = ch.from; k <= ch.to; k++) {
    const code = `  ${fmt(defs[k])},`, note = comments[order[k].i];
    const lines = code.split('\n');
    lines[lines.length - 1] = note ? `${lines[lines.length - 1].padEnd(Math.max(lines[lines.length - 1].length + 2, 60))}// ${note}` : lines[lines.length - 1];
    out += lines.join('\n') + '\n';
  }
});
fs.writeFileSync(GAME, html.slice(0, start + 'const LEVELS = [\n'.length) + out + html.slice(end + 1));
fs.writeFileSync(path.join(__dirname, 'jobs', 'order-mapping.json'), JSON.stringify(Object.fromEntries(order.map((x, k) => [x.i + 1, k + 1])), null, 1));
console.log('Đã ghi thứ tự mới vào', path.relative(process.cwd(), GAME), '· bảng cũ→mới: tools/jobs/order-mapping.json');
