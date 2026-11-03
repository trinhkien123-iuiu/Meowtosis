# Meowtosis — luật chơi và các cơ chế

Đây là **bản luật chuẩn** của Meowtosis: mỗi cơ chế làm gì, tương tác với cái khác ra sao, diệt bằng cách nào. Dùng để trả lời khi có người hỏi "sao lại thế này?", để viết hướng dẫn trong game, và để chép luật sang Unity.

- **Code là chuẩn, tài liệu phải theo code.** Mọi ví dụ có mã (`V4`, `N5`...) đều được chạy bằng đúng luật của game trong `tools/test-doc-examples.js` (`npm test`). Sửa luật mà ví dụ nào sai thì test báo đỏ, và phải sửa cả tài liệu này.
- **Có cơ chế mới thì thêm vào đây** theo mẫu ở mục 9.

> Cập nhật: 2026-11-03 · luật nằm trong khối `// ==LOGIC==` của `prototype/meowtosis.html`

## Mục lục

1. [Bàn chơi và ký hiệu](#1-bàn-chơi-và-ký-hiệu)
2. [Thắng và thua](#2-thắng-và-thua)
3. [Một lần vuốt](#3-một-lần-vuốt)
4. [Thứ tự xử lý một lượt](#4-thứ-tự-xử-lý-một-lượt)
5. [Các cơ chế](#5-các-cơ-chế)
6. [Bảng: cái gì diệt được cái gì](#6-bảng-cái-gì-diệt-được-cái-gì)
7. [Booster](#7-booster)
8. [Câu hỏi hay gặp](#8-câu-hỏi-hay-gặp)
9. [Thêm cơ chế mới](#9-thêm-cơ-chế-mới)

---

## 1. Bàn chơi và ký hiệu

Bàn là lưới ô vuông; ô nào thuộc "cơ thể" thì chơi được. Toạ độ viết là (hàng, cột), đếm từ 0 ở góc trên trái.

| Ký hiệu | Nghĩa |
|---|---|
| `#` | Ngoài cơ thể (lỗ hoặc phần ngoài bàn) |
| `.` | Ô trống |
| `~` | Ô độc |
| `A` `B` `C` `D` `E` | Mèo đỏ, xanh, vàng, hồng, tím |
| `a` `b` `c` `d` `e` | Mèo ngủ cùng màu |
| `G` | Vi khuẩn (chỉ ở level diệt khuẩn) |
| `^` `v` `<` `>` | Virus; mũi tên là hướng nó lan |

Các trường thêm của một level: `goal` (hộp mục tiêu), `oneway` (màng một chiều), `kill` (diệt khuẩn), `tut` (hướng dẫn tương tác), `tip` (mẹo đầu màn). Chi tiết định dạng: [unity-handoff.md §3](unity-handoff.md#3-dữ-liệu-level).

Trong các ví dụ dưới đây, bàn được viết theo từng hàng, ví dụ `AB. / B..` là hàng trên `AB.`, hàng dưới `B..`. Nước đi viết là "vuốt (hàng, cột) sang phải".

---

## 2. Thắng và thua

**Thắng** khi đồng thời:
1. **Kín ổ:** mọi ô (trừ ô độc) có một tế bào không phải virus. Mèo ngủ và vi khuẩn cũng tính là đã lấp.
2. **Đúng hộp:** mọi hộp có mèo đúng màu (mèo ngủ cũng được tính).
3. **Sạch khuẩn:** ở level diệt khuẩn, không còn vi khuẩn nào.
4. **Và không thua.**

**Thua** khi một màu mèo có mặt lúc đầu màn không còn con nào, kể cả con đang ngủ. Vi khuẩn và virus không phải màu mèo: vi khuẩn chết hết là chuyện tốt, không phải thua.

Kiểm tra sau mỗi lượt. Thua thì mất 1 tim (dùng Hồi sinh để quay lại, mục 7).

| Mã | Ví dụ |
|---|---|
| W1 | `AB`: vuốt (0,0) sang phải → `AA`. Bàn đã kín nhưng mèo xanh bị thay mất con cuối → **thua**, không thắng |
| W2 | `A. / .b`: mèo xanh chỉ còn một con đang ngủ → vẫn tính là còn sống, chưa thua |
| K3 | `AA. / B..`: vuốt (0,1) sang phải → 3 mèo đỏ bị khử, đỏ tuyệt chủng → thua |

---

## 3. Một lần vuốt

**Vuốt được:** mèo đang thức, vi khuẩn. **Không vuốt được:** mèo ngủ, virus.

Con được vuốt **nhân đôi**: bản sao đi vào ô kề theo hướng vuốt.

| Ô kề | Kết quả | Mã |
|---|---|---|
| Trống | Bản sao vào ô đó | S1: `A.` → `AA` |
| Có tế bào, phía sau dãy còn ô trống | **Đẩy** cả dãy liền nhau đi 1 ô tới ô trống đầu tiên, bản sao vào ô kề. Dãy gồm mèo, virus, vi khuẩn; **mèo ngủ không bị đẩy** | S2: `AB.` → `AAB` |
| Có tế bào, phía sau không còn ô trống (gặp mép, lỗ, màng chặn hoặc **mèo ngủ**) | **Thay thế:** bản sao chiếm ô kề, con cũ ở đó chết | S3: `AB / B.` → `AA / B.` |

**Nước không hợp lệ** (game từ chối, không tính nước):
- Vuốt ra ngoài cơ thể hoặc vào lỗ (S5).
- Vuốt ngược chiều màng một chiều (M1).
- Thay thế một con cùng màu với con đang vuốt (S4: `AA`, vuốt (0,0) sang phải).
- Đè lên hoặc đẩy một con mèo ngủ (N4).
- Vuốt mèo ngủ (N1) hoặc virus (V10).

Mỗi nước sinh **đúng một** tế bào mới. Vì vậy một level cần **ít nhất** bằng ngần ấy nước: số ô trống, cộng số ô đang có virus hoặc vi khuẩn phải dọn rồi lấp lại. Bộ giải A* dựa vào điều này.

---

## 4. Thứ tự xử lý một lượt

Sau mỗi nước (kể cả nước dùng booster Đột biến, Cấy ghép), game xử lý theo **đúng thứ tự** sau:

```text
1. Đặt bản sao        đẩy dãy hoặc thay thế (mục 3)
2. Ô độc giết         mọi tế bào đang đứng trên ô độc chết
3. Khử 3              mọi dãy ≥ 3 cùng màu (ngang, dọc) biến mất; mèo ngủ cũng tính, virus thì không
4. Virus lan          mỗi virus có từ trước lượt này lan 1 ô theo mũi tên
5. Ô độc giết lần 2   virus vừa lan vào ô độc chết
6. Mèo ngủ thức dậy   mèo ngủ kề mèo cùng màu đang thức thì dậy, lan dây chuyền
```

Thứ tự này sinh ra vài hệ quả hay bị hỏi:

| Mã | Hệ quả | Vì sao |
|---|---|---|
| N5 | 3 mèo ngủ cùng màu thẳng hàng thì **bị khử trước khi kịp thức** | Bước 3 trước bước 6 |
| V4 | Diệt con virus đầu dãy, con phía sau **ăn lại ngay** ô đó trong cùng lượt | Bước 1 diệt, bước 4 con sau lan vào |
| V6 | Virus lan vào ô độc thì chết, nhưng con sinh ra nó vẫn còn | Bước 5 chỉ giết con đứng trên ô độc |
| B4 | Dùng booster xong virus vẫn lan | Booster cũng chạy bước 2–6 |

---

## 5. Các cơ chế

Mỗi cơ chế theo cùng một mẫu: ký hiệu · xuất hiện lần đầu · luật · tương tác · cách xử lý · ví dụ.

### 5.1 Khử 3

- **Xuất hiện:** level 12 (hướng dẫn tương tác).
- **Luật:** 3 tế bào cùng màu trở lên nằm liền nhau theo hàng ngang hoặc dọc thì biến mất ở bước 3. Đường chéo không tính.
- **Tương tác:** mèo ngủ cũng tính vào dãy (K2, N5). Vi khuẩn cũng bị khử khi đủ 3 (G2). Virus không bao giờ bị khử 3 (V11). Màng một chiều không ngăn khử (M5).
- **Dùng để:** dọn chỗ khi bàn kẹt. Có hại: khử hết một màu là thua (K3).
- **Ví dụ:** K1 `AA. / A..`, vuốt (0,1) sang phải → hàng trên `AAA` bị khử → `... / A..`.
- **Ghi chú thiết kế:** với 3 màu trở lên, lời giải ngắn nhất gần như không bao giờ cần khử 3; level bắt buộc khử 3 dùng 2 màu, như level 10, 12, 15, 16 (xem [level-design.md](level-design.md)).

### 5.2 Hộp mục tiêu

- **Ký hiệu:** trường `goal`, chữ cái là màu hộp cần.
- **Xuất hiện:** level 17 (hướng dẫn tương tác).
- **Luật:** vẫn phải kín ổ; thêm điều kiện mỗi hộp có mèo đúng màu **khi thắng**. Hộp không giữ mèo lại: mèo trên hộp vẫn bị đẩy, thay thế, khử như thường.
- **Tương tác:** mèo ngủ đúng màu trên hộp vẫn tính (H1). Virus trên hộp thì hộp chưa đạt (H2).

### 5.3 Màng một chiều

- **Ký hiệu:** `oneway: [[hàng, cột, hướng]]`: màng nằm giữa ô (hàng, cột) và ô kề theo `hướng`; chỉ cho đi từ ô đó sang ô kề theo đúng hướng ấy (mũi tên trên màng).
- **Xuất hiện:** level 29 (hướng dẫn tương tác).
- **Chặn:** vuốt ngược chiều (M1); đường đẩy ngược chiều, khi đó dãy coi như hết chỗ nên bản sao **thay thế** (M3); virus lan ngược chiều (V7).
- **Không chặn:** đi xuôi chiều (M2); đánh thức mèo ngủ (M4); khử 3 (M5).

### 5.4 Ô độc

- **Ký hiệu:** `~`.
- **Xuất hiện:** level 33 (hướng dẫn tương tác).
- **Luật:** mọi tế bào đứng trên ô độc chết ở bước 2 và bước 5: mèo, mèo ngủ, vi khuẩn, virus. Ô độc không cần lấp (T3) và không bao giờ mất.
- **Các cách vào ô độc:** bản sao vuốt thẳng vào (T1: chết ngay, coi như mất một nước), bị đẩy vào (T2), virus lan vào (V6).
- **Dùng để:** loại bớt tế bào thừa, diệt virus và vi khuẩn. Coi chừng làm một màu tuyệt chủng.

### 5.5 Mèo ngủ

- **Ký hiệu:** chữ thường `a`–`e`. Trên màn hình: bịt mắt, chữ z.
- **Xuất hiện:** level 38 (hướng dẫn tương tác); thức dây chuyền ở level 41 (có mẹo).
- **Luật:**
  - Không vuốt được (N1).
  - **Say như hòn đá:** không bị đẩy, không bị bản sao đè lên (N4). Dãy bị đẩy chạm mèo ngủ thì dừng lại như gặp tường, nên bản sao thay thế con kề (N6).
  - Vẫn chết vì khử 3 (K2), ô độc và virus ăn.
  - Tính là đã lấp ô, tính cho hộp (H1), tính là màu còn sống (W2).
  - **Thức dậy** ở bước 6 nếu kề (trên, dưới, trái, phải) một mèo **cùng màu đang thức**. Con vừa thức lại đánh thức con ngủ cùng màu kề nó: **dây chuyền** (N3).
  - Mèo ngủ kề mèo cùng màu ngay từ đầu màn thì thức luôn khi vào màn (N2).
- **Hay bị hỏi:** 3 con ngủ cùng màu thẳng hàng thì bị khử trước khi kịp thức (N5). Màng không ngăn đánh thức (M4).

### 5.6 Virus

- **Ký hiệu:** `^ v < >`; mũi tên (và mắt virus) chỉ hướng lan.
- **Xuất hiện:** level 42 (hướng dẫn tương tác); hai virus ở level 44 (có mẹo).
- **Luật lan (bước 4):**
  - **Mọi** virus, con gốc lẫn con mới sinh, đều là virus đầy đủ: cùng hướng, cùng cách lan.
  - Mỗi lượt, mỗi virus có từ trước lượt đó lan **1 ô** theo hướng của nó: ô trống thì thành virus, ô có mèo, mèo ngủ hoặc vi khuẩn thì **ăn** con đó và thành virus.
  - Virus **không lan** khi ô phía trước là ngoài cơ thể, là màng chặn chiều đó (V7), hoặc đã là virus (V2).
  - Virus vừa sinh trong lượt này chỉ bắt đầu lan từ lượt sau.
- **Hệ quả: tốc độ lây.** Một dãy virus nối đuôi thẳng hàng thì chỉ **con đầu dãy** lan, nên mỗi lượt dãy dài thêm 1 (V1). Có 2 nguồn virus thì mỗi lượt thêm 2 (V2). Virus chạm mép thì đứng yên, hết lây (V3).
- **Cách diệt:**
  - **Thay thế:** vuốt một con mèo vào virus khi phía sau virus không còn chỗ đẩy; bản sao chiếm chỗ, virus chết.
  - **Ô độc:** đẩy virus vào ô độc, hoặc để virus lan vào ô độc (V6; chỉ con lan vào chết, con gốc vẫn còn).
  - Đột biến không dùng được trên virus (B1). Virus không bị khử 3 (V11).
- **Diệt từ đuôi lên đầu.** Diệt con đầu dãy thì con ngay sau ăn lại ô đó trong cùng lượt (V4). Diệt con cuối dãy trước, rồi lần lên, thì sạch (V5).
- **Nguy hiểm:** virus ăn con mèo cuối cùng của một màu là thua (V8). Virus nằm trên ô không tính là đã lấp (V11): còn virus thì chưa thắng.
- **Virus và vi khuẩn:** virus ăn được vi khuẩn, nhưng để lại virus ở đó (V9).
- **Virus bị đẩy** theo dãy như mọi tế bào khác (V10).

| Mã | Bàn đầu | Nước | Kết quả |
|---|---|---|---|
| V1 | `A.B.A / >....` | (0,0) phải, rồi (0,2) phải | `AAB.A / >>...` rồi `AABBA / >>>..`: 1 → 2 → 3 virus |
| V2 | `AB.C / >..<` | (0,1) phải | `ABBC / >><<`: hai nguồn, thêm 2; sau đó đứng yên vì gặp nhau |
| V4 | `AB / >>` | (0,1) xuống | Bản sao xanh thay con đầu dãy (1,1), con (1,0) ăn lại ngay → vẫn `AB / >>` |
| V5 | `AB / >>` | (0,0) xuống, rồi (0,1) xuống | Diệt con đuôi, rồi con đầu → `AB / AB`: thắng |
| V6 | `A.. / >~.` | (0,0) phải | Virus lan vào ô độc và chết; con gốc (1,0) còn nguyên |

### 5.7 Vi khuẩn

- **Ký hiệu:** `G`, level có `kill: 'G'`.
- **Xuất hiện:** level 49 (hướng dẫn tương tác); khử 3 vi khuẩn ở level 51 (có mẹo).
- **Luật:** vi khuẩn là một tế bào bình thường **không thuộc màu mèo nào**: tính là đã lấp ô, nhưng còn vi khuẩn thì chưa thắng; vi khuẩn chết hết không phải là thua (G4).
- **Vuốt được:** vuốt vi khuẩn thì nó **tự nhân lên** (G1). Có lợi khi cần đủ 3 con để khử, có hại nếu nhân bừa.
- **Cách diệt:** thay thế bằng bản sao mèo (G3); khử 3 vi khuẩn thẳng hàng (G2); ô độc; virus ăn (V9, nhưng để lại virus); Đột biến biến vi khuẩn thành mèo (B2).

### 5.8 Màu mèo

- 5 màu: `A` đỏ, `B` xanh, `C` vàng (từ đầu game), `D` hồng (lần đầu ở level 18), `E` tím (lần đầu ở level 31); hai level đó có mẹo đầu màn.
- Từ khối 2 tới khối 6, một số level dùng hồng hoặc tím **thay cho trọn một màu cũ** (ví dụ mọi mèo đỏ thành hồng): luật chỉ quan tâm các màu khác nhau, nên lời giải và độ khó không đổi. Mỗi level ở các chương này vẫn chỉ có tối đa 3 màu. Màu tím không dùng ở level có virus (virus cũng tím).
- Khối 7 là nơi **lần đầu có 4 màu (level 62) rồi 5 màu (level 66) cùng lúc**. Hai level mở đầu chỉ có đúng 1 bé màu thứ 4 / thứ 5: bé bị thay thế là tuyệt chủng.
- Màu nào có mặt lúc đầu màn đều phải sống tới cuối (mục 2). Nhiều màu hơn thì mỗi màu ít con hơn: dễ tuyệt chủng hơn, khó khử 3 hơn.
- Ảnh mèo lấy từ Shikacat (12 màu có sẵn); đã bỏ các màu dễ lẫn: WarmPink giống đỏ, Orange giống vàng, Green và Lime giống vi khuẩn và ô độc, các màu xanh nhạt giống `B`.

---

## 6. Bảng: cái gì diệt được cái gì

| | Mèo thức | Mèo ngủ | Virus | Vi khuẩn |
|---|---|---|---|---|
| Vuốt được | Có | Không | Không | Có (tự nhân lên) |
| Bị đẩy | Có | **Không** (chặn dãy như tường) | Có | Có |
| Bị bản sao thay thế | Có (trừ cùng màu với con vuốt) | **Không** | Có | Có |
| Bị khử 3 | Có | Có | **Không** | Có (3 vi khuẩn) |
| Chết trên ô độc | Có | Có | Có | Có |
| Bị virus ăn | Có | Có | Không (virus không lan vào virus) | Có |
| Đột biến | Có (trừ con cuối của một màu) | Có, và thức luôn | **Không** | Có, thành mèo |
| Tính là đã lấp ô | Có | Có | **Không** | Có |
| Là màu mèo (hết là thua) | Có | Có | Không | Không (phải diệt hết) |

---

## 7. Booster

Mỗi booster dùng là **một nước** (tính vào số nước, ảnh hưởng sao), kết thúc lượt như nước vuốt (bước 2–6 ở mục 4, B4), và **hoàn tác được**: hoàn tác thì trả lại lượt booster.

| Booster | Làm gì | Không dùng được khi | Mã |
|---|---|---|---|
| **Đột biến** | Đổi một tế bào sang màu khác có trong màn. Mèo ngủ bị đổi thì thức luôn; vi khuẩn bị đổi thì thành mèo | Chọn virus; chọn con cuối cùng của một màu (đổi đi là tuyệt chủng) | B1, B2 |
| **Cấy ghép** | Chọn một tế bào, bản sao của nó mọc ở **bất kỳ** ô trống nào, không đẩy gì | Ô đích là ô độc hoặc đã có tế bào; tế bào nguồn là virus | B3 |
| **Hồi sinh** | Chỉ dùng ngay sau khi thua: quay lại trước nước thua, trả lại 1 tim và booster đã dùng ở nước đó | Chưa thua | — |

Số lượt booster lúc đầu: mỗi loại 3.

---

## 8. Câu hỏi hay gặp

| Câu hỏi | Trả lời | Mã |
|---|---|---|
| Virus mỗi lượt sinh mấy con? | Mỗi virus không bị chặn sinh 1. Dãy nối đuôi thì chỉ con đầu sinh, nên mỗi nguồn thêm 1 con/lượt | V1, V2 |
| Sao màn này virus sinh 2, màn kia sinh 1? | Số nguồn virus đang lan được khác nhau | V2 |
| Diệt con virus gốc có hết lây không? | Không. Con nào cũng lây; phải diệt hết, từ đuôi lên đầu | V4, V5 |
| Virus có tự hết không? | Không tự chết. Chạm mép hoặc chạm virus khác thì đứng yên nhưng vẫn phải diệt mới thắng | V3, V11 |
| Sao vuốt mèo mà con bên cạnh biến mất? | Phía sau hết chỗ đẩy nên bản sao thay thế | S3, M3 |
| Sao mèo ngủ chưa dậy đã biến mất? | 3 con cùng màu thẳng hàng bị khử trước bước đánh thức | N5 |
| Mèo ngủ có được tính cho hộp không? | Có | H1 |
| Kín ổ rồi sao vẫn thua? | Có màu đã tuyệt chủng | W1 |
| Vi khuẩn vuốt được à? | Được, nó tự nhân lên | G1 |
| Màng có chặn đánh thức, chặn khử 3 không? | Không | M4, M5 |
| Sao không đẩy được, không đè lên được mèo ngủ? | Mèo ngủ say như hòn đá: dãy chạm nó thì dừng (bản sao thay con kề); vuốt thẳng vào nó là nước không hợp lệ. Muốn dùng ô đó thì đánh thức nó, hoặc dùng khử 3, ô độc | N4, N6 |
| Level này có chắc giải được không? | Có. Mọi level có lời giải do máy tìm, lưu ở `data/levels.json`; `npm test` kiểm tra lại | — |

---

## 9. Thêm cơ chế mới

Làm đủ các bước để luật, game, công cụ và tài liệu không lệch nhau:

1. **Luật** trong khối `// ==LOGIC==`: ký hiệu ở `parseLevel`, đặt vào đúng bước trong `resolve` (mục 4), cập nhật `keyOf` nếu có trạng thái mới, `isFull` / `isComplete` / `isLost` nếu đổi điều kiện thắng thua.
2. **Bộ giải A\*:** ước lượng hiện tại là "số mèo còn thiếu" và dựa trên việc **mỗi nước sinh tối đa một mèo**. Cơ chế nào sinh nhiều hơn một mèo mỗi nước thì phải sửa ước lượng, nếu không số nước tối ưu sẽ sai. Chạy `npm run compare-bfs` để so với BFS.
3. **Trong game:** thẻ hướng dẫn (`MECHANICS`), hình vẽ, VFX, SFX; level dạy có `tut` hoặc `tip`.
4. **Công cụ:** sự kiện mới trong `tools/lib.js` (`events`) để bộ sinh dùng được `need`.
5. **Ví dụ có test:** thêm vào `tools/test-doc-examples.js`, đặt mã mới.
6. **Tài liệu này:** thêm một mục ở §5 theo mẫu dưới, thêm cột hoặc dòng ở bảng §6, câu hỏi ở §8; cập nhật [unity-handoff.md §4](unity-handoff.md#4-luật-chơi-chép-sang-c) và ghi nhật ký ở [level-design.md](level-design.md).
7. `npm run export` rồi `npm test`.

Mẫu một mục cơ chế:

```markdown
### 5.x Tên cơ chế

- **Ký hiệu:** …
- **Xuất hiện:** level … (hướng dẫn / mẹo)
- **Luật:** … (chạy ở bước nào trong mục 4)
- **Tương tác:** với khử 3, ô độc, màng, mèo ngủ, virus, vi khuẩn, hộp, booster
- **Cách xử lý / diệt:** …
- **Hay bị hỏi:** …
- **Ví dụ:** mã X1, X2 (trong tools/test-doc-examples.js)
```
