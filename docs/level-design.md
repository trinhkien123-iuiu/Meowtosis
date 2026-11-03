# Meowtosis — quy trình làm level và nhật ký thiết kế

Tài liệu này ghi lại **cách làm level** cho Meowtosis: nguyên tắc, chỉ số đo, công cụ, từng bước, và những điều đã học được (kể cả các lần sai). Mục đích: người sau (hoặc chính mình sau vài tháng) làm thêm level mà không phải dò lại từ đầu, và khi chuyển sang Unity thì giữ nguyên được cách làm.

Tài liệu liên quan: [mechanics.md](mechanics.md) (luật chuẩn của từng cơ chế), [unity-handoff.md](unity-handoff.md) (prototype có gì, chuyển sang Unity thế nào).

---

## 1. Luật chơi tóm tắt (để đọc phần sau)

- Vuốt một con mèo: nó **nhân đôi** sang ô kề. Ô kề có mèo thì **đẩy cả dãy** tới ô trống gần nhất; không còn chỗ thì bản sao **thay thế** con kề (không thay được con cùng màu).
- 3 con cùng màu thẳng hàng thì **bị khử**.
- **Thắng:** lấp kín mọi ô (trừ ô độc) + điều kiện của cơ chế (hộp đúng màu, hết vi khuẩn, hết virus).
- **Thua:** một màu mèo có lúc đầu bị mất hết.
- Cơ chế: hộp mục tiêu, màng một chiều, ô độc, mèo ngủ, virus, vi khuẩn. Thứ tự xử lý một lượt xem [unity-handoff.md §4](unity-handoff.md#4-luật-chơi-chép-sang-c).

---

## 2. Nguyên tắc thiết kế đã chốt

| # | Nguyên tắc | Vì sao |
|---|---|---|
| 1 | Mọi level cùng mục tiêu **lấp kín**; cơ chế chỉ thêm điều kiện | Playtest: đổi mục tiêu giữa chừng (chỉ cần vào hộp, chỉ cần diệt virus) làm người chơi ngợp |
| 2 | Mỗi level **một khung riêng** (không trùng kể cả xoay/lật) | Góp ý: level 8 và 9 cùng là hình vuông 9 ô, thấy lặp |
| 3 | Không dùng khung **vòng khuyên, chữ C** | Đã chứng minh bằng duyệt toàn bộ: không bao giờ lấp kín được (tối đa 6 mèo) |
| 4 | Mỗi yếu tố mới đi theo nhịp **dạy → luyện → mở rộng → bất ngờ → tổng hợp** | Người chơi làm quen dần, rồi mới bị "lừa" bằng mẹo (mục 3) |
| 5 | Level dạy có **hướng dẫn tương tác** (`tut`); yếu tố nhỏ chỉ cần **mẹo đầu màn** (`tip`) | Hướng dẫn khoá nước, chỉ dùng khi thật cần; mẹo không khoá gì |
| 6 | Đi trọn hướng dẫn = đúng **số nước tối ưu** | Người làm theo luôn được 3 sao |
| 7 | **Màu là danh tính**, trạng thái (ngủ...) báo bằng kênh khác | Mèo ngủ bị làm xám thì người chơi không biết màu. **Chưa áp dụng:** mèo ngủ trong prototype vẫn xám, tạm bù bằng mẹo nói rõ màu và ô đếm mèo theo màu |
| 8 | Bàn lớn thì **nhiều mèo đặt sẵn** | Mỗi nước chỉ sinh đúng một mèo, nên số nước ≥ số ô trống; bàn lớn mà ít mèo thì dài lê thê và bộ giải không kham nổi |
| 9 | Bộ giải (Gợi ý, tính sao) phải chạy **dưới ~300 ms** trên PC | Điện thoại chậm hơn vài lần; Gợi ý phải gần như tức thì |
| 10 | Khử 3 và ô độc phải được **dạy riêng** | Trên khung nhỏ, lời giải ngắn nhất không bao giờ cần chúng (chúng làm mất mèo), nên level ngẫu nhiên không tự dạy được |

---

## 3. Đường cong độ khó

Level chia thành **7 khối theo cơ chế** (mảng `CHAPTERS`): cơ bản, hộp, màng + độc, ngủ + virus, vi khuẩn, trộn mọi cơ chế, nhiều màu. Bản đồ không hiện khối, chỉ một đường liền; level có yếu tố mới mang huy hiệu ở góc phải dưới (👆 hướng dẫn, 💡 mẹo, icon cơ chế, 🩷 / 💜 màu mới).

Trong mỗi khối, level được **xếp từ dễ đến khó, xen kẽ cơ chế** (từ đợt 4, bằng `tools/order.js`):

1. Khối chia thành **giai đoạn** theo cơ chế, ví dụ khối 3: màng → độc → màng + độc.
2. Mỗi giai đoạn mở bằng level **nhập môn**: level có `tut`, hoặc mẹo "… màu cùng lúc".
3. Sau đó luôn lấy level **dễ nhất** đang chơi được (cơ chế của nó đã mở).
4. Chỉ sang cơ chế kế khi đã **luyện ít nhất 3 level**, và level dễ nhất còn lại khó hơn hẳn bài nhập môn kế tiếp (hơn 8 điểm). Level khó còn sót lại của cơ chế cũ sẽ thành **bài ôn trộn cơ chế** ở phía sau.
5. Giai đoạn trộn (không có nhập môn riêng) mở sớm, nên cuối khối là các level khó nhất.

```text
khó │                          ▲ trùm
    │              ▲  ôn trộn  │
    │         ▲    │  ▲   ▲    │
    │    ▲    │  ▼ nhập môn kế │
    │▲  luyện │   (dễ)         │
    └┴────────┴───┴────────────┴──▶ level trong khối
     nhập môn
```

Độ khó dùng để xếp là **độ khó theo quyết định** (mục 4): bao nhiêu nước phải chọn đúng giữa nhiều nước đi được, có bao nhiêu nước dẫn vào ngõ cụt.

| Nhịp | Level trông thế nào | Ví dụ |
|---|---|---|
| **Dạy** | Rất nhỏ, có `tut`, 1–3 nước | Mèo ngủ: khung 5 ô, 2 nước |
| **Luyện** | Nhỏ, cách giải gần như hiển nhiên | Mèo ngủ trên khung tam giác, 4 nước |
| **Mở rộng** | Bàn lớn hơn, nhiều mèo hơn, nhưng **dễ** (ít ô trống) | 4×4, 10 mèo, 3–5 nước |
| **Bất ngờ** | Nước "hiển nhiên" sai; phải dùng mẹo (thay thế, khử 3, cho virus ăn...) | Level 15: phải tự khử 3 mèo của mình để có chỗ, chỉ 2/11 nước đầu tối ưu |
| **Tổng hợp / trùm** | Kết hợp cơ chế của chương với cơ chế cũ, bàn lớn | 5×5, độc + màng |

---

## 4. Chỉ số đo level

Công cụ (`tools/analyze.js`, `tools/gen.js`) tính các chỉ số dưới đây cho từng level:

| Chỉ số | Nghĩa | Dùng để |
|---|---|---|
| `par` | Số nước tối ưu (bộ giải A*) | Mốc 3 sao; độ dài level |
| `fill` | Số ô còn phải lấp lúc đầu = số nước tối thiểu | So với `par` |
| `thừa` (`extra`) | `par − fill`: số nước phải "tiêu" thêm (thay thế, khử...) | Level có mẹo thì `thừa > 0` |
| `nước đầu` | `đi được / còn thắng được / vẫn tối ưu` | Ít nước đầu tối ưu = khó đoán |
| `cụt` | Tỷ lệ trạng thái không còn đường thắng (chỉ tính được khi duyệt hết) | Level "bẫy" |
| `thắng / thua` | Tỷ lệ khi bấm ngẫu nhiên 300 ván | Chỉ phân biệt được level nhỏ |
| `mẹo` (`tricks`) | Số loại mẹo trong lời giải: thay thế, khử 3, ô độc, đánh thức dây chuyền, virus ăn mèo, diệt virus, khử 3 vi khuẩn | Yếu tố bất ngờ |
| `bitsOpt` | Đi dọc lời giải tối ưu, mỗi bước cộng log2(số nước đi được / số nước vẫn tối ưu) (`lib.decisionBits`) | Phải nghĩ bao nhiêu để giữ 3 sao |
| `bitsWin` | Như trên nhưng với số nước còn thắng được | Rủi ro sa vào ngõ cụt. Bàn lớn thường ra gần 0 vì bộ giải không kịp chứng minh ngõ cụt trong 800 ms |
| **điểm xếp** | `bitsOpt + 2×bitsWin + 0,3×par` (`tools/order.js`) | **Xếp level từ dễ đến khó** trong khối (từ đợt 4) |
| `khó` | `par×0.6 + (1−thắng)×3 + cụt×3 + (tỷ lệ nước đầu không tối ưu)×2` | Chỉ số cũ của bộ sinh; ít phân biệt được bàn lớn |
| `bất ngờ` | `bẫy×3 + (tỷ lệ nước đầu không tối ưu)×2 + mẹo + min(thừa,3)×0.5` | Chọn level có "aha" |
| sự kiện | `push` đẩy, `longPush` đẩy dãy ≥2, `replace` thay thế, `clear` khử 3, `toxic` vào ô độc, `wake` đánh thức, `chainWake` thức dây chuyền (≥2 con trong một nước), `wakeUse` vuốt một con lúc đầu đang ngủ (đánh thức là cần thiết), `spread` virus lan, `eaten` virus ăn mèo, `virusDie` diệt virus, `germDie` diệt khuẩn, `germClear` khử 3 vi khuẩn | Biết cơ chế có thật sự cần không |

Mốc của bộ level hiện có: xem mục 8.

---

## 5. Công cụ (`tools/`)

Mọi công cụ đọc luật **thẳng từ khối `// ==LOGIC==` của game** (`lib.js` → `loadLogic`), nên luôn khớp với game đang chạy. Chạy bằng Node ở thư mục gốc repo.

| File | Việc |
|---|---|
| `lib.js` | Nạp luật, khung chuẩn hoá (`canon`), số ngẫu nhiên có seed, phân tích level (sự kiện, duyệt trạng thái, nước đầu, điểm) |
| `analyze.js` | Bảng chỉ số của mọi level trong game, hoặc của một file JSON |
| `gen.js` | Sinh level ngẫu nhiên theo job, lọc và xếp hạng |
| `run-jobs.js` | Chạy song song nhiều job (mỗi job một tiến trình), gộp kết quả vào `jobs/out/` |
| `check.js` | Kiểm tra cả danh sách level: giải được, đủ nhanh, không trùng khung, hướng dẫn đúng, chương phủ kín |
| `bfs-compare.js` | So bộ giải A* của game với BFS gốc (cùng số nước tối ưu) |
| `export-levels.js` | Xuất level + chương + `par` + lời giải ra `data/levels.json` (`npm run export`) |
| `serve.js` | Máy chủ chơi thử (`npm start`, `npm run lan`, thêm `--port=` để đổi cổng) |
| `difficulty.js` | Đo độ khó theo quyết định của mọi level, song song, nhớ kết quả trong `jobs/difficulty.json` (`npm run difficulty`; chỉ đo level mới hoặc vừa sửa) |
| `order.js` | Sắp level từ dễ đến khó trong từng khối (mục 3). `npm run order` chỉ in đề xuất; `node tools/order.js --write` ghi vào game và lưu bảng cũ → mới ở `jobs/order-mapping.json` |
| `test-lose.js`, `test-boosters.js`, `test-mechanics.js` | Test luật: thua khi tuyệt chủng, booster, các cơ chế |
| `jobs/*.json` | Các job đã dùng (giữ lại để sinh lại hoặc làm tiếp); `jobs/picks*.json` là ứng viên đã đo lại khi máy rảnh |
| `jobs/out/` | Kết quả từng lần chạy: `<job>.txt` (đọc được) và `<job>.json`; `jobs/run*.log` là log tương ứng |
| `jobs/analyze-61.txt` | Bảng chỉ số của cả 61 level sau đợt 2 (mốc cho đợt sau) |

### Định dạng job

```json
{ "name": "c4-a ngủ dây chuyền", "shape": { "w": 4, "h": 4, "holes": [1, 3] }, "cells": "AAACCCBbbb",
  "need": ["chainWake"], "min": 4, "max": 7, "twist": false, "ms": 60000, "seed": 41, "top": 5 }
```

| Trường | Nghĩa |
|---|---|
| `frame` hoặc `shape` | Khung cố định, hoặc khung ngẫu nhiên `w×h` khoét `holes` ô (liền khối, không trùng khung đã có) |
| `cells` | Mèo đặt ngẫu nhiên: `A B C` mèo, `a b c` mèo ngủ, `G` vi khuẩn |
| `toxic`, `virus`, `membranes` | Số ô độc / virus (hoặc chuỗi mũi tên như `"<>"`) / màng |
| `mode`, `targets` | `fill`, `target` (kèm số hộp), `kill` |
| `need` | Sự kiện bắt buộc có trong lời giải ngắn nhất (tên như cột sự kiện ở mục 4; thêm `oneway` = màng làm đổi lời giải, `virus` = virus lan và bị diệt) |
| `min`, `max` | Khoảng `par` |
| `twist` | Đòi có ít nhất 1 mẹo và **không quá nửa** nước đầu là tối ưu |
| `maxMs`, `nodes` | Bỏ level mà bộ giải chạy lâu hơn / cần nhiều trạng thái hơn (mặc định 300 ms, 80.000). `maxMs` cũng là trần thời gian mỗi lần giải, nên bàn không giải được bị loại nhanh |
| `ms`, `seed`, `top` | Thời gian chạy, hạt giống ngẫu nhiên, số ứng viên in ra |

---

## 6. Quy trình làm một đợt level

1. **Đo mốc:** `npm run analyze` → biết chương nào đang thiếu nhịp nào (mục 3), `par` và `khó` đang ở đâu.
2. **Lên kế hoạch chỗ trống:** với mỗi chương, ghi ra cần thêm level nhịp nào, kích thước bàn, số mèo, cơ chế, khoảng `par`. Quy tắc nhanh: `par ≈ số ô trống + thừa`, nên muốn `par` 6–10 trên bàn 20 ô thì đặt sẵn 11–14 mèo.
3. **Viết job** vào `tools/jobs/<tên>.json` (mục 5).
4. **Sinh:** `npm run gen -- tools/jobs/<tên>.json` → `tools/jobs/out/<tên>.txt` (dễ đọc) và `.json`. Đọc dòng "bị loại" để chỉnh job nếu ra ít ứng viên (ví dụ toàn "quá dài" thì thêm mèo).
5. **Chọn tay:** trong mỗi job, chọn ứng viên theo: đúng nhịp cần, `bất ngờ` cao (với level bất ngờ) hoặc `thừa = 0` và nước đầu dễ (với level mở rộng), khung đẹp/dễ nhìn trên điện thoại (rộng ≤ 5 cột).
6. **Tự chơi thử** từng level đã chọn trong trình duyệt; nếu cảm giác sai thì bỏ, đừng tin chỉ số tuyệt đối.
7. **Chèn vào `LEVELS`** vào đúng khối (vị trí trong khối không quan trọng), viết chú thích một dòng (hình khung · cơ chế · mẹo). Thêm `tut` hoặc `tip` nếu là yếu tố mới. Sửa `from`/`to` của `CHAPTERS`.
8. **Xếp lại:** `npm run difficulty`, rồi `npm run order` để xem đề xuất, rồi `node tools/order.js --write`. Mẹo "lần đầu gặp màu" và "bàn đông mèo" tự chuyển sang đúng level gặp đầu tiên; mẹo khác phải tự xem lại lời có còn đúng không.
9. **Xuất và kiểm tra:** `npm run export` (ghi `data/levels.json` cho Unity) rồi `npm test`. Tất cả phải qua. Xem khung các level mới trên trình duyệt.
10. **Ghi nhật ký** vào mục 8 của tài liệu này: đợt nào, thêm gì, vì sao.

---

## 7. Bài học (những lần sai và cách sửa)

| Chuyện gì | Bài học |
|---|---|
| Từng bịa luật khi chưa chơi bản gốc | Làm prototype theo game tham khảo thì **chơi bản gốc trước**, không tự đặt luật còn thiếu |
| Level 8 bị nghi không giải được | Mọi level phải có lời giải do máy tìm, và hướng dẫn/Gợi ý lấy từ đó |
| Level 8 và 9 cùng khung vuông | Kiểm tra trùng khung bằng dạng chuẩn 8 phép xoay/lật (`canon`) |
| Khung vòng khuyên (level 18 cũ) không bao giờ lấp kín | Khung có "lỗ" giữa hoặc hình chữ C: bỏ; bộ giải sẽ báo không giải được |
| Đổi sang luật "luôn lấp kín", level 29 cũ không còn giải được | Đổi luật là phải chạy lại toàn bộ kiểm tra và thay level hỏng |
| Một level cần 14 nước, bộ giải mất 32 giây | Đặt trần thời gian giải; level quá sâu thì thay |
| Mèo ngủ bị làm xám, người chơi không biết màu | Không đổi màu để báo trạng thái |
| Onboarding ban đầu yếu hơn game tham khảo | Thêm level nhập môn siêu nhỏ + hướng dẫn tương tác (bàn tay, làm tối, chỉ nhận đúng nước) |
| Mở rộng lên bàn 4×4–5×6: BFS chạm trần 80.000 trạng thái | Đổi bộ giải sang **A\***: mỗi nước sinh đúng một mèo nên "số mèo còn thiếu" là ước lượng không vượt quá thực tế → vẫn ra số nước tối ưu; level 21 từ 817 ms xuống 6 ms. Đã so khớp với BFS trên cả 36 level cũ (`bfs-compare.js`) |
| Bàn lớn ít mèo: hàng chục nước, không gian nổ | Bàn lớn phải nhiều mèo đặt sẵn (nguyên tắc 8) |
| Hộp mục tiêu trên bàn lớn: BFS tìm "trạng thái kín ổ" không đủ sâu | Lấy trạng thái kín ổ bằng các ván chơi ngẫu nhiên, ưu tiên đặt hộp ở ô lúc đầu trống hoặc khác màu |
| Khung level 16, 26, 27, 28, 37, 57, 58 vẽ méo (đường chéo, mất viền) | Hàm vẽ viền chỉ giữ một cạnh đi ra ở mỗi đỉnh; chỗ hai lỗ (hoặc lỗ và phần ngoài) chạm nhau ở một góc có hai cạnh nên bị ghi đè. Đã sửa: giữ mọi cạnh, ở góc chạm chéo luôn rẽ trái. **Mỗi đợt level mới phải xem khung trên trình duyệt**, máy kiểm tra không thấy lỗi hình |

---

## 8. Nhật ký các đợt level

### Đợt 1 — 36 level, 5 chương (trước 2026-11-03)

- Bắt đầu từ cơ chế Mitoosis (chia, đẩy, thay thế, khử 3, vòng mục tiêu), thêm luật thua khi một màu tuyệt chủng.
- Thêm 5 cơ chế: màng một chiều, ô độc, mèo ngủ, virus, vi khuẩn; mỗi cơ chế có thẻ hướng dẫn lần đầu gặp.
- Mỗi level một khung riêng; thay level 18 (vòng khuyên không lấp được) bằng khung cái cốc.
- Gộp luật thắng: mọi level phải lấp kín; thay level 29 (hết lời giải sau khi gộp luật).
- Thêm level nhập môn siêu nhỏ và hướng dẫn tương tác ở 10 level.
- Mốc: bàn lớn nhất 4×4 (14 ô), nhiều nhất 3 mèo đặt sẵn, `par` 1–11.

### Đợt 2 — mở rộng lên 61 level, 6 chương (2026-11-03)

> Số level trong đợt này là số thứ tự **lúc đó**; đợt 4 đã xếp lại, xem bảng đối chiếu ở đợt 4.

**Yêu cầu:** thêm level với nhiều mèo hơn, bàn rộng hơn, vẫn có yếu tố bất ngờ, đi từ dễ đến khó để người chơi quen dần với yếu tố mới.

**Làm gì:**
1. Đo mốc 36 level cũ (`analyze.js`): nhiều nhất 3 mèo đặt sẵn, bàn lớn nhất 14 ô, `par` 1–11.
2. Thử sinh bàn 4×4 có 7 mèo: BFS cũ chạm trần 80.000 trạng thái, mỗi lần thử mất cả giây. **Đổi bộ giải của game sang A\*** (mục 7), so khớp với BFS trên cả 36 level. Thêm giới hạn thời gian cho bộ giải (`solve(level, cells, maxNodes, maxMs)`, `solve.cut`).
3. Gợi ý trên bàn lớn: còn đi đúng lời giải đã tính thì trả ngay; lệch thì giải lại tối đa 0,6 giây; không ra thì bảo hoàn tác mấy bước để về lại đường thắng.
4. Viết job cho từng chỗ trống theo nhịp ở mục 3, sinh song song (`run-jobs.js`), chọn tay, chơi thử.
5. Thêm trường `tip` (mẹo đầu màn, không khoá nước) cho các yếu tố mới nhỏ.
6. Thêm chương 6 "Đại gia đình" cho bàn 5×5–5×6; level tổng hợp cũ (bậc thang) chuyển sang đầu chương này.

**Level mới (số thứ tự sau khi chèn):**

| Level | Khung | Mèo | `par` | Nhịp | Điểm nhấn |
|---|---|---|---|---|---|
| 13 | 4×4 | 10 | 4 | mở rộng | Bàn lớn đầu tiên, ít ô trống; có `tip` |
| 14 | 4×4 | 7 | 9 | bất ngờ | Phải thay thế mèo của chính mình 2 lần |
| 15 | 4×4 | 8 (2 màu) | 9 | bất ngờ | Phải tự khử 3 mèo của mình để có chỗ; chỉ 2/11 nước đầu tối ưu |
| 16 | 5×5 | 12 (2 màu) | 11 | trùm | Khử 3 + thay thế; 8/15 nước đầu dẫn vào ngõ cụt |
| 26 | 4×4 | 9 | 4 | mở rộng | Hộp trên bàn lớn, dễ |
| 27 | 4×5 | 11 | 7 | bất ngờ | 3 hộp, phải thay thế |
| 28 | 5×5 | 13 | 9 | trùm | 3 hộp, nửa số nước đầu là ngõ cụt |
| 32 | 4×4 | 8 | 7 | mở rộng | Màng trên bàn lớn; chỉ 2/13 nước đầu tối ưu |
| 35 | 4×5 | 10 | 8 | mở rộng | 2 ô độc trên bàn lớn |
| 36 | 4×5 | 10 | 8 | tổng hợp | Màng + độc |
| 37 | 5×5 | 12 | 9 | trùm | 3 màng + 2 ô độc |
| 41 | 5×3 | 6 | 7 | luyện | **Thức dây chuyền:** nước đầu đánh thức cùng lúc 2 bé nằm sát nhau, bé thứ 3 thức sau; phải dùng cả 3; có `tip` (nói rõ bé ngủ là màu gì) |
| 42 | 4×5 | 11 | 9 | mở rộng | Mèo ngủ trên bàn lớn, phải dùng 2 con vừa thức |
| 46 | 4×4 | 7 | 7 | luyện | **Hai virus** hai hướng; có `tip`; chỉ 1/13 nước đầu tối ưu |
| 47 | 4×5 | 9 | 11 | bất ngờ | Phải để virus ăn bớt một con mèo |
| 48 | 5×5 | 12 | 9 | trùm | Phải dùng mèo vừa thức; virus ăn một con; chỉ 1/18 nước đầu tối ưu |
| 51 | 4×3 | 5 | 7 | luyện | **Khử 3 vi khuẩn:** nước đầu là vuốt chính vi khuẩn cho nó nhân thành 3 con thẳng hàng rồi tự khử; có `tip` |
| 53 | 4×5 | 9 | 9 | mở rộng | Vi khuẩn trên bàn lớn |
| 54 | 4×4 | 6 | 8 | tổng hợp | Vi khuẩn + virus; chỉ 1/17 nước đầu tối ưu |
| 55 | 5×5 | 16 | 7 | mở rộng | Bàn lớn nhất tới lúc đó, ít ô trống; có `tip` |
| 57 | 5×5 | 13 | 7 | tổng hợp | Hộp + ô độc |
| 58 | 5×5 | 13 | 9 | tổng hợp | Mèo ngủ + màng, phải dùng mèo vừa thức |
| 59 | 5×5 | 11 | 9 | tổng hợp | Virus + vi khuẩn |
| 60 | 5×5 | 13 | 9 | bất ngờ | Đủ mọi cơ chế; dẫn virus lan vào ô độc để nó tự chết; phải dùng mèo vừa thức; chỉ 1/29 nước đầu tối ưu |
| 61 | 5×6 | 14 | 12 | trùm cuối | Bàn lớn nhất, level dài nhất; phải dùng mèo vừa thức, virus ăn 3 con mèo trên đường giải |

**Đường cong sau đợt 2** (`par`, kèm khung và số mèo; ᵗ = có hướng dẫn, ᵐ = có mẹo):

```text
1 Phòng nuôi cấy  1ᵗ 1ᵗ 2 3ᵗ 4 4 5 8 8ᵗ 10 10 10 | 4ᵐ(4×4,10) 9(4×4,7) 9(4×4,8) 11(5×5,12)
2 Vào vị trí      2ᵗ 4 5 7 10 9 9 9 11          | 4(4×4,9) 7(4×5,11) 9(5×5,13)
3 Màng và độc     2ᵗ 4 9 7(4×4,8) | 3ᵗ 11 8(4×5,10) 8(4×5,10) 9(5×5,12)
4 Ngủ và virus    2ᵗ 4 9 7ᵐ(5×3,6) 9(4×5,11) | 3ᵗ 2 7 7ᵐ(4×4,7) 11(4×5,9) 9(5×5,12)
5 Diệt khuẩn      2ᵗ 4 7ᵐ(4×3,5) 8 9(4×5,9) 8(4×4,6)
6 Đại gia đình    7ᵐ(5×5,16) 9 7(5×5,13) 9(5×5,13) 9(5×5,11) 9(5×5,13) 12(5×6,14)
```

Mỗi chương: level nhỏ để dạy và luyện, rồi thả xuống một bàn lớn dễ (`par` thấp, nhiều mèo, ít ô trống), rồi tăng lại tới level trùm. Level trùm không nhất thiết dài nhất chương mà là **khó đoán nhất**: số nước đầu tối ưu rất ít (ví dụ level 48 chỉ 1/18, level 60 chỉ 1/29).

**Những gì học được trong đợt này:**
- Với 3 màu mèo, lời giải ngắn nhất gần như **không bao giờ cần khử 3** (thử hơn 23.000 bàn 4×4, không bàn nào): luôn xếp được để tránh dãy 3. Muốn level bắt buộc khử 3 thì dùng **2 màu**, như level 9, 10, 15, 16.
- Đòi "có mèo thức dậy" là chưa đủ: mèo có thể thức tình cờ ở nước cuối mà không dùng tới. Thêm sự kiện `wakeUse` (vuốt một con lúc đầu đang ngủ) để biết đánh thức có thật sự cần. Bản đầu của level 41, 42, 48, 60, 61 mắc đúng lỗi này và đã được sinh lại với `need: ["wakeUse"]`.
- **Mẹo đầu màn phải khớp lời giải tối ưu:** bản đầu của mẹo level 41 chỉ một nước đúng luật nhưng không tối ưu, ai làm theo sẽ mất sao. Viết mẹo theo kiểu nói luật ("một bé thức sẽ đánh thức bé bên cạnh"), không chỉ nước cụ thể, trừ khi đó đúng là nước đầu của lời giải.
- Mèo ngủ hiện màu xám nên người chơi không biết màu: mẹo của level dạy mèo ngủ phải nói rõ màu (vấn đề hình ảnh mèo ngủ vẫn còn để ngỏ).
- Seed ngẫu nhiên liền nhau (206, 207) từng cho ra cùng ứng viên: đã trộn seed trong `rng`.
- Hàm đếm sự kiện từng cho mọi mèo mới sinh cùng id `-1`, nên virus mới lan có thể trùng id với mèo ngủ và bị đếm nhầm là "thức dậy" (một level hiện `wake28`). Đã sửa: cấp id mới như game. Số liệu `wake`/`chainWake` trong các file `jobs/out/` chạy trước khi sửa có thể bị thổi phồng; `wakeUse` không bị ảnh hưởng.
- Khi tự chạy lời giải trên giao diện, phải chờ game hết bận (`busy`) giữa các nước: hiệu ứng khử 3 dài hơn, gửi nước sớm sẽ bị bỏ qua.
- Bàn trên 20 ô, mỗi lần thử bàn không giải được tốn vài giây: đặt `maxMs` cho bộ giải để loại nhanh.

### Chuyển sang repo riêng (2026-11-03)

- Toàn bộ Meowtosis chuyển từ `D:GitHubPuzzle_Prototype` sang `D:GitHubMeowtosis`: `prototype/`, `tools/`, `docs/`, `data/`.
- File game đổi tên `mitoosis.html` → `meowtosis.html`; `prototype/mitoosis.html` chỉ chuyển hướng để link cũ vẫn chạy. Khoá lưu tiến độ giữ nguyên nên người chơi không mất sao.
- Hai bản cũ trong `prototype/archive/` dùng khoá lưu riêng để không ghi đè tiến độ game chính.
- Thêm `package.json` (`npm start/test/export`), `data/levels.json` cho Unity; `check.js` báo lỗi khi file này cũ hơn game.
- Ban đầu chặn Shikacat và mp3 Mixkit trong `.gitignore` vì repo GitHub đang công khai; cùng ngày, theo ý chủ repo (repo cá nhân, phi thương mại), bỏ chặn để lưu đủ asset trong git. Repo cần chuyển sang riêng tư trước khi đẩy lên.

### Đợt 3 — sửa khung, mèo hồng và tím, chương 7 (2026-11-03)

> Số level trong đợt này là số thứ tự **lúc đó**; đợt 4 đã xếp lại, xem bảng đối chiếu ở đợt 4.

**Yêu cầu:** sửa các bàn bị méo khung; thêm mèo màu khác ở các màn sau; viết luật chuẩn của mọi cơ chế.

**Làm gì:**
1. **Sửa khung méo** ở level 16, 26, 27, 28, 37, 57, 58 (xem bài học ở mục 7). Đã xem lại cả 7 bàn trên trình duyệt.
2. **Màu mới:** bản export Shikacat có 12 màu mèo, mỗi màu có mèo, mèo lúc thắng và hộp; đã chép cả 12 vào `prototype/shikacat/cats`. Dùng thêm `D` hồng và `E` tím. Hai màu này khác hẳn đỏ, xanh, vàng và không lẫn với vi khuẩn hay ô độc. Hình tự vẽ khi thiếu ảnh: hồng là trái tim, tím là lục giác. Tiếng mèo: hồng cao hơn (×1,26), tím trầm hơn (×0,78). Bảng chọn màu của Đột biến xếp lưới 2 cột khi có hơn 2 màu.
3. **Chương 7 "Đủ sắc màu"** (level 62–69, lúc đầu tên "Bạn mới"), sinh bằng `tools/jobs/colors-c7.json`:

| Level | Khung | Mèo | `par` | Nhịp | Điểm nhấn |
|---|---|---|---|---|---|
| 62 | 4×3 | 7 | 4 | dạy | Lần đầu 4 màu cùng lúc, chỉ 1 bé hồng; có `tip` |
| 63 | 4×4 | 10 | 6 | bất ngờ | 4 màu, phải thay thế |
| 64 | 4×5 | 11 | 7 | luyện | 4 màu + 3 hộp (có hộp hồng) |
| 65 | 4×5 | 10 | 10 | bất ngờ | 4 màu + mèo ngủ hồng + virus; chỉ 1/22 nước đầu tối ưu |
| 66 | 4×4 | 9 | 5 | dạy | Lần đầu 5 màu cùng lúc, chỉ 1 bé tím; có `tip` |
| 67 | 5×5 | 12 | 7 | tổng hợp | 5 màu + màng + ô độc |
| 68 | 5×5 | 11 | 10 | tổng hợp | 5 màu + vi khuẩn + virus |
| 69 | 5×6 | 15 | 12 | trùm cuối | 5 màu, đủ cơ chế; phải dùng mèo vừa thức, virus ăn 3 con trên đường giải |

4. **Luật chuẩn:** viết [mechanics.md](mechanics.md). Mọi ví dụ trong đó được kiểm bằng `tools/test-doc-examples.js` (nằm trong `npm test`). Thẻ hướng dẫn virus trong game thêm câu "virus mới sinh cũng lan, diệt từ đuôi lên".

**Những gì học được trong đợt này:**
- Viết ví dụ luật thì phải chạy thử: 2 trong 3 ví dụ đầu tiên viết sai vì quên rằng khử 3 chạy trước đánh thức. Đó cũng là một luật hay bị hỏi, nên đã thành ví dụ N5.
- Nhiều màu hơn thì mỗi màu ít con hơn: level nhập môn màu mới nên chỉ có 1 bé màu đó để người chơi thấy ngay luật tuyệt chủng.
- Với 4–5 màu, các job đòi `twist` cho bàn 4×4 bị loại nhiều vì "không có mẹo": nhiều màu thì ít khi buộc phải thay thế hay khử. Cần chạy nhiều seed hơn.
5. **Rải màu mới sang các chương khác:** đổi trọn một màu ở 13 level không có hướng dẫn: 19, 26 (chương 2); 31, 35, 37 (chương 3); 40, 42, 47 (chương 4); 50, 53 (chương 5); 57, 58, 59 (chương 6). Hồng đi với xanh và vàng (đổi đỏ → hồng), tím đi với đỏ và vàng (đổi xanh → tím); level 57 đổi cả hai. Không dùng tím ở level có virus. Mèo hồng gặp lần đầu ở level 19, mèo tím ở level 31, cả hai có `tip`. Chương 7 đổi tên thành "Đủ sắc màu", mẹo ở 62 và 66 nói về "lần đầu 4 / 5 màu cùng lúc". Đã so trước/sau: đúng 13 level đổi, `par` và lời giải giữ nguyên; chơi thử cả 13 trên giao diện.

**Thêm bài học:**
- **Đổi trọn một màu là cách an toàn để tăng đa dạng hình ảnh:** lời giải giữ nguyên, không cần sinh lại. Muốn tăng độ khó thì phải thêm màu (như chương 7), không phải đổi màu.
- Không đổi màu ở level có `tut`, vì lời thoại hướng dẫn gọi tên màu ("vuốt mèo xanh...").
- Đổi màu làm mất sao đã đạt ở level đó, vì prototype lưu tiến độ theo nội dung level (bản Unity cần `id` cố định).
- Tìm dòng level trong file bằng chuỗi con dễ khớp nhầm: `'AB#', '#.#'` của level 50 khớp cả dòng level 30. Đã phát hiện nhờ so trước/sau và sửa lại; lần sau nên tìm theo cả mảng `rows` đầy đủ.

### Đợt 4 — bỏ chia chương trên bản đồ, xếp lại từ dễ đến khó (2026-11-03)

**Yêu cầu:** bỏ việc chia chương trên bản đồ, đánh dấu yếu tố mới bằng huy hiệu góc phải dưới để dễ test; xếp level từ dễ đến khó nhưng vẫn giữ khối theo cơ chế, chơi đủ lâu một cơ chế rồi mới sang cơ chế mới, rồi trộn.

**Làm gì:**
1. **Bản đồ:** một đường liền 69 nút, không còn khung chương. Huy hiệu góc phải dưới: icon cơ chế ở level đầu tiên của nó, 🩷 / 💜 lần đầu gặp mèo hồng / tím, 👆 level có hướng dẫn, 💡 level có mẹo. Thẻ chi tiết level ghi rõ yếu tố mới thay cho tên chương. `CHAPTERS` vẫn giữ, giờ là **khối** dùng để xếp level và xuất dữ liệu.
2. **Thước đo độ khó mới** (`lib.decisionBits`, `tools/difficulty.js`): đếm số bit quyết định dọc lời giải (mục 4). Đo cả 69 level song song mất vài phút.
3. **Xếp lại** bằng `tools/order.js` (mục 3): nhập môn → luyện ít nhất 3 level → cơ chế kế → trộn, trong mỗi khối từ dễ đến khó. Chạy lại lần hai không đổi gì (thứ tự đã ổn định).
4. Sửa mẹo bàn 5×5: "lớn nhất" thành "đông mèo nhất", vì giờ trước nó đã có các bàn 5×5 khác.

**Bảng đối chiếu số cũ → mới** (level không có trong bảng giữ nguyên số): 5→8, 6→5, 8→9, 9→12, 11→14, 12→11, 13→6, 14→13, 18→19, 19→18, 20→21, 21→25, 22→24, 24→28, 25→26, 26→20, 27→22, 28→27, 34→37, 35→34, 37→35, 40→41, 41→40, 42→46, 43→42, 44→43, 46→44, 47→48, 48→47, 53→54, 54→53, 55→57, 56→59, 57→55, 58→56, 59→58, 63→64, 64→63.

**Thứ tự mới, theo khối** (`*` = nhập môn, `|` = mở cơ chế kế):

```text
1 Cơ bản       1* 2* 3 | 4* 5 6 7 8 9 10 11 | 12* 13 14 15 16
2 Hộp          17* 18 19 … 28
3 Màng + độc   29* 30 31 32 | 33* 34 | 35 36 (trộn) 37 (độc, khó nhất)
4 Ngủ + virus  38* 39 40 41 | 42* 43 44 45 | 46 (ôn ngủ) 47 (trộn) 48 (virus, khó nhất)
5 Vi khuẩn     49* 50 51 | 52 53 (trộn) 54 (khó nhất)
6 Trộn         55 … 61
7 Nhiều màu    62* 63 64 65 | 66* 67 68 69
```

**Những gì học được:**
- Xếp thuần theo giai đoạn thì cứng: một level "chỉ cần đẩy" nhưng dài 10 nước bị kéo lên vị trí 4. Xếp thuần theo độ khó thì mất nhịp dạy. Cách xen kẽ (mục 3) giữ được cả hai.
- `bitsWin` gần như bằng 0 ở bàn lớn vì bộ giải không kịp chứng minh ngõ cụt; tạm dựa vào `bitsOpt`. Muốn chính xác hơn thì tăng thời gian giải trong `difficulty.js` (chạy lâu hơn).
- Tiến độ lưu theo nội dung level nên xếp lại không làm mất sao.
- Sau đó, theo yêu cầu: bàn đông mèo đầu tiên (level 6) chuyển xuống sau vuông 3×3, thành level 10 (level 7–10 cũ thành 6–9). Ghi ở `AFTER` trong `tools/order.js` nên chạy xếp lại vẫn giữ.
- Viết script dài trong `node -e "..."` trên Git Bash thì backtick bị shell hiểu thành lệnh: viết ra file .js rồi chạy.

### Đợt 5 — hiệu ứng mới, mèo ngủ "say như hòn đá" (2026-11-03)

**Hiệu ứng:** đẩy (con mẹ chồm tới, dãy dồn rồi nảy), hồn mèo mắt @@ bay lên từ ô độc, mắt @@ và sao bay vòng khi bị đẩy khỏi hộp đúng màu, virus đớp mèo. Sửa vệt bóng sáng lòi ra ngoài của vi khuẩn và trái tim (mỗi hình một vị trí vệt sáng).

**Đổi luật mèo ngủ:** trước đây mèo ngủ bị đẩy và bị bản sao đè lên như mèo thường, nên người chơi có thể xoá mèo ngủ mà không cần đánh thức. Đã thử hai phương án trên bản sao luật với 12 level có mèo ngủ: (1) chỉ cấm đè lên: không level nào đổi số nước; (2) cấm cả đẩy lẫn đè lên: chỉ một level tăng từ 9 lên 10 nước. Chủ dự án chọn **(2): mèo ngủ say như hòn đá**. Dãy đẩy chạm mèo ngủ thì dừng như gặp tường, nên bản sao thay con kề. Mèo ngủ vẫn chết vì khử 3, ô độc, virus. Thẻ hướng dẫn mèo ngủ có thêm ảnh minh hoạ; ví dụ N4, N6 trong `mechanics.md`.

**Xếp lại** sau khi đo lại 12 level có mèo ngủ (độ khó đổi theo luật mới). Cũ → mới: 40→41, 41→46, 46→40, 58→59, 59→60, 60→58. Mẹo thức dây chuyền giờ ở level 41.

**Bài học:**
- Đổi luật thì phải đo lại độ khó của các level dính tới luật đó: `difficulty.json` nhớ theo nội dung level nên không tự biết luật đã đổi. Xoá các mục liên quan rồi chạy lại `npm run difficulty`.
- Đặt tên lớp CSS cho hiệu ứng thì rà trùng tên trước: lớp `ghost` của hồn mèo từng trùng nút "Bản đồ" trên bảng thắng.

