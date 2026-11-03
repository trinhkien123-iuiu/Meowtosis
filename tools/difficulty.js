// Đo độ khó theo quyết định (lib.decisionBits) của mọi level, chạy song song, ghi tools/jobs/difficulty.json.
// Kết quả nhớ theo nội dung level, nên chạy lại chỉ đo level mới hoặc vừa sửa.
//   node tools/difficulty.js            -> đo những level chưa có trong file
//   node tools/difficulty.js --all      -> đo lại tất cả
// Dùng cho tools/order.js (sắp level từ dễ đến khó).
const fs = require('fs');
const os = require('os');
const path = require('path');
const { execFile } = require('child_process');
const { loadLogic, asDef, census, events, decisionBits } = require('./lib');

const OUT = path.join(__dirname, 'jobs', 'difficulty.json');
const keyOfDef = d => JSON.stringify(asDef(d));

// Tiến trình con: đo các level có chỉ số trong argv, in JSON ra màn hình
if (process.argv[2] === '--worker') {
  const ctx = loadLogic();
  const out = {};
  for (const i of process.argv.slice(3).map(Number)) {
    const def = asDef(ctx.LEVELS[i]), lv = ctx.parseLevel(def), p = ctx.solve(lv, lv.init);
    const ev = events(ctx, lv, p);
    out[keyOfDef(def)] = { par: p.length, ...census(def), ...decisionBits(ctx, lv, p, 800),
      need: Object.keys(ev).filter(k => ev[k] && ['replace', 'clear', 'toxic', 'wakeUse', 'chainWake', 'eaten', 'virusDie', 'germClear'].includes(k)) };
  }
  process.stdout.write(JSON.stringify(out));
  return;
}

const ctx = loadLogic();
const cache = !process.argv.includes('--all') && fs.existsSync(OUT) ? JSON.parse(fs.readFileSync(OUT, 'utf8')) : {};
const todo = ctx.LEVELS.map((d, i) => i).filter(i => !cache[keyOfDef(ctx.LEVELS[i])]);
const workers = Math.max(1, Math.min(todo.length, os.cpus().length - 2));
const chunks = Array.from({ length: workers }, () => []);
todo.forEach((i, k) => chunks[k % workers].push(i));
console.log(`Đo ${todo.length} level bằng ${workers} tiến trình...`);
let left = chunks.filter(c => c.length).length;
if (!left) console.log('Không có level mới');
for (const chunk of chunks) {
  if (!chunk.length) continue;
  execFile(process.execPath, [__filename, '--worker', ...chunk.map(String)], { maxBuffer: 1 << 26 }, (err, stdout, stderr) => {
    if (err) console.log('LỖI', stderr || err.message);
    else Object.assign(cache, JSON.parse(stdout));
    if (--left === 0) {
      // Chỉ giữ level còn trong game
      const live = new Set(ctx.LEVELS.map(keyOfDef));
      for (const k of Object.keys(cache)) if (!live.has(k)) delete cache[k];
      fs.writeFileSync(OUT, JSON.stringify(cache, null, 1));
      console.log(`Đã ghi ${Object.keys(cache).length} level vào ${path.relative(process.cwd(), OUT)}`);
    }
  });
}
