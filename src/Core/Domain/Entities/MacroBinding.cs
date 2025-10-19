using System;

namespace SimBlock.Core.Domain.Entities
{
    public enum MacroTriggerDevice
    {
        Keyboard,
        Mouse
    }

    public sealed class MacroTrigger
    {
        public MacroTriggerDevice Device { get; set; }

        // Keyboard
        public int? VirtualKeyCode { get; set; }
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool OnKeyDown { get; set; } = true;

        // Mouse
        // 0=L,1=R,2=M
        public int? Button { get; set; }
        public bool OnButtonDown { get; set; } = true;

        public override string ToString()
        {
            return Device switch
            {
                MacroTriggerDevice.Keyboard => $"Keyboard: {(Ctrl ? "Ctrl+" : string.Empty)}{(Alt ? "Alt+" : string.Empty)}{(Shift ? "Shift+" : string.Empty)}VK({VirtualKeyCode}) {(OnKeyDown ? "Down" : "Up")}",
                MacroTriggerDevice.Mouse => $"Mouse: Button {(Button ?? -1)} {(OnButtonDown ? "Down" : "Up")}",
                _ => "Unknown"
            };
        }
    }

    public sealed class MacroBinding
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string MacroName { get; set; } = string.Empty;
        public MacroTrigger Trigger { get; set; } = new MacroTrigger();
        public bool Enabled { get; set; } = true;
    }
}
