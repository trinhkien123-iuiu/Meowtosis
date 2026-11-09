// Tính lại số nước tối ưu, giới hạn chuyến và chứng minh "bắt buộc" cho các màn có hầm hoặc đèn sau khi đổi luật.
//   node backup-prototypes/tools/recheck-rail.js [--write]
const fs = require('fs');
const path = require('path');
const Rail = require('../rail-core.js');

const file = path.join(__dirname, '..', 'rail-levels.js');
const text = fs.readFileSync(file, 'utf8');
const levels = JSON.parse(text.slice(text.indexOf('['), text.lastIndexOf(']') + 1));
const CAP = 120000;
const write = process.argv.includes('--write');
const broken = [];

levels.forEach((level, index) => {
  const needsCheck = (level.tunnels && level.tunnels.length) || (level.gates && level.gates.length);
  if (!needsCheck) return;
  const board = Rail.prepare(level);
  const start = Rail.startState(level);
  const info = Rail.solve(board, start, null, CAP, true);
  if (!info || info.minMoves === Infinity) { broken.push(`màn ${index + 1}: không thắng được hoặc vượt giới hạn`); return; }
  const proof = {};
  if (level.tunnels && level.tunnels.length) {
    const without = Rail.solve(board, start, { noTunnel: true }, CAP, false);
    proof.tunnel = without !== null && without.minMoves === Infinity;
  }
  if (level.gates && level.gates.length) {
    const open = Rail.solve(board, start, { gatesOpen: true }, CAP, false);
    proof.gate = !!open && open.minMoves !== Infinity && open.minMoves < info.minMoves ? `+${info.minMoves - open.minMoves} chuyến` : false;
  }
  const slack = level.limit - level.par;
  const changed = info.minMoves !== level.par;
  const failed = Object.entries(proof).filter(([, ok]) => !ok).map(([name]) => name);
  console.log(`màn ${index + 1} [${level.block}] tối ưu ${level.par} -> ${info.minMoves}, trạng thái ${info.states}, ngõ cụt ${Math.round(info.trapped / info.states * 100)}%, bắt buộc ${JSON.stringify(proof)}${failed.length ? '  <-- KHÔNG ĐẠT: ' + failed.join(',') : ''}`);
  if (failed.length) broken.push(`màn ${index + 1} [${level.block}]: ${failed.join(',')} không còn bắt buộc`);
  if (changed || write) {
    level.par = info.minMoves;
    level.limit = info.minMoves + slack;
    level.stats = { states: info.states, trap: Math.round(info.trapped / info.states * 100), proof };
  }
});
if (write) fs.writeFileSync(file, 'window.RAIL_LEVELS = ' + JSON.stringify(levels) + ';' + String.fromCharCode(10));
console.log(broken.length ? '\nCẦN SINH LẠI:\n' + broken.join('\n') : '\ntất cả đạt');
