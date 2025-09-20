using System;
using System.Collections.Generic;

namespace SimBlock.Core.Domain.Entities
{
    public enum MacroEventDevice
    {
        Keyboard,
        Mouse
    }

    public enum MacroEventType
    {
        // Keyboard
        KeyDown,
        KeyUp,
        // Mouse
        MouseMove,
        MouseDown,
        MouseUp,
        MouseWheel
    }

    public sealed class MacroEvent
    {
        public MacroEventDevice Device { get; init; }
        public MacroEventType Type { get; init; }
        public long TimestampMs { get; init; }

        // Keyboard
        public int? VirtualKeyCode { get; init; }
        public bool Ctrl { get; init; }
        public bool Alt { get; init; }
        public bool Shift { get; init; }

        // Mouse
        public int? X { get; init; }
        public int? Y { get; init; }
        public int? Button { get; init; } // 0=L,1=R,2=M,4=X1,5=X2
        public int? WheelDelta { get; init; }
    }

    public sealed class Macro
    {
        public string Name { get; set; } = "Untitled";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public List<MacroEvent> Events { get; } = new List<MacroEvent>();
    }

    // Lightweight metadata for listing macros without loading full event data
    public sealed class MacroInfo
    {
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime LastModifiedUtc { get; set; }
        public int EventCount { get; set; }
        public long? DurationMs { get; set; }
    }
}
