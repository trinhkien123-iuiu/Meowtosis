# Cat Café Jam — prototype (creative day XGame)

File: `prototype/cat-cafe-jam.html` (một file HTML, chạy bằng static server; asset lấy từ `prototype/shikacat/` và `prototype/sfx/`).

```
cd prototype && python -m http.server 8765   # rồi mở http://localhost:8765/cat-cafe-jam.html
?level=N  để nhảy màn
```

## Ý tưởng
Bus Jam × Mèo × Quán cà phê. Thị trường: Bus Fever Party / Arrow Puzzle (tap-to-clear), Meowdoku (cat theme), Gardenscapes (meta).

## Luật
- Mèo xếp thành các cột; chỉ mèo **hàng đầu** của mỗi cột chạm được. Cột dồn lên khi mèo đi.
- Bàn đang mở có màu X, 3 ghế. Chạm mèo màu X -> nhảy lên ghế. Khác màu -> ra **ghế chờ** (4–5 chỗ).
- Đủ 3 mèo -> ăn xong, bàn rời đi, bàn kế trượt vào; mèo ở ghế chờ cùng màu tự nhảy lên (có thể chain).
- Thắng: phục vụ hết bàn. Thua: ghế chờ đầy và không mèo hàng đầu nào hợp bàn hiện tại.
- Combo: phục vụ liên tiếp trong 5 giây.

## Hệ thống màn (22 màn, dữ liệu ở `LEVELS` trong file)
Màn chọn từ trang **Chọn màn** (bấm `☰ n` trên HUD để quay lại): lưới 4 cột, khóa/mở theo tiến độ, 3 sao, huy hiệu đánh dấu
mechanic mới (không chia chương), link "Demo: mở khóa tất cả" hoặc `?all=1`. Tiến độ lưu `localStorage` key `ccj-prog`.
Sao = số lần phải ngồi ghế chờ so với lời giải của solver (≤ +1: 3★, ≤ +4: 2★, còn lại 1★).

| Màn | Nội dung |
|---|---|
| 1–5 | Cơ bản: 3→5 màu, 9→24 mèo, 5 ghế chờ |
| 6–9 | 🍽 **Bàn 2 / 4 chỗ** (màn 6 giới thiệu), ghế chờ giảm còn 4 |
| 10–12 | ❓ **Mèo bí ẩn**: bóng đen "?", lộ màu khi lên hàng đầu |
| 13–16 | 💤 **Mèo ngủ**: dậy sau n bàn được phục vụ (bàn thứ n được mở đủ chỗ) |
| 17–22 | Trộn mechanic, 3–4 ghế chờ, 6–7 màu, tới 44 mèo |

Mechanic mới ảnh hưởng solver: sức chứa từng bàn, mèo ngủ chỉ tap được khi `wake <= số bàn đã đầy`; mèo bí ẩn chỉ là che màu
(không đổi luật). Thua khi không còn nước đi hợp lệ (`canMove()`), không chỉ khi ghế chờ đầy.

## Gamefeel đã làm
Anticipation squash trước khi nhảy, nhảy theo cung + stretch khi bay + squash khi đáp (spring), mèo thở idle,
âm "đặt ghế" tăng pitch theo mèo thứ 1/2/3, mèo đổi mặt trái tim khi ăn xong + tim/sao/ring/confetti,
popup chữ nảy, rung màn hình khi combo/ghế chờ đầy, bàn spring trượt vào, gợi ý (vòng sáng) sau 3.5 giây không chạm.

## Đảm bảo giải được
`generate()` sinh màn ngẫu nhiên rồi chạy DFS `solve()` (giới hạn 120k node) cùng luật game; chỉ nhận màn có lời giải
(thử tối đa 400 lần). Lời giải nằm ở `G.solution` (dùng cho bot test: `__ccd.tapCol(i)`).

## Số đo / kiểm thử
- Màn 1: 3×3 mèo, 3 màu, 5 ghế chờ. Màn 4: 5×6 mèo, 5 màu, 10 bàn.
- Bot chạy theo lời giải màn 4: 30 lượt -> 10/10 bàn -> popup thắng. Kiểm thua bằng `__ccd.step` -> hiện thua đúng sau 0.5s.

- Bot theo lời giải chạy thắng cả 22 màn trong game thật (`__ccd.goto(n)`, `tapCol`, `step`). Sinh màn mất < 110ms/màn.
- Lỗi đã sửa: mèo đang bay tới ghế chờ đúng lúc bàn cùng màu mở -> bị bỏ sót, kẹt ở ghế chờ (solver coi như đã lên bàn).
  Sửa trong `onLand`: nếu bàn đang mở cùng màu và còn chỗ thì nhảy thẳng lên bàn.

## Bài học
- Trình duyệt ẩn thì requestAnimationFrame dừng: test logic bằng `__ccd.step(dt, n)`.
- Đừng set màu bàn ngoài bảng màu khi test (drawTable sẽ lỗi).

## Việc tiếp theo (nếu làm tiếp)
Meta trang trí quán (mở đồ nội thất mỗi màn), booster (xáo hàng, thêm ghế chờ), mèo bị khóa/ngủ, art riêng cho bàn/bánh,
cân độ khó bằng số nước giải ngắn nhất.
