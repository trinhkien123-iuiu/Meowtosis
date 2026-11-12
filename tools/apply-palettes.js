const fs = require('fs');
const path = require('path');

const CATALOG = path.join(__dirname, '../unity/Assets/_Project/Levels/LevelCatalog.txt');
let content = fs.readFileSync(CATALOG, 'utf8');

const CLASSIC = 'Red Blue Yellow Pink Purple';
const BLUE_FIRST = 'Blue Red Yellow Pink Purple';
const YELLOW_FIRST = 'Yellow Blue Red Pink Purple';

const ROTATIONS = [
  'Green Orange Teal Yellow Purple',
  'Teal WarmPink Lime BabyBlue Purple',
  'Lime Purple Orange Green Yellow',
  'BabyBlue Orange ElectricBlue WarmPink Yellow',
  'ElectricBlue Yellow WarmPink Green Purple',
  'Green WarmPink Teal Orange BabyBlue',
  'Teal Yellow Purple Lime WarmPink',
  'Lime Orange BabyBlue Purple Red',
  'ElectricBlue WarmPink Teal Yellow Green',
  'Green Purple Orange BabyBlue WarmPink',
  'BabyBlue WarmPink Yellow Lime ElectricBlue',
  'Lime ElectricBlue Orange Purple Green',
];

// Specific dialog-tied levels that require specific colors
const SPECIFIC = {
  1: CLASSIC,       // ":red:"
  2: BLUE_FIRST,
  3: YELLOW_FIRST,
  4: CLASSIC,       // ":red::red::red:"
  5: CLASSIC,       // "mèo đỏ"
  8: CLASSIC,       // ":red:", ":blue:"
  11: BLUE_FIRST,   // ":blue:", ":red:"
  23: BLUE_FIRST,   // "hộp xanh"
  38: BLUE_FIRST,   // "mèo xanh"
  44: BLUE_FIRST,   // "mèo xanh"
  51: CLASSIC,       // "mèo đỏ"
  56: CLASSIC,       // "mèo đỏ", "mèo xanh"
  61: BLUE_FIRST,   // "mèo xanh"
  66: CLASSIC,       // "mèo đỏ", "mèo xanh"
  80: CLASSIC,       // "mèo đỏ"
  87: BLUE_FIRST,   // "mèo vàng", "mèo xanh", "mèo đỏ"
};

function paletteForLevel(levelNum) {
  if (SPECIFIC[levelNum]) return SPECIFIC[levelNum];
  const rotIndex = Math.floor(levelNum / 2.5) % ROTATIONS.length;
  return ROTATIONS[rotIndex];
}

// Split by level sections
const parts = content.split(/(?=\/\/ Level \d+)/);
const header = parts[0];
const levelBlocks = parts.slice(1);

const updatedBlocks = levelBlocks.map(block => {
  const match = block.match(/\/\/ Level (\d+)/);
  if (!match) return block;
  const levelNum = parseInt(match[1], 10);
  const palette = paletteForLevel(levelNum);

  // Check if palette already in block
  if (block.includes('\npalette ')) {
    return block.replace(/\npalette [^\r\n]+/, '\npalette ' + palette);
  }

  // Insert palette after "par <num>"
  return block.replace(/(\npar \d+)/, '$1\npalette ' + palette);
});

fs.writeFileSync(CATALOG, header + updatedBlocks.join(''), 'utf8');
console.log('Successfully applied 5-color rotating palettes to all 96 levels!');
