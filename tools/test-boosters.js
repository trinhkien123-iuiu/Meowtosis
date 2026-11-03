// Booster (đột biến, cấy ghép) và level mục tiêu.
const assert = require('assert');
const { loadLogic } = require('./lib');
const ctx = loadLogic();

// Đột biến: đổi màu, có khử 3 sau khi đổi
let lv = ctx.parseLevel(['AAB.', 'B...']); let c = lv.init;
let r = ctx.mutate(lv, c, 2, 'A'); assert.strictEqual(ctx.keyOf(r.moved), 'AAA.B...'); assert.strictEqual(ctx.keyOf(r.cells), '....B...');
assert.strictEqual(r.removed.length, 3);
// Không đổi tế bào cuối cùng của một màu, không đổi sang cùng màu, không đổi sang màu ngoài level
lv = ctx.parseLevel(['AB.']); assert.strictEqual(ctx.mutate(lv, lv.init, 1, 'A'), null);
lv = ctx.parseLevel(['AAB']); assert.strictEqual(ctx.mutate(lv, lv.init, 0, 'A'), null); assert.strictEqual(ctx.mutate(lv, lv.init, 0, 'C'), null);
assert.strictEqual(ctx.keyOf(ctx.mutate(lv, lv.init, 0, 'B').cells), 'BAB');

// Cấy ghép: bản sao vào ô trống bất kỳ, không vào ô có tế bào / ngoài cơ thể
lv = ctx.parseLevel(['A..', '#.B']); c = lv.init;
assert.strictEqual(ctx.keyOf(ctx.transplant(lv, c, 0, 5 - 1).cells), 'A...AB'.replace('A...AB', 'A..#AB').replace('#', '.'));
assert.strictEqual(ctx.transplant(lv, c, 0, 5), null);
assert.strictEqual(ctx.transplant(lv, c, 0, 3), null);
r = ctx.transplant(lv, ctx.parseLevel(['AA.', '#.B']).init, 0, 2); assert.strictEqual(ctx.keyOf(r.cells), '.....B');   // khử 3 -> đỏ tuyệt chủng
assert(ctx.isLost(ctx.parseLevel(['AA.', '#.B']), r.cells));

// Level mục tiêu: thắng khi mọi vòng đúng màu, không cần đầy; tuyệt chủng vẫn là thua
const T = { rows: ['A..', '..B'], goal: ['..A', '...'] };
lv = ctx.parseLevel(T); assert(lv.hasTarget); assert(!ctx.isComplete(lv, lv.init));
c = ctx.swipe(lv, lv.init, 0, 'right').cells; c = ctx.swipe(lv, c, 1, 'right').cells;     // AAA -> khử -> thua
assert(ctx.isLost(lv, c)); assert(!ctx.isWon(lv, c));
const path = ctx.solve(lv, lv.init); assert(path && path.length > 0);
c = lv.init; for (const [f, d] of path) c = ctx.swipe(lv, c, f, d).cells;
assert(ctx.isWon(lv, c)); assert.strictEqual(c[2].color, 'A');
// Level thường vẫn cần đầy
lv = ctx.parseLevel(['A.']); assert(!ctx.isComplete(lv, lv.init)); assert(!lv.hasTarget);
for (const def of ctx.LEVELS) { const L = ctx.parseLevel(def); assert(ctx.solve(L, L.init, 80000), 'unsolvable ' + JSON.stringify(def)); }
console.log('test-boosters: all passed;', ctx.LEVELS.length, 'levels solvable');
