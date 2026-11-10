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
| 11 | Luật thua phải được **tự tay trải nghiệm sớm**, không chỉ đọc | Góp ý: người chơi tưởng chỉ cần lấp kín vì luật khử chỉ là chữ ở thẻ cơ chế. Bản Unity dạy luật này ở màn hướng dẫn đầu game (câu luật sau bước thay thế; phần tự chơi chặn nước làm hết màu). Level 1 `AA. / ##B` để người chơi tự áp dụng: vuốt đỏ sang phải là hết màu đỏ |

> **Số level:** ngày 2026-11-04 đã chèn 2 level vào vị trí 2 và 3 (mọi level sau cộng thêm 2). Ngày 2026-11-07 bỏ level 1 `A.` và level 4 `AB.` vì màn hướng dẫn đầu game đã dạy, nên **từ level 3 trở đi số level trùng lại số trong các bảng phía dưới**. Level 1, 2 nay là `AA. / ##B` và `##B# / AA..`. Tổng cộng 69 level.

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
| `study-clear.js` | So các phương án luật khử 3 (hiện tại, khử an toàn, khử từ 4, không khử) bằng mô hình người chơi casual; kết quả ở `jobs/study/clear.txt` |
| `study-mechanics.js` | Mỗi cơ chế có đổi cách giải không: so từng level với bản bỏ cơ chế (par, nước thừa, độ khó theo quyết định, tỷ lệ thắng casual); kết quả ở `jobs/study/mechanics-*.txt` |
| `export-levels.js` | Xuất level + chương + `par` + lời giải ra `data/levels.json` (`npm run export`) |
| `serve.js` | Máy chủ chơi thử (`npm start`, `npm run lan`, thêm `--port=` để đổi cổng) |
| `difficulty.js` | Đo độ khó theo quyết định của mọi level, song song, nhớ kết quả trong `jobs/difficulty.json` (`npm run difficulty`; chỉ đo level mới hoặc vừa sửa) |
| `order.js` | Sắp level từ dễ đến khó trong từng khối (mục 3). `npm run order` chỉ in đề xuất; `node tools/order.js --write` ghi vào game và lưu bảng cũ → mới ở `jobs/order-mapping.json` |
| `pick-virus.js` | Lọc ứng viên level virus theo ý đồ: mọi virus có ô để lan ngay từ đầu, mắt các virus không cùng hướng, lời giải ngắn nhất có virus lan nhiều lần |
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


### Màn hướng dẫn đầu game kiểu Meowdoku (2026-11-07)

**Vì sao:** chơi thử từ tiến độ trống cho thấy luật được dạy rải rác qua nhiều level. Level 2 dạy hai luật mới cùng lúc (khử 3 và hết màu là thua). Mẹo đầu màn và thẻ cơ chế đã bị tắt trong Unity nên nhiều luật không còn được giải thích. Chủ dự án muốn làm như Meowdoku: trước level 1 có một bàn duy nhất, làm tối mọi thứ không liên quan và bắt người chơi làm theo để hiểu hết luật.

**Cách Meowdoku làm** (xem video `D:\GitHub\asset_meowtosis\onboarding.mp4`, tách khung hình 1 giây một lần bằng OpenCV):
1. Một bàn 4×4, không có HUD. Mỗi bước chỉ sáng ô liên quan, có bàn tay chỉ và lời dẫn ở trên ("Nhấn hai lần để đặt mèo vào ô").
2. Làm xong thì cả bàn sáng lại, lời dẫn đổi thành luật vừa thấy ("Tốt lắm! Mỗi màu chỉ một chú mèo.") kèm nút "Đã hiểu!".
3. Lặp lại cho từng luật. Bước cuối người chơi tự làm ("Tìm chú mèo cuối cùng!"), có nút gợi ý.
4. "Xuất sắc! Bạn đã nắm được luật!" rồi "Bắt đầu chơi". Level 1 nhắc lại 3 luật bằng icon nhỏ trên HUD.

**Ràng buộc khi chọn bàn:** muốn dạy theo thứ tự nhân đôi → đẩy → thay thế → khử 3 rồi lấp kín, mà không nước nào làm thua.
- Mỗi nước nhân đôi hay đẩy lấp thêm 1 ô, thay thế không lấp thêm ô nào, khử 3 làm trống 3 ô. Bàn có E ô trống thì cả màn dài E + 4 nước.
- Sau hai nước đầu bàn phải còn chỗ trống, nếu không là đã thắng. Vậy E ≥ 3, tức ít nhất 7 nước.

**Tìm bàn:** script duyệt mọi bàn 2×3, 2×4, 3×3 có 2 màu (`#` `.` `A` `B`), tìm chuỗi nước đúng thứ tự trên mà không lúc nào thua. Có 9848 bàn đạt mức ngắn nhất là 7 nước. Chọn bàn chữ nhật 2×3 không có lỗ, phần tự chơi chỉ cần vuốt từng mèo xuống:

```text
B A .      1. (0,1) xuống   nhân đôi         B A . / A A .
A . .      2. (0,0) phải    đẩy              B B A / A A .
           3. (0,2) trái    thay thế xanh    B A A / A A .   (xanh còn 1 bé)
           4. (0,2) xuống   khử 3 đỏ         B A A / . . .
           5–7. tự lấp 3 ô hàng dưới
```

Ở nước tự chơi đầu tiên có 5 nước đi được, 4 nước còn đường thắng. Nước còn lại là mèo đỏ (0,1) vuốt trái: bản sao thay mất bé xanh cuối cùng, hết màu xanh. Game từ chối nước đó và hiện bàn tay chỉ nước đúng, nên người mới không thể thua ở màn này.

**Bài học:**
- Khi duyệt bàn, chống trùng phải theo cả nội dung bàn. `lib.canon` chỉ chuẩn hoá hình khung, nên lần chạy đầu chỉ thử một bàn cho mỗi hình khung và chỉ ra bàn 2×4 dài 8 nước. Sửa xong thì tìm được bàn 7 nước.
- Duyệt 2×5 (hơn 1 triệu bàn, mỗi bàn tìm sâu 8 nước) không xong trong 10 phút. Hãy giới hạn độ sâu và cỡ bàn trước khi mở rộng.

**Gọn level đầu sau khi có màn hướng dẫn:** màn hướng dẫn đã dạy nhân đôi, đẩy, thay thế, khử 3 và luật thua, nên các level đầu không cần dạy lại:

| Level cũ | Bàn | Làm gì |
|---|---|---|
| 1 | `A.` | Bỏ: trùng bước 1 của màn hướng dẫn |
| 2 | `AA. / ##B` | Bỏ bẫy và bàn tay. Nay là level 1: tự áp dụng luật thua (vuốt đỏ sang phải là khử 3 mèo đỏ, hết màu) |
| 3 | `##B# / AA..` | Giữ (mẹo chỉ hiện ở bản web) |
| 4 | `AB.` | Bỏ: 1 nước, vuốt con nào cũng thắng, trùng bước 2 |
| 6 | `.B. / #A#` | Bỏ bàn tay 3 bước. Nay là level 4: tự tìm nước thay thế (cần đúng 1 nước thay thế) |

Không chạy lại `tools/order.js`: bỏ `tut` thì giai đoạn "chia" của khối 1 không còn bài nhập môn và cả khối sẽ bị xếp lại. Chỉ bỏ 2 level và lùi `from`/`to` của `CHAPTERS` đi 2. Bản đồ Unity không còn đánh dấu "cơ chế mới" vì nhân đôi, đẩy, thay thế, khử 3 (`MechanicDetector` chỉ còn tính cơ chế theo nội dung level).

**Lưu ý:**
- Bản web không có màn hướng dẫn, nên giờ vào thẳng level `AA. / ##B` mà không có bàn tay.
- Level 6 `B.. / A..` cùng khung 2×3 với màn hướng dẫn `BA. / A..`, chỉ khác vị trí mèo.

### Đo lại độ khó cho người chơi casual và ý nghĩa từng cơ chế (2026-11-07)

**Góp ý playtest:** người chơi không hardcore thấy khó, hay quên luật khử 3 nên hay thua. Vừa lấp xong một góc lại phải phá để đưa mèo khác tới đúng chỗ, nên mệt và không thấy thành tựu. Cơ chế vi khuẩn bị chê vô nghĩa: không có vi khuẩn thì vẫn phải đưa mèo vào ô đó.

**Cách đo:** dùng mô hình người chơi casual, không nhìn trước:
- **Quên luật:** chọn ngẫu nhiên một nước làm thêm một ô có mèo.
- **Cẩn thận:** như trên nhưng bỏ các nước thua ngay.

Mỗi level chơi 200 ván cho mỗi kiểu (`tools/study-clear.js`). Cơ chế đo bằng phản chứng: so từng level với bản bỏ cơ chế (`tools/study-mechanics.js`).

**Luật khử 3 là gốc của cả hai lời phàn nàn.** Tỷ lệ thắng của người chơi casual (quên luật / cẩn thận), số lần khử trong một ván, số nước thừa trung bình trong lời giải ngắn nhất:

| Khối | Hiện tại | Khử an toàn (không khử nếu làm tuyệt chủng) | Khử từ 4 | Không khử |
|---|---|---|---|---|
| 1 | 10% / 37%, khử 3,2 lần, thừa 1,56 | 41% / 41%, thừa 1,00 | 85% / 92%, khử 0,4 lần, thừa 0 | 100% / 100% |
| 2 (hộp) | 4% / 12%, thừa 1,75 | 12% / 14% | 25% / 32%, thừa 0,83 | 31% / 40% |
| 3 (màng, độc) | 16% / 34%, thừa 2,22 | 29% / 35% | 66% / 73%, thừa 0 | 86% / 92% |
| 4 (ngủ, virus) | 17% / 31% | 35% / 36% | 83% / 87% | 99% / 99% |
| 6 (bàn lớn) | 0% / 0% | 1% / 1% | 26% / 27% | 48% / 49% |
| 7 (nhiều màu) | 2% / 13% | 20% / 20% | 32% / 33% | 51% / 52% |

- Với luật hiện tại, người quên luật thua 66% số ván ở khối 1, vì 3 mèo cùng màu thẳng hàng tự khử trung bình 3,2 lần mỗi ván.
- "Nước thừa" (par trừ số ô phải lấp) chính là số nước phá đi làm lại. Nó gần như chỉ đến từ việc phải xếp mèo sao cho không có 3 con cùng màu thẳng hàng. Bỏ được điều đó thì nước thừa ở khối 1 và 3 về 0, tức lấp tới đâu chắc tới đó.
- Khử an toàn hết thua oan nhưng vẫn phải phá đi làm lại, nên tỷ lệ thắng chỉ lên 41%.
- Không khử thì khối 1 thành chơi bừa cũng thắng: mất hết câu đố.
- Khử từ 4 giữ được khử như một công cụ (vẫn có lúc khử, vẫn có thể thua) mà hết phá đi làm lại ở phần lớn level.

**Từng cơ chế có đổi cách giải không** (trung bình trên các level có cơ chế, có so với bỏ đi):

| Cơ chế | Số level | Δpar | Δ độ khó quyết định (bitsOpt) | Δ thắng casual | Nhận xét |
|---|---|---|---|---|---|
| Hộp | 14 | +1,36 | +6,0 | −31 điểm | Có nghĩa nhất, nhưng cũng là nguồn "phải đưa mèo tới đúng chỗ" |
| Virus | 13 | +1,15 | +7,3 | −1,5 | Có nghĩa: tạo áp lực thời gian và thứ tự diệt |
| Vi khuẩn | 11 | **0,00** | +4,6 | −7 | Par y hệt khi đổi vi khuẩn thành ô trống (chỉ 2 level lệch 1 nước). Vuốt cho vi khuẩn tự nhân lên chỉ dùng ở 1/11 level. Đúng như góp ý: vi khuẩn chỉ là "ô trống phải lấp bằng thay thế" |
| Màng | 12 | +0,75 | +2,8 | −2 | Yếu: 4/12 level bỏ màng vẫn y nguyên |
| Mèo ngủ | 12 | +0,75 | +1,0 | −1 | Yếu: đa số chỉ đánh thức 1 con, hai level không cần đánh thức |
| Ô độc | 13 | **0,00** | +0,6 | −1 | Gần như chỉ là một cái lỗ: 5/13 level lời giải không chạm ô độc |

Đề xuất và lựa chọn của chủ dự án ghi tiếp ở mục sau khi đã chốt.

**Chốt (2026-11-07):** giữ luật khử 3. **Bỏ vi khuẩn:**
- 10 level có vi khuẩn đổi vi khuẩn thành ô trống và giữ id cũ, nên sao không mất.
- Bỏ level `GA / .#`: bài dạy vi khuẩn, không có vi khuẩn thì chỉ còn 2 nước.
- Khối 5 thành "Ôn tập" (5 level). Còn 68 level.
- Code Unity không còn `Species.Germ`, `KillGoal`, từ khoá `kill`, thẻ `Kill`/`GermClear`. Số enum `Mechanic` 9 và 12 để trống, không dùng lại.

### Cơ chế Băng trơn (2026-11-07)

**Luật** (chi tiết và ví dụ I1–I6 ở [mechanics.md §5.7](mechanics.md)): bản sao vào ô băng `=` đang trống thì trượt theo hướng vuốt tới khi gặp vật cản hoặc ra khỏi băng. Dãy bị đẩy, bản sao thay thế, virus và Cấy ghép không trượt.

**Làm level:**
1. `tools/gen.js` có thêm `ice: n` (số ô băng) và `need: ['ice']`. Điều kiện này loại level mà lời giải của bản không băng đem chơi trên bàn có băng vẫn thắng, tức băng không đổi gì; chừng 85–95% ứng viên bị loại vì lý do đó.
2. Job ở `tools/jobs/ice.json`. Các job nhập môn 3×2, bất ngờ, băng + độc, băng + virus không ra ứng viên nào. Bài nhập môn `#B= / A==` tìm bằng cách duyệt hết các bàn nhỏ có 2 mèo, chọn bàn 3 nước có trượt mà không cần thay thế hay khử.
3. Chọn tay 8 level theo nhịp nhập môn → luyện (2×4, 3×3) → 3 màu → mở rộng 4×4. Đặt ở đầu khối 5, trước 5 level ôn. Không chạy lại `order.js`; `PHASES` khối 5 nay là `băng` rồi `ôn tập`.

**Đo** (`node tools/study-mechanics.js băng`): par trung bình −0,75 khi có băng (tới −3 ở level 54–55), độ khó theo quyết định −1,0, tỷ lệ thắng casual gần như không đổi. Băng là cơ chế **giúp sức**: đưa mèo đi xa bằng một nước. Nó làm đổi lời giải mà không làm khó thêm.

**Lưu ý:** bài nhập môn 2×3 khuyết góc trùng khung level 19. `check.js` chỉ ghi chú, vì mọi bàn nhỏ có lời giải 3 nước dùng băng đều có khung này.

### Cơ chế phải bắt buộc dùng mới qua màn (2026-11-07)

**Yêu cầu của chủ dự án:** level có cơ chế nào thì người chơi phải dùng tới cơ chế đó mới thắng được. Đó mới là ý nghĩa của việc tạo ra cơ chế.

**Cách kiểm** (`tools/mechanic-use.js`): với mỗi cơ chế trên bàn, cấm mọi nước dùng tới nó rồi tìm đường thắng (A\*, tối đa 200.000 trạng thái hoặc 4 giây). Còn thắng được thì cơ chế đó chỉ là **trang trí**. "Dùng" nghĩa là:

| Cơ chế | Dùng nghĩa là |
|---|---|
| Băng | có nước làm bản sao trượt |
| Ô độc | có quân chết trên ô độc |
| Mèo ngủ | có nước đánh thức mèo ngủ |
| Màng | có nước đưa quân xuyên màng (bản sao hoặc dãy bị đẩy) |
| Hộp | điều kiện thắng nên không cấm được: bỏ hộp đi thì lời giải ngắn nhất không còn đúng hộp |
| Virus | luôn bắt buộc (còn virus thì chưa thắng) |

**Kết quả lần đầu:**
- 22 level có cơ chế chỉ là trang trí: hộp 17, 23, 26; màng 29, 36, 63, 65, 67, 68, 74, 76; ô độc 34, 35, 36, 59, 62, 65, 67, 68, 74, 76; mèo ngủ 40, 41, 65, 72, 76; băng 49, 51, 52, 53.
- Cả 3 bài nhập môn hộp, màng, băng cũng hỏng: không làm theo cơ chế vẫn thắng.
- Thêm 6 level chưa chắc (32, 47, 56, 63, 68).

**Sửa:**
1. `gen.js` có thêm `strict: true`, chỉ nhận level mà mọi cơ chế đều bắt buộc. `tools/jobs/make-strict-jobs.js` tạo job cùng cỡ bàn, số mèo, số cơ chế với từng level hỏng (`tools/jobs/strict.json`).
2. Đợt đầu ra được 10 level. Ô độc gần như không bao giờ bắt buộc trên bàn ngẫu nhiên, vì muốn bỏ bớt mèo thì luôn có cách thay thế. Đợt hai (`tools/jobs/strict2.json`) dùng bàn nhỏ 4×3, 5×2, 4×4 và thử nhiều hơn, thì ra đủ level ô độc, mèo ngủ, độc + màng.
3. Bài nhập môn tìm bằng cách duyệt hết bàn nhỏ: bàn liền khối, 2–3 nước, không cần thay thế hay khử.
   - Hộp `#B.A / A.##`: mèo đỏ vào hộp là sai.
   - Màng `#BAA / ..##`: hàng dưới chỉ vào được qua màng.
   - Băng `B==A / A#.#`.
4. Level trộn lớn ở khối 6–7 (62, 63, 65, 68, 74, 76) bỏ phần trang trí: ô độc thừa thành lỗ, màng thừa bỏ đi, mèo ngủ thừa cho thức sẵn hoặc thành lỗ. Các level này vẫn giữ hộp hoặc virus khi cơ chế đó bắt buộc.
5. Thay bằng `tools/jobs/apply-strict.js` (25 level, giữ `tip`/`card` của level 41). Level mới nhận id mới.

**Sau khi sửa:** 0 level trang trí, 0 level chưa chắc. `check.js` (trong `npm test`) nay báo **lỗi** nếu có cơ chế trang trí. 4 cặp level trùng hình khung (17–29, 23–36, 49–51, 52–53) chỉ là lưu ý.

### Gọn hướng dẫn trong level (2026-11-07)

- **Level 12** `.B# / ..# / A..`: bỏ bàn tay 4 bước và thẻ "Dọn chỗ", thành màn chơi bình thường.
- **Bỏ mẹo thừa:**
  - Level 2 ("đừng để 3 mèo thẳng hàng"): màn hướng dẫn đầu game đã dạy.
  - Level 10 và 64 ("bàn to, đông mèo"): không dạy gì mới.
- **Lời dẫn ở 6 màn đầu tiên có cơ chế mới nói rõ cơ chế làm gì:**
  - Hộp (17): chỉ nhận mèo cùng màu.
  - Màng (29): chỉ cho đi theo mũi tên.
  - Ô độc (33): mèo vào là biến mất, không cần lấp.
  - Mèo ngủ (38): không vuốt được, đặt mèo cùng màu sát bên để đánh thức.
  - Virus (42): lan 1 ô mỗi nước và ăn mèo; còn virus là chưa thắng.
  - Băng (49): bản sao trượt tới khi gặp vật cản.
- **Bản Unity:** mỗi bước hướng dẫn làm tối phần bàn không liên quan, chừa sáng các ô nước đó chạm tới (`MoveFocus`, gồm cả đường trượt trên băng). Vùng sáng giữ nguyên trong lúc nước đi đang diễn để thấy rõ kết quả.
- **Còn lại** mẹo màu mới (18, 31, 69, 73), thức dây chuyền (41) và hai virus (44). Bản Unity hiện không hiện mẹo đầu màn (`GamePresenter.CurrentTip` chỉ hiện lời hướng dẫn), nên các mẹo này chỉ còn thấy ở bản web.

### Thiết kế lại level virus: nhìn mắt, rồi khó chịu (2026-11-08)

- **Ý đồ:** level virus đầu tiên là màn hướng dẫn để người chơi **nhìn ánh mắt virus** (mắt nhìn đâu thì lượt sau nó lan tới đó). Từ level virus thứ hai trở đi, **mọi level có ít nhất 2 virus mắt khác hướng nhau**, để người chơi tự nhận ra cách virus lan và thấy sự khó chịu của nó khi phải lấp kín ổ; giải được mới thấy level hay.
- **Level hướng dẫn (cũ 42):** giữ khung `.# / A# / B# / >.`, viết lại ba lời dẫn xoay quanh ánh mắt. Trong Unity, bước hướng dẫn chừa sáng **mọi virus và ô ngay trước mắt chúng** (`MoveFocus.AddVirusGaze`) để lời "nhìn mắt virus" chỉ đúng chỗ.
- **Thay 11 level virus một con** (cũ 43, 45, 47, 48, 60, 65, 66, 68, 72, 75, 76) bằng level hai đến ba virus, giữ vai trò cũ (cỡ bàn, ngủ, bất ngờ, trùm khối). Sinh bằng `tools/jobs/virus-redesign.json` và `virus-redesign2.json` (`virus` đặt sẵn chuỗi hướng, `need: ["virus"]`, `strict`), lọc bằng `pick-virus.js`: virus đối đầu hoặc vuông góc, lan 2 lần trở lên, ưu tiên level có virus ăn mèo, độ khó đo được 8–13 (tương đương các level virus cũ, không tăng đường cong).
- **Kết quả:** level 42 hướng dẫn (1 virus), level 43 là màn hai virus đầu tiên (nhỏ nhất, par 5), mọi level virus còn lại có 2 hoặc 3 virus (kiểm tra trên danh mục Unity).
- **Bài học:**
  - Bàn lớn mà ít mèo (5×5 trở lên, 8 mèo) khiến bộ giải quá giới hạn và không ra ứng viên: phải nới `maxMs`/`nodes` và thu gọn bàn.
  - Số virus càng nhiều, par càng dài (11–15) dù mèo ít: chọn bàn nhỏ để level không dài quá.
  - Mẹo chữ đầu màn không hiện trong game (chỉ bước hướng dẫn có chữ), nên màn hai virus đầu tiên để người chơi tự quan sát.
  - `order.js --write` giờ đọc và ghi được file game dùng CRLF; chạy `node tools/order.js --write` đã dời 36 level theo độ khó đo lại, gồm cả level không liên quan virus.

### Hai cơ chế mới: cổng dịch chuyển và mèo hai màu (2026-11-08)

- **Công cụ:** `gen.js` thêm `portals`, `dual`, `period`, `phase`; `need` thêm `portal`, `dual`; `lib.events` đếm `portal` (qua cổng) và `flip` (đổi màu); `mechanic-use.js` thêm hai cơ chế: **cổng** bắt buộc khi cấm mọi nước đi xuyên cổng thì không thắng được, **hai màu** bắt buộc khi lời giải ngắn nhất của bàn không có tính hai màu không còn thắng trên bàn có. `order.js` thêm hai giai đoạn (`cổng`, `hai màu`) và chịu được file game dùng CRLF.
- **Thêm 2 khối, 11 level (nay 87 level, 9 khối):**
  - Khối 6 **Cổng dịch chuyển** (5 level, level 62–66): màn hướng dẫn tự thiết kế `A.#..` (hố giữa bàn, chỉ cổng nối hai nửa; lời giải là ba lần vuốt cùng một nước, nước 2 và 3 đẩy dãy chui qua cổng), rồi 4 level sinh bằng `jobs/portal-dual.json`, `portal-dual-intro.json`.
  - Khối 7 **Mèo hai màu** (6 level, level 67–72): màn hướng dẫn `..###/#AABB` (mèo xanh đổi sang đỏ, hướng dẫn ba bước), 4 level luyện (một rồi hai mèo hai màu, lệch nhịp), và một level kết hợp cổng + hai màu.
- **Độ khó đo được** (`bit` quyết định, chuẩn của `tools/difficulty.js`): khối cổng 1 → 22, khối hai màu 8 → 42.
- **Bài học:**
  - Khung nhỏ (3×2, 4×2) đã bị level cũ dùng hết, bộ sinh không ra: phải tự thiết kế màn nhập môn bằng tay hoặc đổi cỡ khung. Màn nhập môn cổng dùng **hố giữa bàn** để cổng là cách duy nhất qua hố.
  - Cổng đặt ở cạnh nhìn ra ngoài nên bộ sinh chỉ chọn ô có cạnh sát mép hoặc lỗ; nhiều ứng viên bị loại vì "cổng trang trí" (lời giải không cổng vẫn thắng): cần `strict` và bàn có lỗ.
  - Mèo hai màu cần số nước tối ưu ≥ 3 mới thấy được đổi màu; chu kỳ 2 dễ đọc hơn chu kỳ 1 (con số 1 luôn hiện).
  - Chữ nổi báo đổi màu phải ngắn (`→ VÀNG!`): chữ dài bị cắt ở mép màn hình khi mèo đứng sát mép.

### Làm lại cổng và mèo hai màu: bỏ cơ chế đi thì không thắng được (2026-11-08)

- **Phản hồi:** hai level cổng cũ xếp cổng cạnh ô đã vuốt ngang được (cổng thừa), huy hiệu số của mèo hai màu vướng mắt, và người chơi không cần dùng cơ chế vẫn thắng.
- **Chuẩn mới "bắt buộc":** gỡ cơ chế khỏi level (cổng thành tường, mèo hai màu thành mèo thường) thì level phải **không thể thắng**. `tools/mechanic-use.js` chứng minh bằng hai cách: (1) cấu trúc, cổng nối hai vùng mà một vùng có ô cần lấp nhưng không có mèo, mèo hai màu sinh một màu mà hộp cần, bàn không có con nào màu đó; (2) không chứng minh được thì giải vét cạn bản đã gỡ cơ chế, thua hết mới tính là bắt buộc. Chạy `node tools/mechanic-use.js`: 0 level trang trí.
- **Luật mới đi kèm:** màu kia của mèo hai màu được phép là **màu chưa có con nào trên bàn** (mèo hai màu là nguồn duy nhất của màu ấy), màu ấy không tính vào việc hết màu.
- **Bộ sinh:** `gen.js` thêm `split` (cổng nối hai vùng tách rời, mèo chỉ đặt ở một vùng), `hidden` (màu kia và hộp chỉ ở màu ẩn), `allowUsed`. Job mới `tools/jobs/portal-dual-v2.json`.
- **Level mới (sau `order.js`):** cổng là level 62–67 (62 là màn hướng dẫn `A.#..`), mèo hai màu là 68–73 (68 là màn hướng dẫn: hộp vàng mà bàn không có mèo vàng); level cổng + hai màu nằm trong khối hai màu. 88 level, 9 khối.
- **Giao diện mèo hai màu mới:** bỏ đĩa số. Bản đầu cắt nửa thân mèo đổi màu (`SpriteMask`) nhưng trông rợn, nên đổi sang **bong bóng**: thân mèo giữ một màu, góc trên phải ô có bong bóng chứa mèo màu kế tiếp, chấm dưới bong bóng là số nước còn lại (còn 1 thì nhấp nháy), đổi màu thì bong bóng phồng lên (`TwoToneCat`). Thẻ cơ chế dùng `DemoCell` có bong bóng + đầu mèo + chấm.
- **Bài học:**
  - `SpriteMask` ở chế độ khoảng tuỳ chỉnh không tác động lên ảnh có đúng thứ tự vẽ bằng hai đầu khoảng: phải nới `front = order + 1`, `back = order - 1`; `PieceView.SetHighlightOrder` đổi thứ tự vẽ nên mặt nạ phải theo dõi.
  - Cổng thừa là lỗi thiết kế dễ mắc nhất: luôn hỏi "bỏ cổng đi level còn thắng được không".
  - Tự mở level để thử: đặt `GameBootstrap.openLevelNumber`, xoá `PlayerPrefs` thì sẽ vào hướng dẫn đầu game (gọi `StartGame` để bỏ qua).
- **Onboarding gọn:** màn hướng dẫn đầu game ẩn cả thanh trên (TopBar, HeartPill, RestartButton) ngoài các phần đã ẩn từ trước, chỉ còn bàn chơi, hàng đếm mèo và lời hướng dẫn (danh sách OnboardingView.hiddenWhileOnboarding trong scene Game).
- **Vòng sáng ô (Highlight):** vòng cũ là sprite vuông to hơn ô gỗ nên bị hở; thay bằng viền ôm đúng hình bóng ô (sprite `CellOutline` sinh từ alpha của `WoodCube`: quầng vàng, dải trắng, dải vàng, ruột vàng nhạt; lệch xuống 0,05 như thân ô), prefab `Prefabs/Board/Highlight`.

### Đổi luật virus: virus không bị đẩy, vuốt vào là ăn (2026-11-09)
- **Vì sao đổi:** muốn ăn virus bằng cách vuốt mèo vào nó, nhưng nếu phía sau virus còn ô trống thì virus bị đẩy đi thay vì chết, người chơi khó hiểu vì sao "ăn" mà không chết.
- **Luật mới:** virus không bao giờ bị đẩy. Ô kề là virus thì bản sao chiếm ô virus, virus chết. Dãy mèo bị đẩy kết thúc bằng virus thì dãy dồn tới, con cuối chiếm ô virus và virus chết (mèo giẫm virus). Virus giữa hai dãy mèo là điểm dừng của dãy đang đẩy. Chi tiết và ví dụ V10, V12, V13 ở `docs/mechanics.md` mục 5.6.
- **Đã làm:** sửa `swipe` trong prototype (nguồn luật) và `SwipeRule` trong Unity, ví dụ V10, V12, V13 ở cả `tools/test-doc-examples.js` (63 ví dụ) và `MechanicsExampleTests.cs`, lời dẫn level 42 và thẻ cơ chế Virus, hoạt ảnh nổ ở đúng ô virus (`MovePlayback`).
- **Ảnh hưởng tới 13 level virus:** cả 88 level vẫn giải được; `node tools/mechanic-use.js` vẫn 0 level trang trí. Đo lại độ khó (`node tools/difficulty.js --all`): chỉ một level đổi số nước tối ưu (11 xuống 10, ô `B##. / .<AC / B... / A#.^ / C..D`, không còn cần cảnh virus ăn mèo) và các level virus nhìn chung dễ hơn chút. **Chưa chạy `order.js --write`**, nên thứ tự level giữ nguyên; lệnh `node tools/order.js` đang gợi ý đổi chỗ 7 level nhỏ, phần lớn là nhiễu của phép đo (hàm đo lấy mẫu ngẫu nhiên).
- **Xuất lại:** `npm run export` (danh mục Unity có lời giải mới). `npm test` và `dotnet test` đều qua (388 test C#).

### Dạy luật theo từng giai đoạn trong 15 level đầu (2026-11-09)

**Vì sao:** màn hướng dẫn kiểu Meowdoku (2026-11-07) dồn cả bốn luật vào một bàn trước level 1, mỗi luật dạy xong lại bắt bấm "Đã hiểu": quá nhiều luật một lúc và người chơi lướt qua nút mà không đọc. Chủ dự án muốn dạy **từng luật một** bằng chính các level, mỗi luật mở đầu bằng một thẻ "Luật mới" có minh họa chạy lặp (kiểu popup "New Feature!" của Catdom, video `D:\GitHub\asset_meowtosis\onboarding cách mới.mp4`: bàn nhỏ, bàn tay chỉ thao tác, tên luật, một câu giải thích, nút "Tiếp tục"), sau đó người chơi tự chơi để hiểu.

**Kịch bản** (4 giai đoạn, thẻ ở level mở đầu mỗi giai đoạn; level 16 trở đi là các level cũ, từ dễ tới khó):

| Giai đoạn | Level | Thẻ | Luật | Ràng buộc thiết kế (đo bằng `tools/onboarding-design.js`) |
|---|---|---|---|---|
| 1 | 1–5 | `Divide` "Lấp kín là thắng" | vuốt là nhân đôi, kín là thắng | mọi đoạn thẳng của khung dài tối đa 2 ô và mỗi vùng chỉ một màu: **mọi nước** đều là lấp ô trống, không thể khử, đẩy, đè hay thua (đã duyệt hết không gian trạng thái) |
| 2 | 6–9 | `Clear` "3 con thì biến mất" | 3 cùng màu thẳng hàng biến mất; mất hết một màu là thua | thắng được chỉ bằng lấp ô; level 6 và 9 **bắt buộc** khử 3 an toàn, level 7 có bẫy khử 3 con cuối, level 8 phải xếp xen kẽ màu |
| 3 | 10–12 | `Push` "Đẩy cả dãy" | ô kề có mèo mà phía sau trống thì cả dãy bị đẩy | không lấp ô nào mà thắng được: **đẩy bắt buộc**; đè không giúp đi tắt |
| 4 | 13–15 | `Replace` "Hết chỗ thì đè" | phía sau hết chỗ thì bản sao đè con khác màu | **đè bắt buộc** (không đè thì không thắng được) |

**15 level và ý đồ từng màn:**

| # | Bàn | Số nước | Ý đồ |
|---|---|---|---|
| 1 | `A.` | 1 | Một nước là thắng: vuốt là nhân đôi, kín là thắng |
| 2 | `B. / ..` | 3 | Bản sao cũng vuốt được, đi được mọi hướng |
| 3 | `C.## / #..# / ##..` | 5 | Bậc thang: phải rẽ hướng liên tục |
| 4 | `A.## / ..## / ##.. / ##.B` | 6 | Hai vùng, hai màu: vùng nào cũng phải kín |
| 5 | `A.##. / ..#.. / ##B.# / #..#. / ###.C` | 11 | Ba vùng ba màu, dòng sông bậc thang ở giữa: bàn lớn đầu tiên, vẫn không thể sai |
| 6 | `BAB / A.A` | 4 | Ô giữa chỉ mèo đỏ lấp được: 3 đỏ biến mất (vẫn còn 1 con), rồi lấp lại hàng dưới. Thấy khử 3 xảy ra một cách an toàn |
| 7 | `AA. / #.B` | 2 | Bẫy: kéo dài hàng đỏ là mất cả 3 con đỏ cuối cùng (thua). 2 trong 5 nước đầu là thua |
| 8 | `A.B / ... / B.A` | 5 | Lấp 3×3 với hai màu ở 4 góc: phải xen kẽ để không hàng, cột nào 3 con cùng màu |
| 9 | `#AA / ..A / .BB` | 6 | Tổng kết: chủ động khử 3 đỏ (vẫn còn 2) để có chỗ, tránh khử hết |
| 10 | `ABB.` | 1 | 3 trong 4 nước đầu là thua (xanh tự kéo dài, đè mất màu); chỉ đỏ đẩy cả dãy là kín |
| 11 | `BAA. / ##..` | 3 | Đẩy trước rồi lấp hàng dưới |
| 12 | `BA.. / ..#.` | 5 | Phải xếp một mèo xanh dưới chân trước rồi mới đẩy để chen xanh vào hàng trên |
| 13 | `B.B / A.A` | 3 | Cùng khung với level 6 (gợi nhớ): xanh đè một con đỏ để hai hàng xen kẽ màu |
| 14 | `.#.B / ..A#` | 5 | Đè giữa chừng để đổi màu một ô, tránh 3 đỏ thẳng hàng; không nước đầu nào thua |
| 15 | `B... / A#..` | 6 | Tổng kết: mèo đỏ kẹt ở góc, đè lên xanh để thoát ra rồi đẩy cả dãy; dùng đủ lấp, đè, đẩy |

**Cách tìm:** `tools/onboarding-design.js` duyệt hết không gian trạng thái của một bàn nhỏ, xếp loại từng nước (lấp, đẩy, đè; sau nước đó có khử 3 hay thua) và tính số nước ít nhất khi chỉ được dùng một số loại nước (`chỉ-lấp`, `lấp+khử`, `lấp+đẩy`, `không-đè`), tỷ lệ trạng thái bị kẹt (còn sống mà hết đường thắng) và số nước đầu tốt/thua. Giai đoạn 1 dựng tay theo quy tắc đoạn thẳng ≤ 2. Giai đoạn 2–4 tìm bằng cách **duyệt hết** các bàn 2×3, 3×2, 2×4, 4×2 (mỗi ô `#`, `.`, `A`, `B`, bỏ trùng khi xoay, lật, đổi màu) lọc theo ràng buộc giai đoạn, rồi chọn bằng tay theo ý đồ, độ rõ ràng và sự nối tiếp giữa các màn.

**Phát hiện khi đo:** trong 815 bàn nhỏ mà khử 3 là bắt buộc khi chỉ lấp ô, **không bàn nào** có lời giải chỉ lấp ngắn bằng lời giải dùng cả đẩy, đè: khử 3 làm mất 3 ô phải lấp lại, còn đẩy, đè thì không. Vì vậy level 6 và 9 khai `par` và `solution` theo lời giải chỉ lấp (4 và 6 nước); người chơi lỡ dùng đè, đẩy thì thắng nhanh hơn và vẫn đủ 3 sao. Gợi ý (Mèo mách) đi theo lời giải đã lưu nên không chỉ nước đè ở giai đoạn này. Test C# `SolverFindsPar` cho phép par của khối 1 lớn hơn số nước ít nhất.

**Sắp lại danh sách:** 5 level cơ bản cũ (`AA./##B`, `.B/.A`, `##B#/AA..`, dấu cộng, chữ T `.B./#A#`) bị bỏ vì trùng bài với khối mới; 11 level còn lại của khối cũ thành khối 2 "Phòng nuôi cấy: ôn bốn luật trên bàn lớn dần" (level 16–26). Tổng 98 level, 10 khối. Mọi level vẫn giải được, `npm test` và 420 test C# qua.

**Unity:** thẻ "Luật mới" dùng lại `MechanicPopup` (tiêu đề đổi từ "CƠ CHẾ MỚI" thành "LUẬT MỚI!"), hiện ở level khai `card` là một trong bốn luật cơ bản và chỉ lần đầu gặp; minh họa chạy cả chuỗi nước (`MechanicCard.demoMoves`) và dừng một nhịp ở bàn ngay sau khi đặt bản sao để thấy 3 con biến mất. Màn hướng dẫn trước level 1 (`OnboardingPresenter`) đã tắt, nút cheat "Hướng dẫn" nay xoá dấu đã xem bốn thẻ rồi mở level 1. Đã thử trong Play: thẻ ở level 1 và level 6 hiện đúng, minh họa chạy, "Tiếp tục" đóng thẻ.

**Chưa làm:** chưa chơi trọn 15 level bằng tay trong Unity; thua ở các level dạy luật vẫn mất tim như mọi level; code màn hướng dẫn cũ (`OnboardingPresenter`, `OnboardingView`, `Levels/Onboarding.txt`) còn trong dự án, chưa xoá hẳn.

**Sửa cùng ngày: bỏ thẻ "Luật mới", dùng lại bóng chat.** Chủ dự án thấy thẻ popup nhiều chữ quá. Bốn level mở đầu mỗi luật (1, 6, 10, 13) nay dùng hướng dẫn trong level như trước (`tut`): bàn tay chỉ đúng một nước, phần không liên quan tối đi, bóng chat gỗ phía trên bàn chỉ một câu ngắn: "Vuốt mèo để nhân đôi. Lấp kín là thắng!", "3 mèo cùng màu liên tiếp nhau sẽ triệt tiêu nhau" rồi (bước 2, sau khi 3 đỏ biến mất) "Lấp tiếp nhé, đừng để trống ô nào", "Phía sau còn chỗ trống: cả dãy bị đẩy đi!", "Hết chỗ đẩy thì bản sao đè lên mèo khác màu!". Sau nước đó người chơi tự chơi tiếp. `card` vẫn khai để bản đồ gắn nhãn "mới"; `MechanicPopup` không hiện cho bốn luật này nữa. Đã thử trong Play ở level 1 và 6.

**Thêm bài "không để tuyệt chủng" ở level 7 (2026-11-09).** Chưa có lúc nào dạy rõ luật "mất hết một màu là thua". Level 7 (`AA. / #.B`) dùng cơ chế bẫy dạy luật có sẵn (`trap`): lần đầu vào màn, bàn tay chỉ nước bẫy, bóng chat "Thử nối dài hàng mèo đỏ xem!"; 3 mèo đỏ cuối cùng biến mất, hiện "OHH NOO! HẾT SẠCH MÈO ĐỎ!" (không mất tim), rồi bàn chơi lại từ đầu với bóng chat "Hết mèo đỏ là thua! Thử cách khác nhé" và bàn tay chỉ nước đúng (đỏ xuống dưới). Đi xong nước đó, mèo đỏ còn lại được soi sáng kèm "Mỗi màu phải còn ít nhất 1 con mèo!", rồi "Lấp kín mà không để mất màu nào nhé", người chơi tự đi nước cuối. Đã thử trong Play: đủ ba cảnh, tim vẫn 3.

**Hoạt hoạ cho hướng dẫn (2026-11-10).** Bóng chat nay có nhịp và có hình, kèm một nhân vật dẫn chuyện:
- **Mèo cam dẫn chuyện** (`NarratorCat`, Spine `IP_ingame` như avatar thanh trên) đứng dưới đuôi bóng chat. Câu giảng luật: dáng suy nghĩ (`ip_thinking`); vừa thua ở bước bẫy: lo lắng (`ip_worried_start` rồi `ip_worried_loop`); làm xong các bước hướng dẫn: reo mừng (`ip_cheer`) với câu "Giỏi lắm! Giờ tự làm tiếp nhé" trong 3 giây (thay cho toast cũ).
- **Bóng chat** (`TipBubbleView`) nảy ra khi hiện, nảy nhẹ khi đổi câu, thu lại khi tắt; nội dung hiện dần từng ký tự, mỗi hình dừng thêm một nhịp.
- **Hình minh hoạ trong lời thoại** để người lười đọc vẫn hiểu: dòng trên là phương trình bằng hình, dòng dưới là chữ. Viết `:ten:` trong dữ liệu level, `|` là xuống dòng (`Pictograms`); hình lấy từ `Art/UI/Pictograms.png` (tạo bằng `tools/make-pictograms.py`: đầu mèo đỏ, xanh, vàng, khói, mũi tên, ô trống, dấu tích, dấu X) và TMP Sprite Asset `Art/UI/Pictograms.asset`. Bản web đổi các hình này thành emoji.

| Level | Dòng hình | Dòng chữ |
|---|---|---|
| 1 | đỏ → đỏ đỏ | Vuốt để nhân đôi. Lấp kín là thắng! |
| 6 (bước 1) | đỏ đỏ đỏ → khói | 3 mèo cùng màu liên tiếp nhau sẽ triệt tiêu nhau |
| 6 (bước 2) | ô trống → tích | Lấp tiếp nhé, đừng để trống ô nào |
| 7 (bẫy) | đỏ đỏ → đỏ đỏ đỏ | Thử nối dài hàng mèo đỏ xem! |
| 7 (sau khi thua) | đỏ → X | Hết mèo của một màu là thua! Thử cách khác xem nhé |
| 7 (soi mèo đỏ còn lại) | đỏ tích | Mỗi màu phải còn ít nhất 1 con mèo! |
| 10 | đỏ → xanh xanh → ô trống | Phía sau còn chỗ trống: cả dãy bị đẩy đi! |
| 13 | xanh → đỏ → xanh | Hết chỗ đẩy thì bản sao đè lên mèo khác màu! |

Đã thử trong Play ở level 1 và 6: mèo dẫn chuyện đổi dáng theo câu, hình và chữ hiện dần, câu khen hiện sau bước cuối. Lần đầu dùng hình, TMP vẽ sai khung đầu (ô vuông xanh); `TipBubbleView.Prewarm` vẽ trước tất cả hình một lần để tránh.

**Khung cửa sổ cho mèo dẫn chuyện (2026-11-10).** Mèo dẫn chuyện nay ngồi trong một ô cửa sổ tròn (`TipBubble/NarratorWindow`, 270×270, cùng kiểu ô avatar ở thanh trên): lòng đỏ nâu, vành gỗ, đuôi bóng chat chỉ vào cửa sổ. Mèo được **tách làm hai lớp** bằng separator slot của Spine (`SkeletonGraphic.enableSeparatorSlots`, cắt ở **cả bốn slot tay** `handL1`, `handR1`, `hand_like1`, `hand_think1`): `Part[0]` (tai, thân, đầu, mặt) nằm trong mặt nạ tròn `NarratorMask` đặt dưới vành nên phần thân bị vành che; `Part[1..4]` (tay, râu) vẽ đè lên vành. Lúc đầu chỉ cắt ở `handL1`, nhưng Spine bỏ qua slot không có hình: ở dáng suy nghĩ tay trái đổi sang `hand_think1` nên không cắt nữa, cả con mèo rơi về lớp sau và tay bị vành che. Đã kiểm tra tự động 7 động tác, mỗi động tác 3 thời điểm: lúc nào tay cũng ở lớp trên, nên hai tay gác ngang mép cửa sổ. Avatar mèo động ở thanh trên (`TopBar/AvatarCat`, mặt nạ `AvatarCatMask` 150 ở lỗ tròn của thanh) làm cùng cách, sửa lỗi thân mèo lòi ra cùng lớp với tay. Ảnh mặt nạ chung `Art/UI/CircleMask.png`. Ảnh tạo bằng `tools/make-narrator-window.py` (`Art/UI/NarratorWindowBack/Frame.png`). `NarratorCat` gắn trên cửa sổ nên cả khung bật lên và ẩn cùng mèo. Dấu X trong hình minh hoạ được phóng to (`tools/make-pictograms.py` nay phóng ảnh nhỏ lên vừa ô). Đã thử trong Play ở level 1 và level 7 (mèo lo lắng sau bẫy).
