// Xuất mọi level kèm chương, số nước tối ưu (par) và một lời giải ra JSON: dữ liệu để nhập vào Unity,
// đồng thời là "đáp án vàng" để test bản C# (cùng par, chạy lại lời giải phải thắng).
//   npm run export                              -> ghi data/levels.json (check.js báo lỗi nếu file này cũ hơn game)
//   node tools/export-levels.js [đường dẫn]     -> ghi ra file khác; không có đường dẫn thì in ra màn hình
const fs = require('fs');
const path = require('path');
const { loadLogic, exportLevels } = require('./lib');

const text = JSON.stringify(exportLevels(loadLogic()), null, 1) + '\n';
const out = process.argv[2];
if (!out) process.stdout.write(text);
else {
  fs.mkdirSync(path.dirname(path.resolve(out)), { recursive: true });
  fs.writeFileSync(out, text);
  console.log(`Đã ghi ${JSON.parse(text).length} level vào ${out}`);
}
