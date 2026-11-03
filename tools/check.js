// Kiểm tra toàn bộ danh sách level của game. Chạy sau mỗi lần sửa level:
//   node tools/check.js
// - mọi level giải được trong giới hạn của game (80.000 trạng thái), không quá chậm cho nút Gợi ý
// - lúc đầu không có sẵn dãy 3, chưa thua, chưa thắng
// - không hai level nào trùng khung (kể cả xoay/lật)
// - các bước hướng dẫn đi được, không dẫn tới thua; đi trọn thì phải thắng đúng số nước tối ưu
// - tip là một câu chữ; chương phủ kín danh sách level, không chồng nhau
// - data/levels.json (dữ liệu cho Unity) khớp với level trong game
const fs = require('fs');
const { LEVELS_JSON, loadLogic, loadChapters, exportLevels, asDef, canon } = require('./lib');

const ctx = loadLogic();
const LEVELS = ctx.LEVELS;
const SLOW_MS = 300;
const errors = [], notes = [];
const fail = msg => errors.push(msg);

const frames = new Map();
LEVELS.forEach((raw, n) => {
  const def = asDef(raw), name = `level ${n + 1}`;
  const lv = ctx.parseLevel(def);
  if (ctx.findRuns(lv, lv.init).length) fail(`${name}: có sẵn dãy 3 lúc đầu`);
  if (ctx.isLost(lv, lv.init)) fail(`${name}: thua ngay từ đầu`);
  if (ctx.isComplete(lv, lv.init)) fail(`${name}: thắng ngay từ đầu`);
  const k = canon(def.rows);
  if (frames.has(k)) fail(`${name}: trùng khung với level ${frames.get(k) + 1}`); else frames.set(k, n);
  const t0 = Date.now();
  const path = ctx.solve(lv, lv.init);
  const ms = Date.now() - t0;
  if (!path) { fail(`${name}: không giải được trong giới hạn`); return; }
  if (ms > SLOW_MS) notes.push(`${name}: bộ giải mất ${ms}ms (Gợi ý sẽ chậm trên điện thoại)`);
  if (def.tip !== undefined && (typeof def.tip !== 'string' || !def.tip)) fail(`${name}: tip phải là một câu`);
  if (def.tut) {
    let c = lv.init;
    for (const [k2, [r, col, dir, text]] of def.tut.entries()) {
      if (typeof text !== 'string' || !text) { fail(`${name} bước ${k2 + 1}: thiếu lời thoại`); return; }
      const res = ctx.swipe(lv, c, r * lv.w + col, dir);
      if (!res) { fail(`${name} bước ${k2 + 1}: nước [${r},${col},${dir}] không đi được`); return; }
      c = res.cells;
      if (ctx.isLost(lv, c)) { fail(`${name} bước ${k2 + 1}: dẫn tới thua`); return; }
    }
    if (def.tut.length >= path.length && !(ctx.isWon(lv, c) && def.tut.length === path.length))
      fail(`${name}: hướng dẫn dài ${def.tut.length} nước nhưng tối ưu là ${path.length}`);
  }
});

const CHAPTERS = loadChapters(ctx);
let at = 0;
for (const ch of CHAPTERS) {
  if (ch.from !== at) fail(`chương "${ch.name}" bắt đầu ở level ${ch.from + 1}, đáng ra là ${at + 1}`);
  if (ch.to < ch.from) fail(`chương "${ch.name}" rỗng`);
  at = ch.to + 1;
}
if (at !== LEVELS.length) fail(`các chương kết thúc ở level ${at}, nhưng có ${LEVELS.length} level`);

// Dữ liệu cho Unity phải khớp với game: sửa level xong mà quên xuất lại thì báo
let saved = null;
try { saved = fs.readFileSync(LEVELS_JSON, 'utf8'); } catch (e) { /* chưa có file */ }
if (saved === null) fail('chưa có data/levels.json: chạy npm run export');
else if (JSON.stringify(JSON.parse(saved)) !== JSON.stringify(exportLevels(ctx)))
  fail('data/levels.json cũ hơn level trong game: chạy npm run export');

for (const n of notes) console.log('lưu ý:', n);
if (errors.length) { for (const e of errors) console.log('LỖI:', e); process.exit(1); }
console.log(`check: ${LEVELS.length} level, ${frames.size} khung khác nhau, ${CHAPTERS.length} chương, tất cả đều ổn`);
