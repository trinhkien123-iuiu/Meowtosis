// Sinh các màn "Mèo tắm nắng" có lời giải, độ khó tăng dần, ghi ra ../sunbeam-levels.js.
//   node backup-prototypes/tools/gen-sunbeam.js
const fs = require('fs');
const path = require('path');
const Sunbeam = require('../sunbeam-core.js');

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

// W, H: cỡ bàn; sun/shade: số mèo; plants: số chậu cây; walls: số đồ đứng yên;
// rows/cols: số tia nắng từ trái/từ trên; target: số cách xếp mong muốn (càng ít càng khó).
const TIERS = [
  { W: 4, H: 3, sun: 3, shade: 0, plants: 0, walls: 0, rows: 1, cols: 0, target: 4 },
  { W: 5, H: 3, sun: 3, shade: 1, plants: 0, walls: 2, rows: 1, cols: 1, target: 4 },
  { W: 5, H: 3, sun: 3, shade: 2, plants: 1, walls: 2, rows: 3, cols: 0, target: 3 },
  { W: 5, H: 4, sun: 4, shade: 2, plants: 1, walls: 3, rows: 4, cols: 0, target: 3 },
  { W: 6, H: 4, sun: 5, shade: 3, plants: 2, walls: 3, rows: 4, cols: 1, target: 3 },
  { W: 6, H: 5, sun: 6, shade: 3, plants: 2, walls: 4, rows: 5, cols: 1, target: 3 },
  { W: 6, H: 5, sun: 6, shade: 4, plants: 3, walls: 4, rows: 5, cols: 2, target: 3 },
];

function pickDistinct(random, count, limit) {
  const all = Array.from({ length: limit }, (_, i) => i);
  for (let i = all.length - 1; i > 0; i--) {
    const j = Math.floor(random() * (i + 1));
    [all[i], all[j]] = [all[j], all[i]];
  }
  return all.slice(0, count);
}

function candidate(tier, random) {
  const { W, H } = tier;
  const cells = pickDistinct(random, tier.walls + tier.plants + tier.sun + tier.shade, W * H);
  const at = (index) => [Math.floor(index / W), index % W];
  let next = 0;
  const walls = cells.slice(next, (next += tier.walls)).map(at);
  const plants = cells.slice(next, (next += tier.plants)).map(at);
  const cats = [];
  for (let i = 0; i < tier.sun; i++) cats.push({ kind: 'sun', r: at(cells[next])[0], c: at(cells[next++])[1] });
  for (let i = 0; i < tier.shade; i++) cats.push({ kind: 'shade', r: at(cells[next])[0], c: at(cells[next++])[1] });
  const rows = pickDistinct(random, tier.rows, H).sort();
  const cols = pickDistinct(random, tier.cols, W).sort();
  return { W, H, walls, rows, cols, cats, plants };
}

function acceptable(tier, level) {
  const count = Sunbeam.countSolutions(level);
  if (count < 2) return null;
  if (Sunbeam.isSolved(level, Sunbeam.startState(level))) return null;
  // Có chậu cây thì phải cần chậu: bàn không có chậu thì không đủ chỗ bóng cho mèo thích bóng.
  if (tier.plants > 0) {
    const bare = { ...level, plants: [] };
    const free = Sunbeam.freeCells(bare);
    const lit = Sunbeam.litCells(bare, new Set());
    const dark = free.filter((index) => !lit.has(index)).length;
    if (dark >= tier.shade) return null;
  }
  return count;
}

const random = makeRandom(20261108);
const levels = [];
for (const tier of TIERS) {
  let best = null;
  for (let attempt = 0; attempt < 150000 && (!best || attempt < 40000); attempt++) {
    const level = candidate(tier, random);
    const count = acceptable(tier, level);
    if (count === null) continue;
    const distance = Math.abs(count - tier.target);
    if (!best || distance < best.distance) best = { level, count, distance };
  }
  if (!best) throw new Error('Không sinh được màn cho ' + JSON.stringify(tier));
  levels.push(best.level);
  console.log(`màn ${levels.length}: ${tier.W}x${tier.H}, ${best.count} cách xếp`);
}

const output = 'window.SUNBEAM_LEVELS = ' + JSON.stringify(levels) + ';\n';
fs.writeFileSync(path.join(__dirname, '..', 'sunbeam-levels.js'), output);
console.log('đã ghi sunbeam-levels.js');
