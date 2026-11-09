// Sinh các màn "Tàu mèo", ghi ra ../rail-levels.js.   node backup-prototypes/tools/gen-rail.js [số màn bắt đầu]
//
// Sơ đồ đường ray do người thiết kế vẽ tay (SPECS bên dưới), bộ sinh chỉ đặt toa: chọn ô, màu, hướng, rồi leo đồi tới khi
// thắng được. Một màn chỉ được giữ khi:
//   - thắng được, số nước ít nhất nằm trong khoảng mong muốn;
//   - mỗi cơ chế trong `need` thật sự cần: gỡ cơ chế đi thì không thắng được
//       cross  : cấm đi xuyên giao lộ          curve : cấm đi qua chỗ cong
//       switch : cấm gạt ghi                    tunnel: cấm chui hầm (hai bên hầm không thông nhau thì thua)
//       gate   : đèn tín hiệu phải làm tốn thêm chuyến (nếu đèn luôn mở thì đi được ít nước hơn)
//       cargo : toa hàng phải được dời đi (đóng băng toa hàng thì thua)
//                                                       hoặc là chỗ chặn có ích (bỏ toa hàng đi thì thua)
//   - tỉ lệ trạng thái ngõ cụt gần mức mong muốn (để người chơi phải tính, nhưng không quá nản).
// Giới hạn số chuyến = số nước ít nhất + `slack`.
const fs = require('fs');
const path = require('path');
const Rail = require('../rail-core.js');

function makeRandom(seed) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

// block: khối cơ chế (hiện thẻ cơ chế mới ở màn đầu khối). tip: lời dẫn đầu màn. guide: số nước đầu có bàn tay chỉ.
const SPECS = [
  // ── Khối 1 · Đường ray
  { block: 'rail', rails: ['.....', '-----', '.....'], fixed: [
      { color: 0, dir: 'R', r: 1, c: 0 }, { color: 0, dir: 'L', r: 1, c: 3 }, { color: 0, dir: 'L', r: 1, c: 4 }],
    slack: 2, guide: 1, tip: 'Chạm toa để nó chạy theo mũi tên. 3 toa cùng màu nối đuôi nhau thì móc lại và rời bến!' },
  { block: 'rail', rails: ['........', '--------', '........'], groups: [3, 3], par: [3, 5], trap: 0.2, slack: 2,
    tip: 'Toa trên cùng một ray không vượt qua nhau được. Chạm theo thứ tự nào?' },
  // ── Khối 2 · Giao lộ
  { block: 'cross', rails: ['..|..', '..|..', '--+--', '..|..', '..|..'], groups: [3, 3], need: ['cross'], par: [3, 6], trap: 0.25, slack: 2, guide: 1,
    tip: 'Giao lộ: toa đi thẳng qua, không rẽ. Dùng nó để đổi toa sang tuyến khác!' },
  { block: 'cross', rails: ['.|..|.', '-+--+-', '.|..|.', '.|..|.', '-+--+-', '.|..|.'], groups: [3, 3, 3], need: ['cross'], par: [5, 9], trap: 0.35, slack: 2,
    tip: 'Bốn giao lộ. Tính trước toa nào chặn toa nào nhé.' },
  // ── Khối 3 · Ray cong
  { block: 'curve', rails: ['r----', '|....', '|....', '|....'], groups: [3], need: ['curve'], par: [2, 4], trap: 0.1, slack: 2, guide: 1,
    tip: 'Ray cong: toa rẽ theo ray và quay mặt theo hướng mới. Đoàn tàu nối qua chỗ cong cũng được!' },
  { block: 'curve', rails: ['..|...', 'r-+---', '|.|...', 'L-+---', '..|...'], groups: [3, 3], need: ['curve'], par: [4, 8], trap: 0.3, slack: 2,
    tip: 'Vòng chữ U: toa chạy sang trái sẽ quay đầu về tuyến dưới.' },
  { block: 'curve', rails: ['.r--7.', '.|..|.', '-+--+-', '.|..|.', '.|..L-'], groups: [3, 3, 3], need: ['curve'], par: [6, 10], trap: 0.4, slack: 2 },
  // ── Khối 4 · Toa hàng
  { block: 'cargo', rails: ['..|..', '..|..', '--+--', '..|..', '..|..'], groups: [3], cargo: 1, need: ['cargo'], par: [2, 5], trap: 0.2, slack: 2, guide: 1,
    tip: 'Toa hàng không chở mèo, không bao giờ rời bến. Dời nó đi, hoặc dùng nó làm chỗ chặn!' },
  { block: 'cargo', rails: ['..|...', 'r-+---', '|.|...', 'L-+---', '..|...'], groups: [3, 3], cargo: 1, need: ['cargo'], par: [5, 9], trap: 0.35, slack: 2 },
  { block: 'cargo', rails: ['.|..|.', '-+--+-', '.|..|.', '.|..|.', '-+--+-', '.|..|.'], groups: [3, 3], cargo: 2, need: ['cargo'], par: [5, 9], trap: 0.35, slack: 2 },
  // ── Khối 5 · Bẻ ghi
  { block: 'switch', rails: ['.....', '--S--', '..|..', '..|..'], switches: [{ r: 1, c: 2, stem: 'L', a: 'R', b: 'D', state: 0 }],
    groups: [3], need: ['switch'], par: [2, 4], trap: 0.1, slack: 2, guide: 1,
    tip: 'Bẻ ghi: chạm cần gạt để đổi nhánh (tốn 1 chuyến). Toa đi vào nhánh đang đóng sẽ bị chặn.' },
  { block: 'switch', rails: ['-S----', '.|....', '.|....', '-S----'],
    switches: [{ r: 0, c: 1, stem: 'R', a: 'L', b: 'D', state: 0 }, { r: 3, c: 1, stem: 'R', a: 'L', b: 'U', state: 0 }],
    groups: [3, 3], need: ['switch'], par: [4, 8], trap: 0.3, slack: 2 },
  { block: 'switch', rails: ['---S--', '...|..', 'r--+--', '|..|..', 'L--J..'], switches: [{ r: 0, c: 3, stem: 'L', a: 'R', b: 'D', state: 0 }],
    groups: [3, 3], need: ['switch'], par: [5, 9], trap: 0.35, slack: 2 },
  // ── Khối 6 · Tổng hợp
  { block: 'mix', rails: ['---S--', '...|..', 'r--+--', '|..|..', 'L--J..'], switches: [{ r: 0, c: 3, stem: 'L', a: 'R', b: 'D', state: 1 }],
    groups: [3, 3], cargo: 1, need: ['switch', 'cargo'], par: [6, 10], trap: 0.4, slack: 1 },
  { block: 'mix', rails: ['.r--7.', '.|..|.', '-+--S.', '.|..|.', '.|..L-', '.|....'], switches: [{ r: 2, c: 4, stem: 'U', a: 'D', b: 'L', state: 0 }],
    groups: [3, 3, 3], cargo: 1, need: ['switch', 'curve'], par: [7, 12], trap: 0.45, slack: 1 },
  // ── Khối 7 · Đèn tín hiệu: mỗi nước đi làm mọi đèn đổi màu; có nút Chờ để đổi đèn mà không chạy toa
  { block: 'gate', rails: ['.........', '----G----', '.........'], gates: [{ r: 1, c: 4, axis: 'H', open: 0 }],
    groups: [3, 3], need: ['gate'], par: [3, 7], trap: 0.2, slack: 2, guide: 1,
    tip: 'Đèn tín hiệu: mỗi chuyến đèn đổi màu. Đèn đỏ thì toa không qua được. Đi nước khác hoặc bấm Chờ để đổi đèn!' },
  { block: 'gate', rails: ['..|...', '..|...', '--+---', '..G...', '..|...', '..|...'], gates: [{ r: 3, c: 2, axis: 'V', open: 1 }],
    groups: [3, 3], need: ['gate'], par: [4, 9], trap: 0.3, slack: 2,
    tip: 'Đèn nằm trên tuyến dọc. Canh đúng nhịp: đèn đổi sau MỖI chuyến, kể cả chuyến của toa khác.' },
  { block: 'gate', rails: ['r-G-7', '|...|', '|...|', 'L-G-J'], gates: [{ r: 0, c: 2, axis: 'H', open: 0 }, { r: 3, c: 2, axis: 'H', open: 1 }],
    groups: [3, 3, 3], need: ['gate'], par: [5, 11], trap: 0.35, slack: 2,
    tip: 'Hai đèn lệch pha: một đỏ một xanh. Vòng ray này chỉ có một cửa mở mỗi lúc.' },
  // ── Khối 8 · Hầm xuyên núi: cửa hầm nối hai đầu ray ở xa nhau
  { block: 'tunnel', rails: ['.........', '---..----', '.........'], tunnels: [[1, 2, 'R', 1, 5, 'L']],
    groups: [3], need: ['tunnel'], par: [2, 5], trap: 0.1, slack: 2, guide: 1,
    tip: 'Hầm: toa chạy ra cửa này sẽ chui vào cửa kia và chạy tiếp. Mỗi đầu hầm là một đoạn ray riêng: toa chỉ móc với toa sát bên cạnh!' },
  { block: 'tunnel', rails: ['|....|', '|....|', '+-..-+', '|....|', '|....|'], tunnels: [[2, 1, 'R', 2, 4, 'L']],
    groups: [3, 3], need: ['tunnel'], par: [4, 9], trap: 0.3, slack: 2,
    tip: 'Hai cột ray chỉ thông nhau qua hầm.' },
  { block: 'tunnel', rails: ['|....|', '+-..-+', '|....|', '+-..-+', '|....|'], tunnels: [[1, 1, 'R', 1, 4, 'L'], [3, 1, 'R', 3, 4, 'L']],
    groups: [3, 3, 3], need: ['tunnel'], par: [6, 12], trap: 0.4, slack: 2,
    tip: 'Hai hầm song song: chọn hầm nào để toa ra đúng chỗ cần?' },
  // ── Khối 9 · Phối hợp
  { block: 'mix', rails: ['|.....|', '|.....|', '+-G..-+', '|.....|', '|.....|'], gates: [{ r: 2, c: 2, axis: 'H', open: 0 }], tunnels: [[2, 2, 'R', 2, 5, 'L']],
    groups: [3, 3], need: ['tunnel', 'gate'], par: [5, 11], trap: 0.4, slack: 2,
    tip: 'Đèn đứng ngay cửa hầm: phải canh đèn mới chui được.' },
  { block: 'mix', rails: ['---S---', '...|...', '...G...', '...|...'], switches: [{ r: 0, c: 3, stem: 'L', a: 'R', b: 'D', state: 0 }],
    gates: [{ r: 2, c: 3, axis: 'V', open: 0 }], groups: [3, 3], need: ['switch', 'gate'], par: [5, 11], trap: 0.4, slack: 2 },
  { block: 'mix', rails: ['.|..|.', '-+--+-', '.G..|.', '.|..|.', '-+--+-', '.|..|.'], gates: [{ r: 2, c: 1, axis: 'V', open: 0 }],
    groups: [3, 3], need: ['gate'], par: [5, 12], trap: 0.5, slack: 2 },
  { block: 'mix', rails: ['|....|', '+-..-+', '|....|', 'S-...|', '|....|'],
    switches: [{ r: 3, c: 0, stem: 'U', a: 'D', b: 'R', state: 0 }], tunnels: [[1, 1, 'R', 1, 4, 'L']],
    groups: [3, 3, 3], need: ['tunnel'], par: [6, 13], trap: 0.45, slack: 2 },
  { block: 'mix', rails: ['r-G-7', '|...|', '|...|', '|...|', 'L-G-J'], gates: [{ r: 0, c: 2, axis: 'H', open: 1 }, { r: 4, c: 2, axis: 'H', open: 0 }],
    groups: [3, 3], cargo: 1, need: ['gate'], par: [5, 12], trap: 0.5, slack: 2 },
  { block: 'mix', rails: ['|.....|', '+-G..-+', '|.....|', 'S-....|', '|.....|'], switches: [{ r: 3, c: 0, stem: 'U', a: 'D', b: 'R', state: 0 }],
    gates: [{ r: 1, c: 2, axis: 'H', open: 0 }], tunnels: [[1, 2, 'R', 1, 5, 'L']],
    groups: [3, 3], cargo: 1, need: ['tunnel'], par: [6, 13], trap: 0.45, slack: 2 },
  { block: 'mix', rails: ['|....|', '+-..-+', '|....|', '|....|'], tunnels: [[1, 1, 'R', 1, 4, 'L']],
    groups: [3, 3], cargo: 1, need: ['tunnel'], par: [5, 11], trap: 0.5, slack: 2 },
  { block: 'mix', rails: ['r-G-7', '|...|', 'L---J'], gates: [{ r: 0, c: 2, axis: 'H', open: 0 }],
    groups: [3, 3], need: ['gate'], par: [4, 10], trap: 0.5, slack: 2 },
  { block: 'mix', rails: ['|.....|', '+-G..-+', '|.....|', '+-..-G+', '|.....|'], gates: [{ r: 1, c: 2, axis: 'H', open: 0 }, { r: 3, c: 5, axis: 'H', open: 1 }],
    tunnels: [[1, 2, 'R', 1, 5, 'L'], [3, 1, 'R', 3, 4, 'L']], groups: [3, 3], cargo: 1, need: ['tunnel'], par: [6, 14], trap: 0.5, slack: 2 },
];

const TIME_PER_LEVEL_MS = Number(process.env.RAIL_GEN_MS || 30000);
const CAP = 60000;

const shuffled = (list, random) => {
  const copy = list.slice();
  for (let i = copy.length - 1; i > 0; i--) {
    const j = Math.floor(random() * (i + 1));
    [copy[i], copy[j]] = [copy[j], copy[i]];
  }
  return copy;
};

function carSpots(spec) {
  const board = Rail.prepare({ rails: spec.rails, switches: spec.switches || [], gates: spec.gates || [], tunnels: spec.tunnels || [], cars: [] });
  const spots = [];
  for (let cell = 0; cell < board.W * board.H; cell++) {
    if (!Rail.isRail(board, cell) || board.kind[cell] === 'S' || board.kind[cell] === 'G') continue;
    const dirs = [...new Set(Rail.pairs(board, cell, []).flat())];
    spots.push({ r: Math.floor(cell / board.W), c: cell % board.W, dirs });
  }
  return spots;
}

function candidate(spec, spots, random) {
  const colors = [];
  (spec.groups || []).forEach((size, color) => { for (let i = 0; i < size; i++) colors.push(color); });
  for (let i = 0; i < (spec.cargo || 0); i++) colors.push(Rail.CARGO);
  if (colors.length > spots.length - 2) return null;
  const chosen = shuffled(spots, random).slice(0, colors.length);
  return {
    rails: spec.rails,
    switches: (spec.switches || []).map((s) => ({ ...s })),
    gates: (spec.gates || []).map((g) => ({ ...g })),
    tunnels: spec.tunnels || [],
    cars: colors.map((color, index) => ({ color, dir: chosen[index].dirs[Math.floor(random() * chosen[index].dirs.length)], r: chosen[index].r, c: chosen[index].c })),
  };
}

function mutate(level, spots, random) {
  const copy = JSON.parse(JSON.stringify(level));
  const roll = random();
  if (roll < 0.15 && copy.switches.length) {
    const s = copy.switches[Math.floor(random() * copy.switches.length)];
    s.state ^= 1;
    return copy;
  }
  if (roll < 0.22 && copy.gates && copy.gates.length) {
    const g = copy.gates[Math.floor(random() * copy.gates.length)];
    g.open ^= 1;
    return copy;
  }
  const car = copy.cars[Math.floor(random() * copy.cars.length)];
  const spotOf = (r, c) => spots.find((s) => s.r === r && s.c === c);
  if (roll < 0.55) {
    const dirs = spotOf(car.r, car.c).dirs;
    car.dir = dirs[Math.floor(random() * dirs.length)];
  } else if (roll < 0.8) {
    const taken = new Set(copy.cars.map((other) => other.r + ',' + other.c));
    const free = spots.filter((s) => !taken.has(s.r + ',' + s.c));
    if (free.length) {
      const spot = free[Math.floor(random() * free.length)];
      car.r = spot.r;
      car.c = spot.c;
      car.dir = spot.dirs[Math.floor(random() * spot.dirs.length)];
    }
  } else {
    // Đổi chỗ hai toa khác màu.
    const other = copy.cars[Math.floor(random() * copy.cars.length)];
    [car.r, car.c, car.dir, other.r, other.c, other.dir] = [other.r, other.c, other.dir, car.r, car.c, car.dir];
    if (!spotOf(car.r, car.c).dirs.includes(car.dir)) car.dir = spotOf(car.r, car.c).dirs[0];
    if (!spotOf(other.r, other.c).dirs.includes(other.dir)) other.dir = spotOf(other.r, other.c).dirs[0];
  }
  return copy;
}

function evaluate(level) {
  if (Rail.validate(level)) return null;
  const board = Rail.prepare(level);
  const start = Rail.startState(level);
  // Lúc đầu không được có sẵn đoàn tàu.
  if (Rail.trains(board, start, Rail.occupancy(board, start)).length) return null;
  const info = Rail.solve(board, start, null, CAP, true);
  return info ? { info, board, start } : null;
}

// Gỡ cơ chế đi mà vẫn thắng thì cơ chế chỉ là trang trí.
function mechanicsNeeded(spec, board, start, minMoves) {
  const wins = (opts) => {
    const result = Rail.solve(board, start, opts, CAP, false);
    return result === null || result.minMoves !== Infinity; // vượt giới hạn thì coi như còn thắng được (không chắc là bắt buộc)
  };
  const proof = {};
  for (const need of spec.need || []) {
    if (need === 'cross') proof.cross = !wins({ noCross: true });
    if (need === 'curve') proof.curve = !wins({ noCurve: true });
    if (need === 'switch') proof.switch = !wins({ noSwitch: true });
    if (need === 'tunnel') proof.tunnel = !wins({ noTunnel: true });
    if (need === 'gate') {
      // Đèn luôn mở mà đi được ít chuyến hơn thì đèn thật sự cản trở, phải tính nhịp.
      const open = Rail.solve(board, start, { gatesOpen: true }, CAP, false);
      proof.gate = open && open.minMoves !== Infinity && open.minMoves < minMoves ? `+${minMoves - open.minMoves} chuyến` : false;
    }
    if (need === 'cargo') {
      const mustMove = !wins({ frozenCargo: true });
      const usefulStop = !wins({ noCargo: true });
      proof.cargo = mustMove ? 'phải dời' : usefulStop ? 'làm chỗ chặn' : false;
    }
  }
  return proof;
}

function generate(spec, random) {
  if (spec.fixed) {
    const level = { rails: spec.rails, switches: spec.switches || [], gates: spec.gates || [], tunnels: spec.tunnels || [], cars: spec.fixed };
    const result = evaluate(level);
    if (!result || result.info.minMoves === Infinity) throw new Error('màn viết tay không thắng được');
    return { level, info: result.info, proof: {}, trap: result.info.trapped / result.info.states };
  }
  const spots = carSpots(spec);
  let best = null;
  const started = Date.now();
  while (Date.now() - started < TIME_PER_LEVEL_MS) {
    let level = candidate(spec, spots, random);
    if (!level) throw new Error('không đủ chỗ đặt toa');
    let result = evaluate(level);
    for (let step = 0; step < 250 && Date.now() - started < TIME_PER_LEVEL_MS; step++) {
      if (result && result.info.minMoves !== Infinity) break;
      const tried = mutate(level, spots, random);
      const triedResult = evaluate(tried);
      if (triedResult && (!result || triedResult.info.mostCleared >= result.info.mostCleared)) { level = tried; result = triedResult; }
    }
    if (!result || result.info.minMoves === Infinity) continue;
    const { info } = result;
    const [low, high] = spec.par;
    if (info.minMoves < low || info.minMoves > high) continue;
    const trap = info.trapped / info.states;
    if (trap > 0.8) continue;
    const proof = mechanicsNeeded(spec, result.board, result.start, info.minMoves);
    if (Object.values(proof).some((ok) => !ok)) continue;
    const score = Math.abs(trap - spec.trap) - info.minMoves * 0.02 - Math.log(info.states) * 0.02;
    if (!best || score < best.score) best = { level, info, proof, trap, score };
  }
  return best;
}

// Sinh lại đúng một khung (RAIL_ONLY=số thứ tự trong SPECS, tính từ 1) rồi thay vào màn cùng khung trong rail-levels.js.
if (process.env.RAIL_ONLY) {
  const spec = SPECS[Number(process.env.RAIL_ONLY) - 1];
  const best = generate(spec, makeRandom(Date.now() & 0xffffff));
  if (!best) { console.log('không sinh được'); process.exit(1); }
  const level = best.level;
  level.block = spec.block;
  level.par = best.info.minMoves;
  level.limit = best.info.minMoves + spec.slack;
  if (spec.tip) level.tip = spec.tip;
  if (spec.guide) level.guide = spec.guide;
  level.stats = { states: best.info.states, trap: Math.round(best.trap * 100), proof: best.proof };
  const file = path.join(__dirname, '..', 'rail-levels.js');
  const text = fs.readFileSync(file, 'utf8');
  const all = JSON.parse(text.slice(text.indexOf('['), text.lastIndexOf(']') + 1));
  const key = (l) => JSON.stringify([l.rails, l.tunnels || [], l.gates || [], (l.switches || []).length]);
  const at = all.findIndex((l) => key(l) === key(level));
  if (at < 0) { console.log('không tìm thấy màn cùng khung để thay'); process.exit(1); }
  all[at] = level;
  fs.writeFileSync(file, 'window.RAIL_LEVELS = ' + JSON.stringify(all) + ';' + String.fromCharCode(10));
  console.log(`đã thay màn ${at + 1}: ${level.cars.length} toa, tối ưu ${level.par}, giới hạn ${level.limit}, ${best.info.states} trạng thái, bắt buộc ${JSON.stringify(best.proof)}`);
  process.exit(0);
}

const firstIndex = Number(process.argv[2] || 1) - 1;
const outFile = path.join(__dirname, '..', 'rail-levels.js');
let existing = [];
if (firstIndex > 0 && fs.existsSync(outFile)) {
  const text = fs.readFileSync(outFile, 'utf8');
  existing = JSON.parse(text.slice(text.indexOf('['), text.lastIndexOf(']') + 1));
}
const random = makeRandom(20261109 + firstIndex);
const levels = existing.slice(0, firstIndex);
for (let index = firstIndex; index < SPECS.length; index++) {
  const spec = SPECS[index];
  const best = generate(spec, random);
  if (!best) { console.log(`màn ${index + 1}: KHÔNG sinh được (${spec.block})`); continue; }
  const level = best.level;
  level.block = spec.block;
  level.par = best.info.minMoves;
  level.limit = best.info.minMoves + spec.slack;
  if (spec.tip) level.tip = spec.tip;
  if (spec.guide) level.guide = spec.guide;
  level.stats = { states: best.info.states, trap: Math.round(best.trap * 100), proof: best.proof };
  levels.push(level);
  console.log(`màn ${index + 1} [${spec.block}]: ${level.cars.length} toa, tối ưu ${level.par}, giới hạn ${level.limit}, ${best.info.states} trạng thái, ngõ cụt ${level.stats.trap}%, bắt buộc ${JSON.stringify(best.proof)}`);
  fs.writeFileSync(outFile, 'window.RAIL_LEVELS = ' + JSON.stringify(levels) + ';' + String.fromCharCode(10));
}
console.log(`đã ghi ${levels.length} màn vào rail-levels.js`);
