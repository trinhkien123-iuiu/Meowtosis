// Thay các level có cơ chế chỉ là trang trí bằng level mới (sinh bằng tools/jobs/strict*.json, hoặc bỏ phần trang trí),
// ghi thẳng vào LEVELS của prototype/meowtosis.html theo số level hiện tại. Chạy một lần (2026-11-07).
const fs = require('fs');
const path = require('path');
const GAME = path.join(__dirname, '..', '..', 'prototype', 'meowtosis.html');
const R = {
  17: "{ rows: ['#B.A', 'A.##'], goal: ['..B.', '....'], tut: [\n    [0, 1, 'right', 'Hộp xanh cần mèo xanh: vuốt mèo xanh vào hộp!'],\n    [1, 0, 'right', 'Lấp nốt ô trống: vừa kín ổ vừa đúng hộp!']] },  // dạy hộp: mèo đỏ vào hộp là sai",
  23: "{ rows: ['.#.B', '...A', '...#'], goal: ['....', '..B.', '..A.'] },  // 3×4 · hộp bắt buộc",
  26: "{ rows: ['#AC.', '..B#', '..#.', '....'], goal: ['...C', '....', 'A..A', '....'] },  // 4×4 · ba hộp",
  29: "{ rows: ['#BAA', '..##'], oneway: [[0, 1, 'down']], tut: [\n    [0, 1, 'down', 'Mũi tên chỉ xuống: vuốt mèo xanh xuống qua màng'],\n    [1, 1, 'left', 'Lấp nốt ô trống']] },  // dạy màng: hàng dưới chỉ vào được qua màng",
  32: "{ rows: ['CB#.', 'C.#A', '.BAA', 'B##.'], oneway: [[2, 2, 'right'], [0, 1, 'down']] },  // 4×4 · hai màng",
  34: "['#...', 'BA~.', '.#B.', 'A#~#'],                     // 4×4 · phải bỏ mèo vào ô độc",
  35: "{ rows: ['#..#B', '#.~A.', '..A..'], oneway: [[0, 4, 'down']] },  // 5×3 · độc + màng",
  36: "{ rows: ['...#', 'AB~A', '.#..'], oneway: [[1, 0, 'right']] },  // 4×3 · độc + màng",
  40: "['..B.', '#..#', '#.cA', '.C..'],                     // 4×4 · phải đánh thức mèo vàng",
  41: null,  // giữ tip và card, chỉ đổi bàn (xem dưới)
  47: "['###.a', '..#.^', 'ABA.b', 'BA.CC', '##BCA'],       // 5×5 · trùm khối: ngủ + virus",
  49: "{ rows: ['B==A', 'A#.#'], tut: [\n    [0, 3, 'left', 'Vuốt mèo đỏ sang trái: trên băng, bản sao trượt tới khi gặp mèo xanh!'],\n    [0, 0, 'right', 'Vuốt mèo xanh sang phải, đẩy cả dãy'],\n    [0, 2, 'down', 'Lấp nốt ô dưới']] },  // dạy băng",
  51: "['.==A', '#=#B'],                                     // 2×4 · trượt hai lần",
  52: "['ABA', '=#B', '==#'],                                // 3×3 · băng + thay thế",
  53: "['AB#', 'B#=', 'A=='],                                // 3×3 · băng + thay thế",
  56: "['BB#=', 'C##=', 'A=AC', 'A==#'],                     // 4×4 · mở rộng băng",
  59: "['.B#A', '.~C.', '...#'],                             // 3×4 · ôn ô độc",
  62: "{ rows: ['ECE#C', '..E#D', 'D#.EC', '...#D', '#C#DC'], goal: ['.....', 'E....', '..E..', '..E..', '.....'] },  // 5×5 · hộp",
  63: "['.#EAA', '.A.#.', 'E##AE', 'E.ACC', '..EC.'],       // 5×5 · bàn đông mèo",
  65: "['C...B', 'C#B.B', '.BABA', 'AC.v.', '#A.A#'],       // 5×5 · virus",
  67: "{ rows: ['.A~.', '##A.', '#.B.'], oneway: [[2, 2, 'up']] },  // 4×3 · độc + màng",
  68: "['#.AC#', '#>BBA', 'CB.B.', '..#A#', 'B.B.A', '##.CA'],  // 5×6 · trùm chương: virus",
  72: "['dB.B', 'CACA', '..A#', '.>B.', '..D#'],             // 4×5 · ngủ + virus",
  74: "['..BB#', '..A##', 'BA#.#', 'DC#AD', '#.CEE'],       // 5×5 · 5 màu",
  76: "['##CBC', 'C#AA.', '..B#v', 'BD..E', '...#E', '##DA#'],  // 5×6 · trùm cuối, 5 màu + virus",
};
const html = fs.readFileSync(GAME, 'utf8');
const nl = html.includes('\r\n') ? '\r\n' : '\n';
const lines = html.split(nl);
const start = lines.findIndex(l => l.startsWith('const LEVELS = ['));
const entries = [];   // [đầu, cuối] theo dòng
for (let i = start + 1; lines[i] !== '];'; i++) {
  if (!/^  [\[{]/.test(lines[i])) continue;
  let depth = 0, j = i;
  for (;; j++) {
    const code = lines[j].replace(/'[^']*'/g, "''").replace(/\/\/.*$/, '');
    for (const ch of code) { if (ch === '[' || ch === '{') depth++; if (ch === ']' || ch === '}') depth--; }
    if (depth <= 0) break;
  }
  entries.push([i, j]);
  i = j;
}
// Level 41 giữ tip, card: chỉ thay rows
{
  const [a, b] = entries[40];
  const text = lines.slice(a, b + 1).join(nl);
  R[41] = text.replace(/rows: \[[^\]]*\]/, "rows: ['#.b#.', 'bb.A.', '.AB#.']");
}
for (const n of Object.keys(R).map(Number).sort((x, y) => y - x)) {
  const [a, b] = entries[n - 1];
  lines.splice(a, b - a + 1, ...('  ' + R[n]).split('\n').map(l => l.replace(/^(?! {2})/, '')));
}
fs.writeFileSync(GAME, lines.join(nl));
console.log('đã thay', Object.keys(R).length, 'level; tổng entry', entries.length);
