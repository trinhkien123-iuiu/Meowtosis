using System;

namespace Meowtosis.Gameplay
{
    // Đồng hồ thật hoặc giả (trong test), để tính hồi tim không phụ thuộc giờ máy.
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
