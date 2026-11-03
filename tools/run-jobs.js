// Chạy song song mọi job trong một hay nhiều file job (mỗi job một tiến trình gen.js), gộp kết quả.
//   node tools/run-jobs.js jobs/a.json jobs/b.json ... -> jobs/out/<tên file>.txt + .json
const fs = require('fs');
const os = require('os');
const path = require('path');
const { execFile } = require('child_process');

const outDir = path.join(__dirname, 'jobs', 'out');
fs.mkdirSync(outDir, { recursive: true });
const tasks = [];
for (const file of process.argv.slice(2)) {
  const base = path.basename(file, '.json');
  JSON.parse(fs.readFileSync(file, 'utf8')).forEach((job, k) => tasks.push({ base, k, job }));
}
const limit = Math.max(1, os.cpus().length - 2);
const done = {};
let next = 0, running = 0;

function launch() {
  while (running < limit && next < tasks.length) {
    const t = tasks[next++];
    const jobFile = path.join(outDir, `${t.base}.${t.k}.job.json`), resFile = path.join(outDir, `${t.base}.${t.k}.res.json`);
    fs.writeFileSync(jobFile, JSON.stringify([t.job]));
    running++;
    execFile(process.execPath, [path.join(__dirname, 'gen.js'), jobFile, resFile], { maxBuffer: 1 << 24 }, (err, stdout, stderr) => {
      running--;
      t.log = err ? `== ${t.job.name}: LỖI ${stderr || err.message}` : stdout.replace(/DONE\s*$/, '').trimEnd();
      t.res = fs.existsSync(resFile) ? JSON.parse(fs.readFileSync(resFile, 'utf8')) : [];
      fs.rmSync(jobFile, { force: true });
      fs.rmSync(resFile, { force: true });
      console.log(t.log);
      (done[t.base] ||= []).push(t);
      if (next >= tasks.length && running === 0) finish(); else launch();
    });
  }
}

function finish() {
  for (const [base, list] of Object.entries(done)) {
    list.sort((a, b) => a.k - b.k);
    fs.writeFileSync(path.join(outDir, `${base}.txt`), list.map(t => t.log).join('\n') + '\n');
    fs.writeFileSync(path.join(outDir, `${base}.json`), JSON.stringify(list.flatMap(t => t.res), null, 1));
  }
  console.log('XONG', tasks.length, 'job');
}

launch();
