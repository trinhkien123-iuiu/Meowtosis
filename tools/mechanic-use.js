// Cơ chế phải BẮT BUỘC dùng mới thắng được: level có cơ chế nào thì cấm mọi nước dùng tới cơ chế đó rồi tìm đường thắng.
// Còn thắng được thì cơ chế chỉ là trang trí, level không đạt.
//   node tools/mechanic-use.js            -> bảng mọi level có cơ chế
//   require('./mechanic-use').check(...)  -> dùng trong check.js và gen.js
//
// "Dùng" một cơ chế là:
//   băng  : có nước làm bản sao trượt trên băng
//   độc   : có quân chết trên ô độc
//   ngủ   : có nước đánh thức mèo ngủ
//   màng  : có nước đưa quân đi xuyên màng (bản sao hoặc dãy bị đẩy, theo chiều mũi tên)
//   hộp   : (điều kiện thắng, không cấm được) bỏ hộp đi thì lời giải ngắn nhất không còn đúng hộp
//   cổng  : (không cấm được) bỏ hẳn cổng đi thì bàn không còn thắng được
//   hai màu: (không cấm được) bỏ tính hai màu (mèo giữ mãi màu hiện tại) thì bàn không còn thắng được
// Hai cơ chế này dùng phép thử mạnh hơn: người chơi coi cổng như tường, coi mèo hai màu như mèo thường mà vẫn thắng thì
// cơ chế chỉ là trang trí. Có chứng minh nhanh bằng cấu trúc (vùng bàn tách rời không có mèo; hộp đòi màu không ai có)
// rồi mới tới tìm kiếm vét cạn.
//   virus : còn virus thì chưa thắng, nên luôn bắt buộc
const { loadLogic, asDef } = require('./lib');

const MAX_NODES = 200000, MAX_MS = 4000;

const has = {
  'băng': lv => lv.ice.some(Boolean),
  'độc': lv => lv.toxic.some(Boolean),
  'ngủ': lv => lv.init.some(c => c && c.sleep),
  'màng': lv => lv.oneway.length > 0,
  'hộp': lv => lv.hasTarget,
  'cổng': lv => lv.portals.length > 0,
  'hai màu': lv => lv.init.some(c => c && c.alt),
};

const OPPOSITE = { up: 'down', down: 'up', left: 'right', right: 'left' };

// Nước `res` (vuốt `from` theo `dir` từ bàn `before`) có dùng cơ chế `name` không.
function uses(ctx, lv, name, before, from, dir, res) {
  switch (name) {
    case 'băng': return !!res.slid;
    case 'độc': return res.poisoned.length > 0 || res.moved.some((c, i) => c && lv.toxic[i]);
    case 'ngủ': {
      const asleep = new Set(before.filter(c => c && c.sleep).map(c => c.id));
      return res.cells.some(c => c && !c.sleep && asleep.has(c.id));
    }
    case 'màng':
      return lv.oneway.some(([i, j, d]) => (from === i && dir === d)
        || (before[i] && res.moved[j] && res.moved[j].id === before[i].id));
    default: return false;
  }
}

// Như solve của game nhưng bỏ qua các nước `forbidden`. Trả: 'thắng' | 'không thắng' | 'không chắc' (hết giới hạn).
function winsWithout(ctx, lv, forbidden) {
  const t0 = Date.now();
  const need = lv.slot.filter((s, i) => s && !lv.toxic[i]).length;
  const h = cells => need - cells.filter(c => c && c.color !== 'V' && c.color !== lv.kill).length;
  const best = new Map([[ctx.keyOf(lv.init), { g: 0, cells: lv.init }]]);
  const buckets = [];
  buckets[h(lv.init)] = [ctx.keyOf(lv.init)];
  let cut = false, expanded = 0;
  for (let f = h(lv.init); f < buckets.length; f++) {
    const bucket = buckets[f] || [];
    while (bucket.length) {
      const node = best.get(bucket.pop());
      if (node.done || node.g + h(node.cells) !== f) continue;
      node.done = true;
      if (ctx.isComplete(lv, node.cells)) return 'thắng';
      if ((++expanded & 127) === 0 && Date.now() - t0 > MAX_MS) return 'không chắc';
      for (const [from, dir] of ctx.allMoves(lv, node.cells)) {
        const res = ctx.swipe(lv, node.cells, from, dir);
        if (!res || ctx.isLost(lv, res.cells) || forbidden(node.cells, from, dir, res)) continue;
        const k2 = ctx.keyOf(res.cells), g2 = node.g + 1, old = best.get(k2);
        if (!old && best.size >= MAX_NODES) { cut = true; continue; }
        if (old && old.g <= g2) continue;
        best.set(k2, { g: g2, cells: res.cells });
        (buckets[g2 + h(res.cells)] ||= []).push(k2);
      }
    }
  }
  return cut ? 'không chắc' : 'không thắng';
}

// Hộp bắt buộc khi lời giải ngắn nhất của bàn không hộp không còn đúng hộp.
function boxesMatter(ctx, def) {
  const plainDef = { ...def };
  delete plainDef.goal;
  const plain = ctx.parseLevel(plainDef), lv = ctx.parseLevel(def);
  const path = ctx.solve(plain, plain.init, MAX_NODES, MAX_MS);
  if (!path) return 'không chắc';
  let c = lv.init;
  for (const [f, d] of path) { const r = ctx.swipe(lv, c, f, d); if (!r) return 'bắt buộc'; c = r.cells; }
  return ctx.isWon(lv, c) ? 'trang trí' : 'bắt buộc';
}

// Các vùng liền nhau của bàn (theo ô kề thật, không tính cổng).
function regions(lv) {
  const seen = new Set(), out = [];
  for (let i = 0; i < lv.slot.length; i++) {
    if (!lv.slot[i] || seen.has(i)) continue;
    const region = [i];
    seen.add(i);
    for (let k = 0; k < region.length; k++) {
      const r = Math.floor(region[k] / lv.w), c = region[k] % lv.w;
      for (const [dr, dc] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const rr = r + dr, cc = c + dc, j = rr * lv.w + cc;
        if (rr >= 0 && cc >= 0 && rr < lv.h && cc < lv.w && lv.slot[j] && !seen.has(j)) { seen.add(j); region.push(j); }
      }
    }
    out.push(region);
  }
  return out;
}

// Bỏ cơ chế đi (bàn `plain`) mà vẫn thắng được thì cơ chế chỉ là trang trí.
function plainResult(ctx, plain) {
  const path = ctx.solve(plain, plain.init, MAX_NODES, MAX_MS);
  if (path) return 'trang trí';
  return ctx.solve.cut ? 'không chắc' : 'bắt buộc';
}

// Cổng bắt buộc khi bỏ cổng thì không thắng được: chứng minh nhanh nếu có vùng bàn cần lấp mà lúc đầu không có quân nào.
function portalMatters(ctx, def) {
  const plain = ctx.parseLevel({ ...def, portal: undefined });
  const emptyRegion = regions(plain).some(region => region.some(i => !plain.toxic[i]) && region.every(i => !plain.init[i]));
  return emptyRegion ? 'bắt buộc' : plainResult(ctx, plain);
}

// Mèo hai màu bắt buộc khi bỏ tính hai màu thì không thắng được: chứng minh nhanh nếu có hộp đòi một màu không con nào có.
function dualMatters(ctx, def) {
  const plain = ctx.parseLevel({ ...def, dual: undefined });
  const have = new Set(plain.init.filter(c => c && c.color !== 'V').map(c => c.color));
  if (plain.target.some(t => t && !have.has(t))) return 'bắt buộc';
  return plainResult(ctx, plain);
}

// Kết quả từng cơ chế có trong level: 'bắt buộc' | 'trang trí' | 'không chắc'.
function check(ctx, raw) {
  const def = asDef(raw), lv = ctx.parseLevel(def), out = {};
  for (const name of Object.keys(has)) {
    if (!has[name](lv)) continue;
    if (name === 'hộp') { out[name] = boxesMatter(ctx, def); continue; }
    if (name === 'hai màu') { out[name] = dualMatters(ctx, def); continue; }
    if (name === 'cổng') { out[name] = portalMatters(ctx, def); continue; }
    const r = winsWithout(ctx, lv, (before, from, dir, res) => uses(ctx, lv, name, before, from, dir, res));
    out[name] = r === 'thắng' ? 'trang trí' : r === 'không thắng' ? 'bắt buộc' : 'không chắc';
  }
  return out;
}

if (require.main === module) {
  const ctx = loadLogic();
  let bad = 0, unsure = 0;
  ctx.LEVELS.forEach((raw, i) => {
    const r = check(ctx, raw);
    const names = Object.keys(r);
    if (!names.length) return;
    const fails = names.filter(n => r[n] === 'trang trí');
    bad += fails.length ? 1 : 0;
    unsure += names.some(n => r[n] === 'không chắc') ? 1 : 0;
    console.log(`${String(i + 1).padStart(3)} ${fails.length ? '✗' : '✓'} ${names.map(n => `${n}: ${r[n]}`).join(', ').padEnd(48)} ${asDef(raw).rows.join('/')}`);
  });
  console.log(`\n${bad} level có cơ chế chỉ là trang trí, ${unsure} level chưa chắc (hết giới hạn tìm)`);
}

module.exports = { check, uses, winsWithout, regions };
