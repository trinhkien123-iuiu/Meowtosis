using System.Linq;
using Meowtosis.Domain;
using Meowtosis.Gameplay;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Điều phối các sự kiện vui trong màn chơi: đến giờ thì chọn ngẫu nhiên một sự kiện có chỗ để diễn trên bàn hiện tại.
    public sealed class AmbientEvents : MonoBehaviour, IAmbientEvents
    {
        [SerializeField] private AmbientEvent[] events;
        [Tooltip("Thời gian chờ ít nhất giữa hai sự kiện, tính bằng giây lúc người chơi rảnh.")]
        [SerializeField, Min(1f)] private float minimumSeconds = 60f;
        [Tooltip("Thời gian chờ nhiều nhất.")]
        [SerializeField, Min(1f)] private float maximumSeconds = 120f;

        private AmbientSchedule schedule;
        private Board lastBoard;

        public void Tick(float deltaSeconds, bool isIdle, Board board)
        {
            // Nước đi đơn giản diễn xong ngay trong một khung hình nên không kịp thấy "hết rảnh": bàn đổi cũng phải dừng,
            // kẻo mèo đập tay vào ô vừa có quân.
            var boardChanged = !ReferenceEquals(board, lastBoard);
            lastBoard = board;
            if (boardChanged) StopAll();
            if (!isIdle)
            {
                StopAll();
                return;
            }
            if (events.Any(ambient => ambient.IsPlaying)) return;
            if (schedule.Tick(deltaSeconds, true)) StartRandom(board);
        }

        private void Awake() => schedule = new AmbientSchedule(minimumSeconds, Mathf.Max(minimumSeconds, maximumSeconds), () => Random.value);

        // Thử lần lượt theo thứ tự ngẫu nhiên, để bàn không có chỗ cho sự kiện này thì vẫn còn sự kiện kia.
        private void StartRandom(Board board)
        {
            foreach (var ambient in events.OrderBy(unused => Random.value))
            {
                if (ambient.TryStart(board)) return;
            }
        }

        private void StopAll()
        {
            foreach (var ambient in events)
            {
                if (ambient.IsPlaying) ambient.Stop();
            }
        }

        // Xem thử trong Play mà không phải chờ: chuột phải vào component trong Inspector rồi chọn mục này.
        [ContextMenu("Xem thử một sự kiện")]
        private void PreviewNow()
        {
            if (!Application.isPlaying || lastBoard == null) return;
            StopAll();
            StartRandom(lastBoard);
        }
    }
}
