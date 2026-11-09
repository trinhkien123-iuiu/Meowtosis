// Sinh các màn "Mèo trượt" có lời giải, độ khó tăng dần, ghi ra ../slide-levels.js.
//   node backup-prototypes/tools/gen-slide.js
const fs = require('fs');
const path = require('path');
const Slide = require('../slide-core.js');

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

// groups: số mèo của từng màu. Nên chia hết cho 3: dư 1 hoặc 2 con thì con lẻ không bao giờ khử được.
// walls: số ô tường; minMoves: ít nhất bao nhiêu nước tối ưu; trap: tỉ lệ trạng thái "hết đường thắng" mong muốn.
const TIERS = [
  { W: 4, H: 4, groups: [3], walls: 0, minMoves: 2, trap: 0.0 },
  { W: 4, H: 4, groups: [3, 3], walls: 0, minMoves: 4, trap: 0.05 },
  { W: 5, H: 4, groups: [3, 3], walls: 1, minMoves: 5, trap: 0.1 },
  { W: 5, H: 5, groups: [3, 3, 3], walls: 1, minMoves: 7, trap: 0.15 },
  { W: 5, H: 5, groups: [3, 3, 3, 3], walls: 2, minMoves: 8, trap: 0.2 },
  { W: 6, H: 5, groups: [3, 3, 3, 3], walls: 2, minMoves: 9, trap: 0.25 },
  { W: 6, H: 6, groups: [3, 3, 3], walls: 3, minMoves: 8, trap: 0.3 },
];
const DIR_KEYS = ['U', 'D', 'L', 'R'];

function candidate(tier, random) {
  const cells = Array.from({ length: tier.W * tier.H }, (_, i) => i);
  for (let i = cells.length - 1; i > 0; i--) {
    const j = Math.floor(random() * (i + 1));
    [cells[i], cells[j]] = [cells[j], cells[i]];
  }
  const at = (cell) => [Math.floor(cell / tier.W), cell % tier.W];
  let next = 0;
  const walls = cells.slice(next, (next += tier.walls)).map(at);
  const cats = [];
  tier.groups.forEach((size, color) => {
    for (let i = 0; i < size; i++) {
      const [r, c] = at(cells[next++]);
      cats.push({ color, dir: DIR_KEYS[Math.floor(random() * 4)], r, c });
    }
  });
  return { W: tier.W, H: tier.H, walls, cats };
}

// Đột biến nhẹ: đổi hướng một con mèo, hoặc dời nó sang ô trống khác.
function mutate(level, random) {
  const copy = JSON.parse(JSON.stringify(level));
  const cat = copy.cats[Math.floor(random() * copy.cats.length)];
  if (random() < 0.5) {
    cat.dir = DIR_KEYS[Math.floor(random() * 4)];
  } else {
    const taken = new Set([...copy.walls.map(([r, c]) => r * copy.W + c), ...copy.cats.map((other) => other.r * copy.W + other.c)]);
    const free = [];
    for (let cell = 0; cell < copy.W * copy.H; cell++) if (!taken.has(cell)) free.push(cell);
    if (free.length) { const cell = free[Math.floor(random() * free.length)]; cat.r = Math.floor(cell / copy.W); cat.c = cell % copy.W; }
  }
  return copy;
}

function evaluate(level) {
  const board = Slide.prepare(level);
  const start = Slide.startPositions(level);
  // Bàn lúc đầu không được có sẵn bộ ba (nếu không mèo biến mất trước khi chạm).
  if (Slide.findRuns(board, start, Slide.occupancy(board, start)).size > 0) return null;
  return Slide.analyse(board, start, 120000);
}

const random = makeRandom(20261108);
const levels = [];
for (const tier of TIERS) {
  let best = null;
  const started = Date.now();
  while (Date.now() - started < 40000) {
    // Leo dần: giữ đột biến nếu số mèo dọn được không giảm, dừng khi dọn hết được.
    let level = candidate(tier, random);
    let info = evaluate(level);
    for (let step = 0; step < 400 && info; step++) {
      if (info.minMoves !== Infinity) break;
      const tried = mutate(level, random);
      const triedInfo = evaluate(tried);
      if (triedInfo && triedInfo.mostCleared >= info.mostCleared) { level = tried; info = triedInfo; }
    }
    if (!info || info.minMoves === Infinity) continue;
    const trap = info.trapped / info.states;
    // Quá nhiều ngõ cụt thì người chơi casual dễ nản: bỏ.
    if (trap > 0.85) continue;
    // Ưu tiên màn có nhiều nước tối ưu (≥ minMoves) và tỉ lệ bẫy gần mong muốn.
    const score = Math.abs(trap - tier.trap) + Math.max(0, tier.minMoves - info.minMoves) * 0.08;
    if (!best || score < best.score) best = { level, info, trap, score };
    if (best.score < 0.01) break;
  }
  if (!best) throw new Error('Không sinh được màn cho ' + JSON.stringify(tier));
  best.level.par = best.info.minMoves;
  levels.push(best.level);
  console.log(`màn ${levels.length}: ${tier.W}x${tier.H}, ${best.level.cats.length} mèo, tối ưu ${best.info.minMoves} nước, ${best.info.states} trạng thái, bẫy ${(best.trap * 100).toFixed(0)}%`);
}
fs.writeFileSync(path.join(__dirname, '..', 'slide-levels.js'), 'window.SLIDE_LEVELS = ' + JSON.stringify(levels) + ';' + String.fromCharCode(10));
console.log('đã ghi slide-levels.js');
