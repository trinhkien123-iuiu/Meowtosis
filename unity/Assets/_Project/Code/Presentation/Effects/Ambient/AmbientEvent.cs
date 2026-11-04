using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Một sự kiện vui chạy trong màn chơi, chỉ để ngắm: không chặn thao tác và không đổi bàn chơi.
    public abstract class AmbientEvent : MonoBehaviour
    {
        // Sự kiện đang diễn dở.
        public abstract bool IsPlaying { get; }

        // Bắt đầu nếu bàn hiện tại có chỗ để diễn; trả false nếu không có chỗ.
        public abstract bool TryStart(Board board);

        // Dừng ngay và ẩn hết, gọi khi người chơi bắt đầu thao tác.
        public abstract void Stop();
    }
}
