// Kiểm tra luật "Tàu mèo" trên vài bàn nhỏ viết tay.  node backup-prototypes/tools/test-rail.js
const assert = require('assert');
const Rail = require('../rail-core.js');

const play = (level, action) => {
  const board = Rail.prepare(level);
  return Rail.apply(board, Rail.startState(level), action);
};
let passed = 0;
const test = (name, fn) => { fn(); passed++; console.log('  ✓ ' + name); };

test('toa chạy thẳng tới cản đầu ray', () => {
  const r = play({ rails: ['----'], cars: [{ color: 0, dir: 'R', r: 0, c: 0 }] }, { type: 'car', index: 0 });
  assert.strictEqual(r.next.pos[0], 3);
  assert.strictEqual(r.slide.stop, 'edge');
});

test('toa rẽ qua chỗ cong và quay mặt theo hướng mới', () => {
  // ┌ ở (0,0): toa đi lên từ (2,0) rẽ phải
  const r = play({ rails: ['r--', '|..', '|..'], cars: [{ color: 0, dir: 'U', r: 2, c: 0 }] }, { type: 'car', index: 0 });
  assert.strictEqual(r.next.pos[0], 2);
  assert.strictEqual(r.next.dir[0], 'R');
  assert.deepStrictEqual(r.slide.cells, [6, 3, 0, 1, 2]);
});

test('toa dừng khi gặp toa khác', () => {
  const r = play({ rails: ['-----'], cars: [{ color: 0, dir: 'R', r: 0, c: 0 }, { color: 1, dir: 'L', r: 0, c: 3 }] }, { type: 'car', index: 0 });
  assert.strictEqual(r.next.pos[0], 2);
  assert.strictEqual(r.slide.stop, 'car');
});

test('bị chắn ngay thì không đi', () => {
  const r = play({ rails: ['--'], cars: [{ color: 0, dir: 'R', r: 0, c: 0 }, { color: 1, dir: 'L', r: 0, c: 1 }] }, { type: 'car', index: 0 });
  assert.strictEqual(r, null);
});

test('giao lộ: đi thẳng, không rẽ', () => {
  const r = play({ rails: ['.|.', '-+-', '.|.'], cars: [{ color: 0, dir: 'R', r: 1, c: 0 }] }, { type: 'car', index: 0 });
  assert.strictEqual(r.next.pos[0], 5);
  assert.strictEqual(r.next.dir[0], 'R');
});

test('ghi: vào từ gốc thì theo nhánh đang bật; vào từ nhánh đóng thì bị chặn', () => {
  const level = { rails: ['-S-', '.|.'], switches: [{ r: 0, c: 1, stem: 'L', a: 'R', b: 'D', state: 1 }], cars: [{ color: 0, dir: 'R', r: 0, c: 0 }] };
  const r = play(level, { type: 'car', index: 0 });
  assert.strictEqual(r.next.pos[0], 4);
  assert.strictEqual(r.next.dir[0], 'D');
  const closed = play({ ...level, cars: [{ color: 0, dir: 'L', r: 0, c: 2 }] }, { type: 'car', index: 0 });
  assert.strictEqual(closed, null);
});

test('gạt ghi đổi nhánh; có toa đứng trên ghi thì không gạt được', () => {
  const level = { rails: ['-S-', '.|.'], switches: [{ r: 0, c: 1, stem: 'L', a: 'R', b: 'D', state: 0 }], cars: [{ color: 0, dir: 'R', r: 0, c: 0 }] };
  assert.strictEqual(play(level, { type: 'switch', index: 0 }).next.sw[0], 1);
  const blocked = play({ ...level, cars: [{ color: 0, dir: 'R', r: 0, c: 1 }] }, { type: 'switch', index: 0 });
  assert.strictEqual(blocked, null);
});

test('ba toa cùng màu nối đuôi qua chỗ cong thì rời bến', () => {
  // Toa 2 chạy sang trái, vào ┌ ở (0,0) thì rẽ xuống, gặp toa 0 ở (1,0) nên dừng ở (0,0): dãy (0,0)-(1,0)-(2,0) nối qua chỗ cong.
  const level = { rails: ['r---', '|...', '|...'], cars: [{ color: 0, dir: 'U', r: 1, c: 0 }, { color: 0, dir: 'U', r: 2, c: 0 }, { color: 0, dir: 'L', r: 0, c: 3 }] };
  const r = play(level, { type: 'car', index: 2 });
  assert.strictEqual(r.slide.cells[r.slide.cells.length - 1], 0);
  assert.strictEqual(r.slide.dir, 'D');
  assert.deepStrictEqual(r.cleared.sort(), [0, 1, 2]);
});

test('hai toa sát nhau trên hai ray khác nhau thì không móc', () => {
  const level = { rails: ['---', '---', '---'], cars: [{ color: 0, dir: 'R', r: 0, c: 0 }, { color: 0, dir: 'R', r: 1, c: 0 }, { color: 0, dir: 'L', r: 2, c: 2 }] };
  const r = play(level, { type: 'car', index: 2 });
  assert.strictEqual(r.next.pos[2], 6);
  assert.strictEqual(r.cleared.length, 0);
});

test('đoàn tàu ở thẳng hàng trên một ray thì rời bến', () => {
  const level = { rails: ['-----'], cars: [{ color: 0, dir: 'R', r: 0, c: 0 }, { color: 0, dir: 'R', r: 0, c: 3 }, { color: 0, dir: 'R', r: 0, c: 4 }] };
  const r = play(level, { type: 'car', index: 0 });
  assert.deepStrictEqual(r.cleared.sort(), [0, 1, 2]);
});

test('bộ giải tìm số nước ít nhất và gợi ý', () => {
  const level = { rails: ['-----'], cars: [{ color: 0, dir: 'R', r: 0, c: 0 }, { color: 0, dir: 'R', r: 0, c: 2 }, { color: 0, dir: 'R', r: 0, c: 4 }] };
  const board = Rail.prepare(level);
  const info = Rail.solve(board, Rail.startState(level));
  assert.strictEqual(info.minMoves, 2);
});

test('toa hàng không bao giờ rời bến, không cần dọn để thắng', () => {
  const level = { rails: ['------'], cars: [{ color: 0, dir: 'R', r: 0, c: 0 }, { color: 0, dir: 'R', r: 0, c: 2 }, { color: 0, dir: 'R', r: 0, c: 3 }, { color: -1, dir: 'L', r: 0, c: 5 }] };
  const board = Rail.prepare(level);
  const r = Rail.apply(board, Rail.startState(level), { type: 'car', index: 0 });
  assert.deepStrictEqual(r.cleared.sort(), [0, 1, 2]);
  assert.ok(Rail.isWon(board, r.next));
});

const gateLevel = (open) => ({ rails: ['--G--'], gates: [{ r: 0, c: 2, axis: 'H', open }], cars: [{ color: 0, dir: 'R', r: 0, c: 0 }] });

test('đèn đóng thì toa dừng trước đèn, đèn mở thì chạy qua', () => {
  const closed = play(gateLevel(0), { type: 'car', index: 0 });
  assert.strictEqual(closed.next.pos[0], 1);
  const open = play(gateLevel(1), { type: 'car', index: 0 });
  assert.strictEqual(open.next.pos[0], 4);
});

test('mỗi nước đi làm đèn đổi trạng thái', () => {
  const r = play(gateLevel(0), { type: 'car', index: 0 });
  assert.strictEqual(r.next.sw[0], 1);
});

test('Chờ chỉ đổi đèn, tốn một nước; không có đèn thì không chờ được', () => {
  const r = play(gateLevel(0), { type: 'wait' });
  assert.strictEqual(r.next.sw[0], 1);
  assert.deepStrictEqual(r.next.pos, [0]);
  assert.strictEqual(play({ rails: ['---'], cars: [{ color: 0, dir: 'R', r: 0, c: 0 }] }, { type: 'wait' }), null);
});

test('đoàn tàu được xét trước khi đèn đổi', () => {
  const level = { rails: ['-G---'], gates: [{ r: 0, c: 1, axis: 'H', open: 0 }],
    cars: [{ color: 0, dir: 'R', r: 0, c: 0 }, { color: 0, dir: 'R', r: 0, c: 2 }, { color: 0, dir: 'L', r: 0, c: 4 }] };
  // toa 2 (ô 4) chạy trái, dừng sát toa ở ô 2: ba toa cùng màu ở ô 0, 2, 3? không liền vì đèn đóng chắn giữa 0 và 2
  const r = play(level, { type: 'car', index: 2 });
  assert.strictEqual(r.cleared.length, 0);
});

const tunnelLevel = (extra) => ({ rails: ['--..--'], tunnels: [[0, 1, 'R', 0, 4, 'L']], cars: [{ color: 0, dir: 'R', r: 0, c: 0 }].concat(extra || []) });

test('hầm: toa chui vào cửa này, ra cửa kia và chạy tiếp', () => {
  const r = play(tunnelLevel(), { type: 'car', index: 0 });
  assert.strictEqual(r.next.pos[0], 5);
  assert.strictEqual(r.slide.hops.filter(Boolean).length, 1);
});

test('hầm: không có hầm thì toa dừng ở cuối ray', () => {
  const board = Rail.prepare(tunnelLevel());
  const r = Rail.apply(board, Rail.startState(tunnelLevel()), { type: 'car', index: 0 }, { noTunnel: true });
  assert.strictEqual(r.next.pos[0], 1);
});

test('hầm: toa đầu kia chắn thì dừng ngay ở cửa', () => {
  const r = play(tunnelLevel([{ color: 1, dir: 'L', r: 0, c: 4 }]), { type: 'car', index: 0 });
  assert.strictEqual(r.next.pos[0], 1);
});

test('hầm: hai toa hai đầu hầm không nối thành đoàn', () => {
  const level = { rails: ['--..--'], tunnels: [[0, 1, 'R', 0, 4, 'L']],
    cars: [{ color: 0, dir: 'R', r: 0, c: 0 }, { color: 0, dir: 'R', r: 0, c: 1 }, { color: 0, dir: 'L', r: 0, c: 4 }] };
  const board = Rail.prepare(level);
  const state = Rail.startState(level);
  const trains = Rail.trains(board, state, Rail.occupancy(board, state));
  assert.strictEqual(trains.length, 0);
});

test('hầm: dữ liệu sai bị từ chối', () => {
  assert.ok(Rail.validate({ rails: ['--..--'], tunnels: [[0, 1, 'U', 0, 4, 'L']], cars: [] }));
});

console.log(`${passed} phép thử đều qua`);
