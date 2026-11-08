// Lọc ứng viên level virus theo ý đồ "virus gây khó chịu": cả hai virus đều có ô để lan ngay từ đầu (trước mắt không phải tường,
// mép bàn hay virus khác), mắt các virus không cùng một hướng, và lời giải ngắn nhất để virus lan nhiều lần.
//   node tools/pick-virus.js tools/jobs/out/virus-redesign.json
const fs = require('fs');

const DIRS = { '^': [-1, 0], v: [1, 0], '<': [0, -1], '>': [0, 1] };

function viruses(rows) {
  const found = [];
  rows.forEach((row, r) => [...row].forEach((ch, c) => { if (DIRS[ch]) found.push({ r, c, ch }); }));
  return found;
}

// Virus "sống": ô ngay trước mắt nằm trong bàn, không phải tường và không phải virus khác.
function threatened(rows, v) {
  const [dr, dc] = DIRS[v.ch];
  const r = v.r + dr, c = v.c + dc;
  if (r < 0 || c < 0 || r >= rows.length || c >= rows[0].length) return false;
  const ch = rows[r][c];
  return ch !== '#' && !DIRS[ch];
}

function feat(feats, key) {
  const m = new RegExp('\\b' + key + '(\\d+)').exec(feats);
  return m ? Number(m[1]) : 0;
}

const candidates = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const byJob = new Map();
for (const cand of candidates) {
  const rows = cand.def.rows;
  const list = viruses(rows);
  if (list.length < 2) continue;
  if (new Set(list.map(v => v.ch)).size < 2) continue;
  const alive = list.filter(v => threatened(rows, v)).length;
  if (alive < list.length) continue;
  const spread = feat(cand.feats, 'spread'), eaten = feat(cand.feats, 'eaten'), dies = feat(cand.feats, 'virusDie');
  const score = spread * 2 + (eaten ? 2 : 0) + dies * 0.5 + cand.surprise * 0.3 - Math.max(0, cand.difficulty - 11) * 0.6 + (cand.firstOk / cand.first);
  if (!byJob.has(cand.job)) byJob.set(cand.job, []);
  byJob.get(cand.job).push({ cand, score, spread, eaten });
}
for (const [job, list] of byJob) {
  list.sort((a, b) => b.score - a.score);
  console.log('== ' + job + ' (' + list.length + ' đạt)');
  for (const { cand, score, spread, eaten } of list.slice(0, 3)) {
    console.log('  điểm=' + score.toFixed(1) + ' par=' + cand.par + ' khó=' + cand.difficulty + ' lan=' + spread + ' ăn=' + eaten + ' ' + JSON.stringify(cand.def.rows));
  }
}
