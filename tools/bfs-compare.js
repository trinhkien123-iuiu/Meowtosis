// So bộ giải của game (A*) với BFS gốc: cùng số nước tối ưu, lời giải A* chạy lại phải thắng.
//   node tools/bfs-compare.js [levels.json]
const fs = require('fs');
const { loadLogic, asDef } = require('./lib');
const ctx = loadLogic();

function bfs(level, start, maxNodes = 2000000) {
  if (ctx.isLost(level, start)) return null;
  if (ctx.isComplete(level, start)) return 0;
  const depth = new Map([[ctx.keyOf(start), 0]]);
  const queue = [start];
  for (let q = 0; q < queue.length && depth.size < maxNodes; q++) {
    const cells = queue[q], d = depth.get(ctx.keyOf(cells));
    for (const [from, dir] of ctx.allMoves(level, cells)) {
      const res = ctx.swipe(level, cells, from, dir);
      if (!res) continue;
      const k2 = ctx.keyOf(res.cells);
      if (depth.has(k2)) continue;
      depth.set(k2, d + 1);
      if (ctx.isLost(level, res.cells)) continue;
      if (ctx.isComplete(level, res.cells)) return d + 1;
      queue.push(res.cells);
    }
  }
  return null;
}

const list = process.argv[2] ? JSON.parse(fs.readFileSync(process.argv[2], 'utf8')) : ctx.LEVELS;
let bad = 0;
list.forEach((d, i) => {
  const lv = ctx.parseLevel(asDef(d));
  let t = Date.now();
  const p = ctx.solve(lv, lv.init);
  const msA = Date.now() - t;
  t = Date.now();
  const ref = bfs(lv, lv.init);
  const msB = Date.now() - t;
  let c = lv.init;
  for (const [f, dir] of p || []) c = ctx.swipe(lv, c, f, dir).cells;
  const ok = p && p.length === ref && ctx.isWon(lv, c);
  if (!ok) bad++;
  console.log(`L${i + 1}`.padEnd(5), `A*=${p ? p.length : '-'} (${msA}ms)`.padEnd(16), `BFS=${ref} (${msB}ms)`.padEnd(18), ok ? 'ok' : 'LỆCH');
});
console.log(bad ? `${bad} level lệch` : 'Khớp hết');
