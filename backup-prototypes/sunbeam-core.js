// Luật của "Mèo tắm nắng", dùng chung cho trang chơi và bộ sinh màn (node).
// Ô lưới đánh số theo hàng: chỉ số = hàng * rộng + cột.
//   level = { W, H, walls: [[r,c]], rows: [r], cols: [c], cats: [{kind:'sun'|'shade', r, c}], plants: [[r,c]] }
// Nắng chiếu từ cửa sổ bên trái theo từng hàng trong `rows`, từ cửa sổ phía trên theo từng cột trong `cols`.
// Tia nắng đi thẳng tới khi gặp tường (đồ đứng yên) hoặc chậu cây thì dừng; mèo không cản nắng.
(function (root) {
  function litCells(level, plantCells) {
    const { W, H } = level;
    const walls = new Set(level.walls.map(([r, c]) => r * W + c));
    const blocked = (index) => walls.has(index) || plantCells.has(index);
    const lit = new Set();
    for (const r of level.rows) {
      for (let c = 0; c < W; c++) {
        const index = r * W + c;
        if (blocked(index)) break;
        lit.add(index);
      }
    }
    for (const c of level.cols) {
      for (let r = 0; r < H; r++) {
        const index = r * W + c;
        if (blocked(index)) break;
        lit.add(index);
      }
    }
    return lit;
  }

  // state = { cats: [{kind, pos}], plants: [pos] }. Trả về mảng true/false: mèo nào đang hài lòng.
  function catsHappy(level, state) {
    const lit = litCells(level, new Set(state.plants));
    return state.cats.map((cat) => (cat.kind === 'sun' ? lit.has(cat.pos) : !lit.has(cat.pos)));
  }

  function isSolved(level, state) {
    return catsHappy(level, state).every(Boolean);
  }

  function choose(n, k) {
    if (k < 0 || k > n) return 0;
    let result = 1;
    for (let i = 1; i <= k; i++) result = (result * (n - k + i)) / i;
    return Math.round(result);
  }

  function eachCombo(items, k, visit, start = 0, picked = []) {
    if (picked.length === k) { visit(picked); return; }
    for (let i = start; i < items.length; i++) {
      picked.push(items[i]);
      eachCombo(items, k, visit, i + 1, picked);
      picked.pop();
    }
  }

  function freeCells(level) {
    const walls = new Set(level.walls.map(([r, c]) => r * level.W + c));
    const free = [];
    for (let i = 0; i < level.W * level.H; i++) if (!walls.has(i)) free.push(i);
    return free;
  }

  // Số cách xếp khác nhau (các chậu giống nhau, mèo cùng loại giống nhau) làm mọi mèo hài lòng.
  function countSolutions(level) {
    const free = freeCells(level);
    const sun = level.cats.filter((cat) => cat.kind === 'sun').length;
    const shade = level.cats.length - sun;
    let total = 0;
    eachCombo(free, level.plants.length, (plants) => {
      const plantSet = new Set(plants);
      const lit = litCells(level, plantSet);
      let litFree = 0;
      let darkFree = 0;
      for (const index of free) {
        if (plantSet.has(index)) continue;
        if (lit.has(index)) litFree++; else darkFree++;
      }
      total += choose(litFree, sun) * choose(darkFree, shade);
    });
    return total;
  }

  function startState(level) {
    return {
      cats: level.cats.map((cat) => ({ kind: cat.kind, pos: cat.r * level.W + cat.c })),
      plants: level.plants.map(([r, c]) => r * level.W + c),
    };
  }

  const api = { litCells, catsHappy, isSolved, countSolutions, freeCells, startState };
  if (typeof module !== 'undefined') module.exports = api; else root.Sunbeam = api;
})(typeof window !== 'undefined' ? window : globalThis);
