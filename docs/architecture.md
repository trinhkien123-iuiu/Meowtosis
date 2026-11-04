# Meowtosis — kiến trúc bản Unity

Tài liệu gốc về kiến trúc của project Unity (`unity/`, Unity 2022.3.62f3, URP 2D). Sửa cấu trúc code thì sửa tài liệu này cùng lúc.

- Luật chơi chi tiết: [mechanics.md](mechanics.md). Đây là nguồn chuẩn cho luật, code C# phải khớp với nó.
- Lịch sử chuyển từ prototype, asset, VFX, số liệu meta: [unity-handoff.md](unity-handoff.md).

---

## 1. Tổng quan: bốn tầng, phụ thuộc một chiều

```text
 Bootstrap ──────────► Presentation ──► Application (Gameplay) ──► Domain
 (nối mọi thứ           (View + Presenter,  (phiên chơi, lệnh,          (luật chơi thuần C#,
  ở một chỗ)             MonoBehaviour)      hoàn tác, sao)              không biết Unity)
```

| Tầng | Assembly | Được dùng | Không được dùng |
|---|---|---|---|
| **Domain** | `Meowtosis.Domain` | Chỉ .NET (`noEngineReferences: true`) | `UnityEngine`, mọi tầng khác |
| **Application** | `Meowtosis.Gameplay` (`noEngineReferences: true`) | Domain | `UnityEngine`, scene, asset |
| **Presentation** | `Meowtosis.Presentation` | Gameplay, Domain, UnityEngine, uGUI | Tự sửa luật: chỉ gửi lệnh và vẽ lại theo kết quả |
| **Bootstrap** | `Meowtosis.Bootstrap` | Mọi tầng | Logic game: chỉ tạo và nối đối tượng |
| Editor | `Meowtosis.Editor` *(chưa làm)* | Mọi tầng, UnityEditor | Chạy trong bản build |
| Test | `Meowtosis.Tests.EditMode` | Domain, Gameplay | Presentation (để chạy được cả ngoài Unity) |

Tầng Application đặt tên **Gameplay** chứ không phải `Application`: namespace `Meowtosis.Application` sẽ che mất `UnityEngine.Application` trong mọi code thuộc `Meowtosis.*` (lỗi biên dịch khó hiểu khi gọi `Application.dataPath`).

Vì sao chia như vậy:
- Luật nằm ở Domain thuần C#, nên test chạy không cần scene và chạy được cả ngoài Unity (`npm run test:unity`, khoảng 10 giây).
- Giao diện đổi bao nhiêu cũng không làm sai luật.
- Mọi thay đổi trạng thái đi qua một chỗ (Application), nên hoàn tác, lưu ván và gợi ý dùng chung một luồng.

---

## 2. Thư mục

```text
unity/Assets/_Project/
├── Code/
│   ├── Domain/                 Meowtosis.Domain.asmdef
│   │   ├── Board/              Direction, Species, Piece, Board
│   │   ├── Levels/             LevelDefinition, LevelCatalogParser, Level, LevelSymbols, Move, Membrane, TutorialStep
│   │   ├── Goals/              ILevelGoal, FillGoal, TargetBoxGoal, KillGoal, GameOutcome
│   │   ├── Turn/               ITurnStep, TurnResolver, PoisonStep, ClearRunsStep, VirusSpreadStep, WakeStep, TurnStage, TurnEvent
│   │   ├── Moves/              SwipeRule, BoosterRules, MoveResult
│   │   └── Solving/            Solver, SolveResult
│   ├── Gameplay/               Meowtosis.Gameplay.asmdef: GameSession, LevelRun, PlayerProgress, IGameCommand (Swipe/Mutate/Transplant),
│   │                           StarRating + StarMeter, HintAdvisor, GoalProgress, SpeciesCensus, Mechanic + MechanicDetector, LevelMap,
│   │                           GameRules, IClock, IProgressStore
│   ├── Presentation/           Meowtosis.Presentation.asmdef: GamePresenter, GameViews, BoosterTargeting, GameTexts
│   │   ├── Board/              IBoardView, BoardView, BoardFrame, DistanceTransform, MovePlayback, BoardIdle, PieceView, PieceMotions, BoardLayout, BoardArt, SpriteSizing
│   │   ├── Home/               IHomeView + HomeTab, HomeView, HomePager, HomeBottomBar, HomeToggleRow, HomePresenter,
│   │   │                       LevelMapView, LevelMapNode, LevelMapEffects
│   │   ├── Hud/                IHudView, HudView, SafeAreaFitter, BoosterButton, StarMeterView, SpeciesChip, CoinCounterView
│   │   ├── Popups/             PopupView, WinPopup, LosePopup, SettingsPopup, NoHeartsPopup, MechanicPopup, MechanicCard,
│   │   │                       MechanicDemoView, DemoCell (popup nào cũng kèm interface của nó trong cùng file)
│   │   ├── Effects/            BoardEffects, WinEffects, UiSparkle, SoulEffect, JawsEffect, DizzyEffect, FloatingText, Spin, TutorialHand
│   │   ├── Audio/              ISoundPlayer + SoundId, SoundPlayer, GameSounds
│   │   ├── Platform/           PlayerPrefsProgressStore, SystemClock, ISceneNavigator, SceneNavigator
│   │   ├── Input/              SwipeInput
│   │   └── Camera/             CameraFraming
│   ├── Bootstrap/              Meowtosis.Bootstrap.asmdef: GameBootstrap, HomeBootstrap
│   └── Editor/                 (chưa làm) công cụ trong Editor
├── Levels/LevelCatalog.txt     71 level, sinh từ prototype (mục 5)
├── Prefabs/Board/              BoardFrame, Tile, ToxicTile, Box, Membrane, Piece, Highlight, HintArrow,
│                               TutorialHand
├── Prefabs/Home/               LevelMapNode, MapPathDot, MapBackgroundTile, MapRing
├── Prefabs/Audio                 SoundPlayer và nhóm AudioSource, dùng chung cho hai scene
├── Prefabs/Hud/                BoosterButton, SpeciesChip, ColorChoice, DemoCell, DemoMembrane, WinSparkle, WinConfetti, WinCoin
├── Prefabs/Effects/            hạt: Burst, Ring, Sparkle, Smoke, Splat, Rainbow, Confetti, Glow; hình: Soul, Jaws, Dizzy, FloatingText
├── Data/                       PieceMotions (đường cong hoạt ảnh quân), GameSounds (bảng âm thanh), Mechanics/ (10 thẻ cơ chế)
├── Art/BoardArt.asset          hình đổi theo loài (mèo từng màu, hộp, vi khuẩn, virus, mặt): đổi art ở đây, không đụng code
├── Art/Home/                    ảnh nền màn Home và bản đồ level, tạo bằng Figma AI (file "Meowtosis Home Art")
├── Art/Effects/                material của hiệu ứng hạt
├── Art/Placeholder/            hình tạm vẽ theo prototype (vi khuẩn, virus, ô độc, màng, bịt mắt ngủ, mặt mèo, hạt, hồn mèo…), sinh bằng tools/make-placeholder-art.py
├── Art/UI/                     nút tròn, huy hiệu, thanh sao, bong bóng, icon; sinh bằng tools/make-ui-art.py
├── Art/UI/Bounce/              nút cài đặt, ô level, tim, panel, nút xanh/cam, huy hiệu lấy từ dự án Bounce_Blast (tools/import-bounce-ui.py)
├── Fonts/                      Baloo 2 (ExtraBold, Bold; OFL.txt), font TMP động + material viền chữ (Outline, Title)
├── Art/Shikacat/              Cats/ (12 màu mèo, ảnh lúc thua, hộp màu), UI/ (nền, ô bàn, tim, khoá…): Sprite
├── Audio/                      Shikacat/ (nhạc nền, thắng, thua, nút…), Meows/ (5 tiếng mèo Mixkit, giấy phép ở NGUON.txt),
│                               Synth/ (đẩy, thay thế, lấp lánh, ô độc, virus đớp…, sinh bằng tools/make-sfx.py)
├── Scenes/Home.unity           màn đầu tiên (Build Settings số 0): avatar, xu, tim, nút Play, thanh Home/Shop/Setting
├── Scenes/Game.unity           màn chơi (Build Settings số 1); về Home bằng nút trong popup cài đặt
└── Tests/EditMode/             Meowtosis.Tests.EditMode.asmdef
    ├── Support/                TestLevel, Playthrough, Rules, TestCatalog
    ├── MechanicsExampleTests   45 ví dụ của mechanics.md
    ├── GameSessionTests        phiên chơi, hoàn tác, chơi lại, sao, tiến độ nhiệm vụ
    ├── LevelMapTests           mở khoá, chơi lại level cũ, level hiện tại, đánh dấu cơ chế mới
    ├── LevelRunTests           tim, xu, booster (tiêu, trả khi hoàn tác, mua), hồi sinh, thanh sao, gợi ý, thẻ cơ chế
    └── LevelCatalogTests       71 level: bàn đầu hợp lệ, lời giải thắng đúng par, bộ giải ra đúng par, hướng dẫn đi được, bẫy thua vì khử

tools/dotnet-tests/             Chạy cùng bộ test trên bằng .NET, không cần mở Unity
```

**Một namespace cho mỗi assembly** (`Meowtosis.Domain`, `Meowtosis.Tests`…). Thư mục con chỉ để gom file cho dễ tìm.

---

## 3. Quy tắc viết code

| Quy tắc | Ví dụ |
|---|---|
| Tên đầy đủ, đọc là hiểu; không viết tắt khó hiểu, không tên một chữ (trừ không có) | `nextPieceId`, `closedExits`, không phải `nid`, `cx` |
| **Không gạch dưới, không tiền tố** cho field | `private readonly Board board;` không phải `_board`, `m_board` |
| Kiểu, phương thức, property: PascalCase. Biến, field, tham số: camelCase | `TrySwipe`, `CellsToFill`, `runStart` |
| Field Unity hiện trong Inspector: `[SerializeField] private` + camelCase | `[SerializeField] private PieceView pieceTemplate;` |
| Thử làm một việc có thể bị từ chối: mẫu `TryXxx(..., out result)` trả `bool` | `SwipeRule.TrySwipe`, `BoosterRules.TryMutate` |
| Domain **bất biến**: luật trả về `Board` mới, không sửa bàn cũ | Hoàn tác chỉ cần giữ lại `Board` trước |
| Comment giải thích **luật hoặc lý do**, không kể lại code | `// mèo ngủ đứng như bức tường` |
| Tên (lớp, phương thức, biến) bằng tiếng Anh; comment, thông báo lỗi, tài liệu bằng tiếng Việt (giống prototype và Glimmith) | |
| C# 9, .NET Standard 2.1 (giới hạn của Unity 2022.3): không `record`, không `init` | |

---

## 4. Tầng Domain

### 4.1 Các lớp chính

| Lớp | Một việc duy nhất |
|---|---|
| `Level` | Phần cố định của màn: khung bàn, ô độc, hộp, màng, các điều kiện thắng. Ô đánh số `row * Width + column`; `Step(cell, direction)` đã tính sẵn cả màng |
| `Board` | Một thời điểm của ván: `Level` + quân ở từng ô + id cho quân mới. Bất biến. `ToKey()` dùng làm khoá trạng thái cho bộ giải |
| `Piece` | Một con mèo, vi khuẩn hoặc virus: loài, đang ngủ, hướng (virus), id để giao diện theo dõi khi nó di chuyển |
| `SwipeRule` | Một lần vuốt: nhân đôi, đẩy cả dãy hoặc thay thế, rồi gọi `TurnResolver` |
| `BoosterRules` | Đột biến, Cấy ghép; cũng kết thúc lượt qua `TurnResolver` |
| `TurnResolver` | Chạy lần lượt các `ITurnStep` của cuối lượt |
| `PoisonStep`, `ClearRunsStep`, `VirusSpreadStep`, `WakeStep` | Mỗi lớp một luật cuối lượt |
| `ILevelGoal` + `FillGoal`, `TargetBoxGoal`, `KillGoal` | Mỗi lớp một điều kiện thắng |
| `GameOutcome` | Thắng / thua / loài tuyệt chủng |
| `Solver` | A* tìm lời giải ngắn nhất (Gợi ý, kiểm tra level) |
| `LevelCatalogParser` | Đọc `LevelCatalog.txt` thành `LevelDefinition` |

### 4.2 Một nước đi

```text
SwipeRule.TrySwipe(board, cell, direction, out MoveResult result)
  1. đặt bản sao  ─► result.Placed      (đẩy: result.Pushes, thay thế: result.Replaced)
  2. TurnResolver ─► result.Stages:
        PoisonStep        ô độc giết         TurnEvent Poisoned
        ClearRunsStep     khử dãy ≥ 3        TurnEvent Cleared
        VirusSpreadStep   virus lan 1 ô      TurnEvent Eaten, VirusSpread
        PoisonStep        ô độc giết tiếp    TurnEvent Poisoned
        WakeStep          mèo ngủ thức dậy   TurnEvent WokeUp
  3. result.After = bàn sau cùng
```

Giao diện diễn lại `MoveResult` theo đúng thứ tự: chạy hoạt ảnh đặt mèo, rồi từng `TurnStage` với các `TurnEvent` của nó. Ví dụ `Poisoned` cho linh hồn bay lên, `Eaten` cho virus đớp mèo. Giao diện không tự tính lại luật.

### 4.3 SOLID áp dụng ở đâu

| Nguyên tắc | Chỗ áp dụng |
|---|---|
| **S**: mỗi lớp một lý do để sửa | Mỗi luật cuối lượt, mỗi điều kiện thắng, mỗi booster là một lớp riêng |
| **O**: mở để thêm, đóng để sửa | Cơ chế mới = thêm một `ITurnStep` hoặc `ILevelGoal`, không sửa các bước đang có. Thứ tự các bước nằm ở một chỗ duy nhất: `TurnResolver.CreateStandard()` |
| **L**: lớp con thay được cho nhau | Mọi `ITurnStep` nhận một `Board` và trả `TurnStage`, không bước nào cần biết bước khác |
| **I**: interface nhỏ | `ITurnStep` và `ILevelGoal` mỗi cái một phương thức |
| **D**: phụ thuộc vào thứ được truyền vào | `SwipeRule` và `BoosterRules` nhận `TurnResolver` qua constructor, `Solver` nhận `SwipeRule`. `GamePresenter` chỉ biết `IBoardView`, `IHudView` (không biết MonoBehaviour); `GameSession` chỉ biết `IGameCommand`. Sau này lưu trữ, đồng hồ cũng qua interface (`IProgressStore`, `IClock`) |

Khởi tạo bộ luật chuẩn (làm một lần ở Bootstrap, test làm giống hệt ở `Tests/EditMode/Support/Rules.cs`):

```csharp
var turns = TurnResolver.CreateStandard();
var swipes = new SwipeRule(turns);
var boosters = new BoosterRules(turns);
var solver = new Solver(swipes);
```

### 4.4 Từ cú vuốt tới màn hình (Gameplay + Presentation)

```text
SwipeInput ──(điểm, hướng)──► BoardView ──CellSwiped(ô, hướng)──► GamePresenter
                                                                  │ run.TryPlay(new SwipeCommand(...))
                                                                  ▼
                                               LevelRun ──► GameSession ── IGameCommand ──► SwipeRule (Domain)
                                               (tim, xu, sao)     │ PlayOutcome (MoveResult + sao, xu, mất tim)
   BoardView.Play(move, xong) ◄───────────────────────────────────┤
     └ MovePlayback diễn từng chặng: BoardEffects (hạt, hình), SoundPlayer, PieceView
   HudView, WinPopup, LosePopup ◄─────────────────────────────────┘ (khi diễn xong)
```

| Lớp | Việc |
|---|---|
| `GameSession` | Bàn hiện tại, lịch sử để hoàn tác, chơi lại, trạng thái Đang chơi / Thắng / Thua |
| `LevelRun` | Một lượt chơi level: bọc `GameSession`, cộng thêm tim, xu, booster, hồi sinh, số sao, rồi lưu qua `IProgressStore` |
| `IGameCommand`, `SwipeCommand`, `MutateCommand`, `TransplantCommand` | Mỗi loại nước là một lệnh; thêm booster mới không phải sửa `GameSession` |
| `MechanicDetector` | Đọc nội dung level, trả về các cơ chế có trong đó; `PlayerProgress.MarkMechanicSeen` nhớ cơ chế đã giới thiệu |
| `GamePresenter` | Nghe các view, gọi `LevelRun`, chọn chữ hiện ra (`GameTexts`), xếp hàng thẻ cơ chế, ép đúng nước khi đang hướng dẫn. Không biết MonoBehaviour |
| `BoardView` + `PieceView` | Vẽ khung bàn, ô, ô độc, hộp, màng; mỗi quân một `PieceView` theo id: quân cũ trượt sang ô mới, mèo vừa nhân đôi mọc ra từ ô mèo mẹ |
| `BoardFrame` | Vẽ khung gỗ liền một khối thành một ảnh theo hình dạng từng level, cùng thông số với `baseSvg` của prototype. Màu, độ dày, bán kính chỉnh trên prefab. Độ phân giải tính theo cỡ bàn. Mở level tốn thời gian vì ảnh được tính lại mỗi lần |
| `MovePlayback` | Diễn một `MoveResult` theo thời gian của prototype: đặt mèo, rồi từng `TurnStage`. Giao diện không tự tính lại luật |
| `BoardIdle` | Hoạt ảnh lúc rảnh: chớp mắt, liếc, quay đầu, ngáp, vươn vai, dụi nhau; tắt khi đang diễn nước đi |
| `PieceMotions` | ScriptableObject chứa đường cong của mọi hoạt ảnh quân, chỉnh trong Inspector |
| `Pulse` | Phồng xẹp nhẹ cho vòng sáng hướng dẫn (gắn trên prefab `Highlight`) |
| `BoardEffects` | Sinh hiệu ứng từ `Prefabs/Effects`, chỉ đặt chỗ và tô màu. Confetti, hạt sáng co theo khung nhìn để luôn cùng cỡ trên màn hình |
| `SoundPlayer` + `GameSounds` | Phát âm theo `SoundId` qua nhóm `AudioSource` trong scene, tự gắn tiếng bấm cho mọi nút. Presenter chỉ biết `ISoundPlayer` |
| `MechanicPopup` + `MechanicDemoView` | Thẻ "Cơ chế mới": bàn minh hoạ chạy lặp, bàn sau khi vuốt do chính luật chơi tính ra. Nội dung nằm trong `Data/Mechanics/*.asset` |
| Bẫy dạy luật (`LevelDefinition.Trap`, `LevelRun.GuidedStep`, `FinishTrap`) | Level 2: thẻ Khử 3 hiện trước; rồi bàn tay chỉ nước làm thua, bộ 3 được khoanh rồi mới nổ, không mất tim; bàn về đầu và hướng dẫn nước thắng. Lúc mở level mà đã biết cơ chế Khử thì bỏ qua bẫy |
| `LossReport` | Từ nước vừa đi suy ra màu nào hết mèo và vì sao (khử, thay thế, ô độc, virus) để popup thua nói rõ lý do |
| `BoardLayout` | Đổi ô ↔ toạ độ: mỗi ô rộng 1 đơn vị, tâm bàn ở gốc |
| `BoardArt` | ScriptableObject chứa mọi hình của bàn. Hình đặt cỡ theo đơn vị ô (`SpriteLayers`), nên thay ảnh khác cỡ vẫn vừa |
| `SwipeInput` | Chạm/kéo chuột thành cú vuốt (đủ 0,3 ô là tính), bỏ qua khi bấm lên nút |
| `CameraFraming` | Căn camera cho bàn vừa màn hình, chừa chỗ cho HUD trên/dưới, ảnh nền phủ kín |
| `HudView` | uGUI: ô level, tim, xu, thanh sao, dòng nhiệm vụ, bong bóng lời thoại, hàng đếm mèo, 4 nút booster |
| `HomePresenter` + `HomeView` | Màn Home: hiện xu, tim, level đang chơi, đổi trang, bật tắt âm thanh, mở màn chơi qua `ISceneNavigator`. Trang Shop mới là chỗ giữ sẵn, chưa bán gì |
| `HomePager` + `HomeBottomBar` | Chuyển từ `MenuPager`, `BottomBarView` của Bounce_Blast. Ba trang Shop, Home, Setting xếp thành một dải, cả dải và thanh dưới chạy theo một vị trí duy nhất nên luôn khớp nhau: vuốt thì bám ngón tay, bấm tab hoặc thả thì trượt tới, đang trượt vẫn nắm lại được. Thứ tự trang, tab và enum `HomeTab` phải trùng nhau |
| `AmbientSchedule` + `AmbientEvents` + `AmbientEvent` | Sự kiện vui trong màn chơi, chỉ để ngắm: 1–2 phút một lần chọn ngẫu nhiên một sự kiện có chỗ diễn. Chỉ đếm giờ lúc người chơi rảnh (không có nước đang diễn, popup, hướng dẫn, booster đang chọn); hết rảnh là dừng ngay. Thêm sự kiện mới: kế thừa `AmbientEvent` rồi thêm vào danh sách trên `Ambient` trong scene Game. Hiện có `CatPeekEvent` (một hai chú mèo ngóc đầu từ mép trên của bàn, đổi sang mắt trái tim rồi bật trái tim hoặc ngôi sao). Hình trong `Prefabs/Ambient`. Xem thử trong Play: chuột phải `AmbientEvents` chọn "Xem thử một sự kiện" |
| `LevelMap` | Tính các nút bản đồ từ danh sách level và tiến độ: thắng level nào mở level kế tiếp, mọi level đã mở chơi lại được, giữ số sao cao nhất |
| `LevelMapView` + `LevelMapNode` + `LevelMapEffects` | Bản đồ phủ kín màn hình, mở khi bấm Play: đường uốn lượn cuộn dọc từ dưới lên, mở ra tại level hiện tại. Nút, chấm đường, nền sinh từ `Prefabs/Home`; chỉnh khoảng cách và độ lượn trên `LevelMap` trong scene Home. Nhấn nút thì lún xuống, thả thì nảy về; chọn level mở thì nảy to kèm vòng sáng, sao toé và tiếng `LevelSelect` rồi mới vào màn chơi; bấm level chưa mở thì rung kèm tiếng `LevelLocked`. Nút Reset ở góc phải là của bản thử: xoá tiến độ, giữ cài đặt âm thanh, gỡ trước khi phát hành (độ nảy trên `Prefabs/Home/LevelMapNode`, hạt trên `Content/EffectLayer`) |
| `WinPopup` + `WinEffects` + `CoinCounterView` | Panel thắng: pháo giấy, tiêu đề nảy, từng sao đáp xuống toé sáng và kêu cao dần, rồi ô tổng xu bật ra, icon xu với số thưởng nảy lên (không có nền), xu bay nối đuôi về ô tổng xu trong khi số thưởng mờ dần; mỗi đồng về thì số tăng và icon nảy (chuyển từ `CollectCoins` của Bounce_Blast). Nút Tiếp tục sang level kế (level cuối thì về bản đồ), nút Trở về mở bản đồ qua `ISceneNavigator.OpenMap`. Hiệu ứng vẽ bằng Image vì hạt Particle System nằm dưới lớp UI phủ màn hình |
| `GameBootstrap` | Đọc `LevelCatalog.txt`, tạo bộ luật và presenter, mở level chơi gần nhất (chỉnh được `openLevelNumber` trong Inspector để test) |

Các quân và ô được tạo bằng code lúc chạy (bàn mỗi level một khác); HUD, camera, đèn nằm sẵn trong `Scenes/Game.unity`.
Dự án dùng **Input Manager cũ** (`Input.GetMouseButton`, chạy được cả chạm trên điện thoại). URP 2D cần **Global Light 2D** trong scene, thiếu thì sprite bị tối.

## 4b. Giao diện: dựng trong Unity, code chỉ nối dữ liệu

Quy ước: **giao diện, prefab và scene dựng trong Unity Editor** (kéo thả, Inspector), không viết code sinh UI. Code chỉ:
- hiển thị dữ liệu lên các thành phần có sẵn (`[SerializeField]` kéo thả trong Inspector);
- instantiate prefab cho phần đổi theo level (ô bàn, quân, chip đếm mèo, ô chọn màu, ô minh hoạ, xu bay, hiệu ứng).

| Muốn sửa | Sửa ở |
|---|---|
| Màu, độ dày, bán kính khung gỗ | Component `BoardFrame` trên `Prefabs/Board/BoardFrame` |
| Cỡ ô, thứ tự vẽ của bàn | Prefab trong `Prefabs/Board` (mỗi lớp là một SpriteRenderer có Order in Layer riêng) |
| Ảnh mèo từng màu, hộp, vi khuẩn, virus | `Art/BoardArt.asset` |
| Vị trí, cỡ chữ, màu HUD và popup | Canvas `Hud` trong `Scenes/Game.unity` |
| Nút booster (vòng, huy hiệu, chữ) | `Prefabs/Hud/BoosterButton` (3 nút trong scene là bản của prefab này) |
| Viền chữ kiểu game | Material `Fonts/Baloo2-ExtraBold Outline` (chữ HUD) và `Baloo2-ExtraBold Title` (tiêu đề popup) |
| Khoảng chừa cho HUD khi căn bàn | `CameraFraming` trên Main Camera (topReserved, bottomReserved) |
| Tên, lời giải thích, bàn minh hoạ của một thẻ cơ chế | `Data/Mechanics/<Tên>.asset` |
| Bố cục thẻ cơ chế | `Hud/MechanicPopup` trong scene, ô minh hoạ ở `Prefabs/Hud/DemoCell` |
| Hình dáng, số lượng hạt, thời lượng hiệu ứng | `Prefabs/Effects` |
| Bố cục, nhịp diễn của panel thắng | `Hud/WinPopup` trong scene (`WinPopup`); số lượng và tốc độ hạt, đường bay của xu ở `WinPopup/EffectLayer` (`WinEffects`); số đồng xu bay ở `WinPopup`; vị trí ô tổng xu ở `WinPopup/CounterArea/CoinCounter`; hình hạt ở `Prefabs/Hud/Win*` |
| Nhịp, độ nảy của hoạt ảnh quân | `Data/PieceMotions.asset` |
| Âm thanh của từng sự kiện | `Data/GameSounds.asset` |

Cây scene `Game`:

```text
Main Camera (CameraFraming) └ Background      Global Light 2D      Board (BoardView, SwipeInput, BoardEffects, BoardIdle)      Game (GameBootstrap)
Audio (SoundPlayer) └ Music, Effect1..6
Hud (Canvas 1080×1920, HudView)
├── SafeArea (SafeAreaFitter)               mọi phần tử HUD nằm trong vùng an toàn, tránh tai thỏ
│   ├── LevelBadge, Hearts, HeartTimer, SettingsButton    thanh trên (màn chơi không hiện xu)
│   ├── StarMeter (StarMeterView), Goal                   thanh sao: tụt theo số nước, sao tắt khi qua mốc
│   └── Layout
│       ├── TipBubble, Toast                              bong bóng lời thoại, thông báo ngắn (tắt sẵn)
│       ├── SpeciesRow, ColorPicker                       hàng đếm mèo, bảng chọn màu (chỉ hiện khi dùng Đột biến)
│       └── BoosterBar: MutateButton, TransplantButton, HintButton   (HorizontalLayoutGroup: thứ tự theo sibling)
└── WinPopup, LosePopup, SettingsPopup, NoHeartsPopup, MechanicPopup   tắt sẵn, presenter bật khi cần
```

Ảnh viền 9 phần (panel, nút, thanh) có Sprite Border ghi trong file `.meta` theo `tools/ui-slices.txt`; vẽ lại ảnh không làm mất viền.
Không dùng công cụ dựng scene bằng code: nếu cần dựng lại thì làm trong Editor.

---

## 5. Dữ liệu level

Level vẫn được **thiết kế trong prototype** (`prototype/meowtosis.html`), nơi có sẵn bộ sinh, bộ giải, đo độ khó, sắp thứ tự (xem [level-design.md](level-design.md)). Unity nhận bản xuất:

```text
prototype/meowtosis.html ─► npm run export ─► data/levels.json            (kèm par + lời giải)
                                           └► unity/Assets/_Project/Levels/LevelCatalog.txt
                                              data/level-ids.json          (id cố định của từng level)
```

`npm test` (`tools/check.js`) báo lỗi nếu hai file xuất cũ hơn level trong game.

**Định dạng `LevelCatalog.txt`:** mỗi dòng là `từ-khoá giá-trị`. Dòng `level` mở một level mới; dòng trống và dòng `//` được bỏ qua.

| Từ khoá | Ví dụ | Nghĩa |
|---|---|---|
| `level` | `level meow-017` | Id cố định (lưu tiến độ theo id này) |
| `block` | `block 2` | Khối cơ chế (nội bộ, bản đồ không hiện) |
| `par` | `par 2` | Số nước tối ưu |
| `row` | `row AB..` | Một hàng của bàn, ký hiệu ở [mechanics.md](mechanics.md) và `LevelSymbols` |
| `goal` | `goal ..B.` | Một hàng hộp mục tiêu (chữ cái = hộp màu đó) |
| `membrane` | `membrane 0 1 left` | Màng một chiều ở ô (hàng 0, cột 1), chỉ cho đi theo hướng `left` |
| `kill` | `kill G` | Loài phải diệt sạch |
| `tutorial` | `tutorial 0 0 right Đẩy mèo xanh vào hộp xanh` | Một bước hướng dẫn: nước đi rồi lời thoại |
| `card` | `card ChainWake` | Thẻ "Cơ chế mới" riêng của level, cho thử thách mới không tự nhận ra được từ bàn chơi. Tên trong enum `Mechanic`, nội dung ở `Data/Mechanics/<Tên>.asset` |
| `trap` | `trap 0 1 right Thử vuốt mèo đỏ sang phải…` | Nước cố ý dẫn tới thua để dạy luật khử, chơi trước các bước `tutorial`. Thua không mất tim, chỉ diễn khi chưa biết cơ chế Khử |
| `tip` | `tip Mèo hồng đến chơi!…` | Mẹo đầu màn |
| `solution` | `solution 0 0 right` | Một nước của lời giải ngắn nhất (lặp lại `par` lần) |

**Id cố định:** `data/level-ids.json` ghi `id → nội dung chơi`.
- Đổi thứ tự level hoặc sửa lời thoại thì id giữ nguyên. Level mới nhận số kế tiếp.
- Số trong id là thứ tự tạo ra, không phải vị trí trên bản đồ.
- Sửa nội dung chơi của một level mà muốn giữ sao của người chơi: sửa nội dung đi kèm id đó trong `level-ids.json` trước khi export.

---

## 6. Test

| Cách chạy | Lệnh |
|---|---|
| Trong Unity | Window → General → Test Runner → EditMode → Run All |
| Ngoài Unity (nhanh, cần .NET SDK 8+) | `npm run test:unity` |
| Luật và level của prototype | `npm test` |

`tools/dotnet-tests/` biên dịch chính các file trong `unity/Assets/_Project` với giới hạn của Unity (.NET Standard 2.1, C# 9). Nó chỉ thay `UnityEngine.Application.dataPath` bằng một lớp nhỏ để tìm `LevelCatalog.txt`.

Bộ test hiện có 292 ca:
- 45 ví dụ luật, mã trùng với [mechanics.md](mechanics.md) (S1, V4…), thấy ở `Description` của từng test.
- 71 level × 3 ca: bàn đầu hợp lệ, lời giải xuất từ prototype thắng đúng `par`, bộ giải C# tìm ra đúng `par`.
- 10 level có hướng dẫn: các bước hướng dẫn đi được.
- 1 ca kiểm tra id không trùng.
- 9 ca phiên chơi: đi nước, nước bị từ chối, hết nhận nước khi thua, hoàn tác, chơi lại, số sao, tiến độ nhiệm vụ.
- 13 ca tài nguyên: thắng được xu theo sao, thua mất tim và hồi sinh trả tim, booster tiêu/trả/mua, hồi tim theo giờ, thanh sao tụt theo nước, gợi ý đi theo lời giải.
- 2 ca thẻ cơ chế: nhận đúng cơ chế theo nội dung level, mỗi cơ chế chỉ giới thiệu một lần.
- 5 ca bản đồ level: mở khoá, chơi lại level cũ, level hiện tại, đánh dấu cơ chế mới.

Màn chơi không có nút Hoàn tác (`LevelRun.TryUndo` vẫn còn và có test, chờ khi cần dùng lại).

Phần Presentation (hoạt ảnh, popup, hiệu ứng) không có test tự động; kiểm tra bằng cách chơi trong Editor.

---

## 7. Các tầng tiếp theo (kế hoạch)

| Tầng | Nội dung | Ghi chú |
|---|---|---|
| Gameplay | Đã có đủ: phiên chơi, lệnh vuốt và booster, hoàn tác, sao, tim, xu, hồi sinh, gợi ý, lưu tiến độ. Còn: lưu sao theo id level cho bản đồ | Số liệu ở [unity-handoff.md §5](unity-handoff.md#5-hệ-thống-meta-và-các-con-số) |
| Presentation | Đã có đủ màn chơi: HUD, popup, thẻ cơ chế, hướng dẫn, diễn từng chặng, VFX, SFX. Màn Home và bản đồ level đã có (Home → Play → Map → Game). Còn: Shop thật | VFX ở [unity-handoff.md §7](unity-handoff.md#7-vfx-cần-dựng-lại-unity-particle-system--tween) |
| Art / Audio | Shikacat + hình tạm đã gắn; icon booster, cài đặt đang là emoji Segoe UI (`Art/UI`) nên phải thay trước khi phát hành | Dự án cá nhân, phi thương mại; repo GitHub nên để riêng tư |

---

## 8. Thêm cơ chế mới (phía Unity)

Làm cùng lúc với checklist ở [mechanics.md §9](mechanics.md#9-thêm-cơ-chế-mới) (phía prototype):

1. Ký hiệu mới: `LevelSymbols`, và `Species` hoặc trường mới của `Piece` nếu cần.
2. Luật cuối lượt: thêm một lớp `ITurnStep`, đặt vào đúng chỗ trong `TurnResolver.CreateStandard()`.
3. Điều kiện thắng mới: thêm một lớp `ILevelGoal`, gắn trong `Level.CreateGoals()`.
4. Từ khoá mới trong level: `LevelCatalogParser` và `tools/export-unity.js`.
5. Ví dụ trong mechanics.md có test: thêm vào `MechanicsExampleTests` với mã trong `Description`.
6. `npm run export`, `npm test`, `npm run test:unity`, rồi chạy Test Runner trong Unity.

---

## 9. Nhật ký

| Ngày | Việc | Ghi chú |
|---|---|---|
| 2026-11-04 | Tạo project `unity/` (2022.3.62f3, URP 2D), cài MCP for Unity 10.3.0 (`.mcp.json` trỏ `http://127.0.0.1:8090/mcp`) | Bật server: Window → MCP for Unity → Start Server |
| 2026-11-04 | Chép asset Shikacat (43 ảnh, 9 âm thanh) và 5 tiếng mèo vào `Art/`, `Audio/` | Nhập mặc định dạng Sprite, 100 pixel/đơn vị |
| 2026-11-04 | Chép luật sang Domain C#; xuất `LevelCatalog.txt` + id cố định; 263 test EditMode, đạt cả trong Unity (Test Runner, 3,6 giây) lẫn ngoài Unity | Bộ giải C# ra đúng `par` của cả 69 level. Bộ giải giữ đúng thứ tự duyệt của prototype (cùng thứ tự hướng, ngăn xếp vào sau ra trước) để hai bên luôn cùng kết quả trong cùng giới hạn trạng thái |
| 2026-11-04 | Bố cục kiểu game thị trường: booster tròn, tim, ô level, thanh 3 sao, xu bay sau khi thắng, thẻ "Cơ chế mới" có bàn minh hoạ | Dựng trong Editor qua MCP, không viết code sinh UI |
| 2026-11-04 | Bản đồ level: bấm Play mở map, một đường liền 69 nút, chơi lại được mọi level đã mở | Ghi chú cũ: không chia chương trên bản đồ |
| 2026-11-04 | Panel thắng có xu, VFX, SFX và hai nút Tiếp tục, Trở về (`WinEffects`, `Prefabs/Hud/Win*`); Trở về mở sẵn bản đồ | Bỏ xu bay về HUD vì màn chơi không còn hiện xu |
| 2026-11-04 | Dạy luật khử sớm: thêm level 2 (bẫy `AA. / ##B`) và level 3 (luyện `##B# / AA..`), popup thua nói rõ lý do, thẻ Đẩy và Thay thế dời về level 4 và 6 | Góp ý: người chơi tưởng chỉ cần lấp kín. Số level từ level 2 cũ trở đi tăng thêm 2; tiến độ lưu theo id nên không lệch sao |
| 2026-11-05 | Đổi tên booster hiển thị: Đột biến thành Hoán đổi, Cấy ghép thành Nhân bản, Gợi ý thành Mèo mách; icon mới `Art/UI/IconBooster*` tạo bằng Figma AI (file "Meowtosis Home Art"), tách nền bằng `tools/key-green.py` | Tên trong code (`Mutate`, `Transplant`, `Hint`) giữ nguyên, chỉ đổi chữ người chơi thấy |
| 2026-11-04 | Thêm sự kiện vui trong màn chơi: mèo ngóc đầu đổi sang mắt trái tim, bật trái tim hoặc ngôi sao | Đã thử rồi bỏ mèo đuổi cuộn len, cánh tay mèo đập vào ô và vết chân trên ô |
| 2026-11-04 | Mèo trên bản đồ đổi sang mèo vàng khi level hiện tại có cơ chế mới | Hai hình chỉnh ở `Normal Cat`, `New Mechanic Cat` trên `LevelMap` trong scene Home |
| 2026-11-04 | Tối ưu cho điện thoại: 60 FPS (`Bootstrap/FrameRateSetup`), build IL2CPP + ARMv7 + ARM64, ảnh Android nén ASTC 6x6, hai atlas `Art/Atlases/Atlas_Ui` và `Atlas_Board` | Ảnh mới nhỏ hơn 600 px (UI) hoặc 720 px (bàn, mèo) thì thêm vào atlas; không thêm ảnh lặp (Tiled) như `GamePattern`, ảnh nền, ảnh lớn. Ảnh trong `Art/` cần đặt ASTC 6x6 cho Android. Chưa đo trên máy thật |
| 2026-11-04 | Dải nền dưới hàng booster: `SafeArea/Layout/BoosterBack` (BackFill, BackLine), mép trên cắt ngang tâm nút; chip đếm mèo đổi sang nền kem như ô xu | Kéo dài quá vùng an toàn để phủ tới mép màn hình |
| 2026-11-04 | Nền màn chơi có hoạ tiết chân mèo, đầu mèo, cá, cuộn len: `Main Camera/BackgroundPattern` (SpriteRenderer Tiled, ảnh `Art/UI/GamePattern.png` từ `tools/make-ui-art.py`) | Cỡ hoạ tiết chỉnh `patternTilesHigh` trên `CameraFraming`, độ mờ chỉnh Color của SpriteRenderer. `CameraFraming` tính cỡ theo chiều cao màn hình nên level nào cũng như nhau |
| 2026-11-04 | Gợi ý tính lượt như booster thường (`BoosterKind.Hint`, 3 lượt đầu, 60 xu mua thêm, lưu trong save) | Chỉ trừ lượt khi có nước để chỉ; bấm lại lúc mũi tên còn hiện không trừ thêm. Prototype web giữ Gợi ý miễn phí |
| 2026-11-04 | Sửa nút Gợi ý không hiện mũi tên: `HideTutorial` từng tắt cả mũi tên gợi ý, mà HUD gọi nó ngay sau khi hiện | Mũi tên giờ chỉ tắt khi đi nước, chơi lại, chọn booster hoặc mở level khác |
| 2026-11-04 | Thêm thẻ cho 3 thử thách mới: Dọn chỗ (level 14), Thức dây chuyền (43), Vi khuẩn tự khử (53). Thẻ Khử 3 ở level 2 hiện trước nước bẫy | Quy ước: thẻ chỉ dành cho luật, chướng ngại và thử thách mới; mèo màu mới dùng mẹo đầu màn, không có thẻ |
| 2026-11-04 | Nhận xu theo kiểu Bounce_Blast: ô tổng xu chỉ hiện trong panel thắng (`WinPopup/CounterArea/CoinCounter`), 12 đồng bay nối đuôi về đó, sao lấp lánh quanh ô thưởng | Số trên ô thưởng đứng yên, số trên ô tổng xu mới đếm lên |
| 2026-11-04 | Chuyển VFX, SFX, hoạt ảnh từ prototype: `MovePlayback`, `BoardIdle`, `Prefabs/Effects`, `Data/PieceMotions`, `Data/GameSounds` | Tiếng mới sinh bằng `tools/make-sfx.py` |
