// Màng một chiều, ô độc, tế bào ngủ, virus, diệt khuẩn.
const assert = require('assert');
const { loadLogic } = require('./lib');
const ctx = loadLogic();
const go = (def, moves) => { const lv = ctx.parseLevel(def); let c = lv.init, r;
  for (const [f, d] of moves) { r = ctx.swipe(lv, c, f, d); assert(r, `move ${f} ${d} null`); c = r.cells; } return [lv, c, r]; };
const K = c => ctx.keyOf(c);

// Màng một chiều: cùng chiều đẩy qua, ngược chiều bị chặn -> thay thế; không chia ngược chiều qua màng
let [lv, c] = go({ rows: ['.BA'], oneway: [[0, 2, 'left']] }, [[2, 'left']]); assert.strictEqual(K(c), 'BAA');
[lv, c] = go({ rows: ['AB.'], oneway: [[0, 2, 'left']] }, [[0, 'right']]); assert.strictEqual(K(c), 'AA.');
lv = ctx.parseLevel({ rows: ['.B.'], oneway: [[0, 2, 'left']] }); assert.strictEqual(ctx.swipe(lv, lv.init, 1, 'right'), null);
assert(ctx.swipe(lv, lv.init, 1, 'left'));

// Ô độc: bị đẩy vào là chết, chia vào cũng chết; lấp kín không tính ô độc
let r; [lv, c, r] = go(['ABB~'], [[0, 'right']]); assert.strictEqual(K(c), 'AAB.'); assert.strictEqual(r.removed.length, 1);
[lv, c] = go(['A.~'], [[0, 'right']]); assert(ctx.isFull(lv, c)); assert(ctx.isWon(lv, c));
[lv, c] = go(['.A~'], [[1, 'right']]); assert.strictEqual(K(c), '.A.');

// Tế bào ngủ: không chia, thức khi có cùng màu thức đứng cạnh, bị đẩy vẫn ngủ, thức dây chuyền
lv = ctx.parseLevel(['A.', '.a']); assert(lv.init[3].sleep); assert.strictEqual(ctx.swipe(lv, lv.init, 3, 'left'), null);
assert.strictEqual(JSON.stringify(ctx.allMoves(lv, lv.init).map(m => m[0])), '[0,0,0,0]');
[lv, c] = go(['A.', '.a'], [[0, 'right']]); assert.strictEqual(K(c), 'AA.A'); assert(!c[3].sleep);
[lv, c] = go(['Bb.', '..a'], [[0, 'down']]); assert.strictEqual(K(c), 'BB.B.a');     // b thức ngay từ đầu (cạnh B); a không có đỏ thức nên vẫn ngủ
[lv, c] = go(['A.aa'], [[0, 'right']]); assert.strictEqual(K(c), '....');   // AA + aa thành 4 đỏ thẳng hàng (tế bào ngủ cũng tính) -> khử hết -> thua
assert(ctx.isLost(lv, c));
lv = ctx.parseLevel(['Aa']); assert(!lv.init[1].sleep);                              // cạnh nhau ngay từ đầu thì thức luôn

// Virus: lan sau mỗi lượt, ăn tế bào, không bị khử 3, chết trên ô độc, bị thay thế khi hết chỗ
[lv, c, r] = go(['>.A'], [[2, 'left']]); assert.strictEqual(K(c), '>>A'); assert.strictEqual(r.eaten.length, 1);
[lv, c] = go(['A>'], [[0, 'right']]); assert.strictEqual(K(c), 'AA'); assert(ctx.isWon(lv, c));
[lv, c, r] = go(['>~.A'], [[3, 'left']]); assert.strictEqual(K(c), '>.AA'); assert.strictEqual(r.poisoned.length, 1);
lv = ctx.parseLevel(['>>>', 'A..']); assert.strictEqual(ctx.findRuns(lv, lv.init).length, 0);
assert(!ctx.isFull(ctx.parseLevel(['A>']), ctx.parseLevel(['A>']).init));
lv = ctx.parseLevel(['A>']); assert.strictEqual(ctx.swipe(lv, lv.init, 1, 'left'), null);  // không vuốt được virus
[lv, c] = go(['v.', 'AB'], [[3, 'up']]); assert(ctx.isLost(lv, c));                      // virus ăn đỏ cuối cùng -> thua
[lv, c] = go({ rows: ['>.A'], oneway: [[0, 1, 'left']] }, [[2, 'left']]); assert.strictEqual(K(c), '>AA');   // màng chặn virus

// Diệt khuẩn: vi khuẩn không phải loài cần giữ; thắng khi hết vi khuẩn VÀ cơ thể lấp kín bằng mèo (luật gộp)
lv = ctx.parseLevel({ rows: ['GG.A'], kill: 'G' }); assert.strictEqual(JSON.stringify([...lv.colors]), '["A"]');
[lv, c] = go({ rows: ['GG.A'], kill: 'G' }, [[1, 'right']]); assert.strictEqual(K(c), '...A'); assert(!ctx.isWon(lv, c));   // hết khuẩn nhưng chưa đầy
lv = ctx.parseLevel({ rows: ['AB'], kill: 'G' }); assert(ctx.isComplete(lv, lv.init));                                      // đầy, không khuẩn
lv = ctx.parseLevel({ rows: ['AG'], kill: 'G' }); assert(ctx.isFull(lv, lv.init)); assert(!ctx.isComplete(lv, lv.init));   // đầy nhưng còn khuẩn
// Vào hộp: hộp đúng màu nhưng chưa đầy thì chưa thắng
lv = ctx.parseLevel({ rows: ['A.', '.B'], goal: ['A.', '..'] }); assert(!ctx.isComplete(lv, lv.init));
lv = ctx.parseLevel({ rows: ['AB', 'BA'], goal: ['A.', '..'] }); assert(ctx.isComplete(lv, lv.init));
lv = ctx.parseLevel({ rows: ['BA', 'AB'], goal: ['A.', '..'] }); assert(!ctx.isComplete(lv, lv.init));                     // đầy nhưng hộp sai màu
[lv, c] = go({ rows: ['GA'], kill: 'G' }, [[0, 'right']]); assert.strictEqual(K(c), 'GG'); assert(ctx.isLost(lv, c));   // hết chỗ: thay thế đỏ cuối cùng -> thua

// Booster: không đột biến virus, không cấy vào ô độc; đột biến vi khuẩn thành loài được
lv = ctx.parseLevel(['A>B.']); assert.strictEqual(ctx.mutate(lv, lv.init, 1, 'A'), null);
lv = ctx.parseLevel(['A.~']); assert.strictEqual(ctx.transplant(lv, lv.init, 0, 2), null);
lv = ctx.parseLevel({ rows: ['GA'], kill: 'G' }); r = ctx.mutate(lv, lv.init, 0, 'A'); assert(r && ctx.isWon(lv, r.cells));

for (const def of ctx.LEVELS) { const L = ctx.parseLevel(def); assert(ctx.solve(L, L.init, 80000), 'unsolvable ' + JSON.stringify(def)); }
console.log('test-mechanics: all passed;', ctx.LEVELS.length, 'levels solvable');
