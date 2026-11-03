# Meowtosis — lưu giữ prototype và chuyển sang Unity

Prototype HTML (`prototype/meowtosis.html`) là bản **đặc tả chơi được** của Meowtosis. Tài liệu này ghi lại prototype có những gì, phần nào dùng lại nguyên được trong bản Unity, phần nào chỉ để tham khảo, phần nào phải bỏ, và cách chuyển để không mất gì.

> Cập nhật lần cuối: 2026-11-03 · 61 level · 6 chương · repo riêng `D:\GitHub\Meowtosis` (GitHub `trinhkien123-iuiu/Meowtosis`)

---

## 1. Hiện có gì, đang nằm ở đâu

| Thứ | Vị trí | Trong git? | Bản Unity dùng thế nào |
|---|---|---|---|
| Game (luật, level, giao diện, VFX, SFX) | `prototype/meowtosis.html` (một file) | Chưa commit | Đặc tả + nguồn dữ liệu level và luật |
| Trang chuyển hướng cho link cũ | `prototype/mitoosis.html` | Chưa commit | Chỉ cho prototype (link `…/mitoosis.html` đã gửi người khác vẫn vào được) |
| Các bản cũ | `prototype/archive/` (bản kẹo dẻo, bản trước khi làm đẹp) | Chưa commit | Tham khảo phong cách hình tự vẽ |
| Tiếng mèo (Mixkit) | `prototype/sfx/*.mp3` + `NGUON.txt` | Có (từ 2026-11-03, theo ý chủ repo); giấy phép không cho phân phối lại file đứng riêng nên repo nên để riêng tư | **Dùng được** trong sản phẩm (xem mục 6). Tải lại theo `NGUON.txt` nếu mất |
| Asset Shikacat (mèo, hộp, ô, icon, nhạc) | `prototype/shikacat/` | Có (từ 2026-11-03, theo ý chủ repo); repo nên để riêng tư | **Không dùng được** trong sản phẩm, chỉ là hình tạm |
| Máy chủ chơi thử | `tools/serve.js` (`npm start`, `npm run lan`) | Chưa commit | Chỉ cho prototype |
| Bộ giải, bộ sinh level, test | `tools/` | Chưa commit | Chép bộ giải sang C#; bộ sinh giữ bằng Node hoặc làm Editor tool (mục 9) |
| **Dữ liệu level** | `data/levels.json` (`npm run export`) | Chưa commit | Nhập vào Unity; "đáp án vàng" cho test C# (mục 3.2) |
| Project Unity | `unity/` (chưa tạo) | — | `.gitignore` đã có sẵn các dòng cho Unity |

**Việc nên làm ngay:** chuyển repo GitHub sang riêng tư (đang công khai), rồi commit toàn bộ, kể cả asset Shikacat và mp3. Nếu sau này muốn công khai repo thì phải xoá các asset này khỏi lịch sử git trước, hoặc tạo repo công khai khác không có asset.

---

## 2. Ba nhóm theo khả năng dùng lại

| Nhóm | Gồm | Cách dùng ở Unity |
|---|---|---|
| **Dùng nguyên** | Dữ liệu 61 level, luật chơi, lời thoại hướng dẫn, các con số cân bằng (tim, sao, xu, booster), tiếng mèo Mixkit, phông Baloo 2 | Xuất ra file dữ liệu, nhập vào Unity; luật chép sang C# |
| **Dùng làm đặc tả** | Bố cục màn hình, luồng chơi, hướng dẫn tương tác, danh sách VFX/SFX, cách vẽ khung bàn chơi, hình kẹo dẻo tự vẽ | Quay video/chụp màn hình từng màn; đội art, UI, VFX làm lại theo |
| **Phải bỏ** | Toàn bộ asset Shikacat; âm thanh tổng hợp bằng Web Audio; code giao diện HTML/SVG | Thay bằng art, âm thanh và UI của team |

---

## 3. Dữ liệu level

### 3.1 Định dạng hiện tại

Mỗi level là một phần tử trong mảng `LEVELS` (khối `// ==LOGIC==` của `meowtosis.html`). Lưới viết bằng ký tự:

| Ký tự | Nghĩa |
|---|---|
| `#` | Ngoài cơ thể (không phải ô) |
| `.` | Ô trống |
| `~` | Ô độc (không cần lấp) |
| `A` `B` `C` | Mèo đỏ, xanh, vàng |
| `a` `b` `c` | Mèo ngủ (màu tương ứng) |
| `G` | Vi khuẩn |
| `^` `v` `<` `>` | Virus, mũi tên là hướng lan |

Các trường thêm (đều tuỳ chọn):

| Trường | Ví dụ | Nghĩa |
|---|---|---|
| `goal` | `['..B.']` | Lưới cùng kích thước; chữ cái = hộp mục tiêu cần mèo màu đó |
| `oneway` | `[[0, 1, 'left']]` | Màng một chiều ở ô (hàng 0, cột 1), chỉ cho đi qua theo hướng `left` |
| `kill` | `'G'` | Level diệt khuẩn |
| `tut` | `[[0, 0, 'right', 'Lời thoại']]` | Các bước hướng dẫn: hàng, cột, hướng vuốt, lời thoại |
| `tip` | `'Mẹo: 3 vi khuẩn thẳng hàng cũng bị khử!'` | Một câu mẹo hiện đầu màn, không khoá nước nào, tắt sau nước đầu |

Ví dụ level 17 (nhập môn hộp mục tiêu):

```js
{ rows: ['AB..'], goal: ['..B.'], tut: [
  [0, 0, 'right', 'Đẩy mèo xanh vào hộp xanh'],
  [0, 2, 'right', 'Lấp nốt ô cuối: vừa kín ổ vừa đúng hộp!']] }
```

Chương được khai báo riêng trong mảng `CHAPTERS` (theo chỉ số level):

| Chương | Level | Nội dung |
|---|---|---|
| 1 · Phòng nuôi cấy | 1–16 | Chia, đẩy, thay thế, khử 3; bàn lớn từ level 13 |
| 2 · Vào vị trí | 17–28 | Hộp mục tiêu |
| 3 · Màng và độc | 29–37 | Màng một chiều, ô độc |
| 4 · Ngủ và virus | 38–48 | Mèo ngủ (cả thức dây chuyền), virus (cả hai virus) |
| 5 · Diệt khuẩn | 49–54 | Diệt khuẩn (cả khử 3 vi khuẩn) |
| 6 · Đại gia đình | 55–61 | Bàn 5×5–5×6, đông mèo, trộn mọi cơ chế |

### 3.2 Xuất ra JSON

`npm run export` (`tools/export-levels.js`) đọc khối LOGIC của game, chạy bộ giải và ghi `data/levels.json`: mọi level kèm **chương**, **số nước tối ưu (`par`)** và **một lời giải**. `npm test` báo lỗi nếu file này cũ hơn level trong game, nên dữ liệu cho Unity không bao giờ lệch với bản chơi được.

```json
{ "index": 17, "chapter": 2, "chapterName": "Vào vị trí", "rows": ["AB.."], "goal": ["..B."],
  "tut": [[0, 0, "right", "Đẩy mèo xanh vào hộp xanh"], [0, 2, "right", "Lấp nốt ô cuối: vừa kín ổ vừa đúng hộp!"]],
  "par": 2, "solution": [[0, 0, "right"], [0, 2, "right"]] }
```

`solution` là các nước `[hàng, cột, hướng]` tính từ góc trên trái. Các trường còn lại giữ nguyên như trong game (mục 3.1).

### 3.3 Định dạng đề xuất cho Unity

Đi theo cách repo `Puzzle_Prototype` (game Glimmith) đang làm với file `.pzl` (`D:\GitHub\Puzzle_Prototype\Docs\design\level-format.md`): **mỗi level một file văn bản**, Unity tự nhập thành `LevelAsset` (ScriptableObject) bằng `ScriptedImporter` trong một Editor assembly. Sửa level là sửa file, diff được trên git. Bước đầu có thể nhập thẳng `data/levels.json` rồi tách thành từng file sau.

```text
id: meow-c2-01            // khoá lưu tiến độ, cố định, KHÔNG phụ thuộc thứ tự
chapter: 2
par: 2                    // số nước tối ưu (bộ giải tính, test kiểm tra lại)

rows
AB..

goal
..B.

oneway 0 1 left           // có thể lặp nhiều dòng
kill G

tut 0 0 right  Đẩy mèo xanh vào hộp xanh
tut 0 2 right  Lấp nốt ô cuối: vừa kín ổ vừa đúng hộp!
```

Ghi chú:
- **`id` cố định.** Prototype hiện lưu tiến độ theo *nội dung* level, vì chưa có id. Bản Unity nên có id riêng để sửa nội dung level mà không mất sao của người chơi.
- **Lời thoại nên tách ra bảng dịch** (`tut` chỉ giữ khoá), để làm đa ngôn ngữ về sau.
- **Đưa vào test `LevelCatalogTests`:** mọi level giải được, `par` khớp bộ giải, các bước `tut` đi được và (nếu đi trọn) dẫn tới thắng đúng bằng `par`, hai level không trùng khung (kể cả xoay/lật).

---

## 4. Luật chơi: chép sang C#

Luật nằm trọn trong khối `// ==LOGIC==`, viết bằng hàm thuần (không đụng giao diện), nên chép sang tầng **Domain** C# gần như 1–1.

| Hàm JS | Việc | Gợi ý C# |
|---|---|---|
| `parseLevel` | Đọc lưới, hộp, màng; đánh thức sẵn mèo ngủ đứng cạnh mèo cùng màu | `LevelLoader` → `BoardShape` + trạng thái ban đầu |
| `step` | Ô kế tiếp theo hướng, tính cả màng chặn | `BoardShape.Step(cell, dir)` |
| `swipe` | Một lần vuốt: chia, đẩy cả dãy hoặc thay thế, rồi `resolve` | `SwipeCommand` |
| `resolve` | Kết thúc lượt (thứ tự bên dưới) | `TurnResolver` |
| `findRuns` | Dãy từ 3 cùng màu trở lên (virus không tính) | `RunFinder` |
| `mutate`, `transplant` | Booster Đột biến, Cấy ghép | `IBooster` |
| `isFull`, `isComplete`, `isLost`, `extinct` | Thắng/thua | `WinRule`, `LoseRule` |
| `solve` | A* tìm lời giải ngắn nhất (Gợi ý, tính `par`). Ước lượng = số mèo còn thiếu để kín ổ (không tính virus, vi khuẩn); mỗi nước chỉ sinh một mèo nên ước lượng không vượt thực tế và lời giải vẫn ngắn nhất | `Solver` (chạy nền, có giới hạn trạng thái) |

**Thứ tự kết thúc một lượt** (phải giữ đúng, nếu không level sẽ lệch lời giải):
1. Đặt bản sao: chia vào ô trống; ô kề có mèo thì đẩy cả dãy tới ô trống gần nhất; không còn ô trống thì bản sao **thay thế** ô kề (thay thế cùng màu = nước không hợp lệ).
2. Ô độc giết mọi thứ đang đứng trên nó.
3. Khử mọi dãy ≥ 3 cùng màu (mèo ngủ cũng tính, virus không tính).
4. Mỗi virus lan 1 ô theo hướng của nó: vào ô trống hoặc ăn mèo; không đi vào virus khác, bị màng chặn.
5. Ô độc giết tiếp (virus vừa lan vào ô độc).
6. Mèo ngủ đứng cạnh mèo cùng màu đang thức thì thức dậy (lan dây chuyền).

**Thắng:** mọi ô (trừ ô độc) có mèo, không phải virus · mọi hộp có mèo đúng màu · không còn vi khuẩn.
**Thua:** một màu mèo có mặt lúc đầu không còn con nào (kể cả mèo ngủ). Vi khuẩn và virus không tính là loài.

**Kiểm tra chéo khi chép:** dùng file JSON xuất ở mục 3.2 làm "đáp án vàng". Với mỗi level, test C# phải cho ra cùng `par`, và chạy lại `solution` của JS phải thắng. Các ca test luật trong bộ test prototype (mục 9) chép sang NUnit.

---

## 5. Hệ thống meta và các con số

| Hệ thống | Giá trị trong prototype | Ghi chú |
|---|---|---|
| Tim | 3 tim, thua mất 1, hồi 1 tim mỗi 10 phút | `MAX_HEARTS`, `REGEN_MS` |
| Booster | Mỗi loại 3 lượt; hoàn tác nước dùng booster thì trả lại lượt | Đột biến, Cấy ghép, Hồi sinh (chỉ dùng khi vừa thua, trả lại tim) |
| Sao | Bằng `par`: 3 sao · dư ≤ 2 nước: 2 sao · còn lại: 1 sao | Booster tính là một nước |
| Xu | 10 xu mỗi sao, cộng mỗi lần thắng | `COINS_PER_STAR`; chưa có chỗ tiêu; chơi lại vẫn được thưởng đủ |
| Tiến độ | Sao và kỷ lục tốt nhất mỗi level | Unity: lưu theo `id` level |
| Mở khoá | Theo thứ tự, **đang tắt** để test (`LOCK_IN_ORDER = false`) | |
| Gợi ý | Còn đi đúng lời giải đã tính thì lấy luôn nước tiếp (tức thì); lệch thì giải lại bằng A* tối đa 0,6 giây; không ra thì chỉ cần hoàn tác mấy bước để về đường thắng | Bàn lớn (16 mèo) mỗi trạng thái có ~60 nước, duyệt sâu rất tốn: Unity nên giữ cả ba lớp này và chạy nền |

**Onboarding (3 lớp):**
1. **Thẻ hướng dẫn** hiện lần đầu gặp mỗi cơ chế (`MECHANICS`): ảnh trước/sau một lần vuốt, ảnh "sau" do chính luật tính ra.
2. **Hướng dẫn tương tác** ở 10 level (1, 2, 4, 9, 17, 29, 33, 38, 43, 49): làm tối bàn trừ ô cần chú ý, bàn tay chỉ hướng vuốt, bong bóng lời thoại; chỉ nhận đúng nước được chỉ; hoàn tác thì lùi bước.
3. **Mẹo đầu màn** (`tip`) ở 5 level có yếu tố mới nhỏ (13 bàn lớn, 41 thức dây chuyền, 46 hai virus, 51 khử 3 vi khuẩn, 55 bàn 5×5): chỉ là bong bóng lời thoại, không khoá nước.

---

## 6. Asset: cái gì giữ, cái gì thay

### 6.1 Giấy phép

| Nguồn | Dùng trong sản phẩm? | Lưu ý |
|---|---|---|
| Shikacat (Tripledot) | **Không** | Asset tách từ game khác, chỉ để thử nội bộ. Xoá khỏi máy build, không đưa lên store |
| Mixkit (tiếng mèo) | **Được** | Mixkit Sound Effects Free License: dùng trong game thương mại, không cần ghi nguồn; không phân phối lại file âm thanh đứng riêng. Kiểm tra lại điều khoản lúc phát hành |
| Baloo 2 (Google Fonts) | **Được** | SIL Open Font License. Unity: tạo font asset TextMeshPro, nhớ bộ ký tự tiếng Việt |
| Hình kẹo dẻo, khung bàn, hiệu ứng | **Được** | Tự vẽ bằng code trong prototype |

### 6.2 Danh sách art cần làm (thay hình tạm)

| Hạng mục | Prototype đang dùng | Cần cho bản Unity |
|---|---|---|
| Mèo 3 màu | Mèo Shikacat đỏ/xanh/vàng | Mèo của team, **mỗi màu một đặc điểm riêng ngoài màu** (nơ, tai, hoa văn) để người mù màu phân biệt được |
| Mèo vui | Mèo mắt sao/mắt tim (Shikacat) | Biểu cảm khi vào đúng hộp và khi thắng |
| Mèo ngủ | Mèo xám + bịt mắt vẽ thêm | Mèo ngủ **giữ nguyên màu**, nhắm mắt + chữ z |
| Virus, vi khuẩn | Tự vẽ (quả cầu gai, viên nang có râu) | Vẽ lại cùng phong cách |
| Khung bàn, ô | Khung gỗ tự vẽ + ô kem (`Tile_Board` Shikacat) | Khung 9-slice hoặc tự dựng theo đường viền liền (xem `outlinePath`) |
| Ô độc, màng | Ô xanh + xương cá (Shikacat), dải thạch xanh ngọc | Vẽ lại |
| Hộp mục tiêu | `Panel_Box_*` (Shikacat) | Hộp theo màu của team |
| UI | Tim, ổ khoá (Shikacat); nút, thẻ, xu tự vẽ | Bộ UI của team |
| Minh hoạ thắng/thua | Mèo trên phao, mèo khóc (Shikacat) | Minh hoạ của team |
| Nền | Nền xanh (Shikacat) | Nền của team |

### 6.3 Âm thanh

| Sự kiện | Prototype | Bản Unity |
|---|---|---|
| Mèo chia (ngẫu nhiên, cao độ theo màu) | 4 file Mixkit, đổi cao độ: xanh ×0,86, đỏ ×1, vàng ×1,16 | Giữ hoặc thu thêm biến thể |
| Mèo giận (nước không hợp lệ) | Mixkit | Giữ |
| Nút, popup, thắng, thua, nhạc nền, vào đúng hộp, khử 3 | Shikacat | **Thay** |
| Đẩy, thay thế, ô độc, virus ăn, thức dậy, booster, hồi sinh, xu bay, ka-ching | Tổng hợp Web Audio | Sound designer làm file thật |

---

## 7. VFX cần dựng lại (Unity Particle System / tween)

| Sự kiện | Hiệu ứng trong prototype |
|---|---|
| Chia | Mèo mẹ co giãn theo hướng vuốt; bản sao bật ra (đàn hồi) |
| Khử 3 | Mèo vỡ thành giọt màu, sóng tròn, chữ "Khử 3!", rung bàn |
| Ô độc | Khói xanh bốc lên; bọt sủi liên tục trên ô |
| Virus ăn | Vệt tím bắn ra; gai virus xoay chậm |
| Thức dậy | Lấp lánh, nảy lên, chữ z biến mất |
| Vào đúng hộp | Hộp sáng lên, lấp lánh, mèo đổi mắt sao |
| Thắng | Pháo giấy, cả ổ mèo nhảy + mắt sao; sao bay vào bảng thắng |
| Thua | Rung bàn, mèo xụ xuống |
| Booster | Đột biến: vòng cầu vồng · Cấy ghép: vệt sáng bay tới ô đích · Hồi sinh: loé vàng, sao bay lên, tim bay về |
| Xu | 4–12 đồng bay theo đường cong từ bảng thắng lên ô xu; ô xu nảy và đếm dần |
| Mèo lúc nghỉ | Thở phập phồng, chớp mắt; nghiêng người theo hướng vuốt |
| Hướng dẫn | Làm tối theo hình khung, bàn tay lướt, vệt chấm chạy |

---

## 8. Kế hoạch chuyển

1. **Đóng băng prototype:** commit repo này, gắn tag (ví dụ `meowtosis-proto-v1`), quay video từng màn hình làm tài liệu tham khảo.
2. **Tạo project Unity trong `unity/`** (cùng repo, `.gitignore` đã sẵn), theo bố cục ở mục 8b.
3. **Xuất dữ liệu:** `npm run export` ra `data/levels.json`; đổi sang file level Unity (mục 3.3), đặt `id` cho từng level.
4. **Domain C#:** chép luật (mục 4) và test; test vàng so `par` và lời giải với `data/levels.json`.
5. **Bộ giải + công cụ level:** chép bộ giải A* sang C# (Gợi ý, kiểm tra level trong test); bộ sinh level có thể giữ bằng Node (`tools/`) hoặc viết lại thành Editor tool.
6. **Application:** phiên chơi, hoàn tác, booster, tim, xu, tiến độ theo `id` (mục 8b).
7. **Presentation:** bàn chơi, HUD, bản đồ chương, thẻ hướng dẫn, hướng dẫn tương tác, mẹo đầu màn, bảng thắng/thua (View câm + Presenter).
8. **Art, âm thanh, VFX của team** thay hình tạm (mục 6–7); không chép asset Shikacat vào project Unity.
9. **Cân bằng lại** khi có playtest: độ khó từng chương, mốc sao, giá booster bằng xu.

---

## 8b. Kiến trúc Unity đề xuất

Lấy theo khuôn đã dùng cho game Glimmith (`D:\GitHub\Puzzle_Prototype\ARCHITECTURE.md`), rút gọn cho Meowtosis:

```text
unity/Assets/_Project/
├── Code/Domain/        Meowtosis.Domain (asmdef, noEngineReferences: true) — C# thuần, không using UnityEngine
│                       Board, Cell, Swipe, TurnResolver (thứ tự mục 4), WinRule/LoseRule, Solver (A*)
├── Code/Application/   Meowtosis.Application — GameSession: Execute(ICommand) / Undo; booster trả về command
│                       (nên luôn hoàn tác được); tim, xu, sao; IProgressStore, IClock, IRandom là interface
├── Code/Presentation/  Meowtosis.Presentation — View câm (BoardView, HUD, CoachView, VFX) + Presenter;
│                       PlayerPrefsProgressStore cài IProgressStore
├── Code/Bootstrap/     Composition root: nối mọi thứ ở một chỗ
├── Code/Editor/        LevelImporter (ScriptedImporter: file level → LevelAsset)
├── Levels/             File level (mục 3.3)
└── Tests/              NUnit: luật (chép từ tools/test-*.js), test vàng từ data/levels.json, LevelCatalogTests
```

Nguyên tắc chính: Domain không biết gì về Unity nên test chạy không cần mở scene; mọi thay đổi trạng thái đi qua `GameSession` để hoàn tác thống nhất; giao diện chỉ vẽ theo trạng thái và gửi lệnh, không tự sửa luật.

---

## 9. Bộ công cụ của prototype

Nằm ở `tools/` (đường dẫn tương đối, chạy bằng Node ở thư mục gốc repo, hoặc qua `npm run …`, xem README). Cách dùng từng công cụ, định dạng job và quy trình làm level nằm ở [level-design.md](level-design.md).

| File | Việc |
|---|---|
| `lib.js` | Đọc khối LOGIC trong file HTML để chạy bằng Node; khung chuẩn hoá; phân tích level |
| `gen.js`, `run-jobs.js`, `jobs/` | Bộ sinh level theo job, chạy song song |
| `analyze.js` | Bảng chỉ số độ khó / bất ngờ của mọi level |
| `check.js` | Kiểm tra cả danh sách level (giải được, đủ nhanh, không trùng khung, hướng dẫn, chương) |
| `test-lose.js`, `test-boosters.js`, `test-mechanics.js` | Test luật, booster, các cơ chế |
| `bfs-compare.js` | So bộ giải A* của game với BFS gốc |
| `export-levels.js` | Xuất level ra JSON (mục 3.2) |

---

## 10. Các quyết định thiết kế đã chốt

| Quyết định | Lý do |
|---|---|
| Mọi level cùng mục tiêu **lấp kín**; cơ chế chỉ thêm điều kiện | Góp ý playtest: đổi mục tiêu giữa chừng làm người chơi ngợp |
| Bảng nhiệm vụ luôn hiện (`Lấp kín 3/9 · Hộp 1/2 · Vi khuẩn còn 2`) | Người chơi luôn biết còn thiếu gì |
| Mỗi level một khung riêng (61 khung, không trùng kể cả xoay/lật) | Mỗi màn có cảm giác mới |
| Không dùng khung vòng khuyên, chữ C | Không bao giờ lấp kín được (tối đa 6 mèo) |
| Màu là danh tính, trạng thái báo bằng kênh khác | Mèo ngủ bị xám làm người chơi không biết màu |
| Khử 3 và ô độc được dạy bằng hướng dẫn tương tác thay vì level nhỏ riêng | Trên khung nhỏ, lời giải ngắn nhất không bao giờ cần chúng (chúng làm mất mèo) |
| Hướng dẫn đi trọn lời giải phải bằng `par` (riêng level 9 chỉ dẫn 4/8 nước đầu, phần còn lại người chơi tự làm) | Người làm theo luôn được 3 sao |
| Tên game Meowtosis | Gợi mèo + nguyên phân, tách khỏi tên Mitoosis của Le Slo (game lấy cảm hứng cơ chế) |
| Bàn lớn (4×4 – 5×6) luôn có nhiều mèo đặt sẵn | Mỗi nước chỉ sinh một mèo: bàn lớn ít mèo thì dài lê thê và bộ giải không kham nổi |
| Mỗi chương đi theo nhịp dạy → luyện → mở rộng → bất ngờ → trùm | Người chơi quen dần rồi mới gặp mẹo; chi tiết ở [level-design.md](level-design.md) |
