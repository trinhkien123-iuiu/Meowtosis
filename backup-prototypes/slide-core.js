// Luật của "Mèo trượt", dùng chung cho trang chơi và bộ sinh màn (node).
//   level = { W, H, walls: [[r,c]], cats: [{ color: 0..3, dir: 'U'|'D'|'L'|'R', r, c }] }
// Chạm một chú mèo: nó trượt thẳng theo mũi tên cho tới khi gặp mép bàn, tường hoặc mèo khác.
// Sau mỗi nước, mọi hàng/cột có từ 3 mèo cùng màu liền nhau trở lên đều biến mất. Hết mèo là thắng.
// `positions[i]` là ô của mèo i (hàng * rộng + cột), -1 nếu mèo đã biến mất.
// Mỗi mèo chỉ đi một chiều nên đồ thị trạng thái không có vòng: dễ duyệt hết và tính chính xác.
(function (root) {
  const DIRS = { U: [-1, 0], D: [1, 0], L: [0, -1], R: [0, 1] };

  function prepare(level) {
    return {
      W: level.W,
      H: level.H,
      walls: new Set(level.walls.map(([r, c]) => r * level.W + c)),
      cats: level.cats.map((cat) => ({ color: cat.color, dir: cat.dir })),
    };
  }

  const startPositions = (level) => level.cats.map((cat) => cat.r * level.W + cat.c);

  function occupancy(board, positions) {
    const occupied = new Array(board.W * board.H).fill(-1);
    positions.forEach((cell, index) => { if (cell >= 0) occupied[cell] = index; });
    return occupied;
  }

  // Ô mà mèo `index` sẽ dừng lại nếu được chạm (bằng ô hiện tại nếu bị chắn ngay).
  function slideTarget(board, positions, index, occupied) {
    const [dr, dc] = DIRS[board.cats[index].dir];
    let r = Math.floor(positions[index] / board.W);
    let c = positions[index] % board.W;
    for (;;) {
      const nr = r + dr;
      const nc = c + dc;
      if (nr < 0 || nc < 0 || nr >= board.H || nc >= board.W) break;
      const cell = nr * board.W + nc;
      if (board.walls.has(cell) || occupied[cell] >= 0) break;
      r = nr;
      c = nc;
    }
    return r * board.W + c;
  }

  // Các mèo nằm trong một đoạn liền từ 3 con cùng màu trở lên (theo hàng hoặc cột).
  function findRuns(board, positions, occupied) {
    const gone = new Set();
    const scan = (cells) => {
      let run = [];
      const flush = () => { if (run.length >= 3) run.forEach((index) => gone.add(index)); run = []; };
      for (const cell of cells) {
        const index = occupied[cell];
        if (index < 0) { flush(); continue; }
        if (run.length && board.cats[run[run.length - 1]].color !== board.cats[index].color) flush();
        run.push(index);
      }
      flush();
    };
    for (let r = 0; r < board.H; r++) scan(Array.from({ length: board.W }, (_, c) => r * board.W + c));
    for (let c = 0; c < board.W; c++) scan(Array.from({ length: board.H }, (_, r) => r * board.W + c));
    return gone;
  }

  // Chạm mèo `index`. Trả về null nếu mèo không đi được; ngược lại { landed, cleared, next }.
  // `landed` là ô mèo dừng, `cleared` là danh sách mèo biến mất sau nước đi, `next` là các vị trí mới.
  function move(board, positions, index) {
    const occupied = occupancy(board, positions);
    const landed = slideTarget(board, positions, index, occupied);
    if (landed === positions[index]) return null;
    const next = positions.slice();
    next[index] = landed;
    const gone = findRuns(board, next, occupancy(board, next));
    gone.forEach((cat) => { next[cat] = -1; });
    return { landed, cleared: [...gone], next };
  }

  const isWon = (positions) => positions.every((cell) => cell < 0);
  const keyOf = (positions) => positions.join(',');

  function movableCats(board, positions) {
    const occupied = occupancy(board, positions);
    const result = [];
    positions.forEach((cell, index) => {
      if (cell >= 0 && slideTarget(board, positions, index, occupied) !== cell) result.push(index);
    });
    return result;
  }

  // Duyệt hết trạng thái có thể tới được. Trả về null nếu vượt `cap`.
  // minMoves: số nước ít nhất để thắng (Infinity nếu không thắng được); states/trapped: số trạng thái, số trạng thái hết đường thắng;
  // mostCleared: số mèo nhiều nhất dọn được trên một đường đi (cho bộ sinh màn leo dần tới bàn thắng được).
  function analyse(board, start, cap = 200000) {
    const memo = new Map();
    let overflow = false;
    let mostCleared = 0;
    const visit = (positions) => {
      const key = keyOf(positions);
      if (memo.has(key)) return memo.get(key);
      if (memo.size > cap) { overflow = true; return { best: Infinity, first: -1 }; }
      let cleared = 0;
      for (const cell of positions) if (cell < 0) cleared++;
      if (cleared > mostCleared) mostCleared = cleared;
      if (isWon(positions)) { memo.set(key, { best: 0, first: -1 }); return memo.get(key); }
      let best = Infinity;
      let first = -1;
      for (const index of movableCats(board, positions)) {
        const result = move(board, positions, index);
        const sub = visit(result.next).best;
        if (sub + 1 < best) { best = sub + 1; first = index; }
        if (overflow) break;
      }
      const entry = { best, first };
      memo.set(key, entry);
      return entry;
    };
    const root = visit(start);
    if (overflow) return null;
    let trapped = 0;
    for (const entry of memo.values()) if (entry.best === Infinity) trapped++;
    return { minMoves: root.best, states: memo.size, trapped, firstMove: root.first, mostCleared };
  }

  // Nước đi đầu tiên của một lời giải ngắn nhất từ trạng thái hiện tại (dùng cho gợi ý); -1 nếu hết đường.
  function hint(board, positions, cap = 300000) {
    const info = analyse(board, positions, cap);
    return info && info.minMoves !== Infinity ? info.firstMove : -1;
  }

  const api = { DIRS, prepare, startPositions, slideTarget, findRuns, move, isWon, movableCats, analyse, hint, occupancy };
  if (typeof module !== 'undefined') module.exports = api; else root.Slide = api;
})(typeof window !== 'undefined' ? window : globalThis);
