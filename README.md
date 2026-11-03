# Meowtosis
Game Ý tưởng mưới

Game giải đố vuốt cho mèo nhân đôi, lấp kín ổ (cảm hứng cơ chế từ *Mitoosis* của Le Slo). Hiện là prototype web, 61 level, 6 chương; bản hoàn chỉnh sẽ làm bằng Unity trong cùng repo.

## Chạy (cần Node.js, không cần cài gói)

| Lệnh | Việc |
|---|---|
| `npm start` | Máy chủ chơi thử: http://127.0.0.1:8765/meowtosis.html |
| `npm run lan` | Cho điện thoại cùng Wi-Fi vào chơi |
| `npm test` | Test luật + kiểm tra toàn bộ level |
| `npm run export` | Xuất level ra `data/levels.json` (dữ liệu cho Unity) |
| `npm run analyze` | Bảng chỉ số độ khó |
| `npm run gen -- tools/jobs/<file>.json` | Sinh level mới |

## Cấu trúc

```text
prototype/   game web meowtosis.html, sfx/, archive/ (bản cũ), shikacat/ (asset tạm)
tools/       bộ giải, bộ sinh level, test, máy chủ
data/        levels.json xuất từ game
docs/        unity-handoff.md, level-design.md
unity/       (sẽ tạo) project Unity
```

`prototype/shikacat/` là asset tách từ game Shikacat (Tripledot), chỉ dùng tạm cho prototype phi thương mại, không đưa vào bản phát hành. Tiếng mèo Mixkit (`prototype/sfx/*.mp3`): nguồn và giấy phép ở `prototype/sfx/NGUON.txt`. Cả hai được lưu trong git; repo nên để riêng tư.
