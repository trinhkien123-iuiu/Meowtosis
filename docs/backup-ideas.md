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

## Ý 5 làm lại: Tàu mèo (2026-11-08)

Bản làm lại của "Mèo trượt": bãi đường ray, mỗi chú mèo ngồi trên một toa tàu. Trang `rail.html` (giao diện), `rail-game.js` (diễn hoạt, booster, popup, âm thanh), lõi luật `rail-core.js`, bộ sinh màn `tools/gen-rail.js` (ra `rail-levels.js`), phép thử luật `tools/test-rail.js`. Hình mèo, nền, hình thắng/thua và tiếng UI lấy từ `prototype/shikacat` (hình tạm, không phát hành); toa, ray, ghi, toa hàng tự vẽ bằng CSS/SVG; tiếng tàu (xình xịch, lạch cạch, còi, phanh, gạt ghi) tự tổng hợp bằng WebAudio. Mở qua máy chủ tĩnh: `python -m http.server` ở thư mục gốc repo rồi vào `/backup-prototypes/rail.html`.

### Luật (do mình đặt, chưa có game gốc để đối chiếu)
- Chạm toa: toa chạy theo hướng đang quay mặt, đi theo đường ray (qua chỗ cong thì rẽ), tới khi hết ray (cản đỏ trắng), gặp toa khác hoặc ra mép bàn. Toa dừng vẫn quay mặt theo hướng đang chạy. Bị chắn ngay thì không đi, không tính chuyến.
- 3 toa mèo cùng màu nối đuôi nhau **dọc một đường ray** (đi qua chỗ cong cũng được) thì móc lại và rời bến. Toa sát nhau mà không có ray nối thẳng giữa hai ô thì không móc.
- Thắng khi hết toa mèo (toa hàng được ở lại). Mỗi màn có **giới hạn chuyến** = số nước ít nhất + 1 hoặc 2; hết chuyến là thua (mua thêm 3 chuyến bằng 60 xu, hoặc chơi lại). Sao: đúng số nước ít nhất 3 sao, thừa 1 nước 2 sao.
- Vì toa trên cùng một ray không vượt qua nhau, **thứ tự toa trên một tuyến không đổi được**; mọi cơ chế dưới đây là cách để đổi thứ tự hoặc đổi tuyến, nên chúng là lõi chiến thuật chứ không phải trang trí.

### Cơ chế (mỗi khối có thẻ "Cơ chế mới" ở màn đầu)
| Cơ chế | Luật | Vai trò |
|---|---|---|
| Đường ray | ray thẳng, cản ở đầu cuối | nền tảng; dạy thứ tự chạm |
| Giao lộ `+` | toa đi thẳng qua, không rẽ | chỗ duy nhất để sang tuyến khác |
| Ray cong | toa rẽ và quay mặt theo hướng mới; đoàn nối qua chỗ cong vẫn tính | đổi trục ngang ↔ dọc, vòng chữ U quay đầu |
| Toa hàng | toa xám chở thùng, không có mèo, không bao giờ rời bến | vật cản phải dời đi, hoặc chỗ chặn có ích để toa khác dừng đúng chỗ |
| Bẻ ghi | chạm cần gạt vàng để đổi nhánh (tốn 1 chuyến); toa vào nhánh đóng bị chặn; có toa trên ghi thì không gạt được | chọn đường cho toa, nhưng tốn chuyến |

### Booster
Gợi ý (bàn tay chỉ nước đầu của lời giải ngắn nhất), Hoàn tác (lùi 1 chuyến), Cần cẩu (nhấc toa sang ô ray trống, không tốn chuyến; tạo được đoàn thì đoàn rời bến luôn), Quay đầu (đổi chiều chạy của một toa). Mỗi thứ có lượt miễn phí, hết thì mua 40 xu. Thắng được 15 + 10 x sao xu.

### Thiết kế màn: bắt buộc dùng cơ chế
Sơ đồ ray vẽ tay theo ý đồ từng khối (vòng chữ U, lưới giao lộ, ghi rẽ xuống tuyến dưới...), bộ sinh chỉ đặt toa rồi leo đồi tới khi thắng được. Một màn chỉ được giữ khi **gỡ cơ chế đi thì không thắng được**: cấm đi qua giao lộ / chỗ cong, cấm gạt ghi, đóng băng toa hàng (phải dời) hoặc bỏ toa hàng (nó là chỗ chặn có ích). Cùng lúc giữ số nước ít nhất trong khoảng mong muốn và tỉ lệ trạng thái ngõ cụt 20 đến 45% để người chơi phải tính. Bộ giải duyệt theo chiều rộng (có vòng vì gạt ghi qua lại được), đếm ngõ cụt bằng cách đi ngược từ các trạng thái thắng.

Kết quả bộ sinh (màn 1 viết tay):

```
màn 2 [rail]: 6 toa, tối ưu 3, giới hạn 5, 10 trạng thái, ngõ cụt 20%, bắt buộc {}
màn 3 [cross]: 6 toa, tối ưu 5, giới hạn 7, 8 trạng thái, ngõ cụt 25%, bắt buộc {"cross":true}
màn 4 [cross]: 9 toa, tối ưu 7, giới hạn 9, 309 trạng thái, ngõ cụt 54%, bắt buộc {"cross":true}
màn 5 [curve]: 3 toa, tối ưu 2, giới hạn 4, 14 trạng thái, ngõ cụt 0%, bắt buộc {"curve":true}
màn 6 [curve]: 6 toa, tối ưu 5, giới hạn 7, 53 trạng thái, ngõ cụt 30%, bắt buộc {"curve":true}
màn 7 [curve]: 9 toa, tối ưu 7, giới hạn 9, 496 trạng thái, ngõ cụt 43%, bắt buộc {"curve":true}
màn 8 [cargo]: 4 toa, tối ưu 3, giới hạn 5, 5 trạng thái, ngõ cụt 20%, bắt buộc {"cargo":"làm chỗ chặn"}
màn 9 [cargo]: 7 toa, tối ưu 6, giới hạn 8, 113 trạng thái, ngõ cụt 35%, bắt buộc {"cargo":"làm chỗ chặn"}
màn 10 [cargo]: 8 toa, tối ưu 5, giới hạn 7, 42 trạng thái, ngõ cụt 33%, bắt buộc {"cargo":"phải dời"}
màn 11 [switch]: 3 toa, tối ưu 4, giới hạn 6, 34 trạng thái, ngõ cụt 0%, bắt buộc {"switch":true}
màn 12 [switch]: 6 toa, tối ưu 7, giới hạn 9, 125 trạng thái, ngõ cụt 30%, bắt buộc {"switch":true}
màn 13 [switch]: 6 toa, tối ưu 9, giới hạn 11, 970 trạng thái, ngõ cụt 30%, bắt buộc {"switch":true}
màn 14 [mix]: 7 toa, tối ưu 8, giới hạn 9, 2853 trạng thái, ngõ cụt 40%, bắt buộc {"switch":true,"cargo":"phải dời"}
màn 15 [mix]: 10 toa, tối ưu 9, giới hạn 10, 700 trạng thái, ngõ cụt 44%, bắt buộc {"switch":true,"curve":true}
đã ghi 15 màn vào rail-levels.js
```

### Kiểm tra
- `node backup-prototypes/tools/test-rail.js`: 12 phép thử luật (rẽ, giao lộ, ghi chặn nhánh đóng, nối qua chỗ cong, toa hàng, bộ giải).
- Chơi tự động theo Gợi ý trên trang thật: cả 15 màn thắng đúng số nước ít nhất, popup thắng hiện. Thử booster Quay đầu, Hoàn tác, Cần cẩu, đường thua và nút +3 chuyến: chạy đúng, console không lỗi.
- Chưa chơi tay toàn bộ, chưa thử trên điện thoại thật.

### Nhận xét thiết kế
- Màn 3 và 8 có rất ít trạng thái (8 và 5): gần như chỉ có một đường, hợp làm màn dạy nhưng chưa có thử thách.
- Màn 13 và 14 có nhiều trạng thái nhất (970 và 2853) và giới hạn chuyến sát: đây là kiểu màn "phải tính" mà game hướng tới.
- Giới hạn chuyến = tối ưu + 1 ở khối cuối là chặt; nếu người chơi nản thì nới lên + 2 và để sao phân biệt.
- Chưa có: hầm (cổng), đèn tín hiệu, đầu máy kéo cả đoàn, chỉ tiêu ga, toa hai màu. Đỏ và xanh lá vẫn khó với người mù màu: cần hoa văn hoặc số trên toa.

## Tàu mèo: bản mở rộng cho creative day (2026-11-09)

Thêm 2 cơ chế mới và nút Chờ, nâng từ 15 lên 29 màn (10 khối nhỏ). Lõi luật vẫn ở `rail-core.js`, phép thử `tools/test-rail.js` (nay 21 phép thử), bộ sinh `tools/gen-rail.js`.

### Hai cơ chế mới
| Cơ chế | Luật | Vì sao hay |
|---|---|---|
| **Đèn tín hiệu** (`G`, dữ liệu `gates`) | ô ray có thanh chắn sọc đỏ trắng. Đèn đỏ (đóng) thì toa không vào được; xanh (mở) thì chạy qua. **Mỗi nước đi** (vuốt toa, gạt ghi, hoặc **Chờ**) làm mọi đèn đổi trạng thái, sau khi đã xét đoàn tàu rời bến | biến thời gian thành tài nguyên: phải canh nhịp theo số chuyến, kể cả chuyến của toa khác. Hai đèn lệch pha làm một vòng ray chỉ có một cửa mở mỗi lúc |
| **Hầm xuyên núi** (`tunnels`) | cửa hầm là cửa ray quay ra ngoài; toa chạy ra cửa này thì mờ đi, chui vào cửa kia và chạy tiếp theo ray ở đó. **Hầm không nối đoàn** (xem mục sửa lỗi bên dưới) | nối hai vùng ray xa nhau như cổng dịch chuyển của Meowtosis nhưng gắn vào đường ray, có thể bị toa khác chắn ngay cửa |
| **Chờ** (nút 🚦, chỉ ở màn có đèn) | không toa nào chạy, chỉ đổi đèn, tốn 1 chuyến | cho người chơi chủ động chờ đèn thay vì buộc phải chạy một toa khác làm hỏng thế |

Giao diện: đèn nảy lên và nháy tia sáng mỗi lần đổi, cửa hầm vẽ hai màu theo cặp, toa chui hầm kèm tiếng "vút xuống" rồi "vút ra" và tia sáng ở cả hai cửa; thẻ "Cơ chế mới" có hình mẫu đèn đỏ/xanh và hai cửa hầm.

### Chuẩn thiết kế màn (giữ nguyên "bắt buộc dùng")
- **Hầm:** cấm chui hầm (`noTunnel`) thì không thắng được.
- **Đèn:** đèn là vật cản chứ không phải con đường, nên chuẩn là "đèn thật sự tốn thêm chuyến": nếu cho đèn luôn mở (`gatesOpen`) thì số nước ít nhất phải **nhỏ hơn** số nước có đèn. Bảng dưới ghi số chuyến đèn làm tốn thêm.
- Bộ giải thêm nước Chờ vào mọi trạng thái; khoá trạng thái gồm cả đèn, nên số trạng thái gần gấp đôi ở màn có đèn.

### Kết quả bộ sinh (màn 16 trở đi)

| Màn | Khối | Toa | Tối ưu | Giới hạn | Trạng thái | Ngõ cụt | Bắt buộc |
|---|---|---|---|---|---|---|---|
| 14 | gate | 6 | 6 | 8 | 21 | 19% | {"gate":"+3 chuyến"} |
| 15 | gate | 6 | 6 | 8 | 94 | 30% | {"gate":"+2 chuyến"} |
| 16 | gate | 9 | 6 | 8 | 194 | 31% | {"gate":"+2 chuyến"} |
| 17 | tunnel | 3 | 2 | 4 | 13 | 0% | {"tunnel":true} |
| 18 | tunnel | 6 | 4 | 6 | 48 | 33% | {"tunnel":true} |
| 19 | tunnel | 9 | 6 | 8 | 185 | 43% | {"tunnel":true} |
| 20 | mix | 7 | 5 | 7 | 30 | 50% | {"tunnel":true} |
| 21 | mix | 6 | 4 | 6 | 175 | 49% | {"gate":"+2 chuyến"} |
| 22 | mix | 7 | 6 | 8 | 92 | 52% | {"tunnel":true} |
| 23 | mix | 6 | 6 | 8 | 105 | 46% | {"tunnel":true,"gate":"+1 chuyến"} |
| 24 | mix | 7 | 6 | 8 | 108 | 50% | {"gate":"+3 chuyến"} |
| 25 | mix | 9 | 7 | 9 | 125 | 44% | {"tunnel":true} |
| 26 | mix | 7 | 7 | 9 | 328 | 20% | {"tunnel":true} |
| 27 | mix | 6 | 9 | 11 | 180 | 41% | {"switch":true,"gate":"+1 chuyến"} |
| 28 | mix | 10 | 9 | 10 | 700 | 44% | {"switch":true,"curve":true} |
| 29 | mix | 7 | 8 | 9 | 2853 | 40% | {"switch":true,"cargo":"phải dời"} |

### Kiểm tra bản mở rộng
- `node backup-prototypes/tools/test-rail.js`: 21 phép thử (thêm đèn đóng/mở, đổi sau mỗi nước, Chờ, đoàn tàu xét trước khi đèn đổi, hầm: chui qua, không có hầm, cửa bị chắn, đoàn nối qua hầm, dữ liệu hầm sai).
- Tự chơi theo Gợi ý trên trang thật (gồm nước Chờ, gạt ghi, chui hầm): **cả 29 màn thắng đúng số chuyến tối ưu**, popup thắng hiện, console không lỗi. Lần chạy này bỏ hoạt ảnh và rút ngắn thời gian chờ (khung trình duyệt bị ẩn nên hoạt ảnh không chạy), nên phần hoạt ảnh của đèn và hầm mới chỉ xem bằng mắt ở màn 16 (đèn) và 20 (hầm) lúc trước đó; chưa xem trọn hoạt ảnh chui hầm cả đoàn tàu.
- Màn "đèn + giao lộ" (khối phối hợp) không sinh được dù đã nới điều kiện (đèn không làm tốn thêm chuyến ở các bàn thử); đã bỏ.

### Gợi ý kịch bản demo (khoảng 5 phút)
1. Màn 1 và 3: luật cơ bản và giao lộ (30 giây).
2. Màn 8 hoặc 10: toa hàng, vừa là vật cản vừa là chỗ chặn.
3. Màn 11: bẻ ghi, "tốn một chuyến để chọn đường".
4. Màn 14 (đèn tín hiệu): bấm Chờ để cho thấy thời gian là tài nguyên.
5. Màn 18: hầm, rồi màn 28 hoặc 29 để cho thấy mọi cơ chế gộp lại.
6. Dùng Gợi ý, Hoàn tác, Cần cẩu và Quay đầu khi bị kẹt; mở ☰ để cho xem 29 màn chia khối.

### Sửa lỗi và onboarding (2026-11-09)
- **Lỗi "mất mèo" ở màn 2:** khi chạm một toa bị chắn, hiệu ứng rung dùng `transform: translateX`, mà vị trí ô của toa cũng nằm trong `transform` (inline), nên trong 0,3 giây toa bị đè và nhảy về mép trái bàn (đo được: 108 px xuống 22 đến 34 px). Màn 2 có nhiều toa kẹt nên dễ gặp nhất. Đã đổi sang keyframes `shakeCar` dùng thuộc tính `translate` riêng (cộng thêm vào `transform`); đo lại, toa chỉ lệch ±6 px quanh vị trí cũ.
- **Onboarding 3 màn đầu:** `ONBOARD` trong `rail-game.js`. Mỗi màn đi theo lời giải ngắn nhất, mỗi nước có bàn tay chỉ và một lời dẫn, vài nước có câu luật kèm nút "Đã hiểu!". Chạm sai toa thì rung nhẹ và nhắc "Làm theo bàn tay chỉ nhé!". Trong onboarding ẩn hàng booster, xu và số chuyến, chỉ còn bàn chơi, số mèo, lời dẫn. Màn 1 (1 nước) dạy chạy và móc toa, màn 2 (3 nước) dạy thứ tự và toa chắn đường, màn 3 (5 nước) dạy giao lộ. Số bước phải khớp số nước của lời giải, nếu không thì màn chơi tự do.
- **Kiểm tra:** chạy tự động cả 3 màn onboarding (có tap sai, tap lúc đang hiện câu luật), rồi cả 29 màn: đều thắng đúng số chuyến tối ưu, console không lỗi. Hoạt ảnh được bỏ trong lần chạy tự động (khung trình duyệt của mình hay trễ), nên chuyển động rung và bàn tay chỉ mình xem riêng bằng cách đo vị trí, chưa xem liền mạch bằng mắt.

### Sửa lỗi "mất mèo" lần 2: hầm không nối đoàn (2026-11-09)
- **Triệu chứng:** ở vài màn có hầm, mèo biến mất mà không thấy đoàn tàu nào.
- **Nguyên nhân (luật, không phải hiển thị):** `trains()` cho phép đoàn nối qua hầm, nên ba toa cùng màu đứng cách xa nhau ở hai đầu hầm (giữa là cỏ) vẫn bị "móc" và biến mất tại chỗ. Bộ giải thấy được: ở màn 17 và màn 20 có nước tạo đoàn kiểu đó, và lời giải tối ưu của chúng còn dựa vào nó.
- **Luật mới:** hầm chỉ để chạy xuyên, **không nối đoàn**. Mỗi đầu hầm là một đoạn ray riêng, toa chỉ móc với toa sát bên cạnh trên bàn. Thêm phép thử, bỏ ví dụ "đoàn nối qua hầm" khỏi thẻ cơ chế và lời dẫn.
- **Tính lại các màn có hầm hoặc đèn:** `node backup-prototypes/tools/recheck-rail.js --write` (giải lại, kiểm tra hầm và đèn vẫn bắt buộc, cập nhật số nước tối ưu và giới hạn chuyến). Màn 17 từ 2 lên 3 nước. Màn 20 không còn thắng được nên đã sinh lại bằng `RAIL_ONLY=28 node backup-prototypes/tools/gen-rail.js` (tối ưu 5 nước). Sau đó kiểm tra: không còn đoàn nào gồm các ô không liền kề trong mọi lời giải.
- **Kiểm tra giao diện:** so khớp DOM với trạng thái sau mỗi nước (hiện đủ toa, đúng ô, không mờ, hình mèo tải được) khi chơi theo lời giải tối ưu (161 nước, 29 màn) và chơi ngẫu nhiên kèm Hoàn tác (màn 4 đến 29, 142 nước), cộng Hoàn tác sau khi đoàn rời bến, Cần cẩu, Quay đầu: không có lỗi. Thử cả cửa sổ 360x640 và 1280x560: bàn không bị cắt.

## Tàu mèo: làm lại hình và giao diện (2026-11-09)

Chỉ đổi lớp hiển thị, luật (`rail-core.js`), màn (`rail-levels.js`) và phép thử không đổi (21 phép thử vẫn qua).

- **Cảnh nền:** bỏ nền xanh phẳng, thay bằng bầu trời, nắng, mây trôi và ba lớp đồi (SVG/CSS trong `#scene`).
- **Sân chơi:** khung gỗ có đinh vít bốn góc, nền cỏ có hạt, ô cỏ liền nhau (bỏ khe lưới). Ô không có ray được rải cây tròn, thông, bụi quả, hoa, đá, nấm, cỏ theo số ô (`decoSvg` trong `rail-game.js`, nên mỗi lần dựng đều giống nhau; cây đung đưa nhẹ).
- **Đường ray:** nền đá dăm có sỏi, tà vẹt có sáng tối, thanh ray có bóng và vệt sáng.
- **Toa tàu:** thêm vệt bóng, viền mái, bánh có chiều sâu; toa hàng sáng màu hơn.
- **Thanh trên:** biển gỗ cho tên màn, nút tròn kem có biểu tượng SVG (thay emoji), vé chuyến, huy hiệu mèo mỗi màu có viền đúng màu toa.
- **Lời dẫn:** mèo hướng dẫn đứng cạnh bong bóng thoại (nhún nhẹ).
- **Kệ booster:** kệ gỗ, bốn nút màu (vàng, xanh dương, cam, xanh lá) kèm biểu tượng SVG và số lượng.
- **Popup:** thẻ kem viền gỗ, ruy băng đỏ, sao SVG, nút có bóng; bảng chọn màn cuộn được.
- **Bố cục:** bàn chơi nằm trên một phần ba khoảng trống còn lại, để thấy đồi bên dưới.
- **Kiểm tra:** chụp màn 1 (onboarding), màn 15 (đèn, đủ booster), bảng chọn màn và popup thắng; chơi màn 1 thật tới khi thắng. Không có lỗi script.
- **Bài học:** cửa sổ trình duyệt ẩn thì timer chạy chậm, đợi lâu hơn rồi mới đọc trạng thái.
- **Sửa lỗi mất hình mèo trên toa (2026-11-09):** nguyên nhân là hình mặt vui `Image_Cat_*EndGame.png` đổi vào lúc đoàn xuất bến nhưng chưa tải sẵn, và `renderCars` gán lại `img.src` cùng địa chỉ sau mỗi nước nên trình duyệt tải lại, hình trống một nhịp. Sửa trong `rail-game.js`: tải sẵn 8 hình mèo (4 màu x thường/EndGame) khi mở trang, và chỉ gán `src` khi địa chỉ thật sự khác. Kiểm tra: cả 8 hình ở trạng thái đã sẵn sàng ngay sau khi mở trang. Chưa quay được cảnh xuất bến vì cửa sổ ẩn làm timer chậm.
