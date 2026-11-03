// Bảng chỉ số của mọi level trong game (hoặc của các level truyền vào bằng file JSON).
//   node tools/analyze.js                 -> mọi level trong game
//   node tools/analyze.js candidates.json -> các level trong file
// Cột: kích thước, số ô cần lấp, số mèo, số màu, cơ chế, par (nước tối ưu), thời gian giải, số trạng thái,
// tỷ lệ ngõ cụt, nước đầu (đi được / còn thắng / tối ưu), thắng-thua khi chơi ngẫu nhiên, độ khó, bất ngờ, sự kiện.
const fs = require('fs');
const { loadLogic, analyze, featString } = require('./lib');

const ctx = loadLogic();
const list = process.argv[2] ? JSON.parse(fs.readFileSync(process.argv[2], 'utf8')) : ctx.LEVELS;
const pad = (s, n) => String(s).padEnd(n);
console.log(pad('#', 4) + pad('khung', 6) + pad('ô', 4) + pad('mèo', 5) + pad('màu', 4) + pad('cơ chế', 14) + pad('par', 5)
  + pad('ms', 6) + pad('trạng thái', 12) + pad('cụt', 6) + pad('nước đầu', 10) + pad('thắng', 7) + pad('thua', 7)
  + pad('khó', 7) + pad('bất ngờ', 9) + 'sự kiện');
list.forEach((def, i) => {
  const a = analyze(ctx, def);
  if (!a.solved) { console.log(pad(i + 1, 4) + 'KHÔNG GIẢI ĐƯỢC ' + JSON.stringify(def)); return; }
  console.log(pad(i + 1, 4) + pad(`${a.w}×${a.h}`, 6) + pad(a.slots, 4) + pad(a.cats, 5) + pad(a.colors, 4)
    + pad(a.mech.join(',') || '-', 14) + pad(a.par, 5) + pad(a.ms, 6) + pad(a.states + (a.capped ? '+' : ''), 12)
    + pad(a.deadFrac === null ? '?' : a.deadFrac.toFixed(2), 6) + pad(`${a.first}/${a.firstOk}/${a.firstOpt}`, 10) + pad(a.win.toFixed(2), 7)
    + pad(a.lose.toFixed(2), 7) + pad(a.difficulty, 7) + pad(a.surprise, 9) + featString(a.ev));
});
