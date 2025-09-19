using System;
using System.Windows.Forms;

namespace SimBlock.Core.Domain.Entities
{
    public sealed class KeyboardHookEventArgs : EventArgs
    {
        public Keys Key { get; init; }
        public uint VkCode { get; init; }
        public int Message { get; init; }
        public bool IsKeyDown { get; init; }
        public bool IsKeyUp { get; init; }
        public bool Ctrl { get; init; }
        public bool Alt { get; init; }
        public bool Shift { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public sealed class MouseHookEventArgs : EventArgs
    {
        public int Message { get; init; }
        public int X { get; init; }
        public int Y { get; init; }
        public uint MouseData { get; init; }
        public bool LeftButton { get; init; }
        public bool RightButton { get; init; }
        public bool MiddleButton { get; init; }
        public int WheelDelta { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
