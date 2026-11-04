using System;
using Meowtosis.Gameplay;

namespace Meowtosis.Presentation
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
