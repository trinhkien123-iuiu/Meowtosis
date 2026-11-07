# Meowtosis
Game Ý tưởng mưới

Game giải đố vuốt cho mèo nhân đôi, lấp kín ổ (cảm hứng cơ chế từ *Mitoosis* của Le Slo). Prototype web có 76 level xếp từ dễ đến khó theo 7 khối cơ chế; bản hoàn chỉnh đang làm bằng Unity 2022.3 trong `unity/` (kiến trúc: [docs/architecture.md](docs/architecture.md)).

## Chạy (cần Node.js, không cần cài gói)

| Lệnh | Việc |
|---|---|
| `npm start` | Máy chủ chơi thử: http://127.0.0.1:8765/meowtosis.html |
| `npm run lan` | Cho điện thoại cùng Wi-Fi vào chơi |
| `npm test` | Test luật + kiểm tra toàn bộ level |
| `npm run test:unity` | Chạy test C# của bản Unity bằng .NET, không cần mở Unity (cần .NET SDK 8+) |
| `npm run export` | Xuất level ra `data/levels.json` và `unity/Assets/_Project/Levels/LevelCatalog.txt` |
| `npm run analyze` | Bảng chỉ số độ khó |
| `npm run gen -- tools/jobs/<file>.json` | Sinh level mới |
| `npm run difficulty`, `npm run order` | Đo độ khó, xem thứ tự dễ → khó đề xuất (`node tools/order.js --write` để ghi) |

## Cấu trúc

```text
prototype/   game web meowtosis.html, sfx/, archive/ (bản cũ), shikacat/ (asset tạm)
tools/       bộ giải, bộ sinh level, test, máy chủ; dotnet-tests/ chạy test C# ngoài Unity;
             make-placeholder-art.py, make-ui-art.py, make-sfx.py sinh hình và tiếng tạm cho Unity
data/        levels.json xuất từ game, level-ids.json (id cố định của từng level)
docs/        architecture.md (kiến trúc Unity), mechanics.md (luật chuẩn mọi cơ chế), unity-handoff.md, level-design.md
unity/       project Unity 2022.3 (URP 2D), code trong Assets/_Project
```

`prototype/shikacat/` là asset tách từ game Shikacat (Tripledot), dùng cho cả prototype và bản Unity vì đây là dự án cá nhân, phi thương mại. Tiếng mèo Mixkit (`prototype/sfx/*.mp3`): nguồn và giấy phép ở `prototype/sfx/NGUON.txt`. Cả hai được lưu trong git; repo nên để riêng tư.
