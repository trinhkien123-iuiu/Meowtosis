using System;
using System.Collections.Generic;
using Meowtosis.Domain;

namespace Meowtosis.Presentation
{
    // Bàn chơi nhìn từ phía presenter: chỉ vẽ theo dữ liệu được đưa và báo lại thao tác, không tự xử lý luật.
    public interface IBoardView
    {
        // Người chơi vuốt từ ô `cell` theo hướng `direction`.
        event Action<int, Direction> CellSwiped;

        // Người chơi chạm (không vuốt) vào ô `cell`.
        event Action<int> CellTapped;

        // Cho phép mèo làm động tác lúc rảnh (chớp mắt, ngáp...).
        bool IdleEnabled { get; set; }

        // Dựng khung bàn cho level mới (xoá bàn cũ).
        void Build(Level level);

        // Hiện bàn ngay (lúc mở level, hoàn tác, chơi lại).
        void Show(Board board);

        // Diễn lại một nước vừa đi; `finished` được gọi khi diễn xong.
        // `lingerOnClear`: dừng lại và khoanh bộ 3 trước khi nổ, để người mới kịp thấy vì sao mèo biến mất.
        void Play(MoveResult move, Action finished, bool lingerOnClear = false);

        // Nước không đi được: quân ở ô đó lắc đầu.
        void Refuse(int cell);

        // Chạm vào quân: vuốt ve.
        void Pet(int cell);

        // Thắng: mèo nhảy vui, rồi các khối rơi xuống hố kéo theo mèo; rơi hết thì gọi `finished`.
        void Celebrate(Action finished);

        // Thua: mèo mặt bất lực đổ mồ hôi, bàn rung.
        void Mourn();

        // Hồi sinh: hạt sáng bay lên.
        void PlayRevive();

        // Viền sáng các ô đang chọn được (booster); danh sách rỗng thì tắt.
        void Highlight(IEnumerable<int> cells);

        // Mũi tên gợi ý từ ô `cell` theo hướng `direction`.
        void ShowHint(int cell, Direction direction);

        void HideHint();

        // Bước hướng dẫn: bàn tay lướt theo nước cần vuốt, bóng mờ bản sao trượt sang ô đích.
        void ShowTutorial(int cell, Direction direction);

        void HideTutorial();

        // Làm tối cả màn hình, chỉ chừa sáng các ô `cells` (bàn tay và bóng mờ hướng dẫn vẫn nổi trên lớp tối).
        void Focus(IEnumerable<int> cells);

        void ClearFocus();

        // Tiêu điểm sáng vào một ô: làm tối phần còn lại, quân ở ô đó nhảy lên.
        void Spotlight(int cell);

        void ClearSpotlight();
    }
}
