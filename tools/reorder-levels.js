const fs = require('fs');
const path = require('path');

const CATALOG = path.join(__dirname, '../unity/Assets/_Project/Levels/LevelCatalog.txt');
const rawText = fs.readFileSync(CATALOG, 'utf8');

const hasComment = rawText.includes('// Level ');
const re = hasComment ? /\n(?=\/\/ Level \d+)/ : /\n(?=level meow-)/;
const blocks = rawText.split(re).slice(1);
const map = new Map();
blocks.forEach(b => {
  const id = (b.match(/level (meow-\d+)/) || [])[1];
  if (id) map.set(id, b);
});

// Chapter 1: Full 13 Onboarding levels kept completely intact!
const ONBOARDING_IDS = [
  'meow-001', // Level 1 (Divide)
  'meow-153', // Level 2
  'meow-154', // Level 3
  'meow-157', // Level 4 (Clear)
  'meow-158', // Level 5 (Trap)
  'meow-159', // Level 6
  'meow-160', // Level 7
  'meow-161', // Level 8 (Push)
  'meow-162', // Level 9
  'meow-163', // Level 10
  'meow-164', // Level 11 (Replace)
  'meow-165', // Level 12
  'meow-166', // Level 13
];

// Mechanics Chapters according to User Plan A:
// Hộp mục tiêu -> Cổng dịch chuyển -> Mèo đổi 2 màu -> Mèo ngủ -> Virus -> Ô băng -> Ô độc -> Cổng 1 chiều
const MECHANIC_CHAPTERS = [
  // Chapter 2: TargetBox (6 levels: 14 - 19)
  { chapter: 2, name: 'Hộp mục tiêu', ids: ['meow-094', 'meow-018', 'meow-019', 'meow-020', 'meow-021', 'meow-022'] },
  // Chapter 3: Portal (6 levels: 20 - 25)
  { chapter: 3, name: 'Cổng dịch chuyển', ids: ['meow-130', 'meow-141', 'meow-142', 'meow-143', 'meow-144', 'meow-145'] },
  // Chapter 4: TwoColor (6 levels: 26 - 31)
  { chapter: 4, name: 'Mèo hai màu', ids: ['meow-146', 'meow-147', 'meow-148', 'meow-149', 'meow-150', 'meow-151'] },
  // Chapter 5: Sleep (5 levels: 32 - 36)
  { chapter: 5, name: 'Mèo ngủ & Đánh thức dây chuyền', ids: ['meow-038', 'meow-039', 'meow-102', 'meow-046', 'meow-103'] },
  // Chapter 6: Virus (5 levels: 37 - 41)
  { chapter: 6, name: 'Virus lây lan', ids: ['meow-042', 'meow-119', 'meow-044', 'meow-120', 'meow-121'] },
  // Chapter 7: Ice (6 levels: 42 - 47)
  { chapter: 7, name: 'Băng trơn', ids: ['meow-105', 'meow-087', 'meow-107', 'meow-108', 'meow-109', 'meow-106'] },
  // Chapter 8: Toxic (4 levels: 48 - 51)
  { chapter: 8, name: 'Ô độc', ids: ['meow-033', 'meow-110', 'meow-099', 'meow-037'] },
  // Chapter 9: Membrane (6 levels: 52 - 57)
  { chapter: 9, name: 'Cổng một chiều', ids: ['meow-097', 'meow-030', 'meow-098', 'meow-101', 'meow-100', 'meow-031'] },
];

const mainIds = new Set([...ONBOARDING_IDS, ...MECHANIC_CHAPTERS.flatMap(c => c.ids)]);

// Remaining levels moved to the back, sorted by par
const remaining = [];
for (const [id, block] of map.entries()) {
  if (!mainIds.has(id)) {
    const par = parseInt((block.match(/par (\d+)/) || [])[1], 10);
    remaining.push({ id, par, block });
  }
}
remaining.sort((a, b) => a.par - b.par);

console.log('Onboarding levels:', ONBOARDING_IDS.length);
console.log('Mechanic chapters levels:', MECHANIC_CHAPTERS.reduce((s, c) => s + c.ids.length, 0));
console.log('Remaining levels at back:', remaining.length);
console.log('Total:', ONBOARDING_IDS.length + MECHANIC_CHAPTERS.reduce((s, c) => s + c.ids.length, 0) + remaining.length);

// Palettes
const CLASSIC = 'Red Blue Yellow Pink Purple';
const BLUE_RED = 'Blue Red Yellow Pink Purple';

const ROTATIONS = [
  'Green Orange Teal Yellow Purple',
  'Teal WarmPink Lime BabyBlue Purple',
  'Lime Purple Orange Green Yellow',
  'BabyBlue Orange ElectricBlue WarmPink Yellow',
  'ElectricBlue Yellow WarmPink Green Blue',
  'Green WarmPink Teal Orange BabyBlue',
  'Teal Yellow Purple Lime WarmPink',
  'Lime Orange BabyBlue Purple Red',
  'ElectricBlue WarmPink Teal Yellow Green',
  'Green Purple Orange BabyBlue WarmPink',
  'BabyBlue WarmPink Yellow Lime ElectricBlue',
  'Lime ElectricBlue Orange Red Green',
];

const SPECIFIC_PALETTE = {
  // Chapter 1: Onboarding
  // Level 1: Red (A = Red)
  'meow-001': 'Red Blue Yellow Pink Purple',
  // Level 2: Red cat requested by user ("level 2 vẫn đề mèo đỏ cho mình"). Row has B. -> B = Red!
  'meow-153': 'Blue Red Yellow Pink Purple',
  // Level 3: Blue cat requested by user ("level 3 là mèo màu xanh dương đi"). Row has C.## -> C = Blue!
  'meow-154': 'Red Yellow Blue Pink Purple',
  // Level 4: Red & Blue (BAB / A.A, 3 red clear)
  'meow-157': 'Red Blue Yellow Pink Purple',
  // Level 5: Red & Blue (AA. / #.B, red trap)
  'meow-158': 'Red Blue Yellow Pink Purple',
  // Levels 6, 7, 8: Red and Green requested by user ("level 6, 7, 8 là mèo mới là đỏ và xanh lá cây")
  'meow-159': 'Red Green Yellow Pink Purple',
  'meow-160': 'Red Green Yellow Pink Purple',
  'meow-161': 'Red Green Yellow Pink Purple',
  // Levels 9, 10: Blue and Orange requested by user ("level 9, 10 là màu xanh dương và màu cam")
  // Rows have BAA. / BA.. -> A = Orange, B = Blue!
  'meow-162': 'Orange Blue Yellow Pink Purple',
  'meow-163': 'Orange Blue Yellow Pink Purple',
  // Level 11: Blue & Red (Replace tutorial: :blue: :arrow: :red: :arrow: :blue:)
  'meow-164': 'Red Blue Yellow Pink Purple',
  // Levels 12, 13: Yellow & Blue (New color: Yellow!)
  'meow-165': 'Yellow Blue Red Pink Purple',
  'meow-166': 'Yellow Blue Red Pink Purple',

  // Chapter 2: TargetBox (Levels 14 - 19)
  'meow-094': 'Red Blue Yellow Pink Purple', // Tutorial: "hộp xanh... vuốt mèo xanh vào hộp xanh"
  'meow-018': 'Red Blue Yellow Pink Purple', // Tip: "Mèo hồng đến chơi!... D = Pink"
  'meow-019': 'Red Blue Yellow Pink Purple',
  'meow-020': 'Red Blue Yellow Pink Purple', // Tutorial: "hộp xanh"
  'meow-021': 'Purple Red Yellow Pink Blue', // Intro Purple!
  'meow-022': 'Purple Red Yellow Pink Blue',

  // Chapter 3: Portal (Levels 20 - 25)
  'meow-130': 'Red ElectricBlue Yellow Pink Purple', // Tutorial: "mèo đỏ"
  'meow-141': 'Red ElectricBlue Yellow Pink Purple',
  'meow-142': 'Green WarmPink Teal Orange BabyBlue',
  'meow-143': 'Green WarmPink Teal Orange BabyBlue',
  'meow-144': 'Lime Purple Orange Green Yellow',
  'meow-145': 'Lime Purple Orange Green Yellow',

  // Chapter 4: TwoColor (Levels 26 - 31)
  'meow-146': 'Blue Red Yellow Pink Purple', // Tutorial: "mèo vàng, mèo xanh, mèo đỏ"
  'meow-147': 'Blue Red Yellow Pink Purple',
  'meow-148': 'Teal WarmPink Lime BabyBlue Purple',
  'meow-149': 'Teal WarmPink Lime BabyBlue Purple',
  'meow-150': 'BabyBlue Orange ElectricBlue WarmPink Yellow',
  'meow-151': 'BabyBlue Orange ElectricBlue WarmPink Yellow',

  // Chapter 5: Sleep (Levels 32 - 36)
  'meow-038': 'Red Blue Yellow Pink Purple', // Tutorial: "Đặt mèo đỏ sát bên..."
  'meow-039': 'Red Blue Yellow Pink Purple',
  'meow-102': 'ElectricBlue Orange Yellow Red Purple', // Intro ElectricBlue & Orange! (A = ElectricBlue, B = Orange, C = Yellow)
  'meow-046': 'Orange Blue Yellow Pink ElectricBlue', // Level 29: A = Orange, E = ElectricBlue (high contrast, distinct from sleeping cat)
  'meow-103': 'Red Blue Yellow Pink Purple', // Tip: "Ba bé ngủ đều là mèo xanh"

  // Chapter 6: Virus (Levels 37 - 41)
  'meow-042': 'Red Blue Yellow Pink Purple', // Tutorial: "mèo đỏ", "mèo xanh vào virus"
  'meow-119': 'Red Blue Yellow Pink Purple',
  'meow-044': 'BabyBlue WarmPink Yellow Pink Purple', // Intro BabyBlue!
  'meow-120': 'BabyBlue WarmPink Yellow Pink Purple',
  'meow-121': 'WarmPink Green Yellow Pink Purple', // Intro WarmPink!

  // Chapter 7: Ice (Levels 42 - 47)
  'meow-105': 'Red Blue Yellow Pink Purple', // Tutorial: "mèo đỏ", "mèo xanh"
  'meow-087': 'Teal ElectricBlue Yellow Pink Purple',
  'meow-107': 'Teal ElectricBlue Yellow Pink Purple',
  'meow-108': 'Lime BabyBlue Orange Red Purple',
  'meow-109': 'Lime BabyBlue Orange Red Purple',
  'meow-106': 'WarmPink Orange Green Yellow Blue',

  // Chapter 8: Toxic (Levels 48 - 51)
  'meow-033': 'Red Blue Yellow Pink Purple', // Tutorial: "đẩy mèo xanh vào ô độc"
  'meow-110': 'Teal WarmPink Lime BabyBlue Purple',
  'meow-099': 'Lime Orange Yellow Pink Purple', // Intro Lime!
  'meow-037': 'Lime Orange BabyBlue Purple Red',

  // Chapter 9: Membrane (Levels 52 - 57)
  'meow-097': 'Red Blue Yellow Pink Purple', // Tutorial: "vuốt mèo xanh xuống... màng"
  'meow-030': 'Red Blue Yellow Pink Purple', // Tutorial: "mèo xanh"
  'meow-098': 'Lime Orange Yellow Pink Purple',
  'meow-101': 'Lime Orange Yellow Pink Purple',
  'meow-100': 'Teal WarmPink Yellow Pink Purple', // Intro Teal!
  'meow-031': 'Lime Orange BabyBlue Red Purple', // Tip: "Mèo tím đến chơi!" (E = Purple)

  // Back levels with tips:
  'meow-062': 'Red Blue Yellow Pink Purple', // Tip: "Lần đầu 4 màu cùng lúc! Chỉ có 1 bé hồng"
  'meow-066': 'Green Orange Teal Yellow Purple', // Tip: "Giờ đủ 5 màu cùng lúc! Bé tím chỉ có 1" (E = Purple)
};

// Build final list
const orderedList = [];

// 1. Onboarding Chapter 1 (Levels 1 - 13)
ONBOARDING_IDS.forEach(id => {
  orderedList.push({ id, blockNum: 1 });
});

// 2. Mechanic Chapters (Levels 14 - 53)
MECHANIC_CHAPTERS.forEach(ch => {
  ch.ids.forEach(id => {
    orderedList.push({ id, blockNum: ch.chapter });
  });
});

// 3. Remaining Advanced/Master levels (Levels 54 - 96)
remaining.forEach((r, idx) => {
  const blockNum = idx < 20 ? 9 : 10;
  orderedList.push({ id: r.id, blockNum });
});

const header = `// Meowtosis level catalog, in play order. Generated by tools/export-unity.js from prototype/meowtosis.html:
// do not edit by hand. Format: docs/architecture.md, section "Level data".
`;

const formattedBlocks = orderedList.map((entry, index) => {
  const levelNum = index + 1;
  const originalBlock = map.get(entry.id);

  let block = originalBlock.trim();
  block = block.replace(/^\/\/ Level \d+[\r\n]*/, '');

  const isOriginalBlock1 = entry.id.startsWith('meow-15') || entry.id.startsWith('meow-16') || entry.id === 'meow-001';
  const effectiveBlock = isOriginalBlock1 ? 1 : entry.blockNum;
  block = block.replace(/block \d+/, `block ${effectiveBlock}`);

  let palette = SPECIFIC_PALETTE[entry.id];
  if (!palette) {
    const rotIndex = Math.floor((levelNum - 54) / 2) % ROTATIONS.length;
    palette = ROTATIONS[rotIndex < 0 ? 0 : rotIndex];
  }

  if (block.includes('palette ')) {
    block = block.replace(/palette [^\r\n]+/, `palette ${palette}`);
  } else {
    block = block.replace(/(par \d+)/, `$1\npalette ${palette}`);
  }

  // If Level 5 (meow-158): remove trap & tutorial so player plays naturally without onboarding lock; set solution to 0 1 down & 1 2 up
  if (entry.id === 'meow-158') {
    block = block.replace(/trap [^\r\n]+[\r\n]*/, '');
    block = block.replace(/tutorial [^\r\n]+[\r\n]*/, '');
    block = block.replace(/solution 1 2 left[\r\n]*solution 1 2 up/, 'solution 0 1 down\nsolution 1 2 up');
  }

  // If Level 8 (meow-161): update tutorial text to not reference :blue::blue: since cats are green
  if (entry.id === 'meow-161') {
    block = block.replace(
      /tutorial 0 0 right :red: :arrow: :blue::blue: :arrow: :empty:/,
      'tutorial 0 0 right :red: :arrow: :empty:'
    );
  }

  // If Level 14 (meow-094 - TargetBox intro): only 1 boxchat, remaining step is hand-only
  if (entry.id === 'meow-094') {
    block = block.replace(/tutorial 1 0 right[^\r\n]*/, 'tutorial 1 0 right');
  }

  // If meow-130 (Portal intro): only 1 boxchat at step 1, remaining steps are hand-only
  if (entry.id === 'meow-130') {
    block = block.replace(/(tutorial 0 0 right[^\r\n]*[\r\n]+){3}/,
      'tutorial 0 0 right Vòng xoáy là cổng dịch chuyển: vuốt mèo để chui qua cổng sang bên kia!\ntutorial 0 0 right\ntutorial 0 0 right\n');
  }

  // If meow-146 (TwoColor intro): only 1 boxchat, remaining steps are hand-only
  if (entry.id === 'meow-146') {
    block = block.replace(/tutorial 1 0 right Hộp cần[^\r\n]*/, 'tutorial 1 0 right Mèo hai màu sẽ đổi màu sau mỗi nước: vuốt sang phải!');
    block = block.replace(/tutorial 0 0 right Chấm nhỏ[^\r\n]*/, 'tutorial 0 0 right');
    block = block.replace(/tutorial 1 0 up Giờ nó[^\r\n]*/, 'tutorial 1 0 up');
    block = block.replace(/tutorial 0 1 right Hộp đã[^\r\n]*/, 'tutorial 0 1 right');
  }

  // If meow-038 (Sleep intro): only 1 boxchat, remaining step is hand-only
  if (entry.id === 'meow-038') {
    block = block.replace(/tutorial 2 1 right Mèo đã[^\r\n]*/, 'tutorial 2 1 right');
  }

  // If meow-042 (Virus intro): only 1 boxchat, remaining steps are hand-only
  if (entry.id === 'meow-042') {
    block = block.replace(/tutorial 1 0 up Nhìn mắt[^\r\n]*/, 'tutorial 1 0 up Virus sẽ lan theo hướng mắt nhìn. Đè mèo lên để diệt virus!');
    block = block.replace(/tutorial 2 0 down Thấy chưa[^\r\n]*/, 'tutorial 2 0 down');
    block = block.replace(/tutorial 3 0 right Còn virus[^\r\n]*/, 'tutorial 3 0 right');
  }

  // If meow-105 (Ice intro): only 1 boxchat, remaining steps are hand-only
  if (entry.id === 'meow-105') {
    block = block.replace(/tutorial 0 0 right Vuốt mèo[^\r\n]*/, 'tutorial 0 0 right');
    block = block.replace(/tutorial 0 2 down Lấp nốt[^\r\n]*/, 'tutorial 0 2 down');
  }

  // If meow-033 (Toxic intro): step 1 hand-only (tránh che mèo đỏ ở ô 0,0), step 2 boxchat giải thích ô độc, step 3 hand-only
  if (entry.id === 'meow-033') {
    block = block.replace(/tutorial 0 1 down Nhân đôi[^\r\n]*/, 'tutorial 0 1 down');
    block = block.replace(/tutorial 0 0 right Mèo vào[^\r\n]*/, 'tutorial 0 0 right Mèo vào ô độc sẽ biến mất: đẩy mèo vào để dọn chỗ!');
    block = block.replace(/tutorial 0 1 down Lấp nốt[^\r\n]*/, 'tutorial 0 1 down');
  }

  // If meow-097 (Membrane intro): only 1 boxchat, remaining step is hand-only
  if (entry.id === 'meow-097') {
    block = block.replace(/tutorial 1 1 left[^\r\n]*/, 'tutorial 1 1 left');
  }

  return `\n// Level ${levelNum}\n${block}\n`;
});

fs.writeFileSync(CATALOG, header + formattedBlocks.join(''), 'utf8');
console.log('Successfully applied ordering with full 13 onboarding levels intact!');
