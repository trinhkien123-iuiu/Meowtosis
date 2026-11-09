// Luật của "Tàu mèo" (ý 5 làm lại), dùng chung cho trang chơi và bộ sinh màn (node).
//
// level = {
//   rails: ['r--7', ...]   mỗi ký tự một ô:
//       '.' không có ray     '-' ray ngang (L-R)     '|' ray dọc (U-D)     '+' giao lộ (L-R và U-D, đi thẳng, không rẽ)
//       'r' cong ┌ (R-D)    '7' cong ┐ (L-D)        'L' cong └ (U-R)       'J' cong ┘ (U-L)
//       'S' ghi (bẻ ghi): thông tin ở `switches`
//   switches: [{ r, c, stem: 'L', a: 'R', b: 'D', state: 0 }]   ghi nối cửa `stem` với cửa `a` (state 0) hoặc `b` (state 1)
//   cars: [{ color: 0..3 | -1, dir: 'U'|'D'|'L'|'R', r, c }]  color -1 là toa hàng (không có mèo, không bao giờ rời bến)
//   limit: số chuyến (nước) tối đa, par: số nước ít nhất
// }
//
// Luật:
// - Chạm một toa: nó chạy theo hướng đang quay mặt, đi theo đường ray (qua chỗ cong thì rẽ theo), tới khi hết ray hoặc gặp toa khác.
//   Bị chắn ngay thì không đi, không tính chuyến. Toa dừng lại vẫn quay mặt theo hướng nó đang chạy.
// - Chạm cần gạt của ghi: đổi nhánh (tính một chuyến). Không gạt được khi có toa đang đứng trên ghi.
//   Toa đi vào ghi từ nhánh đang đóng thì bị chặn.
// - Sau mỗi nước, mọi đoạn từ 3 toa mèo cùng màu nối đuôi nhau dọc một đường ray (đi qua chỗ cong cũng được) thì móc vào nhau
//   và rời bến. Toa sát nhau nhưng không có ray nối thẳng giữa hai ô thì không móc được.
// - Đèn tín hiệu ('G', dữ liệu ở `gates`: { r, c, axis: 'H'|'V', open: 0|1 }): ô ray có thanh chắn. Đang đóng thì toa không vào được
//   (như hết ray). MỖI NƯỚC ĐI (vuốt toa, gạt ghi, hoặc bấm Chờ) làm mọi đèn đổi trạng thái sau khi đã xét đoàn tàu rời bến.
//   'Chờ' là một nước không làm gì khác ngoài đổi đèn (chỉ có ở màn có đèn), tốn một chuyến.
// - Hầm (`tunnels`: [[r, c, dir, r2, c2, dir2]]): cửa hầm là cửa ray quay ra ngoài ở ô A (hướng dir) và ô B (hướng dir2).
//   Toa chạy ra khỏi cửa A thì chui vào ô B qua cửa dir2 và chạy tiếp theo ray của B (và ngược lại). Hầm KHÔNG nối đoàn tàu:
//   hai toa ở hai đầu hầm cách xa nhau trên bàn nên không móc được với nhau.
// - Thắng khi hết toa mèo (toa hàng được ở lại). Thua khi hết chuyến mà chưa thắng.
(function (root) {
  const DIRS = { U: [-1, 0], D: [1, 0], L: [0, -1], R: [0, 1] };
  const OPPOSITE = { U: 'D', D: 'U', L: 'R', R: 'L' };
  const ANGLE = { R: 0, D: 90, L: 180, U: 270 };
  const CURVES = { r: ['R', 'D'], 7: ['L', 'D'], L: ['U', 'R'], J: ['U', 'L'] };
  const CARGO = -1;

  function prepare(level) {
    const H = level.rails.length;
    const W = level.rails[0].length;
    const kind = [];
    level.rails.forEach((row) => { for (let c = 0; c < W; c++) kind.push(row[c]); });
    const switches = (level.switches || []).map((s) => ({ cell: s.r * W + s.c, stem: s.stem, a: s.a, b: s.b }));
    const switchAt = new Map(switches.map((s, index) => [s.cell, index]));
    const gates = (level.gates || []).map((g) => ({ cell: g.r * W + g.c, axis: g.axis }));
    const gateAt = new Map(gates.map((g, index) => [g.cell, index]));
    // Hầm: khoá 'ô:hướng' -> ô đích, cửa vào ở đích, số thứ tự cặp hầm.
    const tunnels = new Map();
    (level.tunnels || []).forEach(([r, c, dir, r2, c2, dir2], id) => {
      const a = r * W + c;
      const b = r2 * W + c2;
      tunnels.set(a + ':' + dir, { cell: b, port: dir2, id });
      tunnels.set(b + ':' + dir2, { cell: a, port: dir, id });
    });
    return {
      W, H, kind, switches, switchAt, gates, gateAt, tunnels,
      cars: level.cars.map((car) => ({ color: car.color, cargo: car.color === CARGO })),
    };
  }

  function startState(level) {
    const W = level.rails[0].length;
    return {
      pos: level.cars.map((car) => car.r * W + car.c),
      dir: level.cars.map((car) => car.dir),
      // Trạng thái ghi rồi tới trạng thái đèn (1 là mở), cùng một mảng để khoá và nhân bản gọn.
      sw: (level.switches || []).map((s) => s.state || 0).concat((level.gates || []).map((g) => (g.open ? 1 : 0))),
    };
  }

  const cloneState = (s) => ({ pos: s.pos.slice(), dir: s.dir.slice(), sw: s.sw.slice() });
  const keyOf = (s) => s.pos.join(',') + '|' + s.dir.join('') + '|' + s.sw.join('');

  // Các cặp cửa đang thông trong ô (ghi chỉ có cặp đang bật).
  function pairs(board, cell, sw) {
    const ch = board.kind[cell];
    if (ch === '-') return [['L', 'R']];
    if (ch === '|') return [['U', 'D']];
    if (ch === '+') return [['L', 'R'], ['U', 'D']];
    if (CURVES[ch]) return [CURVES[ch]];
    if (ch === 'S') {
      const s = board.switches[board.switchAt.get(cell)];
      return [[s.stem, sw[board.switchAt.get(cell)] ? s.b : s.a]];
    }
    if (ch === 'G') {
      const index = board.gateAt.get(cell);
      if (!sw[board.switches.length + index]) return [];
      return [board.gates[index].axis === 'H' ? ['L', 'R'] : ['U', 'D']];
    }
    return [];
  }

  // Mọi cặp cửa của ô kể cả nhánh ghi đang đóng (để vẽ).
  function allPairs(board, cell) {
    if (board.kind[cell] === 'S') {
      const s = board.switches[board.switchAt.get(cell)];
      return [[s.stem, s.a], [s.stem, s.b]];
    }
    if (board.kind[cell] === 'G') return [board.gates[board.gateAt.get(cell)].axis === 'H' ? ['L', 'R'] : ['U', 'D']];
    return pairs(board, cell, []);
  }

  const isRail = (board, cell) => board.kind[cell] !== '.';

  function neighbour(board, cell, dir) {
    const r = Math.floor(cell / board.W) + DIRS[dir][0];
    const c = (cell % board.W) + DIRS[dir][1];
    if (r < 0 || c < 0 || r >= board.H || c >= board.W) return -1;
    return r * board.W + c;
  }

  // Một bước từ `cell` theo `dir`: ô kế bên, hoặc ô đầu kia của hầm. { cell, inPort, hop } hoặc null nếu ra khỏi bàn.
  function stepFrom(board, cell, dir, opts) {
    if (!(opts && opts.noTunnel)) {
      const tunnel = board.tunnels.get(cell + ':' + dir);
      if (tunnel) return { cell: tunnel.cell, inPort: tunnel.port, hop: true };
    }
    const next = neighbour(board, cell, dir);
    return next < 0 ? null : { cell: next, inPort: OPPOSITE[dir], hop: false };
  }

  // Đi vào `cell` qua cửa `inPort`: trả về cửa ra, hoặc null nếu không có đường.
  // opts.noCurve / noCross: cấm đi qua ô cong / giao lộ (dùng để kiểm tra cơ chế có bắt buộc không).
  function exitPort(board, cell, inPort, sw, opts) {
    const ch = board.kind[cell];
    if (opts && opts.noCurve && CURVES[ch]) return null;
    if (opts && opts.noCross && ch === '+') return null;
    for (const [a, b] of pairs(board, cell, sw)) {
      if (a === inPort) return b;
      if (b === inPort) return a;
    }
    return null;
  }

  // Hướng của toa có hợp lệ tại ô không (ô có cửa theo hướng ấy).
  const canFace = (board, cell, dir, sw) => pairs(board, cell, sw).some((p) => p.includes(dir));

  function occupancy(board, state) {
    const occupied = new Array(board.W * board.H).fill(-1);
    state.pos.forEach((cell, index) => { if (cell >= 0) occupied[cell] = index; });
    return occupied;
  }

  // Đường chạy của toa `index`: { cells: [ô xuất phát, ...ô đi qua], dirs: [hướng chạy vào từng ô], dir: hướng cuối, stop }.
  // null nếu toa bị chắn ngay, hoặc chạy vòng tròn không dừng.
  // stop: 'end' (hết ray, có cản), 'car' (gặp toa), 'edge' (ray chạy ra mép bàn).
  function slide(board, state, index, occupied, opts) {
    let cell = state.pos[index];
    let dir = state.dir[index];
    const cells = [cell];
    const dirs = [dir];
    const hops = [false];   // hops[k]: bước tới cells[k] đi qua hầm
    const seen = new Set([cell + dir]);
    let stop = 'end';
    for (;;) {
      const hop = stepFrom(board, cell, dir, opts);
      if (!hop) { stop = 'edge'; break; }
      const next = hop.cell;
      const out = exitPort(board, next, hop.inPort, state.sw, opts);
      if (out === null) { stop = 'end'; break; }
      if (occupied[next] >= 0) { stop = 'car'; break; }
      cell = next;
      dir = out;
      if (seen.has(cell + dir)) return null;
      seen.add(cell + dir);
      cells.push(cell);
      dirs.push(dir);
      hops.push(hop.hop);
    }
    if (cells.length === 1) return null;
    return { cells, dirs, hops, dir, stop };
  }

  // Các đoàn tàu: dãy từ 3 toa mèo cùng màu nối đuôi dọc một đường ray. Mỗi đoàn là danh sách ô theo thứ tự trên ray.
  function trains(board, state, occupied, opts) {
    const found = new Map();
    const colorAt = (cell) => {
      const index = occupied[cell];
      return index < 0 || board.cars[index].cargo ? null : board.cars[index].color;
    };
    // Đi tiếp từ `cell` ra cửa `out`, gom các ô cùng màu nối liền.
    const extend = (cell, out, color, chain, seen) => {
      for (;;) {
        const hop = stepFrom(board, cell, out, opts);
        // Hầm chỉ để chạy xuyên: hai toa hai đầu hầm cách xa nhau trên bàn nên không nối thành đoàn.
        if (!hop || hop.hop || seen.has(hop.cell) || colorAt(hop.cell) !== color) return;
        const after = exitPort(board, hop.cell, hop.inPort, state.sw, opts);
        if (after === null) return;
        chain.push(hop.cell);
        seen.add(hop.cell);
        cell = hop.cell;
        out = after;
      }
    };
    state.pos.forEach((cell) => {
      if (cell < 0) return;
      const color = colorAt(cell);
      if (color === null) return;
      for (const [a, b] of pairs(board, cell, state.sw)) {
        const seen = new Set([cell]);
        const back = [];
        const front = [];
        extend(cell, a, color, back, seen);
        extend(cell, b, color, front, seen);
        const chain = back.reverse().concat([cell], front);
        if (chain.length >= 3) {
          const key = chain.slice().sort((x, y) => x - y).join(',');
          if (!found.has(key)) found.set(key, chain);
        }
      }
    });
    return [...found.values()];
  }

  // Một nước: { type: 'car', index } hoặc { type: 'switch', index }.
  // Trả về null nếu không đi được; ngược lại { next, slide, trains: [{ cells, cars }], cleared }.
  function apply(board, state, action, opts) {
    const occupied = occupancy(board, state);
    const next = cloneState(state);
    let run = null;
    if (action.type === 'wait') {
      // Chờ: không toa nào chạy, chỉ để đèn đổi. Vô nghĩa nếu không có đèn.
      if (!board.gates.length || (opts && opts.gatesOpen)) return null;
    } else if (action.type === 'switch') {
      if (opts && opts.noSwitch) return null;
      if (occupied[board.switches[action.index].cell] >= 0) return null;
      next.sw[action.index] ^= 1;
    } else {
      if (state.pos[action.index] < 0) return null;
      if (opts && opts.frozenCargo && board.cars[action.index].cargo) return null;
      run = slide(board, state, action.index, occupied, opts);
      if (!run) return null;
      next.pos[action.index] = run.cells[run.cells.length - 1];
      next.dir[action.index] = run.dir;
    }
    const nextOccupied = occupancy(board, next);
    const formed = trains(board, next, nextOccupied, opts).map((cells) => ({ cells, cars: cells.map((cell) => nextOccupied[cell]) }));
    const cleared = new Set();
    formed.forEach((train) => train.cars.forEach((car) => cleared.add(car)));
    cleared.forEach((car) => { next.pos[car] = -1; });
    // Đèn đổi sau khi đã xét đoàn tàu rời bến.
    if (board.gates.length && !(opts && opts.gatesOpen)) {
      for (let g = 0; g < board.gates.length; g++) next.sw[board.switches.length + g] ^= 1;
    }
    return { next, slide: run, trains: formed, cleared: [...cleared] };
  }

  const isWon = (board, state) => state.pos.every((cell, index) => cell < 0 || board.cars[index].cargo);
  const catsLeft = (board, state) => state.pos.filter((cell, index) => cell >= 0 && !board.cars[index].cargo).length;

  function actions(board, state, opts) {
    const list = [];
    state.pos.forEach((cell, index) => { if (cell >= 0) list.push({ type: 'car', index }); });
    if (!(opts && opts.noSwitch)) board.switches.forEach((_, index) => list.push({ type: 'switch', index }));
    if (board.gates.length && !(opts && opts.gatesOpen)) list.push({ type: 'wait' });
    return list;
  }

  // Duyệt theo chiều rộng mọi trạng thái tới được (có vòng vì gạt ghi qua lại được).
  // Trả về { minMoves, states, trapped, first, mostCleared, path } hoặc null nếu vượt `cap`.
  // trapped: số trạng thái không còn đường thắng; path: một lời giải ngắn nhất (danh sách nước).
  function solve(board, start, opts, cap = 150000, full = true) {
    const ids = new Map();
    const states = [];
    const parent = [];
    const via = [];
    const edges = [];
    const push = (s, from, action) => {
      const key = keyOf(s);
      if (ids.has(key)) return ids.get(key);
      const id = states.length;
      ids.set(key, id);
      states.push(s);
      parent.push(from);
      via.push(action);
      edges.push([]);
      return id;
    };
    let startState0 = start;
    if (opts && opts.gatesOpen) {
      startState0 = cloneState(start);
      board.gates.forEach((_, g) => { startState0.sw[board.switches.length + g] = 1; });
    }
    if (opts && opts.noCargo) {
      startState0 = cloneState(startState0);
      board.cars.forEach((car, index) => { if (car.cargo) startState0.pos[index] = -1; });
    }
    push(startState0, -1, null);
    let firstWin = -1;
    let mostCleared = 0;
    const startLeft = catsLeft(board, startState0);
    for (let head = 0; head < states.length; head++) {
      if (states.length > cap) return null;
      const s = states[head];
      const left = catsLeft(board, s);
      if (startLeft - left > mostCleared) mostCleared = startLeft - left;
      if (isWon(board, s)) {
        if (firstWin < 0) firstWin = head;
        if (!full) break;
        continue;
      }
      for (const action of actions(board, s, opts)) {
        const result = apply(board, s, action, opts);
        if (!result) continue;
        const id = push(result.next, head, action);
        edges[head].push(id);
      }
    }
    // Đếm trạng thái còn đường thắng bằng cách đi ngược từ các trạng thái thắng.
    let trapped = 0;
    if (full) {
      const reverse = states.map(() => []);
      edges.forEach((list, from) => list.forEach((to) => reverse[to].push(from)));
      const good = new Uint8Array(states.length);
      const queue = [];
      states.forEach((s, id) => { if (isWon(board, s)) { good[id] = 1; queue.push(id); } });
      while (queue.length) {
        const id = queue.pop();
        for (const from of reverse[id]) if (!good[from]) { good[from] = 1; queue.push(from); }
      }
      for (let id = 0; id < states.length; id++) if (!good[id]) trapped++;
    }
    if (firstWin < 0) return { minMoves: Infinity, states: states.length, trapped, first: null, mostCleared, path: [] };
    const path = [];
    for (let id = firstWin; parent[id] >= 0; id = parent[id]) path.push(via[id]);
    path.reverse();
    let minMoves = 0;
    for (let id = firstWin; parent[id] >= 0; id = parent[id]) minMoves++;
    return { minMoves, states: states.length, trapped, first: path[0] || null, mostCleared, path };
  }

  // Nước đầu của một lời giải ngắn nhất từ trạng thái hiện tại (cho Gợi ý); null nếu hết đường thắng.
  function hint(board, state, cap = 120000) {
    const result = solve(board, state, null, cap, false);
    return result && result.minMoves !== Infinity ? result.first : null;
  }

  // Booster Đổi chiều: toa quay đầu, chạy ngược lại theo chính đường ray nó đang đứng.
  function reversed(board, state, index) {
    const cell = state.pos[index];
    for (const [a, b] of pairs(board, cell, state.sw)) {
      if (a === state.dir[index]) return b;
      if (b === state.dir[index]) return a;
    }
    return null;
  }

  // Booster Cần cẩu: nhấc toa sang ô ray trống khác (không phải ghi); giữ hướng nếu ô mới cho phép, không thì quay theo ray.
  function craneDir(board, state, index, cell) {
    if (!isRail(board, cell) || board.kind[cell] === 'S' || board.kind[cell] === 'G') return null;
    if (occupancy(board, state)[cell] >= 0) return null;
    if (canFace(board, cell, state.dir[index], state.sw)) return state.dir[index];
    return pairs(board, cell, state.sw)[0][1];
  }

  // Kiểm tra dữ liệu màn: toa đứng trên ray, quay đúng cửa, không trùng ô.
  function validate(level) {
    const board = prepare(level);
    const state = startState(level);
    const seen = new Set();
    for (let index = 0; index < state.pos.length; index++) {
      const cell = state.pos[index];
      if (seen.has(cell)) return 'hai toa cùng một ô';
      seen.add(cell);
      if (!isRail(board, cell)) return `toa ${index} không đứng trên ray`;
      if (!canFace(board, cell, state.dir[index], state.sw)) return `toa ${index} quay sai hướng ray`;
    }
    for (const [key, t] of board.tunnels) {
      const [cellText, dir] = key.split(':');
      if (!allPairs(board, Number(cellText)).flat().includes(dir)) return `cửa hầm ${key} không có ray`;
      if (!allPairs(board, t.cell).flat().includes(t.port)) return `đầu kia của hầm ${key} không có ray`;
    }
    return null;
  }

  const api = {
    DIRS, OPPOSITE, ANGLE, CURVES, CARGO, stepFrom, prepare, startState, cloneState, keyOf, pairs, allPairs, isRail, neighbour,
    exitPort, canFace, occupancy, slide, trains, apply, isWon, catsLeft, actions, solve, hint, reversed, craneDir, validate,
  };
  if (typeof module !== 'undefined') module.exports = api; else root.Rail = api;
})(typeof window !== 'undefined' ? window : globalThis);
