// Tạo job sinh level thay thế cho các level có cơ chế chỉ là trang trí (tools/mechanic-use.js):
// cùng cỡ bàn, số lỗ, mèo, ô độc, băng, màng, virus, hộp như level cũ; par quanh par cũ; strict = mọi cơ chế bắt buộc.
//   node tools/jobs/make-strict-jobs.js > tools/jobs/strict.json
const { loadLogic, asDef } = require('../lib');
const mechanicUse = require('../mechanic-use');
const ctx = loadLogic();
const levels = require('../../data/levels.json');
const jobs = [];
ctx.LEVELS.forEach((raw, i) => {
  const use = mechanicUse.check(ctx, raw);
  if (!Object.values(use).some(v => v !== 'bắt buộc')) return;
  const d = asDef(raw), flat = d.rows.join('');
  const count = re => [...flat].filter(ch => re.test(ch)).length;
  const par = levels[i].par;
  const job = {
    name: `L${i + 1}`, shape: { w: d.rows[0].length, h: d.rows.length, holes: [Math.max(0, count(/#/) - 1), count(/#/) + 1] },
    cells: [...flat].filter(ch => /[A-Ea-e]/.test(ch)).join(''), toxic: count(/~/), ice: count(/=/),
    virus: [...flat].filter(ch => /[<>^v]/.test(ch)).join(''), membranes: (d.oneway || []).length,
    strict: true, min: Math.max(2, par - 2), max: par + 3, ms: 60000, seed: i + 11, top: 3,
  };
  if (d.goal) { job.mode = 'target'; job.targets = [...d.goal.join('')].filter(ch => /[A-E]/.test(ch)).length; }
  if (!job.virus) delete job.virus;
  jobs.push(job);
});
console.log(JSON.stringify(jobs, null, 1));
