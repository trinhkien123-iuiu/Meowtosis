// Luật thua: một màu tuyệt chủng (do bị khử 3 hoặc bị thay thế) là thua, kể cả khi bàn đã đầy.
const assert = require('assert');
const { loadLogic } = require('./lib');
const ctx = loadLogic();
const play = (rows, moves) => { const lv = ctx.parseLevel(rows); let c = lv.init; for (const [f, d] of moves) c = ctx.swipe(lv, c, f, d).cells; return [lv, c]; };

// Khử 3 đỏ cuối cùng -> đỏ tuyệt chủng
let [lv, c] = play(['A..', '..B'], [[0, 'right'], [1, 'right']]);
assert.strictEqual(ctx.keyOf(c), '.....B'); assert.deepStrictEqual([...ctx.extinct(lv, c)], ['A']); assert(ctx.isLost(lv, c));
assert.strictEqual(ctx.solve(lv, c), null);

// Thay thế đỏ cuối cùng, bàn chưa đầy -> thua
[lv, c] = play(['AB.'], [[1, 'left']]);
assert.strictEqual(ctx.keyOf(c), 'BB.'); assert(ctx.isLost(lv, c)); assert(!ctx.isWon(lv, c));

// Bàn đầy nhưng một màu tuyệt chủng -> thua, không phải thắng
[lv, c] = play(['AB'], [[1, 'left']]);
assert.strictEqual(ctx.keyOf(c), 'BB'); assert(ctx.isLost(lv, c)); assert(!ctx.isWon(lv, c));

// Khử 3 nhưng còn đỏ khác -> chưa thua
[lv, c] = play(['A...', 'A...'], [[0, 'right'], [1, 'right']]);
assert.strictEqual(ctx.keyOf(c), '....A...'); assert(!ctx.isLost(lv, c));

// Thắng thường
[lv, c] = play(['AB.'], [[0, 'right']]);
assert.strictEqual(ctx.keyOf(c), 'AAB'); assert(ctx.isWon(lv, c));
console.log('test-lose: all passed');
