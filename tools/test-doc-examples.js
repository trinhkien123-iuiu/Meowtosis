// Mọi ví dụ trong docs/mechanics.md, chạy bằng đúng luật của game. Mã ví dụ (V1, M2...) trùng với mã trong tài liệu:
// sửa luật mà ví dụ nào sai thì test này báo, và phải sửa cả tài liệu.
//   node tools/test-doc-examples.js
const assert = require('assert');
const { loadLogic } = require('./lib');
const ctx = loadLogic();

const DIR = { r: 'right', l: 'left', u: 'up', d: 'down' };
// Bàn dạng chữ như trong tài liệu (khoá của game theo từng hàng)
const rows = (lv, cells) => { const k = ctx.keyOf(cells); return Array.from({ length: lv.h }, (_, r) => k.slice(r * lv.w, r * lv.w + lv.w)); };
// Chơi lần lượt các nước [hàng, cột, 'r'|'l'|'u'|'d']; trả kết quả của từng nước để kiểm tra
function play(def, moves) {
  const lv = ctx.parseLevel(def);
  let cells = lv.init, id = lv.nextId;
  const steps = [];
  for (const [r, c, d] of moves) {
    const res = ctx.swipe(lv, cells, r * lv.w + c, DIR[d], id);
    steps.push(res);
    if (!res) break;
    cells = res.cells;
    id = res.nextId;
  }
  return { lv, cells, steps, last: steps[steps.length - 1], rows: rows(lv, cells), lost: ctx.isLost(lv, cells), won: ctx.isWon(lv, cells) };
}
const viruses = cells => cells.filter(c => c && c.color === 'V').length;
const cases = [];
const ex = (id, text, fn) => cases.push([id, text, fn]);

// --- Vuốt cơ bản ---
ex('S1', 'ô kề trống: bản sao vào ô đó', () => assert.deepStrictEqual(play({ rows: ['A.'] }, [[0, 0, 'r']]).rows, ['AA']));
ex('S2', 'ô kề có mèo: đẩy cả dãy tới ô trống gần nhất', () => assert.deepStrictEqual(play({ rows: ['AB.'] }, [[0, 0, 'r']]).rows, ['AAB']));
ex('S3', 'hết chỗ đẩy: bản sao thay thế con kề', () => {
  const g = play({ rows: ['AB', 'B.'] }, [[0, 0, 'r']]);
  assert.deepStrictEqual(g.rows, ['AA', 'B.']); assert(g.last.replaced);
});
ex('S4', 'không thay thế được con cùng màu', () => assert.strictEqual(play({ rows: ['AA'] }, [[0, 0, 'r']]).last, null));
ex('S5', 'không vuốt ra ngoài thân', () => assert.strictEqual(play({ rows: ['A.'] }, [[0, 0, 'l']]).last, null));

// --- Khử 3 ---
ex('K1', '3 con cùng màu thẳng hàng bị khử', () => assert.deepStrictEqual(play({ rows: ['AA.', 'A..'] }, [[0, 1, 'r']]).rows, ['...', 'A..']));
ex('K2', 'mèo ngủ cũng tính vào dãy 3 (khử trước khi kịp thức)', () =>
  assert.deepStrictEqual(play({ rows: ['A.a', 'B..'] }, [[0, 0, 'r']]).rows, ['...', 'B..']));
ex('K3', 'khử hết một màu là thua', () => assert(play({ rows: ['AA.', 'B..'] }, [[0, 1, 'r']]).lost));

// --- Thắng / thua ---
ex('W1', 'kín ổ nhưng có màu tuyệt chủng vẫn là thua', () => {
  const g = play({ rows: ['AB'] }, [[0, 0, 'r']]);
  assert.deepStrictEqual(g.rows, ['AA']); assert(g.lost && !g.won);
});
ex('W2', 'mèo ngủ vẫn tính là còn sống', () => assert(!ctx.isLost(ctx.parseLevel({ rows: ['A.', '.b'] }), ctx.parseLevel({ rows: ['A.', '.b'] }).init)));

// --- Hộp mục tiêu ---
ex('H1', 'mèo ngủ đúng màu nằm trên hộp vẫn tính là đúng hộp', () => {
  const lv = ctx.parseLevel({ rows: ['Ab'], goal: ['.B'] });
  assert(ctx.targetsDone(lv, lv.init));
});
ex('H2', 'virus nằm trên hộp thì hộp chưa đạt', () => {
  const lv = ctx.parseLevel({ rows: ['A>'], goal: ['.B'] });
  assert(!ctx.targetsDone(lv, lv.init));
});

// --- Màng một chiều ---
ex('M1', 'không vuốt ngược chiều màng', () => assert.strictEqual(play({ rows: ['A.'], oneway: [[0, 1, 'left']] }, [[0, 0, 'r']]).last, null));
ex('M2', 'vuốt xuôi chiều màng được', () => assert.deepStrictEqual(play({ rows: ['.A'], oneway: [[0, 1, 'left']] }, [[0, 1, 'l']]).rows, ['AA']));
ex('M3', 'màng chặn đường đẩy thì thành thay thế', () => {
  const g = play({ rows: ['AB.', 'B..'], oneway: [[0, 2, 'left']] }, [[0, 0, 'r']]);
  assert.deepStrictEqual(g.rows, ['AA.', 'B..']); assert(g.last.replaced);
});
ex('M4', 'mèo ngủ vẫn được đánh thức qua màng (màng chỉ chặn đi lại, không chặn đánh thức)', () =>
  assert.deepStrictEqual(play({ rows: ['.a.', 'A..'], oneway: [[0, 1, 'down']] }, [[1, 0, 'r']]).rows, ['.A.', 'AA.']));

ex('M5', 'màng không ngăn khử 3: dãy 3 nằm vắt qua màng vẫn bị khử', () =>
  assert.deepStrictEqual(play({ rows: ['A.A', 'B..'], oneway: [[0, 1, 'left']] }, [[0, 2, 'l']]).rows, ['...', 'B..']));

// --- Ô độc ---
ex('T1', 'bản sao đặt vào ô độc thì chết ngay (mất một nước)', () => assert.deepStrictEqual(play({ rows: ['A~', 'B.'] }, [[0, 0, 'r']]).rows, ['A.', 'B.']));
ex('T2', 'mèo bị đẩy vào ô độc thì chết', () => assert.deepStrictEqual(play({ rows: ['AB~', 'B..'] }, [[0, 0, 'r']]).rows, ['AA.', 'B..']));
ex('T3', 'ô độc không cần lấp, vẫn thắng', () => assert(play({ rows: ['A.~'] }, [[0, 0, 'r']]).won));

// --- Mèo ngủ ---
ex('N1', 'không vuốt được mèo ngủ', () => assert.strictEqual(play({ rows: ['a.', 'B.'] }, [[0, 0, 'r']]).last, null));
ex('N2', 'mèo ngủ cạnh mèo cùng màu đã thức thì dậy ngay từ đầu', () => assert.deepStrictEqual(rows(ctx.parseLevel({ rows: ['Aa'] }), ctx.parseLevel({ rows: ['Aa'] }).init), ['AA']));
ex('N3', 'cuối lượt, mèo ngủ kề mèo cùng màu thì dậy, lan dây chuyền', () =>
  assert.deepStrictEqual(play({ rows: ['B..', '.bb', '..b'] }, [[0, 0, 'r']]).rows, ['BB.', '.BB', '..B']));
ex('N5', 'mèo ngủ cùng màu thẳng hàng 3 thì bị khử trước khi kịp thức', () =>
  assert.deepStrictEqual(play({ rows: ['A..', '.bb', 'B..'] }, [[2, 0, 'u']]).rows, ['A..', '...', 'B..']));
ex('N4', 'mèo ngủ say như đá: không bị đẩy, không bị bản sao đè lên', () => {
  assert.strictEqual(play({ rows: ['Ab.', 'B..'] }, [[0, 0, 'r']]).last, null);
  assert.strictEqual(play({ rows: ['Ab', 'B.'] }, [[0, 0, 'r']]).last, null);
});
ex('N6', 'dãy bị đẩy chạm mèo ngủ thì dừng như gặp tường: bản sao thay thế con kề', () => {
  const g = play({ rows: ['ABc', 'B..'] }, [[0, 0, 'r']]);
  assert.deepStrictEqual(g.rows, ['AAc', 'B..']); assert(g.last.replaced && g.last.replaced.color === 'B');
});

// --- Virus ---
const LINE = { rows: ['A.B.A', '>....'] };
ex('V1', 'virus lan 1 ô mỗi lượt theo mũi tên; dãy virus chỉ con đầu lan', () => {
  const g1 = play(LINE, [[0, 0, 'r']]);
  assert.deepStrictEqual(g1.rows, ['AAB.A', '>>...']);
  const g2 = play(LINE, [[0, 0, 'r'], [0, 2, 'r']]);
  assert.deepStrictEqual(g2.rows, ['AABBA', '>>>..']);
  assert.strictEqual(viruses(g2.cells), 3);
});
ex('V2', 'hai nguồn virus thì mỗi lượt thêm 2; virus không lan vào virus khác', () => {
  const g = play({ rows: ['AB.C', '>..<'] }, [[0, 1, 'r']]);
  assert.deepStrictEqual(g.rows, ['ABBC', '>><<']);
  const g2 = play({ rows: ['AB.C', '>..<'] }, [[0, 1, 'r'], [0, 3, 'l']]);
  assert.strictEqual(viruses(g2.cells), 4);
});
ex('V3', 'virus kẹt ở mép bàn thì không lan nữa', () => assert.strictEqual(viruses(play({ rows: ['A.', '.>'] }, [[0, 0, 'r']]).cells), 1));
ex('V4', 'diệt con đầu dãy: con phía sau ăn lại ngay trong lượt đó', () => {
  const g = play({ rows: ['AB', '>>'] }, [[0, 1, 'd']]);
  assert(g.last.replaced && g.last.replaced.color === 'V');
  assert.deepStrictEqual(g.rows, ['AB', '>>']);
  assert.strictEqual(g.last.eaten[0].color, 'B');
});
ex('V5', 'diệt từ đuôi lên đầu thì hết virus', () => {
  const g = play({ rows: ['AB', '>>'] }, [[0, 0, 'd'], [0, 1, 'd']]);
  assert.deepStrictEqual(g.rows, ['AB', 'AB']); assert(g.won);
});
ex('V6', 'virus lan vào ô độc thì chết, nhưng con gốc vẫn còn', () => {
  const g = play({ rows: ['A..', '>~.'] }, [[0, 0, 'r']]);
  assert.deepStrictEqual(g.rows, ['AA.', '>..']);
});
ex('V7', 'virus bị màng chặn thì không lan', () => assert.strictEqual(viruses(play({ rows: ['A..', '>..'], oneway: [[1, 1, 'left']] }, [[0, 0, 'r']]).cells), 1));
ex('V8', 'virus ăn con mèo cuối cùng của một màu là thua', () => assert(play({ rows: ['B.', '>A'] }, [[0, 0, 'r']]).lost));
ex('V9', 'virus ăn được vi khuẩn (nhưng virus vẫn còn đó)', () => {
  const g = play({ rows: ['A.', '>G'], kill: 'G' }, [[0, 0, 'r']]);
  assert.deepStrictEqual(g.rows, ['AA', '>>']);
});
ex('V10', 'không vuốt được virus; virus bị đẩy theo dãy như mọi con khác', () => {
  assert.strictEqual(play({ rows: ['>.', 'A.'] }, [[0, 0, 'r']]).last, null);
  assert.deepStrictEqual(play({ rows: ['A>.', '...'] }, [[0, 0, 'r']]).rows, ['AA>', '...']);
});
ex('V11', 'virus không tạo dãy 3 và không tính là ô đã lấp', () => {
  const lv = ctx.parseLevel({ rows: ['>>>', 'AAB'] });
  assert.strictEqual(ctx.findRuns(lv, lv.init).length, 0);
  assert(!ctx.isFull(lv, lv.init));
});

// --- Vi khuẩn ---
ex('G1', 'vi khuẩn vuốt được: nó tự nhân lên', () => assert.deepStrictEqual(play({ rows: ['G.', 'A.'], kill: 'G' }, [[0, 0, 'r']]).rows, ['GG', 'A.']));
ex('G2', '3 vi khuẩn thẳng hàng bị khử', () => assert.deepStrictEqual(play({ rows: ['GG.', 'A..'], kill: 'G' }, [[0, 1, 'r']]).rows, ['...', 'A..']));
ex('G3', 'bản sao thay thế được vi khuẩn', () => assert.deepStrictEqual(play({ rows: ['AG', 'A.'], kill: 'G' }, [[0, 0, 'r']]).rows, ['AA', 'A.']));
ex('G4', 'vi khuẩn chết hết không phải là thua; còn vi khuẩn thì chưa thắng', () => {
  const lv = ctx.parseLevel({ rows: ['AG'], kill: 'G' });
  assert(!ctx.isLost(lv, lv.init) && !ctx.isComplete(lv, lv.init));
});

// --- Booster ---
ex('B1', 'Đột biến: không đổi được virus, không đổi con cuối cùng của một màu', () => {
  const lv = ctx.parseLevel({ rows: ['A>', 'BB'] });
  assert.strictEqual(ctx.mutate(lv, lv.init, 1, 'A'), null);
  assert.strictEqual(ctx.mutate(lv, lv.init, 0, 'B'), null);
  assert(ctx.mutate(lv, lv.init, 2, 'A'));
});
ex('B2', 'Đột biến biến vi khuẩn thành mèo; biến mèo ngủ thành mèo thức', () => {
  const lv = ctx.parseLevel({ rows: ['AG', 'Bb'], kill: 'G' });
  assert.strictEqual(ctx.mutate(lv, lv.init, 1, 'A').cells[1].color, 'A');
  const lv2 = ctx.parseLevel({ rows: ['A.', 'Bc', 'C.'] });
  const r = ctx.mutate(lv2, lv2.init, 3, 'A');
  assert(r && !r.cells[3].sleep);
});
ex('B3', 'Cấy ghép: bản sao mọc ở ô trống bất kỳ, không đẩy gì; không vào ô độc', () => {
  const lv = ctx.parseLevel({ rows: ['A.~', 'B..'] });
  assert.deepStrictEqual(rows(lv, ctx.transplant(lv, lv.init, 0, 5).cells), ['A..', 'B.A']);   // ô độc in ra là "."
  assert.strictEqual(ctx.transplant(lv, lv.init, 0, 2), null);
});
ex('B4', 'sau booster vẫn kết thúc lượt như thường: virus vẫn lan', () => {
  const lv = ctx.parseLevel({ rows: ['A..', '>..'] });
  assert.strictEqual(viruses(ctx.transplant(lv, lv.init, 0, 2).cells), 2);
});

let failed = 0;
for (const [id, text, fn] of cases) {
  try { fn(); } catch (e) { failed++; console.log(`SAI ${id} (${text}): ${e.message.split('\n')[0]}`); }
}
if (failed) { console.log(`test-doc-examples: ${failed}/${cases.length} ví dụ sai`); process.exit(1); }
console.log(`test-doc-examples: all passed (${cases.length} ví dụ)`);
