# Game phụ (cozy): 4 ý tưởng và bản thử

Bản chính (Meowtosis) khó và căng đầu, nên game phụ cần ngược lại: dễ hiểu, thư giãn, puzzle nhẹ. Bốn bản thử nằm trong `backup-prototypes/`, mở `index.html` bằng trình duyệt là chơi được (không cần máy chủ). Chỉ dùng emoji và hình tự vẽ bằng CSS/SVG, không có asset ripped nào.

| # | Ý tưởng | File | Luật gói gọn | Màn |
|---|---|---|---|---|
| 1 | Mèo tắm nắng | `sunbeam.html` | Dời mèo và chậu cây để mèo thích nắng nằm trong nắng, mèo thích bóng nằm trong bóng | 7 màn sinh sẵn |
| 2 | Xếp tổ mèo | `nest.html` | Xoay và xếp các mèo (hình đa ô) vào hộp cho vừa khít | Vô hạn, sinh theo số màn |
| 3 | Vườn mèo lát gạch | `garden.html` | Đặt mảnh lục giác, cạnh khớp loại đất thì được điểm, khớp hết cạnh thì thưởng | Ván 24 mảnh, 3 sao |
| 4 | Cuộn len nối đôi | `yarn.html` | Nối cuộn len với mèo cùng màu, không cắt nhau, phủ kín bàn | Vô hạn, sinh theo số màn |

## Cách sinh màn và kiểm tra

- **Mèo tắm nắng**: luật ở `sunbeam-core.js` (dùng chung cho trang và bộ sinh). `tools/gen-sunbeam.js` sinh ngẫu nhiên theo seed, giữ màn có lời giải, rồi ghi `sunbeam-levels.js`. Từ màn 3 có chậu cây thì bắt buộc phải dùng chậu (bàn không chậu không đủ chỗ bóng).
- **Xếp tổ mèo**: khoét vài ô rìa bàn, chia phần còn lại thành các mảnh liền nhau (3 đến 5 ô), rồi xoay ngẫu nhiên từng mảnh. Vì mảnh lấy từ chính bàn nên luôn có lời giải bằng phép xoay. Đã dùng bộ giải vét cạn thử 10 màn đầu: đều giải được.
- **Vườn mèo lát gạch**: mảnh sinh ngẫu nhiên (1 đến 3 đoạn cạnh cùng loại, 22% có mèo). Mốc sao đo bằng bot: đặt bừa 12 đến 26 điểm, đặt tối ưu từng mảnh khoảng 120, nên mốc là 50 / 75 / 100.
- **Cuộn len**: tạo đường đi phủ kín bàn bằng thuật toán backbite rồi cắt thành các đoạn (mỗi đoạn dài ít nhất 3 ô), hai đầu mỗi đoạn là cuộn len và mèo. Đã mô phỏng nối theo lời giải cho 8 màn đầu: đều thắng, kéo chuột nhanh qua nhiều ô cũng đúng.

## Nhận xét thiết kế (rút ra khi làm bản thử)

- **Mèo tắm nắng** dễ hiểu nhất nhưng **quá lỏng** khi mèo dời tự do: số cách xếp đo được từ vài chục (màn nhỏ) tới hàng triệu (màn lớn), nên gần như đặt đâu cũng được. Muốn thành puzzle thật cần thêm ràng buộc, ví dụ nắng di chuyển theo giờ, chỉ dời được từng ô, hoặc mèo cũng đổ bóng.
- **Xếp tổ mèo** và **Cuộn len** có lời giải ít, cảm giác "đã" rõ nhất, nhưng cùng thể loại với nhiều game có sẵn, cần nét riêng để nổi.
- **Vườn mèo lát gạch** không thua cuộc, hợp chơi thả lỏng, nhưng chưa có mục tiêu gì ngoài điểm.

## Chạy lại bộ sinh

```
node backup-prototypes/tools/gen-sunbeam.js
```

## Ý 5: Mèo trượt (Tap Master + khử 3 cùng màu)

Ý mới ghép từ hai game: đầu vào một chạm của họ Tap Master (chạm khối có mũi tên thì khối đi theo mũi tên) với luật khử 3 cùng màu của Meowtosis. Trang `slide.html`, lõi luật `slide-core.js`, bộ sinh màn `tools/gen-slide.js` (ra `slide-levels.js`).

**Luật (do mình đặt, chưa có game gốc để đối chiếu):**
- Mỗi mèo có màu và một mũi tên cố định. Chạm một mèo: nó trượt thẳng theo mũi tên tới khi gặp mép bàn, tường hoặc mèo khác. Mèo bị chắn ngay thì không đi và không tính nước.
- Sau mỗi nước, mọi hàng hoặc cột có từ 3 mèo cùng màu liền nhau trở lên biến mất. Không có trọng lực, mèo còn lại đứng yên.
- Thắng khi hết mèo. Sao theo số nước so với tối ưu: tối ưu là 3 sao, thừa tối đa 2 nước là 2 sao.
- Có Hoàn tác không giới hạn và Gợi ý (nước đầu của lời giải ngắn nhất). Hết đường thắng thì hiện lời nhắn nhẹ nhàng, không thua cứng.

**Vì sao duyệt được hết trạng thái:** mỗi mèo chỉ đi một chiều nên đồ thị trạng thái không có vòng. Bộ giải duyệt toàn bộ, cho số nước tối ưu chính xác và tỉ lệ "ngõ cụt" của màn.

**Sinh màn:** đặt mèo ngẫu nhiên gần như không bao giờ giải được ở bàn lớn (đo: 0/400 ứng viên giải được ở bàn 5x5 với 12 mèo), nên bộ sinh dùng leo đồi: đột biến hướng hoặc vị trí một mèo, giữ lại nếu số mèo dọn được nhiều nhất không giảm, dừng khi giải được. Mỗi màu phải có số mèo chia hết cho 3, vì dư 1 hoặc 2 con thì con lẻ không bao giờ khử được. 7 màn, từ 4x4 (3 mèo) tới 6x6 (9 mèo) và 6x5 (12 mèo).

**Kiểm tra:** chơi tự động theo gợi ý trên trang thật, cả 7 màn đều thắng đúng số nước tối ưu.

**Nhận xét thiết kế:**
- Số nước tối ưu thấp (3 đến 11) và số trạng thái nhỏ (8 đến 846): mỗi lượt chỉ có vài lựa chọn, ván ngắn, hợp mobile nhưng chưa nhiều chiều sâu. Cần thử bàn lớn hơn, thêm loại ô (băng, cổng) hoặc mèo đặc biệt để tăng độ sâu.
- Màn 6 có 75% trạng thái là ngõ cụt, dễ làm người chơi casual nản dù đã có Hoàn tác và Gợi ý.
- Màu đỏ, xanh, vàng, lục dễ nhầm với người mù màu: cần thêm hình khác nhau nếu làm thật.
